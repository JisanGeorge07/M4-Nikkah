using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace URMARRY.Services
{
    /// <summary>
    /// Background service that periodically checks for premium memberships expiring
    /// within 10 days or having low remaining contact credits (<= 5), and automatically
    /// creates or re-opens Renewal Follow-ups assigned to the profile's assigned staff.
    /// </summary>
    public class AutoRenewalFollowUpService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AutoRenewalFollowUpService> _logger;

        public AutoRenewalFollowUpService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<AutoRenewalFollowUpService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoRenewalFollowUpService is starting.");

            // Warm-up delay so that the app finishes starting up and migrations/DB are ready
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool enabled = _configuration.GetValue<bool>("AutoRenewalFollowUp:Enabled", true);
                    if (enabled)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<IRenewalFollowUpProcessor>();
                        var result = await processor.ProcessAutoRenewalFollowUpsAsync(stoppingToken);
                        _logger.LogInformation("AutoRenewalFollowUp evaluation finished: {Message}", result.Message);
                    }
                    else
                    {
                        _logger.LogInformation("AutoRenewalFollowUpService is disabled in configuration.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while executing AutoRenewalFollowUpService.");
                }

                // Default interval: 6 hours
                var intervalHours = _configuration.GetValue<int>("AutoRenewalFollowUp:CheckIntervalHours", 6);
                if (intervalHours <= 0) intervalHours = 6;

                try
                {
                    await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("AutoRenewalFollowUpService is stopping.");
        }
    }
}
