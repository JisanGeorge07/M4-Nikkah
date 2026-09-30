using Application.Interfaces.Persistence;
using Application.Models.Chat;
using Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly AppDbContext _dbContext;

        public ChatApiController(
            IChatService chatService,
            IHubContext<ChatHub> hubContext,
            PresenceTracker presenceTracker,
            CookieHelper cookieHelper,
            IWebHostEnvironment env,
            ILogger<ChatApiController> logger,
            AppDbContext dbContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
            _presenceTracker = presenceTracker;
            _cookieHelper = cookieHelper;
            _env = env;
            _logger = logger;
            _dbContext = dbContext;
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
            var (userId, isStaff) = await ResolveCallerAsync();
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
        /// Gets strictly live online users for the carousel / horizontal list (max 5).
        /// </summary>
        [HttpGet("online-users")]
        public async Task<IActionResult> GetOnlineUsers([FromQuery] int limit = 5)
        {
            var userId = ResolveCurrentUserId();
            Registration? loginUser = null;
            if (userId.HasValue && userId.Value > 0)
            {
                loginUser = await _dbContext.Registration.AsNoTracking().FirstOrDefaultAsync(r => r.Id == userId.Value);
            }

            string? oppositeGender = null;
            if (loginUser != null && !string.IsNullOrEmpty(loginUser.Gender))
            {
                oppositeGender = string.Equals(loginUser.Gender, "Male", StringComparison.OrdinalIgnoreCase)
                    ? "Female"
                    : (string.Equals(loginUser.Gender, "Female", StringComparison.OrdinalIgnoreCase) ? "Male" : null);
            }

            var allOnlineIds = _presenceTracker.GetAllOnlineUserIds();
            if (userId.HasValue && userId.Value > 0)
            {
                allOnlineIds.Remove(userId.Value);
            }

            if (!allOnlineIds.Any())
            {
                return Ok(Array.Empty<object>());
            }

            IQueryable<Registration> baseQuery = _dbContext.Registration
                .AsNoTracking()
                .Where(r => allOnlineIds.Contains(r.Id) && !r.IsDeleted && r.IsVisible);

            if (!string.IsNullOrEmpty(oppositeGender))
            {
                baseQuery = baseQuery.Where(r => r.Gender == oppositeGender);
            }

            var onlineProfiles = await baseQuery
                .Take(limit)
                .ToListAsync();

            var result = onlineProfiles.Select(r =>
            {
                string defaultImg = string.Equals(r.Gender, "Female", StringComparison.OrdinalIgnoreCase)
                    ? "/assets/images/default-user-women.png"
                    : "/assets/images/default-user-men.png";

                string imgPath = !string.IsNullOrEmpty(r.ImagePath)
                    ? (r.ImagePath.StartsWith("/") || r.ImagePath.StartsWith("http") ? r.ImagePath : "/" + r.ImagePath)
                    : defaultImg;

                return new
                {
                    id = r.Id,
                    name = r.Name ?? "Member",
                    registerNumber = r.RegisterNumber ?? ("ID: " + r.Id),
                    imagePath = imgPath,
                    isOnline = true,
                    gender = r.Gender
                };
            });

            return Ok(result);
        }

        /// <summary>
        /// Gets profiles for the OnlineList page with tab switching & filtering.
        /// </summary>
        [HttpGet("online-list")]
        public async Task<IActionResult> GetOnlineListProfiles(
            [FromQuery] string tab = "online",
            [FromQuery] string? profileId = null,
            [FromQuery] string? age = null,
            [FromQuery] string? location = null,
            [FromQuery] string? profession = null)
        {
            var userId = ResolveCurrentUserId();
            long currentUserId = userId ?? 0;

            Registration? loginUser = null;
            if (currentUserId > 0)
            {
                loginUser = await _dbContext.Registration.AsNoTracking().FirstOrDefaultAsync(r => r.Id == currentUserId);
            }

            string? oppositeGender = null;
            if (loginUser != null && !string.IsNullOrEmpty(loginUser.Gender))
            {
                oppositeGender = string.Equals(loginUser.Gender, "Male", StringComparison.OrdinalIgnoreCase)
                    ? "Female"
                    : (string.Equals(loginUser.Gender, "Female", StringComparison.OrdinalIgnoreCase) ? "Male" : null);
            }

            IQueryable<Registration> query = _dbContext.Registration
                .AsNoTracking()
                .Where(r => r.Id != currentUserId && !r.IsDeleted && r.IsVisible);

            if (!string.IsNullOrEmpty(oppositeGender))
            {
                query = query.Where(r => r.Gender == oppositeGender);
            }

            if (tab.Equals("online", StringComparison.OrdinalIgnoreCase))
            {
                var onlineIds = _presenceTracker.GetAllOnlineUserIds().Where(id => id != currentUserId).ToList();
                if (onlineIds.Any())
                {
                    query = query.Where(r => onlineIds.Contains(r.Id));
                }
                else
                {
                    // Strictly online only: if none are online, return empty
                    return Ok(Array.Empty<object>());
                }
            }
            else if (tab.Equals("matches", StringComparison.OrdinalIgnoreCase))
            {
                if (currentUserId > 0)
                {
                    var matchedUserIds = await _dbContext.Userfavoriteprofile
                        .AsNoTracking()
                        .Where(f => !f.IsDeleted && f.Status == InterestStatus.Accepted && (f.UserId == currentUserId || f.LikedId == currentUserId))
                        .Select(f => f.UserId == currentUserId ? f.LikedId : f.UserId)
                        .Distinct()
                        .ToListAsync();

                    if (matchedUserIds.Any())
                    {
                        query = query.Where(r => matchedUserIds.Contains(r.Id));
                    }
                    else
                    {
                        query = query.Where(r => false);
                    }
                }
                else
                {
                    query = query.Where(r => false);
                }
            }
            else // "recent"
            {
                query = query.OrderByDescending(r => r.LastSeenAt ?? r.CreatedOn);
            }

            if (!string.IsNullOrWhiteSpace(profileId))
            {
                var pId = profileId.Trim();
                query = query.Where(r => (r.RegisterNumber != null && r.RegisterNumber.Contains(pId)) || (r.Name != null && r.Name.Contains(pId)));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var loc = location.Trim();
                query = query.Where(r => (r.PresentCity != null && r.PresentCity.Contains(loc))
                    || (r.District != null && r.District.Contains(loc))
                    || (r.State != null && r.State.Contains(loc))
                    || (r.PresentDistrict != null && r.PresentDistrict.Contains(loc))
                    || (r.PresentState != null && r.PresentState.Contains(loc)));
            }

            if (!string.IsNullOrWhiteSpace(profession))
            {
                var prof = profession.Trim();
                query = query.Where(r => (r.ProfessionType != null && r.ProfessionType.Contains(prof))
                    || (r.HighestEducation != null && r.HighestEducation.Contains(prof)));
            }

            var list = await query.Take(60).ToListAsync();

            // Filter age in-memory since DOB is stored as string
            if (!string.IsNullOrWhiteSpace(age))
            {
                if (int.TryParse(age, out int selectedAgeBracket))
                {
                    list = list.Where(r =>
                    {
                        var calculatedAge = CalculateAge(r.DOB);
                        if (!calculatedAge.HasValue) return true;
                        if (selectedAgeBracket == 25) return calculatedAge.Value >= 25 && calculatedAge.Value <= 27;
                        if (selectedAgeBracket == 28) return calculatedAge.Value >= 28 && calculatedAge.Value <= 30;
                        if (selectedAgeBracket == 31) return calculatedAge.Value >= 31 && calculatedAge.Value <= 35;
                        if (selectedAgeBracket == 36) return calculatedAge.Value >= 36;
                        return true;
                    }).ToList();
                }
            }

            var result = list.Select(r =>
            {
                bool isOnline = _presenceTracker.IsOnline(r.Id);
                int? calculatedAge = CalculateAge(r.DOB);
                string loc = !string.IsNullOrEmpty(r.PresentCity) ? r.PresentCity : (!string.IsNullOrEmpty(r.District) ? r.District : (r.State ?? "Kerala"));
                if (!string.IsNullOrEmpty(r.State) && !loc.Contains(r.State)) loc += ", " + r.State;
                string job = !string.IsNullOrEmpty(r.ProfessionType) ? r.ProfessionType : (!string.IsNullOrEmpty(r.HighestEducation) ? r.HighestEducation : "Professional");

                string defaultImg = string.Equals(r.Gender, "Female", StringComparison.OrdinalIgnoreCase)
                    ? "/assets/images/default-user-women.png"
                    : "/assets/images/default-user-men.png";

                string imgPath = !string.IsNullOrEmpty(r.ImagePath)
                    ? (r.ImagePath.StartsWith("/") || r.ImagePath.StartsWith("http") ? r.ImagePath : "/" + r.ImagePath)
                    : defaultImg;

                string timeText = isOnline ? "Online Now" : GetRelativeTime(r.LastSeenAt);

                return new
                {
                    id = r.Id,
                    name = r.Name ?? "Member",
                    registerNumber = r.RegisterNumber ?? ("ID: " + r.Id),
                    imagePath = imgPath,
                    age = calculatedAge ?? 26,
                    location = loc,
                    profession = job,
                    isOnline = isOnline,
                    status = isOnline ? "online" : "recent",
                    timeText = timeText,
                    gender = r.Gender
                };
            });

            return Ok(result);
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
            var (userId, isStaff) = await ResolveCallerAsync();
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
            var (userId, isStaff) = await ResolveCallerAsync();
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
                    await _hubContext.Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("ReceiveMessage", message);
                    int userUnread = await _chatService.GetTotalUnreadCountAsync(message.ReceiverId.Value);
                    await _hubContext.Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("UnreadCountUpdated", userUnread);
                }
                else if (!isStaff)
                {
                    await _hubContext.Clients.Group(ChatHub.StaffGroup).SendAsync("ReceiveStaffSupportNotification", message);
                    await _hubContext.Clients.Group(ChatHub.StaffGroup).SendAsync("ReceiveMessage", message);
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
            var (userId, isStaff) = await ResolveCallerAsync();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var conv = await _dbContext.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ConversationId && !c.IsDeleted);
            if (conv == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            int count = await _chatService.MarkConversationAsReadAsync(request.ConversationId, userId.Value, isStaff, request.LastReadMessageId);

            var payload = new
            {
                ConversationId = request.ConversationId,
                ReadByUserId = userId.Value,
                IsStaff = isStaff,
                LastReadMessageId = request.LastReadMessageId,
                ReadAt = DateTime.UtcNow
            };

            // 1. Broadcast to conversation room for users actively viewing the chat
            await _hubContext.Clients.Group($"Conversation_{request.ConversationId}").SendAsync("MessagesRead", payload);

            // 2. Broadcast directly to the other participant's User group so ticks update in real-time even across tabs / reconnects
            if (conv.Type == ConversationType.UserToUser)
            {
                long otherUserId = conv.User1Id == userId.Value ? (conv.User2Id ?? 0) : conv.User1Id;
                if (otherUserId > 0)
                {
                    await _hubContext.Clients.Group($"User_{otherUserId}").SendAsync("MessagesRead", payload);
                }
            }
            else if (conv.Type == ConversationType.Support)
            {
                if (isStaff)
                {
                    await _hubContext.Clients.Group($"User_{conv.User1Id}").SendAsync("MessagesRead", payload);
                }
                else
                {
                    await _hubContext.Clients.Group(ChatHub.StaffGroup).SendAsync("MessagesRead", payload);
                }
            }

            // 3. Update reader's total unread count badge
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
            var (userId, isStaff) = await ResolveCallerAsync();
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
            var (userId, isStaff) = await ResolveCallerAsync();
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
            var (userId, isStaff) = await ResolveCallerAsync();
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
            var (userId, isStaff) = await ResolveCallerAsync();
            if (!isStaff)
            {
                return Forbid();
            }

            string staffName = User.Identity?.Name ?? "Staff";
            bool ok = await _chatService.UpdateSupportTicketStatusAsync(request.ConversationId, request.Status, staffName);
            return Ok(new { success = ok });
        }

        /// <summary>
        /// Returns whether mutual interest is accepted and whether to show the interest card.
        /// </summary>
        [HttpGet("interest-status/{targetUserId:long}")]
        public async Task<IActionResult> GetInterestStatus(long targetUserId)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
                return Unauthorized(new { message = "Authentication required." });

            bool isAccepted = await _dbContext.Userfavoriteprofile
                .AsNoTracking()
                .AnyAsync(f => !f.IsDeleted && f.Status == InterestStatus.Accepted && (
                    (f.UserId == userId.Value && f.LikedId == targetUserId) ||
                    (f.UserId == targetUserId && f.LikedId == userId.Value)
                ));

            // Show interest card only once: check if already shown for this conversation
            long u1 = Math.Min(userId.Value, targetUserId);
            long u2 = Math.Max(userId.Value, targetUserId);

            bool alreadyShown = await _dbContext.Conversations
                .AsNoTracking()
                .AnyAsync(c => !c.IsDeleted && c.Type == ConversationType.UserToUser
                    && c.User1Id == u1 && c.User2Id == u2
                    && c.InterestCardShown);

            return Ok(new
            {
                isAccepted,
                showInterestCard = isAccepted && !alreadyShown
            });
        }

        /// <summary>
        /// Marks the interest card as shown for a conversation.
        /// </summary>
        [HttpPost("interest-card-shown/{targetUserId:long}")]
        public async Task<IActionResult> MarkInterestCardShown(long targetUserId)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue || userId.Value <= 0)
                return Unauthorized(new { message = "Authentication required." });

            long u1 = Math.Min(userId.Value, targetUserId);
            long u2 = Math.Max(userId.Value, targetUserId);

            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => !c.IsDeleted && c.Type == ConversationType.UserToUser
                    && c.User1Id == u1 && c.User2Id == u2);

            if (conv != null)
            {
                conv.InterestCardShown = true;
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        #endregion

        #region Private Helpers

        private long? ResolveCurrentUserId()
        {
            // 1. Check custom X-User-Id header (for mobile apps & testing tools)
            if (HttpContext.Request.Headers.TryGetValue("X-User-Id", out var hUserId) && long.TryParse(hUserId, out var headerId) && headerId > 0)
            {
                return headerId;
            }

            // 2. Check userId query parameter (for direct browser/test requests)
            if (HttpContext.Request.Query.TryGetValue("userId", out var qUserId) && long.TryParse(qUserId, out var queryId) && queryId > 0)
            {
                return queryId;
            }

            // 3. Check web session cookie
            var cookieUserId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            if (cookieUserId.HasValue && cookieUserId.Value > 0)
            {
                return cookieUserId.Value;
            }

            // 4. Check JWT / Identity claims
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

        private async Task<(long? UserId, bool IsStaff)> ResolveCallerAsync()
        {
            // 1. Check HttpContext.User directly
            var user = HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                // Authenticate with Identity.Application (Admin Identity cookie)
                var idResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
                if (idResult?.Succeeded == true && idResult.Principal != null)
                {
                    user = idResult.Principal;
                }
                else
                {
                    // Authenticate with Cookies scheme
                    var cookieResult = await HttpContext.AuthenticateAsync("Cookies");
                    if (cookieResult?.Succeeded == true && cookieResult.Principal != null)
                    {
                        user = cookieResult.Principal;
                    }
                    else
                    {
                        // Authenticate with JWT Bearer scheme (if staff mobile/SPA sends token)
                        var jwtResult = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                        if (jwtResult?.Succeeded == true && jwtResult.Principal != null)
                        {
                            user = jwtResult.Principal;
                        }
                    }
                }
            }

            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                bool isStaff = user.IsInRole("Staff") || user.IsInRole("Admin") 
                    || user.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") && (c.Value == "Admin" || c.Value == "Staff" || c.Value == "Administrator"));

                if (isStaff)
                {
                    var staffIdVal = user.FindFirst("StaffId")?.Value
                        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst(ClaimTypes.Name)?.Value;

                    if (long.TryParse(staffIdVal, out var sId) && sId > 0)
                    {
                        return (sId, true);
                    }
                    return (1, true); // Fallback staff/admin ID
                }
            }

            // Fallback: Check if Admin Identity Cookie is present on the request
            if (HttpContext.Request.Cookies.ContainsKey(".AspNetCore.Identity.Application"))
            {
                return (1, true);
            }

            // Fallback for query param (for staff/testing tools)
            if (HttpContext.Request.Query.TryGetValue("isStaff", out var qIsStaff) && (string.Equals(qIsStaff, "true", StringComparison.OrdinalIgnoreCase) || qIsStaff == "1"))
            {
                long staffId = 1;
                if (HttpContext.Request.Query.TryGetValue("staffId", out var qStaffId) && long.TryParse(qStaffId, out var parsedStaffId) && parsedStaffId > 0)
                {
                    staffId = parsedStaffId;
                }
                return (staffId, true);
            }

            var userId = ResolveCurrentUserId();
            return (userId, false);
        }

        private static int? CalculateAge(string? dob)
        {
            if (string.IsNullOrWhiteSpace(dob)) return null;
            if (DateTime.TryParse(dob, out var birthDate))
            {
                var today = DateTime.Today;
                var age = today.Year - birthDate.Year;
                if (birthDate.Date > today.AddYears(-age)) age--;
                return age;
            }
            var parts = dob.Split(new[] { '/', '-' });
            if (parts.Length == 3 && int.TryParse(parts[2], out int year) && int.TryParse(parts[1], out int month) && int.TryParse(parts[0], out int day))
            {
                try
                {
                    var dt = new DateTime(year, month, day);
                    var today = DateTime.Today;
                    var age = today.Year - dt.Year;
                    if (dt.Date > today.AddYears(-age)) age--;
                    return age;
                }
                catch { }
            }
            return null;
        }

        private static string GetRelativeTime(DateTime? dt)
        {
            if (!dt.HasValue) return "Recently active";
            var dtUtc = DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
            var span = DateTime.UtcNow - dtUtc;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} days ago";
            return dtUtc.ToString("dd MMM");
        }

        #endregion
    }
}
