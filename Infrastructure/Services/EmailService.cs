using Application.Interfaces.Infrastructure;
using Application.Interfaces.Infrastructure.Email;
using Application.Models;
using Application.Models.Framework;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpsettings;
    public EmailService(IOptions<SmtpSettings> smtpSettings)
    {
        _smtpsettings = smtpSettings.Value;
    }
    public Task Send(Message message)
    {
        var emailMessage = CreateEmailMessage(message);
        Send(emailMessage, message.From!);
        return Task.CompletedTask;
    }

    public Task Authenticate(EmailDto email)
    {
        using var client = new MailKit.Net.Smtp.SmtpClient();
        try
        {
            switch (email.Port)
            {
                case 465:
                    client.Connect(email.SmtpServer, email.Port, true);
                    break;

                case 25:
                case 587:
                    client.Connect(email.SmtpServer, email.Port, SecureSocketOptions.StartTls);
                    break;

                default:
                    client.Connect(email.SmtpServer, email.Port, false);
                    break;
            }

            // ReSharper disable once StringLiteralTypo
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            client.Authenticate(email.EmailId, email.Password);
        }
        finally
        {
            client.Disconnect(true);
            client.Dispose();
        }

        return Task.CompletedTask;
    }

    private static MimeMessage CreateEmailMessage(Message message)
    {
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(message.From!.Name, message.From!.EmailId));
        emailMessage.To.AddRange(message.To);
        emailMessage.Subject = message.Subject;


        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.Content
        };

        foreach (var attachmentPath in message.AttachmentPaths.Where(x => !string.IsNullOrEmpty(x)))
            bodyBuilder.Attachments.Add("wwwroot/" + attachmentPath);


        emailMessage.Body = bodyBuilder.ToMessageBody();
        return emailMessage;
    }

    private static void Send(MimeMessage mailMessage, EmailDto sender)
    {
        using var client = new MailKit.Net.Smtp.SmtpClient();
        try
        {
            switch (sender.Port)
            {
                case 465:
                    client.Connect(sender.SmtpServer, sender.Port, true);
                    break;

                case 25:
                case 587:
                    client.Connect(sender.SmtpServer, sender.Port, SecureSocketOptions.StartTls);
                    break;

                default:
                    client.Connect(sender.SmtpServer, sender.Port, false);
                    break;
            }

            // ReSharper disable once StringLiteralTypo
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            client.Authenticate(sender.EmailId, sender.Password);
            client.Send(mailMessage);
        }
        finally
        {
            client.Disconnect(true);
            client.Dispose();
        }
    }

    public async Task<bool> SendSmsAsync(string otp, string phoneNumber)
    {
        string key = "ObDeU3zan3orps9PF3pnr0wtV1PoID1W";
        string sender = "MUSLMM";
        string templateId = "1707171636903460920";
        string message = $"Dear Customer,  {otp} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
       
        string url = $"http://thesmsbuddy.com/api/v1/sms/send?key={key}&type=1&to={phoneNumber}&sender={sender}&message={message}&flash=0&template_id={templateId}";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
    }

    public async Task<bool> SendSmsForgotPasswordAsync(string password, string phoneNumber)
    {
        string key = "ObDeU3zan3orps9PF3pnr0wtV1PoID1W";
        string sender = "MUSLMM";
        string templateId = "1707171636903460920";
        string message = $"Dear Customer,  {password} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
       
        string url = $"http://thesmsbuddy.com/api/v1/sms/send?key={key}&type=1&to={phoneNumber}&sender={sender}&message={message}&flash=0&template_id={templateId}";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
    }

    public async Task<bool> SendSmsPaymentLinkAsync(string paymentLink, string phoneNumber)
    {
        string key = "ObDeU3zan3orps9PF3pnr0wtV1PoID1W";
        string sender = "MUSLMM";
        string templateId = "1707171636903460920"; // Can be replaced later with custom template ID
        string message = $"Dear Customer, please use the following link to make payment for your M4nikah Premium membership: {paymentLink}";
       
        string url = $"http://thesmsbuddy.com/api/v1/sms/send?key={key}&type=1&to={phoneNumber}&sender={sender}&message={Uri.EscapeDataString(message)}&flash=0&template_id={templateId}";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
    }
}