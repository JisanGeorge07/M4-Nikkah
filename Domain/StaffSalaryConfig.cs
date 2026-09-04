using Domain.Common;
using System;

namespace Domain
{
    public class StaffSalaryConfig : BaseEntity
    {
        public long StaffId { get; set; }
        public decimal BasicMonthlySalary { get; set; }
        public DateTime? SalaryEffectiveDate { get; set; }
        public DateTime? IncentiveEffectiveDate { get; set; }
        public string? SalaryCalculationType { get; set; } // e.g. "Monthly Fixed", "Daily Wage", "Pro-rata"
        public int MonthlyWorkingDays { get; set; } = 26;
        public string? PerDayDeductionMethod { get; set; } // e.g. "Fixed 26 Days", "Calendar Days"
        public decimal PerDaySalary { get; set; }
        public bool IncentiveEligibility { get; set; }
        public bool VerificationEligibility { get; set; }
        public bool DailyIncentiveEligibility { get; set; }
        public bool ComplaintDeductionEligibility { get; set; }
    }
}
