using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Persistence;
using URMARRY.Hubs;
using URMARRY.Services;

namespace URMARRY.Controllers.Api
{
    [ApiController]
    [Route("api/presence")]
    public class PresenceApiController : ControllerBase
    {
        private readonly PresenceTracker _tracker;
        private readonly IHubContext<PresenceHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PresenceApiController> _logger;

        public PresenceApiController(
            PresenceTracker tracker,
            IHubContext<PresenceHub> hubContext,
            IServiceScopeFactory scopeFactory,
            ILogger<PresenceApiController> logger)
        {
            _tracker = tracker;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public class HeartbeatRequest
        {
            public long UserId { get; set; }
        }

        public class OfflineRequest
        {
            public long UserId { get; set; }
        }

        /// <summary>
        /// Records a heartbeat for a mobile or external client.
        /// Call this every ~30 seconds while the mobile app is active in foreground.
        /// Accepts userId via query string, form data, or JSON body.
        /// If the user was offline, instantly broadcasts UserOnline to all SignalR web clients.
        /// </summary>
        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat(
            [FromQuery] long? userId,
            [FromForm] long? formUserId,
            [FromBody] HeartbeatRequest? body)
        {
            long targetUserId = userId ?? formUserId ?? body?.UserId ?? 0;

            if (targetUserId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid userId is required." });
            }

            bool newlyOnline = _tracker.RecordHeartbeatAndCheckIfNewlyOnline(targetUserId);

            if (newlyOnline)
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("UserOnline", targetUserId, DateTime.UtcNow);
                    _logger.LogInformation("Mobile user {UserId} sent heartbeat (newly online). Broadcasted UserOnline.", targetUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting UserOnline for user {UserId}", targetUserId);
                }
            }

            return Ok(new
            {
                success = true,
                userId = targetUserId,
                isOnline = true,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Explicitly marks a user as offline.
        /// Can be invoked by mobile on app pause/close/logout, or by web beforeunload beacon.
        /// Immediately saves LastSeenAt to the database and broadcasts UserOffline over SignalR.
        /// </summary>
        [HttpPost("offline")]
        public async Task<IActionResult> Offline(
            [FromQuery] long? userId,
            [FromForm] long? formUserId,
            [FromBody] OfflineRequest? body)
        {
            long targetUserId = userId ?? formUserId ?? body?.UserId ?? 0;

            if (targetUserId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid userId is required." });
            }

            var lastSeen = _tracker.RecordExplicitOffline(targetUserId);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var registration = await db.Registration.FindAsync(targetUserId);
                if (registration != null)
                {
                    registration.LastSeenAt = lastSeen;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving LastSeenAt to DB on offline for user {UserId}", targetUserId);
            }

            try
            {
                await _hubContext.Clients.All.SendAsync("UserOffline", targetUserId, lastSeen);
                _logger.LogInformation("User {UserId} marked offline. Broadcasted UserOffline.", targetUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting UserOffline for user {UserId}", targetUserId);
            }

            return Ok(new
            {
                success = true,
                userId = targetUserId,
                isOnline = false,
                lastSeenAt = lastSeen
            });
        }

        /// <summary>
        /// Checks the online status of a specific user.
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus([FromQuery] long userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid userId is required." });
            }

            bool isOnline = _tracker.IsOnline(userId);

            return Ok(new
            {
                success = true,
                userId,
                isOnline
            });
        }
    }
}
