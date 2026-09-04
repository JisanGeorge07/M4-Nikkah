using Domain.Common;

namespace Domain
{
    public class StaffComplaintDeductionConfig : BaseEntity
    {
        public long StaffId { get; set; }
        public string? DeductionMode { get; set; } // "Fixed per Level", "Manual", "% of Salary", "Warning Only"
        public decimal? FixedAmount { get; set; }
        public bool OnlyApprovedComplaintsAffectSalary { get; set; }
    }
}
