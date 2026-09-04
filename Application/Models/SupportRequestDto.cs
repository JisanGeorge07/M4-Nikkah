using Application.Models.Common;

namespace Application.Models
{
    public class SupportRequestDto : BaseDto
    {
        public long UserId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? AdminNotes { get; set; }
        public string? AttachmentPath { get; set; }
        public DateTime? ResolvedOn { get; set; }

        // Mapped properties for display in lists/details views
        public string? UserRegisterNumber { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
    }
}
