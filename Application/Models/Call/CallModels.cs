using Domain;
using System;
using System.Collections.Generic;

namespace Application.Models.Call
{
    public class TurnServerSettings
    {
        public string StunUrl { get; set; } = "stun:stun.l.google.com:19302";
        public string TurnUdpUrl { get; set; } = "turn:YOUR_AWS_ELASTIC_IP:3478?transport=udp";
        public string TurnTcpUrl { get; set; } = "turn:YOUR_AWS_ELASTIC_IP:3478?transport=tcp";
        public string Secret { get; set; } = "YOUR_SUPER_SECRET_KEY";
        public string Realm { get; set; } = "turn.m4nikah.com";
        public int TtlHours { get; set; } = 2;
    }

    public class CallBackupSettings
    {
        public string BackupFolder { get; set; } = "Uploads/backups";
        public int RetentionDays { get; set; } = 3;
        public int CleanupIntervalHours { get; set; } = 1;
        public int MaxFileSizeMB { get; set; } = 100;
    }

    public class IceServerDto
    {
        public object Urls { get; set; } = null!;
        public string? Username { get; set; }
        public string? Credential { get; set; }
    }

    public class IceServerConfigResponse
    {
        public List<IceServerDto> IceServers { get; set; } = new();
    }

    public class CallLogDto
    {
        public long Id { get; set; }

        public long CallerId { get; set; }
        public string CallerName { get; set; } = string.Empty;
        public string? CallerRegisterNumber { get; set; }
        public string? CallerPhotoUrl { get; set; }
        public string? CallerGender { get; set; }

        public long ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ReceiverRegisterNumber { get; set; }
        public string? ReceiverPhotoUrl { get; set; }
        public string? ReceiverGender { get; set; }

        public UserCallType CallType { get; set; }
        public CallLogStatus Status { get; set; }
        public string RoomId { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }
        public DateTime? ConnectedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSeconds { get; set; }
        public string? EndReason { get; set; }

        public bool HasRecording { get; set; }
        public string? RecordingUrl { get; set; }
        public DateTime? RecordingExpiresAt { get; set; }
        public bool IsIncoming { get; set; }
    }

    public class InitiateCallRequest
    {
        public long ReceiverId { get; set; }
        public UserCallType CallType { get; set; } = UserCallType.Voice;
        public string? RoomId { get; set; }
    }

    public class AnswerCallRequest
    {
        public long CallLogId { get; set; }
        public string? RoomId { get; set; }
    }

    public class RejectCallRequest
    {
        public long CallLogId { get; set; }
        public string? Reason { get; set; }
    }

    public class EndCallRequest
    {
        public long CallLogId { get; set; }
        public string? Reason { get; set; }
    }

    public class CallCreditStatus
    {
        public short AudioCallContactsPurchased { get; set; }
        public short AudioCallContactsUsed { get; set; }
        public int AudioCallContactsRemaining => Math.Max(0, AudioCallContactsPurchased - AudioCallContactsUsed);

        public short VideoCallMinutesPurchased { get; set; }
        public short VideoCallMinutesUsed { get; set; }
        public int VideoCallMinutesRemaining => Math.Max(0, VideoCallMinutesPurchased - VideoCallMinutesUsed);

        public bool HasActivePlan { get; set; }
        public DateTime? PlanExpiresAt { get; set; }
    }

    public class CallPermissionResult
    {
        public bool Allowed { get; set; }
        public string? Reason { get; set; }
        public bool RequiresUpgrade { get; set; }
        public bool InterestRequired { get; set; }
        public bool AudioLimitReached { get; set; }
        public bool VideoLimitReached { get; set; }
        public CallCreditStatus? CreditStatus { get; set; }
    }

    public class CallRecordingUploadResponse
    {
        public bool Success { get; set; }
        public string? RecordingUrl { get; set; }
        public string? FilePath { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Message { get; set; }
    }

    public class PaginatedCallLogsDto
    {
        public List<CallLogDto> Calls { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }
}
