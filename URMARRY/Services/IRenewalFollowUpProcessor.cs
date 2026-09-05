using System;
using System.Threading;
using System.Threading.Tasks;

namespace URMARRY.Services
{
    public class RenewalFollowUpProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int EligibleCount { get; set; }
        public int AddedCount { get; set; }
        public int ReopenedCount { get; set; }
        public int SkippedActiveCount { get; set; }
        public int SkippedNotInterestedCount { get; set; }
        public int SkippedNotEligibleCount { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public interface IRenewalFollowUpProcessor
    {
        Task<RenewalFollowUpProcessResult> ProcessAutoRenewalFollowUpsAsync(CancellationToken cancellationToken = default);
    }
}
