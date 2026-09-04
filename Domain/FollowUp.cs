using System;
using System.Collections.Generic;
using Domain.Common;

namespace Domain
{
    public class FollowUp : BaseEntity
    {
        public long ProfileId { get; set; }
        public FollowUpType FollowUpType { get; set; }

        public ContactType? LatestContactType { get; set; }
        public CallStatus? LatestCallStatus { get; set; }
        public PremiumInterestStatus? LatestInterestStatus { get; set; }
        public ProfileVerificationStatus? LatestProfileVerificationStatus { get; set; }
        public RenewalInterestStatus? LatestRenewalInterestStatus { get; set; }
        public string? LatestRemarks { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public long? AssignedStaffId { get; set; }
        public AdminApprovalStatus? LatestAdminApprovalStatus { get; set; }

        // Payment handling fields (set by staff when status reaches Converted/Renewed)
        public string? PaymentMode { get; set; } // "Online" or "Offline"
        public OfflinePaymentMethod? OfflinePaymentType { get; set; }
        public string? TransactionId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public bool PaymentLinkSent { get; set; } = false;
        public DateTime? PaymentLinkSentAt { get; set; }
        public bool PaymentCompleted { get; set; } = false;

        // Navigation properties
        public virtual Registration? Profile { get; set; }
        public virtual ICollection<FollowUpTimeline> Timelines { get; set; } = new List<FollowUpTimeline>();
        public virtual ICollection<FollowUpAdminApproval> AdminApprovals { get; set; } = new List<FollowUpAdminApproval>();
    }
}
