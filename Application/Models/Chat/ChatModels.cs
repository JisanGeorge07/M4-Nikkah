using Domain;
using System;
using System.Collections.Generic;

namespace Application.Models.Chat
{
    public class ConversationDto
    {
        public long Id { get; set; }
        public ConversationType Type { get; set; }
        
        // Matrimony participant (counterpart for 1-to-1 chats, or ticket creator for staff view)
        public long ParticipantUserId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string? ParticipantRegisterNumber { get; set; }
        public string? ParticipantPhotoUrl { get; set; }
        public bool IsParticipantOnline { get; set; }
        public DateTime? ParticipantLastSeenAt { get; set; }
        
        // Support details
        public bool IsSupport => Type == ConversationType.Support;
        public SupportTicketStatus SupportStatus { get; set; }
        public string? SupportSubject { get; set; }
        public long? AssignedStaffId { get; set; }
        public string? AssignedStaffName { get; set; }

        // Last message preview
        public long? LastMessageId { get; set; }
        public string? LastMessagePreview { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public long? LastMessageSenderId { get; set; }
        public MessageSenderRole? LastMessageSenderRole { get; set; }

        // Unread badges
        public int UnreadCount { get; set; }

        // Moderation
        public bool IsBlocked { get; set; }
        public long? BlockedByUserId { get; set; }
        public bool CanReply { get; set; } = true;
        public string? PermissionReason { get; set; }
    }

    public class ChatMessageDto
    {
        public long Id { get; set; }
        public long ConversationId { get; set; }
        
        public long SenderId { get; set; }
        public MessageSenderRole SenderRole { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderPhotoUrl { get; set; }
        public long? ReceiverId { get; set; }

        public string Content { get; set; } = string.Empty;
        public ChatMessageType MessageType { get; set; }

        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public long? AttachmentFileSize { get; set; }

        public ChatMessageStatus Status { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        public bool IsMine { get; set; }
    }

    public class SendMessageRequest
    {
        public long ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public long? AttachmentFileSize { get; set; }
    }

    public class SendSupportMessageRequest
    {
        public long? ConversationId { get; set; }
        public string? Subject { get; set; }
        public string Content { get; set; } = string.Empty;
        public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public long? AttachmentFileSize { get; set; }
    }

    public class ChatPermissionResult
    {
        public bool Allowed { get; set; }
        public string? Reason { get; set; }
        public bool RequiresUpgrade { get; set; }
        public bool IsMutualInterestRequired { get; set; }
    }

    public class PaginatedChatMessagesDto
    {
        public long ConversationId { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }

    public class MarkMessagesReadRequest
    {
        public long ConversationId { get; set; }
        public long? LastReadMessageId { get; set; }
    }

    public class BlockUserChatRequest
    {
        public long TargetUserId { get; set; }
        public bool Block { get; set; } = true;
    }

    public class StaffAssignTicketRequest
    {
        public long ConversationId { get; set; }
        public long StaffId { get; set; }
    }

    public class StaffUpdateTicketStatusRequest
    {
        public long ConversationId { get; set; }
        public SupportTicketStatus Status { get; set; }
    }
}
