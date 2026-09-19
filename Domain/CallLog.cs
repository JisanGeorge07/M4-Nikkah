using Domain.Common;
using System;

namespace Domain
{
    public class CallLog : BaseEntity
    {
        public long CallerId { get; set; }
        public long ReceiverId { get; set; }
        public UserCallType CallType { get; set; } = UserCallType.Voice;
        public CallLogStatus Status { get; set; } = CallLogStatus.Ringing;

        // Unique WebRTC session room identifier
        public string RoomId { get; set; } = string.Empty;

        // Timestamps
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConnectedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSeconds { get; set; } = 0;

        // Reason call terminated (e.g. NormalHangup, ReceiverRejected, MissedTimeout, NetworkFailed)
        public string? EndReason { get; set; }

        // 14-Day Security Call Recording
        public string? RecordingS3Key { get; set; }
        public string? RecordingUrl { get; set; }
        public long? RecordingFileSize { get; set; }
        public DateTime? RecordingExpiresAt { get; set; }

        // Participant soft delete flags
        public bool IsDeletedForCaller { get; set; } = false;
        public bool IsDeletedForReceiver { get; set; } = false;
    }
}
