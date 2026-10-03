using Domain;
using System;
using System.Collections.Generic;

namespace URMARRY.Areas.Admin.Models
{
    public class CallBackupListViewModel
    {
        public List<CallBackupItemViewModel> Backups { get; set; } = new();
        public int TotalCallsCount { get; set; }
        public int BackupsStoredCount { get; set; }
        public int TotalCount => Backups.Count;
        public long TotalSizeBytes { get; set; }
        public string TotalSizeFormatted => FormatFileSize(TotalSizeBytes);
        public int AudioBackupsCount { get; set; }
        public int VideoBackupsCount { get; set; }

        public static string FormatFileSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dBytes = bytes;
            while (dBytes >= 1024 && i < suffixes.Length - 1)
            {
                dBytes /= 1024;
                i++;
            }
            return $"{dBytes:0.##} {suffixes[i]}";
        }
    }

    public class CallBackupItemViewModel
    {
        public long CallLogId { get; set; }
        public string? FileName { get; set; }
        public UserCallType CallType { get; set; }
        public CallLogStatus Status { get; set; }
        public string? RoomId { get; set; }

        // Caller Details
        public long CallerId { get; set; }
        public string CallerName { get; set; } = string.Empty;
        public string? CallerRegisterNumber { get; set; }
        public string? CallerPhone { get; set; }
        public string? CallerPhotoUrl { get; set; }

        // Receiver Details
        public long ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ReceiverRegisterNumber { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverPhotoUrl { get; set; }

        // File & Duration Details
        public long FileSizeBytes { get; set; }
        public string FileSizeFormatted => CallBackupListViewModel.FormatFileSize(FileSizeBytes);
        public string? RecordingUrl { get; set; }
        public string? RecordingFilePath { get; set; }
        public bool FileExistsOnDisk { get; set; }

        // Timestamps
        public DateTime StartedAt { get; set; }
        public DateTime? ConnectedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSeconds { get; set; }
        public string DurationFormatted
        {
            get
            {
                var ts = TimeSpan.FromSeconds(DurationSeconds);
                return ts.Hours > 0 
                    ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                    : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
        }

        public DateTime? ExpiresAt { get; set; }
        public string TimeRemaining
        {
            get
            {
                if (!ExpiresAt.HasValue) return "N/A";
                var remaining = ExpiresAt.Value - DateTime.UtcNow;
                if (remaining.TotalSeconds <= 0) return "Expired (Scheduled for deletion)";
                if (remaining.TotalDays >= 1) return $"{(int)remaining.TotalDays}d {remaining.Hours}h remaining";
                if (remaining.TotalHours >= 1) return $"{(int)remaining.TotalHours}h {remaining.Minutes}m remaining";
                return $"{(int)remaining.TotalMinutes}m remaining";
            }
        }

        public string? EndReason { get; set; }
    }
}
