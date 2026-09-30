using Domain.Common;
using System;

namespace Domain
{
    /// <summary>
    /// Tracks unique audio call contacts per user.
    /// Multiple calls to the same profile consume only 1 credit.
    /// </summary>
    public class AudioCallContact : BaseEntity
    {
        public long UserId { get; set; }
        public long ContactUserId { get; set; }
        public long PlanPurchaseId { get; set; }
        public DateTime FirstCalledAt { get; set; } = DateTime.UtcNow;
    }
}
