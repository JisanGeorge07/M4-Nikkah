using Microsoft.AspNetCore.SignalR;
using Persistence;
using URMARRY.Hubs;

namespace URMARRY.Services
{
    /// <summary>
    /// Background service that periodically inspects mobile clients tracked via heartbeat.
    /// If a mobile client has stopped sending heartbeats (>60 seconds) and has no web connections,
    /// it marks them offline, writes their LastSeenAt timestamp to the database,
    /// and broadcasts the UserOffline event to all SignalR clients in real time.
    /// </summary>
    public class PresenceCleanupService : BackgroundService
    {
        private readonly PresenceTracker _tracker;
        private readonly IHubContext<PresenceHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PresenceCleanupService> _logger;

        // Check for expired mobile heartbeats every 20 seconds
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(20);

        public PresenceCleanupService(
            PresenceTracker tracker,
            IHubContext<PresenceHub> hubContext,
            IServiceScopeFactory scopeFactory,
            ILogger<PresenceCleanupService> logger)
        {
            _tracker = tracker;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PresenceCleanupService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);

                    var expiredUsers = _tracker.SweepExpiredHeartbeats();
                    if (expiredUsers.Count > 0)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        foreach (var (userId, lastSeen) in expiredUsers)
                        {
                            try
                            {
                                var user = await db.Registration.FindAsync(new object[] { userId }, stoppingToken);
                                if (user != null)
                                {
                                    user.LastSeenAt = lastSeen;
                                }

                                // Broadcast to all web clients that this mobile user is now offline
                                await _hubContext.Clients.All.SendAsync("UserOffline", userId, lastSeen, stoppingToken);
                                _logger.LogInformation("Mobile user {UserId} heartbeat expired. Marked offline with last seen {LastSeen}.", userId, lastSeen);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing offline sweep for user {UserId}", userId);
                            }
                        }

                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during presence heartbeat cleanup sweep.");
                }
            }

            _logger.LogInformation("PresenceCleanupService is stopping.");
        }
    }
}
