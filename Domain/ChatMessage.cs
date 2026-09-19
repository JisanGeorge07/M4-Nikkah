using Domain.Common;
using System;

namespace Domain
{
    public class ChatMessage : BaseEntity
    {
        public long ConversationId { get; set; }

        // Sender ID: Registration Id (for User) or ApplicationUser Id (for Staff)
        public long SenderId { get; set; }
        public MessageSenderRole SenderRole { get; set; } = MessageSenderRole.User;

        // Receiver ID: Nullable if addressed to Support team queue
        public long? ReceiverId { get; set; }

        public string Content { get; set; } = string.Empty;
        public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

        // Attachments
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public long? AttachmentFileSize { get; set; }

        // Status & timestamps
        public ChatMessageStatus Status { get; set; } = ChatMessageStatus.Sent;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Soft deletion flags per participant
        public bool IsDeletedForSender { get; set; } = false;
        public bool IsDeletedForReceiver { get; set; } = false;

        // Navigation
        public virtual Conversation Conversation { get; set; } = null!;
    }
}
