using Application.Models.Common;
using Domain;
using Microsoft.AspNetCore.Http;

namespace Application.Models
{
    public class SuccessStoryDto : BaseDto
    {
        public long SubmittedByUserId { get; set; }
        public long PartnerUserId { get; set; }
        public long MatchStatusUpdateId { get; set; }

        public string BrideDisplayName { get; set; } = string.Empty;
        public string GroomDisplayName { get; set; } = string.Empty;
        public DateTime MarriageDate { get; set; }
        public string Story { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? CouplePhotoPath { get; set; }
        public bool ConsentToPublish { get; set; }
        public bool HideFullName { get; set; }

        public SuccessStoryStatus Status { get; set; }
        public bool IsFeatured { get; set; }
        public string? AdminNotes { get; set; }
        public string? AdminEditedStory { get; set; }
        public string? ReviewedBy { get; set; }
        public string? ReviewedByUsername { get; set; }  // Resolved for admin display
        public DateTime? ReviewedOn { get; set; }

        // For form submission (file upload)
        public IFormFile? CouplePhoto { get; set; }
    }
}
