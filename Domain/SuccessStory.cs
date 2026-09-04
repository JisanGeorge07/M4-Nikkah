using Domain.Common;

namespace Domain
{
    public class SuccessStory : BaseEntity
    {
        public long SubmittedByUserId { get; set; }         // Who submitted the story
        public long PartnerUserId { get; set; }             // The partner's user ID
        public long MatchStatusUpdateId { get; set; }       // FK to MatchStatusUpdate

        // Display Info
        public string BrideDisplayName { get; set; } = string.Empty;
        public string GroomDisplayName { get; set; } = string.Empty;
        public DateTime MarriageDate { get; set; }
        public string Story { get; set; } = string.Empty;   // Feedback / success story text
        public int Rating { get; set; }                     // 1-5
        public string? CouplePhotoPath { get; set; }        // Optional photo
        public bool ConsentToPublish { get; set; }          // Must be true
        public bool HideFullName { get; set; }              // Show initials only

        // Admin Moderation
        public SuccessStoryStatus Status { get; set; } = SuccessStoryStatus.PendingApproval;
        public bool IsFeatured { get; set; } = false;
        public string? AdminNotes { get; set; }
        public string? AdminEditedStory { get; set; }       // Admin-corrected version (if edited)
        public string? ReviewedBy { get; set; }             // Admin user ID
        public DateTime? ReviewedOn { get; set; }
    }
}
