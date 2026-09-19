using Application.Interfaces.Persistence;
using Application.Models.Chat;
using Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ChatHub> _logger;

        public const string StaffGroup = "Staff_Support";

        public ChatHub(
            IChatService chatService,
            PresenceTracker presenceTracker,
            CookieHelper cookieHelper,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _presenceTracker = presenceTracker;
            _cookieHelper = cookieHelper;
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

                // Broadcast to conversation room
                await Clients.Group($"Conversation_{message.ConversationId}").SendAsync("ReceiveMessage", message);

                if (caller.IsStaff)
                {
                    // If staff sent the message, notify the user
                    if (message.ReceiverId.HasValue)
                    {
                        await Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("ReceiveSupportMessage", message);
                        int userUnread = await _chatService.GetTotalUnreadCountAsync(message.ReceiverId.Value);
                        await Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("UnreadCountUpdated", userUnread);
                    }
                }
                else
                {
                    // If user sent the message, notify the staff support group
                    await Clients.Group(StaffGroup).SendAsync("ReceiveStaffSupportNotification", message);
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
                int readCount = await _chatService.MarkConversationAsReadAsync(request.ConversationId, caller.UserId.Value, caller.IsStaff, request.LastReadMessageId);
                if (readCount > 0)
                {
                    var payload = new
                    {
                        ConversationId = request.ConversationId,
                        ReadByUserId = caller.UserId.Value,
                        IsStaff = caller.IsStaff,
                        LastReadMessageId = request.LastReadMessageId,
                        ReadAt = DateTime.UtcNow
                    };

                    // Broadcast to conversation room for double blue checks
                    await Clients.OthersInGroup($"Conversation_{request.ConversationId}").SendAsync("MessagesRead", payload);

                    // Update caller's own unread badge
                    if (!caller.IsStaff)
                    {
                        int unread = await _chatService.GetTotalUnreadCountAsync(caller.UserId.Value);
                        await Clients.Caller.SendAsync("UnreadCountUpdated", unread);
                    }
                    else
                    {
                        int staffUnread = await _chatService.GetStaffUnreadSupportCountAsync();
                        await Clients.Group(StaffGroup).SendAsync("StaffUnreadCountUpdated", staffUnread);
                    }
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

            // 1. Check if authenticated staff member via Claims
            if (Context.User != null && Context.User.Identity?.IsAuthenticated == true)
            {
                bool isStaff = Context.User.IsInRole("Staff") || Context.User.IsInRole("Admin");
                var staffIdVal = Context.User.FindFirst(ClaimTypes.Name)?.Value
                    ?? Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User.FindFirst("StaffId")?.Value;

                if (isStaff && long.TryParse(staffIdVal, out var sId) && sId > 0)
                {
                    string staffName = Context.User.Identity.Name ?? "Support Staff";
                    return (sId, true, staffName);
                }
            }

            // 2. Resolve matrimonial user ID from CookieHelper
            if (httpContext != null)
            {
                var cookieUserId = _cookieHelper.GetUserIdFromCookie(httpContext);
                if (cookieUserId.HasValue && cookieUserId.Value > 0)
                {
                    return (cookieUserId.Value, false, null);
                }

                // 3. SignalR handshake query param fallback
                if (httpContext.Request.Query.TryGetValue("userId", out var qUserId) && long.TryParse(qUserId, out var parsedId) && parsedId > 0)
                {
                    return (parsedId, false, null);
                }
            }

            // 4. Fallback user claims
            if (Context.User != null)
            {
                var idClaim = Context.User.FindFirst(ClaimTypes.Name)?.Value
                    ?? Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User.FindFirst("sub")?.Value
                    ?? Context.User.FindFirst("UserId")?.Value;

                if (long.TryParse(idClaim, out var id) && id > 0)
                {
                    return (id, false, null);
                }
            }

            return (null, false, null);
        }
    }
}
