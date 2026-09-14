using Domain.Common;
using System;

namespace Domain
{
    public class StaffPayroll : BaseEntity
    {
        public long StaffId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        // Salary components
        public decimal BasicSalary { get; set; }
        public decimal PremiumIncentiveBoys { get; set; }
        public decimal PremiumIncentiveGirls { get; set; }
        public decimal VerificationIncentive { get; set; }
        public decimal DailyTargetIncentive { get; set; }
        public decimal AdminIncentive { get; set; }
        public string? AdminIncentiveRemarks { get; set; }
        public ICollection<StaffPayrollAdminIncentiveItem> AdminIncentiveItems { get; set; } = new List<StaffPayrollAdminIncentiveItem>();
        public decimal TotalIncentive { get; set; }

        // Deductions
        public decimal LeaveDeduction { get; set; }
        public decimal ComplaintDeduction { get; set; }
        public decimal DeletedProfileDeduction { get; set; }
        public decimal TotalDeduction { get; set; }

        // Payroll totals
        public decimal EstimatedPayroll { get; set; }
        public decimal ApprovedPayroll { get; set; }

        // Status: Draft / UnderReview / Approved / Paid / Locked / Reopened
        public string Status { get; set; } = "Draft";
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? LockedBy { get; set; }
        public DateTime? LockedDate { get; set; }
        public string? Remarks { get; set; }
    }
}
