using System;
using Application.Models.Common;
using Domain;

namespace Application.Models.Call
{
    public class CallReportDto : BaseDto
    {
        public long ReporterUserId { get; set; }
        public string? ReporterName { get; set; }
        public string? ReporterRegisterNumber { get; set; }
        public string? ReporterPhotoUrl { get; set; }
        public string? ReporterPhone { get; set; }

        public long ReportedUserId { get; set; }
        public string? ReportedUserName { get; set; }
        public string? ReportedUserRegisterNumber { get; set; }
        public string? ReportedUserPhotoUrl { get; set; }
        public string? ReportedUserPhone { get; set; }

        public long? CallLogId { get; set; }
        public UserCallType? CallType { get; set; }
        public int? CallDurationSeconds { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Comments { get; set; }

        public bool HasRecording { get; set; }
        public string? RecordingUrl { get; set; }
        public string? RecordingFilePath { get; set; }

        public UserReportStatus Status { get; set; } = UserReportStatus.Pending;
        public string? ModeratorNotes { get; set; }
        public string? ActionedBy { get; set; }
        public string? ActionedByUsername { get; set; }
        public DateTime? ActionedOn { get; set; }
    }

    public class CreateCallReportRequest
    {
        public long ReportedUserId { get; set; }
        public long? CallLogId { get; set; }
        public UserCallType? CallType { get; set; }
        public int? CallDurationSeconds { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }
}
