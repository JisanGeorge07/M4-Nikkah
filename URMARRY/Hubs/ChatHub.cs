using Application.Interfaces.Persistence;
using Application.Models.Chat;
using Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using URMARRY.Services;

namespace URMARRY.Hubs
{
    /// <summary>
    /// Real-time SignalR hub for 1-to-1 User messaging and Staff Support ticketing.
    /// Handles message broadcasting, delivery/read events, typing states, and badge counters.
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly PresenceTracker _presenceTracker;
        private readonly CookieHelper _cookieHelper;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ChatHub> _logger;

        public const string StaffGroup = "Staff_Support";

        public ChatHub(
            IChatService chatService,
            PresenceTracker presenceTracker,
            CookieHelper cookieHelper,
            AppDbContext dbContext,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _presenceTracker = presenceTracker;
            _cookieHelper = cookieHelper;
            _dbContext = dbContext;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var caller = ResolveCaller();
            if (caller.UserId.HasValue && caller.UserId.Value > 0)
            {
                if (caller.IsStaff)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
                    _logger.LogInformation("ChatHub: Staff member {StaffId} ({StaffName}) connected (ConnectionId: {ConnectionId})", caller.UserId.Value, caller.StaffName, Context.ConnectionId);
                }
                else
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{caller.UserId.Value}");
                    _logger.LogInformation("ChatHub: User {UserId} connected (ConnectionId: {ConnectionId})", caller.UserId.Value, Context.ConnectionId);
                }
            }
            else
            {
                _logger.LogWarning("ChatHub: Anonymous or unauthenticated connection {ConnectionId}", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var caller = ResolveCaller();
            if (caller.UserId.HasValue && caller.UserId.Value > 0)
            {
                if (caller.IsStaff)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, StaffGroup);
                }
                else
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{caller.UserId.Value}");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client joins a specific conversation room for active real-time message stream.
        /// </summary>
        public async Task JoinConversation(long conversationId)
        {
            var caller = ResolveCaller();
            if (!caller.UserId.HasValue || caller.UserId.Value <= 0)
            {
                throw new HubException("Unauthorized to join conversation.");
            }

            var conv = await _chatService.GetConversationByIdAsync(conversationId, caller.UserId.Value, caller.IsStaff);
            if (conv == null)
            {
                throw new HubException("Conversation not found or access denied.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
            _logger.LogDebug("ChatHub: Connection {ConnectionId} joined Conversation_{ConversationId}", Context.ConnectionId, conversationId);
        }

        /// <summary>
        /// Client leaves conversation room when switching threads or navigating away.
        /// </summary>
        public async Task LeaveConversation(long conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
            _logger.LogDebug("ChatHub: Connection {ConnectionId} left Conversation_{ConversationId}", Context.ConnectionId, conversationId);
        }

        /// <summary>
        /// Sends a 1-to-1 message between two matrimony profiles.
        /// </summary>
        public async Task<ChatMessageDto> SendMessage(SendMessageRequest request)
        {
            var caller = ResolveCaller();
            if (!caller.UserId.HasValue || caller.UserId.Value <= 0 || caller.IsStaff)
            {
                throw new HubException("Unauthorized to send user messages.");
            }

            try
            {
                var message = await _chatService.SendUserMessageAsync(caller.UserId.Value, request);

                // 1. Broadcast to active conversation room
                await Clients.Group($"Conversation_{message.ConversationId}").SendAsync("ReceiveMessage", message);

                // 2. Broadcast to recipient's private user group (if not currently focused on this thread)
                await Clients.Group($"User_{request.ReceiverId}").SendAsync("ReceiveNewMessageNotification", message);

                // 3. Update recipient's total unread count badge
                int recipientUnread = await _chatService.GetTotalUnreadCountAsync(request.ReceiverId);
                await Clients.Group($"User_{request.ReceiverId}").SendAsync("UnreadCountUpdated", recipientUnread);

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatHub: Error sending message from user {SenderId} to {ReceiverId}", caller.UserId.Value, request.ReceiverId);
                throw new HubException(ex.Message);
            }
        }

        /// <summary>
        /// Sends a message in a Support ticket (from User or Staff).
        /// </summary>
        public async Task<ChatMessageDto> SendSupportMessage(SendSupportMessageRequest request)
        {
            var caller = ResolveCaller();
            if (!caller.UserId.HasValue || caller.UserId.Value <= 0)
            {
                throw new HubException("Unauthorized.");
            }

            try
            {
                var senderRole = caller.IsStaff ? MessageSenderRole.Staff : MessageSenderRole.User;
                var message = await _chatService.SendSupportMessageAsync(caller.UserId.Value, senderRole, request);

                // 1. Broadcast to conversation room
                await Clients.Group($"Conversation_{message.ConversationId}").SendAsync("ReceiveMessage", message);

                if (caller.IsStaff)
                {
                    // If staff sent the message, notify the user's private group
                    if (message.ReceiverId.HasValue)
                    {
                        await Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("ReceiveSupportMessage", message);
                        await Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("ReceiveMessage", message);
                        int userUnread = await _chatService.GetTotalUnreadCountAsync(message.ReceiverId.Value);
                        await Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("UnreadCountUpdated", userUnread);
                    }
                }
                else
                {
                    // If user sent the message, notify the staff support group in real-time
                    await Clients.Group(StaffGroup).SendAsync("ReceiveStaffSupportNotification", message);
                    await Clients.Group(StaffGroup).SendAsync("ReceiveMessage", message);
                    int staffUnread = await _chatService.GetStaffUnreadSupportCountAsync();
                    await Clients.Group(StaffGroup).SendAsync("StaffUnreadCountUpdated", staffUnread);
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatHub: Error sending support message from {SenderId} (Role: {Role})", caller.UserId.Value, caller.IsStaff ? "Staff" : "User");
                throw new HubException(ex.Message);
            }
        }

        /// <summary>
        /// Broadcasts typing indicator status to active participants.
        /// </summary>
        public async Task SendTyping(long conversationId, long? receiverId, bool isTyping)
        {
            var caller = ResolveCaller();
            if (!caller.UserId.HasValue || caller.UserId.Value <= 0) return;

            var payload = new
            {
                ConversationId = conversationId,
                SenderId = caller.UserId.Value,
                IsStaff = caller.IsStaff,
                SenderName = caller.StaffName ?? "User",
                IsTyping = isTyping
            };

            // Broadcast to conversation room
            await Clients.OthersInGroup($"Conversation_{conversationId}").SendAsync("UserTyping", payload);

            if (receiverId.HasValue && receiverId.Value > 0)
            {
                await Clients.Group($"User_{receiverId.Value}").SendAsync("UserTyping", payload);
            }
            else if (!caller.IsStaff)
            {
                await Clients.Group(StaffGroup).SendAsync("UserTyping", payload);
            }
        }

        /// <summary>
        /// Marks messages as read and notifies the other participant in real time (Double Blue Check).
        /// </summary>
        public async Task MarkAsRead(MarkMessagesReadRequest request)
        {
            var caller = ResolveCaller();
            if (!caller.UserId.HasValue || caller.UserId.Value <= 0) return;

            try
            {
                var conv = await _dbContext.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ConversationId && !c.IsDeleted);
                if (conv == null) return;

                int readCount = await _chatService.MarkConversationAsReadAsync(request.ConversationId, caller.UserId.Value, caller.IsStaff, request.LastReadMessageId);

                var payload = new
                {
                    ConversationId = request.ConversationId,
                    ReadByUserId = caller.UserId.Value,
                    IsStaff = caller.IsStaff,
                    LastReadMessageId = request.LastReadMessageId,
                    ReadAt = DateTime.UtcNow
                };

                // 1. Broadcast to conversation room for active viewers
                await Clients.Group($"Conversation_{request.ConversationId}").SendAsync("MessagesRead", payload);

                // 2. Broadcast to other participant's User group so ticks update regardless of where they are
                if (conv.Type == ConversationType.UserToUser)
                {
                    long otherUserId = conv.User1Id == caller.UserId.Value ? (conv.User2Id ?? 0) : conv.User1Id;
                    if (otherUserId > 0)
                    {
                        await Clients.Group($"User_{otherUserId}").SendAsync("MessagesRead", payload);
                    }
                }
                else if (conv.Type == ConversationType.Support)
                {
                    if (caller.IsStaff)
                    {
                        await Clients.Group($"User_{conv.User1Id}").SendAsync("MessagesRead", payload);
                    }
                    else
                    {
                        await Clients.Group(StaffGroup).SendAsync("MessagesRead", payload);
                    }
                }

                // 3. Update caller's own unread badge
                if (!caller.IsStaff)
                {
                    int unread = await _chatService.GetTotalUnreadCountAsync(caller.UserId.Value);
                    await Clients.Caller.SendAsync("UnreadCountUpdated", unread);
                    await Clients.Group($"User_{caller.UserId.Value}").SendAsync("UnreadCountUpdated", unread);
                }
                else
                {
                    int staffUnread = await _chatService.GetStaffUnreadSupportCountAsync();
                    await Clients.Group(StaffGroup).SendAsync("StaffUnreadCountUpdated", staffUnread);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatHub: Error marking conversation {ConversationId} as read", request.ConversationId);
            }
        }

        private (long? UserId, bool IsStaff, string? StaffName) ResolveCaller()
        {
            var httpContext = Context.GetHttpContext();

            // 1. Explicit Staff query parameter (e.g. /hubs/chat?isStaff=true or /hubs/chat?staffId=1)
            if (httpContext != null)
            {
                bool hasStaffFlag = httpContext.Request.Query.TryGetValue("isStaff", out var qIsStaff) && (string.Equals(qIsStaff, "true", StringComparison.OrdinalIgnoreCase) || qIsStaff == "1");
                bool hasStaffId = httpContext.Request.Query.TryGetValue("staffId", out var qStaffId) && long.TryParse(qStaffId, out _);

                if (hasStaffFlag || hasStaffId)
                {
                    long sId = 1;
                    if (httpContext.Request.Query.TryGetValue("staffId", out var staffIdStr) && long.TryParse(staffIdStr, out var parsedStaffId) && parsedStaffId > 0)
                    {
                        sId = parsedStaffId;
                    }
                    return (sId, true, "Support Staff");
                }
            }

            // 2. Check authenticated user via Context.User or HttpContext.User
            var user = Context.User ?? httpContext?.User;
            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                bool isStaff = user.IsInRole("Staff") || user.IsInRole("Admin") 
                    || user.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") && (c.Value == "Admin" || c.Value == "Staff" || c.Value == "Administrator"));

                if (isStaff)
                {
                    var staffIdVal = user.FindFirst("StaffId")?.Value
                        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst(ClaimTypes.Name)?.Value;

                    long sId = 1;
                    if (!string.IsNullOrEmpty(staffIdVal) && long.TryParse(staffIdVal, out var parsedStaffId) && parsedStaffId > 0)
                    {
                        sId = parsedStaffId;
                    }

                    string staffName = user.Identity.Name ?? "Support Staff";
                    return (sId, true, staffName);
                }
            }

            // 3. Check if Admin Identity Cookie is present in HTTP request
            if (httpContext != null && httpContext.Request.Cookies.ContainsKey(".AspNetCore.Identity.Application"))
            {
                return (1, true, "Support Staff");
            }

            // 4. Explicit matrimonial user ID query param (/hubs/chat?userId=123)
            if (httpContext != null && httpContext.Request.Query.TryGetValue("userId", out var qUserId) && long.TryParse(qUserId, out var parsedId) && parsedId > 0)
            {
                return (parsedId, false, null);
            }

            // 5. Custom header
            if (httpContext != null && httpContext.Request.Headers.TryGetValue("X-User-Id", out var hUserId) && long.TryParse(hUserId, out var headerId) && headerId > 0)
            {
                return (headerId, false, null);
            }

            // 6. Resolve matrimonial user ID from CookieHelper
            if (httpContext != null)
            {
                var cookieUserId = _cookieHelper.GetUserIdFromCookie(httpContext);
                if (cookieUserId.HasValue && cookieUserId.Value > 0)
                {
                    return (cookieUserId.Value, false, null);
                }
            }

            // 7. Fallback user claims
            if (user != null)
            {
                var idClaim = user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value
                    ?? user.FindFirst("UserId")?.Value;

                if (long.TryParse(idClaim, out var id) && id > 0)
                {
                    return (id, false, null);
                }
            }

            return (null, false, null);
        }
    }
}
