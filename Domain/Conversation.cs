using Domain.Common;
using System;
using System.Collections.Generic;

namespace Domain
{
    public class Conversation : BaseEntity
    {
        public ConversationType Type { get; set; } = ConversationType.UserToUser;

        // User1 is always the initiating/primary matrimony profile
        public long User1Id { get; set; }

        // User2 is the counterpart matrimony profile (for UserToUser chats, null for Support)
        public long? User2Id { get; set; }

        // For Support conversations: Assigned staff member ID (from AspNetUsers / StaffDetail)
        public long? AssignedStaffId { get; set; }

        // Support ticket attributes
        public SupportTicketStatus SupportStatus { get; set; } = SupportTicketStatus.Open;
        public string? SupportSubject { get; set; }

        // Summary of last message for efficient thread listings
        public long? LastMessageId { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? LastMessagePreview { get; set; }
        public long? LastMessageSenderId { get; set; }

        // Blocking & moderation
        public bool IsBlocked { get; set; } = false;
        public long? BlockedByUserId { get; set; }

        // Unread counters
        public int User1UnreadCount { get; set; } = 0;
        public int User2UnreadCount { get; set; } = 0;
        public int SupportUnreadCount { get; set; } = 0;

        // Navigation
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
