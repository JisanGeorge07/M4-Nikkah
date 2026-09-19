using Application.Interfaces.Persistence;
using Application.Models.Chat;
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
    [Route("api/chat")]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly PresenceTracker _presenceTracker;
        private readonly CookieHelper _cookieHelper;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ChatApiController> _logger;

        public ChatApiController(
            IChatService chatService,
            IHubContext<ChatHub> hubContext,
            PresenceTracker presenceTracker,
            CookieHelper cookieHelper,
            IWebHostEnvironment env,
            ILogger<ChatApiController> logger)
        {
            _chatService = chatService;
            _hubContext = hubContext;
            _presenceTracker = presenceTracker;
            _cookieHelper = cookieHelper;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Gets all conversations for current user (1-to-1 chats and support thread).
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var conversations = await _chatService.GetUserConversationsAsync(userId.Value);
            
            // Enrich with real-time online status from PresenceTracker
            foreach (var conv in conversations)
            {
                if (conv.Type == ConversationType.UserToUser && conv.ParticipantUserId > 0)
                {
                    conv.IsParticipantOnline = _presenceTracker.IsOnline(conv.ParticipantUserId);
                }
            }

            return Ok(conversations);
        }

        /// <summary>
        /// Gets a single conversation by ID.
        /// </summary>
        [HttpGet("conversation/{conversationId:long}")]
        public async Task<IActionResult> GetConversationById(long conversationId)
        {
            var (userId, isStaff) = ResolveCaller();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var conv = await _chatService.GetConversationByIdAsync(conversationId, userId.Value, isStaff);
            if (conv == null)
            {
                return NotFound(new { message = "Conversation not found or access denied." });
            }

            if (conv.Type == ConversationType.UserToUser && conv.ParticipantUserId > 0)
            {
                conv.IsParticipantOnline = _presenceTracker.IsOnline(conv.ParticipantUserId);
            }

            return Ok(conv);
        }

        /// <summary>
        /// Gets or creates a 1-to-1 conversation with the given counterpart user.
        /// </summary>
        [HttpGet("with/{targetUserId:long}")]
        public async Task<IActionResult> GetOrCreateConversationWith(long targetUserId)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            if (userId.Value == targetUserId)
            {
                return BadRequest(new { message = "You cannot start a conversation with yourself." });
            }

            try
            {
                var conv = await _chatService.GetOrCreateUserConversationAsync(userId.Value, targetUserId);
                conv.IsParticipantOnline = _presenceTracker.IsOnline(targetUserId);
                return Ok(conv);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Gets or creates a support ticket conversation for the current user.
        /// </summary>
        [HttpGet("support/conversation")]
        public async Task<IActionResult> GetOrCreateSupportConversation([FromQuery] string? subject)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var conv = await _chatService.GetOrCreateSupportConversationAsync(userId.Value, subject);
            return Ok(conv);
        }

        /// <summary>
        /// Gets paginated message history for a conversation.
        /// </summary>
        [HttpGet("messages/{conversationId:long}")]
        public async Task<IActionResult> GetMessageHistory(long conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            var (userId, isStaff) = ResolveCaller();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            try
            {
                var history = await _chatService.GetMessageHistoryAsync(conversationId, userId.Value, isStaff, page, pageSize);
                return Ok(history);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Conversation not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Sends a 1-to-1 message via REST.
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            try
            {
                var message = await _chatService.SendUserMessageAsync(userId.Value, request);

                // Broadcast real-time SignalR notifications
                await _hubContext.Clients.Group($"Conversation_{message.ConversationId}").SendAsync("ReceiveMessage", message);
                await _hubContext.Clients.Group($"User_{request.ReceiverId}").SendAsync("ReceiveNewMessageNotification", message);

                int recipientUnread = await _chatService.GetTotalUnreadCountAsync(request.ReceiverId);
                await _hubContext.Clients.Group($"User_{request.ReceiverId}").SendAsync("UnreadCountUpdated", recipientUnread);

                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Sends a support message via REST.
        /// </summary>
        [HttpPost("support/send")]
        public async Task<IActionResult> SendSupportMessage([FromBody] SendSupportMessageRequest request)
        {
            var (userId, isStaff) = ResolveCaller();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            try
            {
                var senderRole = isStaff ? MessageSenderRole.Staff : MessageSenderRole.User;
                var message = await _chatService.SendSupportMessageAsync(userId.Value, senderRole, request);

                // Broadcast real-time SignalR notifications
                await _hubContext.Clients.Group($"Conversation_{message.ConversationId}").SendAsync("ReceiveMessage", message);

                if (isStaff && message.ReceiverId.HasValue)
                {
                    await _hubContext.Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("ReceiveSupportMessage", message);
                    int userUnread = await _chatService.GetTotalUnreadCountAsync(message.ReceiverId.Value);
                    await _hubContext.Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("UnreadCountUpdated", userUnread);
                }
                else if (!isStaff)
                {
                    await _hubContext.Clients.Group(ChatHub.StaffGroup).SendAsync("ReceiveStaffSupportNotification", message);
                    int staffUnread = await _chatService.GetStaffUnreadSupportCountAsync();
                    await _hubContext.Clients.Group(ChatHub.StaffGroup).SendAsync("StaffUnreadCountUpdated", staffUnread);
                }

                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Marks messages as read in a conversation.
        /// </summary>
        [HttpPost("read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkMessagesReadRequest request)
        {
            var (userId, isStaff) = ResolveCaller();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            int count = await _chatService.MarkConversationAsReadAsync(request.ConversationId, userId.Value, isStaff, request.LastReadMessageId);
            if (count > 0)
            {
                var payload = new
                {
                    ConversationId = request.ConversationId,
                    ReadByUserId = userId.Value,
                    IsStaff = isStaff,
                    LastReadMessageId = request.LastReadMessageId,
                    ReadAt = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"Conversation_{request.ConversationId}").SendAsync("MessagesRead", payload);

                if (!isStaff)
                {
                    int totalUnread = await _chatService.GetTotalUnreadCountAsync(userId.Value);
                    await _hubContext.Clients.Group($"User_{userId.Value}").SendAsync("UnreadCountUpdated", totalUnread);
                }
                else
                {
                    int staffUnread = await _chatService.GetStaffUnreadSupportCountAsync();
                    await _hubContext.Clients.Group(ChatHub.StaffGroup).SendAsync("StaffUnreadCountUpdated", staffUnread);
                }
            }

            return Ok(new { success = true, markedCount = count });
        }

        /// <summary>
        /// Gets total unread message count for navbar / tab badge.
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Ok(new { unreadCount = 0 });
            }

            int count = await _chatService.GetTotalUnreadCountAsync(userId.Value);
            return Ok(new { unreadCount = count });
        }

        /// <summary>
        /// Validates if current user can chat with target user.
        /// </summary>
        [HttpGet("check-permission/{targetUserId:long}")]
        public async Task<IActionResult> CheckPermission(long targetUserId)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var perm = await _chatService.CheckChatPermissionAsync(userId.Value, targetUserId);
            return Ok(perm);
        }

        /// <summary>
        /// Blocks or unblocks a conversation.
        /// </summary>
        [HttpPost("block")]
        public async Task<IActionResult> BlockUser([FromBody] BlockUserChatRequest request)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            bool success = await _chatService.BlockUserAsync(userId.Value, request.TargetUserId, request.Block);
            return Ok(new { success });
        }

        /// <summary>
        /// Uploads chat media attachments (image, voice note audio, documents).
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(25 * 1024 * 1024)] // 25 MB max
        public async Task<IActionResult> UploadAttachment([FromForm] IFormFile? file)
        {
            var (userId, isStaff) = ResolveCaller();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided." });
            }

            try
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "Uploads", "Chat");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                string safeFileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{ext}";
                string physicalPath = Path.Combine(uploadFolder, safeFileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string relativeUrl = $"/Uploads/Chat/{safeFileName}";

                return Ok(new
                {
                    attachmentUrl = relativeUrl,
                    fileName = file.FileName,
                    fileSize = file.Length,
                    contentType = file.ContentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatApiController: File upload failed");
                return StatusCode(500, new { message = "File upload failed." });
            }
        }

        #region Staff Support APIs

        /// <summary>
        /// Staff API: View support tickets.
        /// </summary>
        [HttpGet("staff/tickets")]
        public async Task<IActionResult> GetStaffTickets([FromQuery] SupportTicketStatus? status, [FromQuery] long? assignedStaffId)
        {
            var (userId, isStaff) = ResolveCaller();
            if (!isStaff)
            {
                return Forbid();
            }

            var tickets = await _chatService.GetStaffSupportTicketsAsync(status, assignedStaffId);
            return Ok(tickets);
        }

        /// <summary>
        /// Staff API: Assign ticket to staff member.
        /// </summary>
        [HttpPost("staff/assign")]
        public async Task<IActionResult> AssignTicket([FromBody] StaffAssignTicketRequest request)
        {
            var (userId, isStaff) = ResolveCaller();
            if (!isStaff)
            {
                return Forbid();
            }

            string staffName = User.Identity?.Name ?? "Staff";
            bool ok = await _chatService.AssignSupportTicketAsync(request.ConversationId, request.StaffId, staffName);
            return Ok(new { success = ok });
        }

        /// <summary>
        /// Staff API: Update ticket status.
        /// </summary>
        [HttpPost("staff/status")]
        public async Task<IActionResult> UpdateTicketStatus([FromBody] StaffUpdateTicketStatusRequest request)
        {
            var (userId, isStaff) = ResolveCaller();
            if (!isStaff)
            {
                return Forbid();
            }

            string staffName = User.Identity?.Name ?? "Staff";
            bool ok = await _chatService.UpdateSupportTicketStatusAsync(request.ConversationId, request.Status, staffName);
            return Ok(new { success = ok });
        }

        #endregion

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

        private (long? UserId, bool IsStaff) ResolveCaller()
        {
            if (User != null && User.Identity?.IsAuthenticated == true)
            {
                bool isStaff = User.IsInRole("Staff") || User.IsInRole("Admin");
                var staffIdVal = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("StaffId")?.Value;

                if (isStaff && long.TryParse(staffIdVal, out var sId) && sId > 0)
                {
                    return (sId, true);
                }
            }

            var userId = ResolveCurrentUserId();
            return (userId, false);
        }

        #endregion
    }
}
