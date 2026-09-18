using System;
using System.Collections.Generic;

namespace URMARRY.Areas.Admin.Models
{
    // ─── Main ViewModel ───────────────────────────────────────────
    public class CrossStaffComparisonViewModel
    {
        // Filter state
        public string Period { get; set; } = "Month"; // Day, Week, Month, Year
        public string? Department { get; set; }
        public string? Gender { get; set; }
        public long? PremiumPackageId { get; set; }
        public string? VerificationGrade { get; set; }
        public string? SalaryStatus { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int FilterYear { get; set; } = DateTime.UtcNow.Year;
        public int FilterMonth { get; set; } = DateTime.UtcNow.Month;

        // Staff Comparison Table
        public List<StaffComparisonRow> StaffRows { get; set; } = new();

        // Org-Wide Aggregates
        public OrgAggregatesModel OrgAggregates { get; set; } = new();

        // Rankings
        public List<StaffRankingRow> TopPerformers { get; set; } = new();
        public List<StaffRankingRow> LowPerformers { get; set; } = new();

        // Payroll Control
        public List<StaffPayrollRow> PayrollRows { get; set; } = new();

        // Filter Options
        public List<string> Departments { get; set; } = new();
        public List<PremiumPackageOption> PremiumPackages { get; set; } = new();
    }

    // ─── Staff Comparison Row ──────────────────────────────────────
    public class StaffComparisonRow
    {
        public long StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool IsActive { get; set; }

        // Premium Conversions
        public int PremiumConversionsBoys { get; set; }
        public int PremiumConversionsGirls { get; set; }
        public int TotalPremiumConversions => PremiumConversionsBoys + PremiumConversionsGirls;

        // Premium Collection
        public decimal PremiumCollectionBoys { get; set; }
        public decimal PremiumCollectionGirls { get; set; }
        public decimal TotalPremiumCollection => PremiumCollectionBoys + PremiumCollectionGirls;

        // Daily Target
        public decimal DailyTargetAchievementPercent { get; set; }

        // Verification Count (grade-wise)
        public int VerificationGradeA { get; set; }
        public int VerificationGradeB { get; set; }
        public int VerificationGradeC { get; set; }
        public int VerificationGradeD { get; set; }
        public int TotalVerifications => VerificationGradeA + VerificationGradeB + VerificationGradeC + VerificationGradeD;

        // Incentive
        public decimal PremiumIncentiveBoys { get; set; }
        public decimal PremiumIncentiveGirls { get; set; }
        public decimal VerificationIncentive { get; set; }
        public decimal DailyTargetIncentive { get; set; }
        public decimal IncentivePayable { get; set; }

        // Deductions
        public decimal LeaveDeductions { get; set; }
        public decimal ComplaintDeductions { get; set; }
        public decimal DeletedProfileDeductions { get; set; }
        public List<DeletedProfileDeductionItem> DeletedProfileItems { get; set; } = new();

        // Composite score for ranking
        public decimal PerformanceScore { get; set; }
    }

    // ─── Org-Wide Aggregates ───────────────────────────────────────
    public class OrgAggregatesModel
    {
        public decimal TotalPremiumCollectionBoys { get; set; }
        public decimal TotalPremiumCollectionGirls { get; set; }
        public decimal TotalPremiumCollection => TotalPremiumCollectionBoys + TotalPremiumCollectionGirls;
        public decimal TotalIncentivePayable { get; set; }
        public decimal TotalEstimatedPayroll { get; set; }
        public decimal TotalApprovedPayroll { get; set; }

        // Grade-wise verification org-wide
        public int OrgVerificationGradeA { get; set; }
        public int OrgVerificationGradeB { get; set; }
        public int OrgVerificationGradeC { get; set; }
        public int OrgVerificationGradeD { get; set; }
        public int OrgTotalVerifications => OrgVerificationGradeA + OrgVerificationGradeB + OrgVerificationGradeC + OrgVerificationGradeD;

        public decimal DailyTargetAchievementRate { get; set; }
    }

    // ─── Rankings ──────────────────────────────────────────────────
    public class StaffRankingRow
    {
        public int Rank { get; set; }
        public long StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public decimal PerformanceScore { get; set; }
        public int TotalConversions { get; set; }
        public decimal TotalCollection { get; set; }
        public int TotalVerifications { get; set; }
        public decimal DailyTargetPercent { get; set; }
    }

    // ─── Payroll Row ───────────────────────────────────────────────
    public class StaffPayrollRow
    {
        public long PayrollId { get; set; }
        public long StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal AdminIncentive { get; set; }
        public string? AdminIncentiveRemarks { get; set; }
        public List<AdminIncentiveItemRow> AdminIncentiveItems { get; set; } = new();
        public decimal TotalIncentive { get; set; }
        public decimal LeaveDeduction { get; set; }
        public decimal ComplaintDeduction { get; set; }
        public decimal DeletedProfileDeduction { get; set; }
        public List<DeletedProfileDeductionItem> DeletedProfileItems { get; set; } = new();
        public decimal TotalDeduction { get; set; }
        public decimal EstimatedPayroll { get; set; }
        public decimal ApprovedPayroll { get; set; }
        public string Status { get; set; } = "Draft";
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string StatusBadgeClass => Status switch
        {
            "Draft" => "badge-soft-secondary",
            "UnderReview" => "badge-soft-warning",
            "Approved" => "badge-soft-success",
            "Paid" => "badge-soft-primary",
            "Locked" => "badge-soft-dark",
            "Reopened" => "badge-soft-danger",
            _ => "badge-soft-secondary"
        };
    }

    // ─── Filter Option Helpers ─────────────────────────────────────
    public class PremiumPackageOption
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // ─── Payroll Audit Log Row ─────────────────────────────────────
    public class PayrollAuditLogRow
    {
        public long Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public decimal? PreviousAmount { get; set; }
        public decimal? NewAmount { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime PerformedOn { get; set; }
    }

    // ─── Update Admin Incentive Model ──────────────────────────
    public class UpdateAdminIncentiveModel
    {
        public long PayrollId { get; set; }
        public decimal Amount { get; set; }
        public string? Label { get; set; }
    }

    // ─── Delete Admin Incentive Item Model ─────────────────────
    public class DeleteAdminIncentiveItemModel
    {
        public long ItemId { get; set; }
        public long PayrollId { get; set; }
    }

    // ─── Admin Incentive Item Row ──────────────────────────────
    public class AdminIncentiveItemRow
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public string? Label { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    // ─── Deleted Profile Deduction Item ────────────────────────
    public class DeletedProfileDeductionItem
    {
        public long ProfileId { get; set; }
        public string RegisterNumber { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string VerificationType { get; set; } = string.Empty;
        public DateTime? VerifiedDate { get; set; }
        public string DeletionStatus { get; set; } = string.Empty;
        public decimal DeductionAmount { get; set; }
    }
}
