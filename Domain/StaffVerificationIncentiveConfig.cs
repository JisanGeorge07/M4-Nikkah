using Domain.Common;

namespace Domain
{
    public class StaffVerificationIncentiveConfig : BaseEntity
    {
        public long StaffId { get; set; }
        public bool VerificationIncentiveEligible { get; set; }
        public string? IncentiveType { get; set; } // "Fixed per Profile", "Grade-Based"
        public decimal? GradeAIncentiveValue { get; set; }
        public decimal? GradeBIncentiveValue { get; set; }
        public decimal? GradeCIncentiveValue { get; set; }
        public decimal? GradeDIncentiveValue { get; set; }
        public int? MonthlyVerificationTarget { get; set; }
        public decimal? MaximumMonthlyVerificationIncentive { get; set; }
        public bool VerificationApprovalRequired { get; set; }
    }
}
