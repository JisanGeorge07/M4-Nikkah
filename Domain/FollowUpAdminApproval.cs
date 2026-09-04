using System;
using Domain.Common;

namespace Domain
{
    public class FollowUpAdminApproval : BaseEntity
    {
        public long FollowUpId { get; set; }
        public AdminApprovalStatus Status { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ActionDate { get; set; }
        public string? ActionBy { get; set; }

        // Navigation properties
        public virtual FollowUp? FollowUp { get; set; }
    }
}
