using Application.Models.Chat;
using Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Persistence
{
    public interface IChatService
    {
        /// <summary>
        /// Validates whether current user is permitted to chat with target user (plan purchase, mutual interest, blocked checks).
        /// </summary>
        Task<ChatPermissionResult> CheckChatPermissionAsync(long currentUserId, long targetUserId);

        /// <summary>
        /// Gets an existing 1-to-1 conversation between two users, or creates one if it doesn't exist.
        /// </summary>
        Task<ConversationDto> GetOrCreateUserConversationAsync(long currentUserId, long targetUserId);

        /// <summary>
        /// Gets or creates a support ticket conversation for a user.
        /// </summary>
        Task<ConversationDto> GetOrCreateSupportConversationAsync(long currentUserId, string? subject = null);

        /// <summary>
        /// Gets all conversations for a user (both 1-to-1 active chats and support thread) ordered by most recent message.
        /// </summary>
        Task<List<ConversationDto>> GetUserConversationsAsync(long currentUserId);

        /// <summary>
        /// Gets support ticket conversations for Staff with optional status or assignment filtering.
        /// </summary>
        Task<List<ConversationDto>> GetStaffSupportTicketsAsync(SupportTicketStatus? statusFilter = null, long? assignedStaffId = null);

        /// <summary>
        /// Retrieves a single conversation by ID with permission checks.
        /// </summary>
        Task<ConversationDto?> GetConversationByIdAsync(long conversationId, long currentUserId, bool isStaff = false);

        /// <summary>
        /// Sends and persists a 1-to-1 user chat message.
        /// </summary>
        Task<ChatMessageDto> SendUserMessageAsync(long senderUserId, SendMessageRequest request);

        /// <summary>
        /// Sends and persists a message inside a Support ticket (from User or Staff).
        /// </summary>
        Task<ChatMessageDto> SendSupportMessageAsync(long senderId, MessageSenderRole senderRole, SendSupportMessageRequest request);

        /// <summary>
        /// Retrieves paginated chat messages for a conversation.
        /// </summary>
        Task<PaginatedChatMessagesDto> GetMessageHistoryAsync(long conversationId, long currentUserId, bool isStaff, int page = 1, int pageSize = 30);

        /// <summary>
        /// Marks all unread messages in a conversation up to a given message ID as Read.
        /// Returns number of updated messages.
        /// </summary>
        Task<int> MarkConversationAsReadAsync(long conversationId, long currentUserId, bool isStaff, long? lastReadMessageId = null);

        /// <summary>
        /// Gets total unread message badge count for a matrimony user across all chats.
        /// </summary>
        Task<int> GetTotalUnreadCountAsync(long currentUserId);

        /// <summary>
        /// Gets total unread support message count across open tickets for staff.
        /// </summary>
        Task<int> GetStaffUnreadSupportCountAsync(long? staffId = null);

        /// <summary>
        /// Blocks or unblocks chat between current user and target user.
        /// </summary>
        Task<bool> BlockUserAsync(long currentUserId, long targetUserId, bool block);

        /// <summary>
        /// Assigns a staff member to handle a support conversation.
        /// </summary>
        Task<bool> AssignSupportTicketAsync(long conversationId, long staffId, string staffName);

        /// <summary>
        /// Updates the status of a support ticket (e.g. InProgress, Resolved, Closed).
        /// </summary>
        Task<bool> UpdateSupportTicketStatusAsync(long conversationId, SupportTicketStatus status, string updatedBy);
    }
}
