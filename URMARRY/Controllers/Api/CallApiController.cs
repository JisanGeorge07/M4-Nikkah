using Application.Interfaces.Persistence;
using Application.Models.Call;
using Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using URMARRY.Hubs;
using URMARRY.Services;

namespace URMARRY.Controllers.Api
{
    [ApiController]
    [Route("api/call")]
    public class CallApiController : ControllerBase
    {
        private readonly ICallService _callService;
        private readonly IHubContext<CallHub> _hubContext;
        private readonly PresenceTracker _presenceTracker;
        private readonly CookieHelper _cookieHelper;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CallApiController> _logger;

        public CallApiController(
            ICallService callService,
            IHubContext<CallHub> hubContext,
            PresenceTracker presenceTracker,
            CookieHelper cookieHelper,
            IWebHostEnvironment env,
            ILogger<CallApiController> logger)
        {
            _callService = callService;
            _hubContext = hubContext;
            _presenceTracker = presenceTracker;
            _cookieHelper = cookieHelper;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Generates dynamic STUN and AWS Coturn TURN server credentials (HMAC-SHA1 time-limited tokens).
        /// </summary>
        [HttpGet("ice-servers")]
        public IActionResult GetIceServers()
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var iceConfig = _callService.GetIceServerConfiguration(userId.Value);
            return Ok(iceConfig);
        }

        /// <summary>
        /// Validates whether the current user is eligible to call the target user (active plan / mutual interest).
        /// </summary>
        [HttpGet("check-permission/{targetUserId:long}")]
        public async Task<IActionResult> CheckPermission(long targetUserId, [FromQuery] UserCallType callType = UserCallType.Voice)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var result = await _callService.CheckCallPermissionAsync(userId.Value, targetUserId, callType);
            return Ok(result);
        }

        /// <summary>
        /// Returns paginated audio/video call history for the current user.
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetCallHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var history = await _callService.GetCallHistoryAsync(userId.Value, page, pageSize);
            return Ok(history);
        }

        /// <summary>
        /// Gets single call session details by ID.
        /// </summary>
        [HttpGet("{callLogId:long}")]
        public async Task<IActionResult> GetCallLogById(long callLogId)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var log = await _callService.GetCallLogByIdAsync(callLogId, userId.Value);
            if (log == null)
            {
                return NotFound(new { message = "Call record not found." });
            }

            return Ok(log);
        }

        /// <summary>
        /// Initiates a call via REST API.
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateCall([FromBody] InitiateCallRequest request)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            try
            {
                var callLog = await _callService.InitiateCallAsync(userId.Value, request);

                // Broadcast real-time SignalR incoming call notification
                await _hubContext.Clients.Group($"User_{request.ReceiverId}").SendAsync("IncomingCall", callLog);

                return Ok(callLog);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Accepts/answers an incoming call via REST API.
        /// </summary>
        [HttpPost("answer")]
        public async Task<IActionResult> AnswerCall([FromBody] AnswerCallRequest request)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            try
            {
                var callLog = await _callService.AnswerCallAsync(request.CallLogId, userId.Value);

                await _hubContext.Clients.Group($"CallRoom_{callLog.RoomId}").SendAsync("CallAccepted", callLog);
                await _hubContext.Clients.Group($"User_{callLog.CallerId}").SendAsync("CallAccepted", callLog);

                return Ok(callLog);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Rejects an incoming call via REST API.
        /// </summary>
        [HttpPost("reject")]
        public async Task<IActionResult> RejectCall([FromBody] RejectCallRequest request)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            try
            {
                var callLog = await _callService.RejectCallAsync(request.CallLogId, userId.Value, request.Reason);

                await _hubContext.Clients.Group($"CallRoom_{callLog.RoomId}").SendAsync("CallRejected", callLog);
                await _hubContext.Clients.Group($"User_{callLog.CallerId}").SendAsync("CallRejected", callLog);

                return Ok(callLog);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Ends active call session via REST API.
        /// </summary>
        [HttpPost("end")]
        public async Task<IActionResult> EndCall([FromBody] EndCallRequest request)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            try
            {
                var callLog = await _callService.EndCallAsync(request.CallLogId, userId.Value, request.Reason);

                await _hubContext.Clients.Group($"CallRoom_{callLog.RoomId}").SendAsync("CallEnded", callLog);
                await _hubContext.Clients.Group($"User_{callLog.CallerId}").SendAsync("CallEnded", callLog);
                await _hubContext.Clients.Group($"User_{callLog.ReceiverId}").SendAsync("CallEnded", callLog);

                return Ok(callLog);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Uploads 14-day security call recording file (.mp4 / .webm / audio stream) for legal and dispute backup.
        /// </summary>
        [HttpPost("recording")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB max
        public async Task<IActionResult> UploadCallRecording(
            [FromForm] long callLogId,
            [FromForm] int durationSeconds,
            [FromForm] IFormFile? file)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No recording file provided." });
            }

            try
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "Uploads", "CallRecordings");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = ".mp4";

                string s3Key = $"recordings/call_{callLogId}_{Guid.NewGuid():N}{ext}";
                string physicalPath = Path.Combine(uploadFolder, Path.GetFileName(s3Key));

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string relativeUrl = $"/Uploads/CallRecordings/{Path.GetFileName(s3Key)}";
                DateTime expiresAt = DateTime.UtcNow.AddDays(14);

                bool saved = await _callService.SaveCallRecordingMetadataAsync(callLogId, s3Key, relativeUrl, file.Length, durationSeconds);

                return Ok(new CallRecordingUploadResponse
                {
                    Success = saved,
                    RecordingUrl = relativeUrl,
                    S3Key = s3Key,
                    ExpiresAt = expiresAt,
                    Message = "Call recording saved for 14-day security retention."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CallApiController: Error uploading call recording for call {CallLogId}", callLogId);
                return StatusCode(500, new { message = "Call recording upload failed." });
            }
        }

        #region Private Helpers

        private long? ResolveCurrentUserId()
        {
            var cookieUserId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            if (cookieUserId.HasValue && cookieUserId.Value > 0)
            {
                return cookieUserId.Value;
            }

            if (User != null)
            {
                var idClaim = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("UserId")?.Value;

                if (long.TryParse(idClaim, out var id) && id > 0)
                {
                    return id;
                }
            }

            return null;
        }

        #endregion
    }
}
