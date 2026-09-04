using Domain.Common;
namespace Domain;

public class UserReport: BaseEntity
{
    public long ReporterUserId { get; set; }
    public long ReportedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }

    public UserReportStatus Status { get; set; } = UserReportStatus.Pending;
    public string? ModeratorNotes { get; set; }
    public string? ActionedBy { get; set; }
    public DateTime? ActionedOn { get; set; }
}
