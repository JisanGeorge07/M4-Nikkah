using Domain.Common;

namespace Domain;

public class SupportRequest : BaseEntity
{
    public long UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // "Pending", "Resolved", "Rejected"
    public string? AdminNotes { get; set; }
    public string? AttachmentPath { get; set; }
    public DateTime? ResolvedOn { get; set; }
}
