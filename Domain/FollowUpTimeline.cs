using System;
using Domain.Common;

namespace Domain
{
    public class FollowUpTimeline : BaseEntity
    {
        public long FollowUpId { get; set; }
        public long StaffId { get; set; }
        public string? StaffName { get; set; }
        public ContactType? ContactType { get; set; }
        public CallStatus? CallStatus { get; set; }
        public PremiumInterestStatus? InterestStatus { get; set; }
        public ProfileVerificationStatus? ProfileVerificationStatus { get; set; }
        public RenewalInterestStatus? RenewalInterestStatus { get; set; }
        public string? Remarks { get; set; }
        public DateTime? NextFollowUpDate { get; set; }

        // Navigation property
        public virtual FollowUp? FollowUp { get; set; }
    }
}
