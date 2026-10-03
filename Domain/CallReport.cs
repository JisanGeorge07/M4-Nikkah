using System;
using Domain.Common;

namespace Domain;

public class CallReport : BaseEntity
{
    public long ReporterUserId { get; set; }
    public long ReportedUserId { get; set; }
    public long? CallLogId { get; set; }
    public UserCallType? CallType { get; set; }
    public int? CallDurationSeconds { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }

    public UserReportStatus Status { get; set; } = UserReportStatus.Pending;
    public string? ModeratorNotes { get; set; }
    public string? ActionedBy { get; set; }
    public DateTime? ActionedOn { get; set; }
}
