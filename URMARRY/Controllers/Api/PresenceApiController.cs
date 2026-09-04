using Microsoft.AspNetCore.Mvc;
using URMARRY.Services;

namespace URMARRY.Controllers.Api
{
    [ApiController]
    [Route("api/presence")]
    public class PresenceApiController : ControllerBase
    {
        private readonly PresenceTracker _tracker;
        private readonly ILogger<PresenceApiController> _logger;

        public PresenceApiController(PresenceTracker tracker, ILogger<PresenceApiController> logger)
        {
            _tracker = tracker;
            _logger = logger;
        }

        public class HeartbeatRequest
        {
            public long UserId { get; set; }
        }

        /// <summary>
        /// Records a heartbeat for a mobile or external client.
        /// Call this every ~30 seconds while the mobile app is active.
        /// Accepts userId via query string, form data, or JSON body.
        /// </summary>
        [HttpPost("heartbeat")]
        public IActionResult Heartbeat(
            [FromQuery] long? userId,
            [FromForm] long? formUserId,
            [FromBody] HeartbeatRequest? body)
        {
            long targetUserId = userId ?? formUserId ?? body?.UserId ?? 0;

            if (targetUserId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid userId is required." });
            }

            _tracker.RecordHeartbeat(targetUserId);

            return Ok(new
            {
                success = true,
                userId = targetUserId,
                isOnline = true,
                timestamp = DateTime.UtcNow
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
