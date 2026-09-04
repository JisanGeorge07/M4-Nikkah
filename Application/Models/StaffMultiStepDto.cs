using System;
using System.Collections.Generic;

namespace Application.Models
{
    public class StaffSalaryConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public decimal BasicMonthlySalary { get; set; }
        public DateTime? SalaryEffectiveDate { get; set; }
        public DateTime? IncentiveEffectiveDate { get; set; }
        public decimal PerDaySalary { get; set; }
        public bool IncentiveEligibility { get; set; }
        public string? SalaryCalculationType { get; set; }
        public int MonthlyWorkingDays { get; set; } = 26;
        public string? PerDayDeductionMethod { get; set; }
        public bool VerificationEligibility { get; set; }
        public bool DailyIncentiveEligibility { get; set; }
        public bool ComplaintDeductionEligibility { get; set; }
    }

    public class StaffIncentiveConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }

        // 1. Male Profile Verification
        public string MaleVerificationType { get; set; } = "TargetBasis"; // "TargetBasis" or "ProfileBasis"
        public int? MaleVerificationTarget { get; set; }
        public decimal MaleVerificationAmount { get; set; }

        // 2. Female Profile Verification
        public string FemaleVerificationType { get; set; } = "TargetBasis"; // "TargetBasis" or "ProfileBasis"
        public int? FemaleVerificationTarget { get; set; }
        public decimal FemaleVerificationAmount { get; set; }

        // 3. Male Premium Conversion
        public string MaleConversionType { get; set; } = "TargetBasis"; // "TargetBasis" or "ProfileBasis"
        public int? MaleConversionTarget { get; set; }
        public decimal MaleConversionAmount { get; set; }

        // 4. Female Premium Conversion
        public string FemaleConversionType { get; set; } = "TargetBasis"; // "TargetBasis" or "ProfileBasis"
        public int? FemaleConversionTarget { get; set; }
        public decimal FemaleConversionAmount { get; set; }
    }

    public class StaffPerformanceTargetConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }

        // 1. Male Profile Verification Targets
        public int MaleVerificationMonthlyTarget { get; set; }
        public int MaleVerificationDailyTarget { get; set; }

        // 2. Female Profile Verification Targets
        public int FemaleVerificationMonthlyTarget { get; set; }
        public int FemaleVerificationDailyTarget { get; set; }

        // 3. Male Premium Conversion Targets
        public int MaleConversionMonthlyTarget { get; set; }
        public int MaleConversionDailyTarget { get; set; }

        // 4. Female Premium Conversion Targets
        public int FemaleConversionMonthlyTarget { get; set; }
        public int FemaleConversionDailyTarget { get; set; }
    }

    // Keep legacy DTOs below for backwards compatibility with any existing reference if needed
    public class StaffBoysIncentiveConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public string? IncentiveType { get; set; }
        public decimal IncentiveValue { get; set; }
        public List<long> SelectedPackageIds { get; set; } = new List<long>();
        public decimal MinimumPremiumAmount { get; set; }
        public decimal? MaximumIncentiveLimit { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }

    public class StaffGirlsIncentiveConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public string? IncentiveType { get; set; }
        public decimal IncentiveValue { get; set; }
        public List<long> SelectedPackageIds { get; set; } = new List<long>();
        public decimal MinimumPremiumAmount { get; set; }
        public decimal? MaximumIncentiveLimit { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }

    public class StaffDailyTargetConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public string? TargetBasis { get; set; }
        public string? SlabType { get; set; }
        public List<StaffTargetSlabDto> Slabs { get; set; } = new List<StaffTargetSlabDto>();
    }

    public class StaffTargetSlabDto
    {
        public long Id { get; set; }
        public long StaffDailyTargetConfigId { get; set; }
        public decimal MinThreshold { get; set; }
        public decimal? MaxThreshold { get; set; }
        public decimal IncentiveAmount { get; set; }
    }

    public class StaffVerificationIncentiveConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public bool VerificationIncentiveEligible { get; set; }
        public string? IncentiveType { get; set; }
        public decimal? GradeAIncentiveValue { get; set; }
        public decimal? GradeBIncentiveValue { get; set; }
        public decimal? GradeCIncentiveValue { get; set; }
        public decimal? GradeDIncentiveValue { get; set; }
        public int? MonthlyVerificationTarget { get; set; }
        public decimal? MaximumMonthlyVerificationIncentive { get; set; }
        public bool VerificationApprovalRequired { get; set; }
    }

    public class StaffLeaveDeductionConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public int PaidLeaveLimit { get; set; }
        public string? UnpaidLeaveDeductionRule { get; set; }
        public decimal HalfDayDeduction { get; set; }
        public decimal AbsentDayDeduction { get; set; }
        public string? LateAttendanceDeduction { get; set; }
        public string? LeaveDeductionFormula { get; set; }
        public bool ManualDeductionPermission { get; set; }
    }

    public class StaffComplaintDeductionConfigDto
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public string? DeductionMode { get; set; }
        public decimal? FixedAmount { get; set; }
        public bool OnlyApprovedComplaintsAffectSalary { get; set; }
    }

    public class StaffFullRegistrationVm
    {
        public StaffDto BasicDetails { get; set; } = new StaffDto();
        public StaffSalaryConfigDto SalaryConfig { get; set; } = new StaffSalaryConfigDto();
        public StaffIncentiveConfigDto IncentiveConfig { get; set; } = new StaffIncentiveConfigDto();
        public StaffPerformanceTargetConfigDto PerformanceTargetConfig { get; set; } = new StaffPerformanceTargetConfigDto();

        // Legacy properties retained for safety
        public StaffBoysIncentiveConfigDto BoysIncentive { get; set; } = new StaffBoysIncentiveConfigDto();
        public StaffGirlsIncentiveConfigDto GirlsIncentive { get; set; } = new StaffGirlsIncentiveConfigDto();
        public StaffDailyTargetConfigDto DailyTarget { get; set; } = new StaffDailyTargetConfigDto();
        public StaffVerificationIncentiveConfigDto VerificationIncentive { get; set; } = new StaffVerificationIncentiveConfigDto();
        public StaffLeaveDeductionConfigDto LeaveDeduction { get; set; } = new StaffLeaveDeductionConfigDto();
        public StaffComplaintDeductionConfigDto ComplaintDeduction { get; set; } = new StaffComplaintDeductionConfigDto();

        public List<Domain.PremiumPackage> AvailablePackages { get; set; } = new List<Domain.PremiumPackage>();
        public int ActiveStep { get; set; } = 1;
    }
}
