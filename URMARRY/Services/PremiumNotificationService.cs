using Application.Interfaces.Infrastructure;
using Application.Interfaces.Infrastructure.Email;
using Application.Models;
using Application.Models.Framework;
using Domain;
using Microsoft.EntityFrameworkCore;
using Persistence;
using MimeKit;
using Microsoft.Extensions.Options;

namespace URMARRY.Services;

public class PremiumNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PremiumNotificationService> _logger;
    private readonly SmtpSettings _smtpSettings;

    public PremiumNotificationService(
        IServiceScopeFactory scopeFactory,
        ILogger<PremiumNotificationService> logger,
        IOptions<SmtpSettings> smtpSettings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _smtpSettings = smtpSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Premium Notification Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotifications(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing premium notifications.");
            }

            // Run every 6 hours
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            // Run every 1 minute (for testing)
            // await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Premium Notification Service is stopping.");
    }

    private async Task ProcessNotifications(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var expiryThreshold = now.AddDays(7);

        // Fetch all active plan purchases (not expired)
        var allActivePlans = await dbContext.PlanPurchases
            .Include(x => x.User)
            .Where(x => x.ExpiresAt > now)
            .ToListAsync(stoppingToken);

        // Group by user and take only the latest plan for each user
        var latestPlans = allActivePlans
            .GroupBy(x => x.UserId)
            .Select(g => g.OrderByDescending(p => p.CreatedOn).First())
            .ToList();

        foreach (var plan in latestPlans)
        {
            if (plan.User == null || string.IsNullOrEmpty(plan.User.Email))
                continue;

            // 1. Check for Expiry (within 7 days)
            if (!plan.ExpiryNotificationSent && plan.ExpiresAt <= expiryThreshold)
            {
                await SendExpiryEmail(emailService, plan);
                plan.ExpiryNotificationSent = true;
                _logger.LogInformation("Sent expiry notification to User ID: {UserId} for Plan ID: {PlanId}", plan.UserId, plan.Id);
            }

            // 2. Check for Low Credits (Remaining <= 5)
            var remainingCredits = plan.ViewCreditsPurchased - plan.ViewCreditsUsed;
            if (!plan.LowCreditNotificationSent && remainingCredits > 0 && remainingCredits <= 5)
            {
                await SendLowCreditEmail(emailService, plan);
                plan.LowCreditNotificationSent = true;
                _logger.LogInformation("Sent low credit notification to User ID: {UserId} for Plan ID: {PlanId}", plan.UserId, plan.Id);
            }
        }

        if (latestPlans.Any(x => dbContext.Entry(x).State == EntityState.Modified))
        {
            await dbContext.SaveChangesAsync("SYSTEM");
        }
    }

    private async Task SendExpiryEmail(IEmailService emailService, PlanPurchase plan)
    {
        var message = new Message
        {
            To = new List<MailboxAddress> { new MailboxAddress(plan.User!.Name ?? "User", plan.User.Email!) },
            From = GetFromDto(),
            Subject = "Premium Plan Expiry Alert - M4Nikah",
            Content = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 8px; overflow: hidden; }}
                        .header {{ background: linear-gradient(135deg, #ff416c, #ff4b2b); color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 30px; }}
                        .footer {{ background-color: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #777; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: #ff4b2b; color: white; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
                        .highlight {{ color: #ff4b2b; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Premium Plan Alert</h1>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{plan.User.Name}</strong>,</p>
                            <p>We hope you are enjoying your premium experience on <strong>M4Nikah</strong>.</p>
                            <p>This is a friendly reminder that your premium membership plan is set to expire on <span class='highlight'>{plan.ExpiresAt:D}</span>.</p>
                            <p>To ensure uninterrupted access to exclusive features like contact details, messaging, and advanced filters, we recommend renewing your plan before it expires.</p>
                            <center>
                                <a href='https://m4nikah.com/user/login' class='button'>Renew Now</a>
                            </center>
                            <p>If you have any questions, our support team is here to help.</p>
                            <p>Best Regards,<br/><strong>The M4Nikah Team</strong></p>
                        </div>
                        <div class='footer'>
                            <p>Contact Us: info@m4nikah.com | +91 6238785898</p>
                            <p>&copy; {DateTime.Now.Year} M4Nikah. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>"
        };

        await emailService.Send(message);
        _logger.LogInformation("Sent expiry notification to {Email}", plan.User.Email);
    }

    private async Task SendLowCreditEmail(IEmailService emailService, PlanPurchase plan)
    {
        var remainingCredits = plan.ViewCreditsPurchased - plan.ViewCreditsUsed;
        var message = new Message
        {
            To = new List<MailboxAddress> { new MailboxAddress(plan.User!.Name ?? "User", plan.User.Email!) },
            From = GetFromDto(),
            Subject = "Low Credits Alert - M4Nikah",
            Content = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 8px; overflow: hidden; }}
                        .header {{ background: linear-gradient(135deg, #f7971e, #ffd200); color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 30px; }}
                        .footer {{ background-color: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #777; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: #f7971e; color: white; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
                        .highlight {{ color: #f7971e; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Low Credits Alert</h1>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{plan.User.Name}</strong>,</p>
                            <p>You are running low on contact view credits!</p>
                            <p>Your current plan has only <span class='highlight'>{remainingCredits} credits</span> remaining.</p>
                            <p>Don't let this slow down your search for a life partner. Top up your credits now to continue connecting with potential matches without any delay.</p>
                            <center>
                                <a href='https://m4nikah.com/user/login' class='button'>Top Up Credits</a>
                            </center>
                            <p>Thank you for choosing M4Nikah.</p>
                            <p>Warm regards,<br/><strong>The M4Nikah Team</strong></p>
                        </div>
                        <div class='footer'>
                            <p>Contact Us: info@m4nikah.com | +91 6238785898</p>
                            <p>&copy; {DateTime.Now.Year} M4Nikah. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>"
        };

        await emailService.Send(message);
        _logger.LogInformation("Sent low credit notification to {Email}", plan.User.Email);
    }

    private EmailDto GetFromDto()
    {
        return new EmailDto
        {
            SmtpServer = _smtpSettings.Host,
            Port = _smtpSettings.Port,
            Name = "M4Nikah Support",
            EmailId = _smtpSettings.SenderEmail,
            Password = _smtpSettings.Password
        };
    }
}
