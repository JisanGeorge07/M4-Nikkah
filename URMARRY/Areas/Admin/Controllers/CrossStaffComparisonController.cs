using Application.Interfaces.Persistence;
using Application.Models.Transactions;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using URMARRY.Areas.Admin.Models;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class CrossStaffComparisonController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Persistence.AppDbContext _dbContext;

        public CrossStaffComparisonController(
            UserManager<ApplicationUser> userManager,
            Persistence.AppDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        // ─── Main Page ────────────────────────────────────────────────
        [HttpGet("/admin/staff/cross-comparison")]
        public async Task<IActionResult> Index(
            string period = "Month",
            string? department = null,
            string? gender = null,
            long? premiumPackageId = null,
            string? verificationGrade = null,
            string? salaryStatus = null,
            int? year = null,
            int? month = null)
        {
            int filterYear = year ?? DateTime.UtcNow.Year;
            int filterMonth = month ?? DateTime.UtcNow.Month;

            // Compute date range based on period
            DateTime dateFrom, dateTo;
            ComputeDateRange(period, filterYear, filterMonth, out dateFrom, out dateTo);

            var model = new CrossStaffComparisonViewModel
            {
                Period = period,
                Department = department,
                Gender = gender,
                PremiumPackageId = premiumPackageId,
                VerificationGrade = verificationGrade,
                SalaryStatus = salaryStatus,
                FilterYear = filterYear,
                FilterMonth = filterMonth,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            // ── Load filter options ────────────────────────────────
            var staffDetails = await _dbContext.StaffDetails
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            model.Departments = staffDetails
                .Where(s => !string.IsNullOrEmpty(s.Department))
                .Select(s => s.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            model.PremiumPackages = await _dbContext.PremiumPackages
                .Where(p => !p.IsDeleted)
                .Select(p => new PremiumPackageOption { Id = p.Id, Name = p.PackageName })
                .ToListAsync();

            // ── Load staff list ────────────────────────────────────
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staffUserIds = staffUsers.Select(u => u.Id).ToList();
            var staffDetailsMap = staffDetails.ToDictionary(s => s.UserId);

            // Apply department filter
            if (!string.IsNullOrEmpty(department))
            {
                staffUsers = staffUsers
                    .Where(u => staffDetailsMap.ContainsKey(u.Id) && staffDetailsMap[u.Id].Department == department)
                    .ToList();
                staffUserIds = staffUsers.Select(u => u.Id).ToList();
            }

            // ── Load all required data ─────────────────────────────
            var assignments = await _dbContext.StaffProfileAssignments
                .Where(x => !x.IsDeleted)
                .Include(x => x.Profile)
                .ToListAsync();

            var followUps = await _dbContext.FollowUps
                .Where(x => !x.IsDeleted)
                .Include(x => x.Profile)
                .ToListAsync();

            var followUpTimelines = await _dbContext.FollowUpTimelines
                .Where(x => !x.IsDeleted && x.CreatedOn >= dateFrom && x.CreatedOn <= dateTo)
                .ToListAsync();

            var transactions = await _dbContext.Transaction
                .Where(t => t.Status == "success" && t.CreatedOn >= dateFrom && t.CreatedOn <= dateTo)
                .ToListAsync();

            var planPurchases = await _dbContext.PlanPurchases
                .Where(p => !p.IsDeleted && p.CreatedOn >= dateFrom && p.CreatedOn <= dateTo)
                .ToListAsync();

            // New Staff Incentive & Performance Target Configs
            var incentiveConfigs = await _dbContext.StaffIncentiveConfigs
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            var performanceTargetConfigs = await _dbContext.StaffPerformanceTargetConfigs
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            // Salary configs
            var salaryConfigs = await _dbContext.StaffSalaryConfigs
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            // Leave and complaint records
            var leaveRecords = await _dbContext.StaffLeaveRecords
                .Where(l => !l.IsDeleted && l.Year == filterYear && l.Month == filterMonth)
                .ToListAsync();

            var complaintRecords = await _dbContext.StaffComplaintRecords
                .Where(c => !c.IsDeleted && c.Year == filterYear && c.Month == filterMonth)
                .ToListAsync();

            // Payroll records
            var payrollRecords = await _dbContext.StaffPayrolls
                .Where(p => !p.IsDeleted && p.Year == filterYear && p.Month == filterMonth)
                .ToListAsync();

            // ── Build per-staff comparison rows ────────────────────
            foreach (var staff in staffUsers)
            {
                long staffId = staff.Id;
                staffDetailsMap.TryGetValue(staffId, out var staffDetail);

                var staffAssignments = assignments.Where(a => a.StaffId == staffId).ToList();

                // Timeline followUp IDs for this staff member in date range
                var timelineStaffFollowUpIds = followUpTimelines
                    .Where(t => t.StaffId == staffId)
                    .Select(t => t.FollowUpId)
                    .Distinct()
                    .ToHashSet();

                bool hasTimelinesInPeriod = timelineStaffFollowUpIds.Any();

                var staffFollowUps = followUps.Where(f => f.AssignedStaffId == staffId || timelineStaffFollowUpIds.Contains(f.Id)).ToList();

                // Resilient profile association: include current assignments, assigned follow-up profiles,
                // and any profiles where this staff logged timeline activity during the selected period.
                var timelineStaffProfileIds = followUpTimelines
                    .Where(t => t.StaffId == staffId)
                    .Select(t => followUps.FirstOrDefault(f => f.Id == t.FollowUpId)?.ProfileId ?? 0)
                    .Where(pid => pid > 0);

                var staffProfileIds = staffAssignments.Select(a => a.ProfileId)
                    .Concat(staffFollowUps.Select(f => f.ProfileId))
                    .Concat(timelineStaffProfileIds)
                    .ToHashSet();

                // ── Premium & Renewal Conversions (Boys vs Girls) ────────────
                // Filter transactions to this staff member's profiles for payment verification
                var staffTransactions = transactions.Where(t => staffProfileIds.Contains(t.userId)).ToList();

                var convertedFollowUps = staffFollowUps
                    .Where(f => 
                        (
                            timelineStaffFollowUpIds.Contains(f.Id) 
                            || (f.ModifiedOn >= dateFrom && f.ModifiedOn <= dateTo)
                            || (f.CreatedOn >= dateFrom && f.CreatedOn <= dateTo)
                        )
                        && (
                            (f.FollowUpType == FollowUpType.PremiumFollowUp && (f.LatestInterestStatus == PremiumInterestStatus.Converted || (f.Profile != null && f.Profile.IsPremiumMember)))
                            || (f.FollowUpType == FollowUpType.RenewalFollowUp && (f.LatestRenewalInterestStatus == RenewalInterestStatus.Renewed || (f.Profile != null && f.Profile.IsPremiumMember)))
                        )
                        && (
                            f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                            || f.PaymentCompleted
                            || staffTransactions.Any(t => t.userId == f.ProfileId)
                        ))
                    .ToList();

                int convBoys = 0, convGirls = 0;
                decimal collectionBoys = 0, collectionGirls = 0;

                // Each distinct converted follow-up (Premium conversion or Renewal conversion) counts towards conversions
                var validConvertedFollowUps = convertedFollowUps
                    .Where(fu => fu.Profile != null)
                    .DistinctBy(fu => fu.Id)
                    .ToList();

                foreach (var fu in validConvertedFollowUps)
                {
                    if (fu.Profile == null) continue;

                    // Apply gender filter
                    if (!string.IsNullOrEmpty(gender) && !string.Equals(fu.Profile.Gender, gender, StringComparison.OrdinalIgnoreCase)) continue;

                    bool isMale = string.Equals(fu.Profile.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                    if (isMale) convBoys++;
                    else convGirls++;
                }

                // Sum transactions per distinct ProfileId to avoid double-counting payments
                var distinctConvertedProfileIds = convertedFollowUps
                    .Where(fu => fu.Profile != null)
                    .GroupBy(fu => fu.ProfileId)
                    .Select(g => g.First())
                    .ToList();

                foreach (var fu in distinctConvertedProfileIds)
                {
                    if (!string.IsNullOrEmpty(gender) && !string.Equals(fu.Profile!.Gender, gender, StringComparison.OrdinalIgnoreCase)) continue;

                    bool isMale = string.Equals(fu.Profile!.Gender, "Male", StringComparison.OrdinalIgnoreCase);

                    var profileTransactions = transactions
                        .Where(t => t.userId == fu.ProfileId)
                        .ToList();

                    decimal profileCollection = 0;
                    foreach (var txn in profileTransactions)
                    {
                        if (decimal.TryParse(txn.Amount, out decimal amt) && amt > 0)
                        {
                            profileCollection += amt;
                        }
                    }

                    // Fallback to recorded payment amount on follow-up if approved by admin and no gateway transaction row exists
                    if (profileCollection == 0)
                    {
                        var profileFollowUps = convertedFollowUps.Where(f => f.ProfileId == fu.ProfileId);
                        foreach (var pfu in profileFollowUps)
                        {
                            if (pfu.PaymentAmount.HasValue && pfu.PaymentAmount > 0 && pfu.LatestAdminApprovalStatus == AdminApprovalStatus.Approved)
                            {
                                profileCollection += pfu.PaymentAmount.Value;
                            }
                        }
                    }

                    if (isMale) collectionBoys += profileCollection;
                    else collectionGirls += profileCollection;
                }

                // Apply premium package filter
                if (premiumPackageId.HasValue)
                {
                    var filteredPlanPurchases = planPurchases
                        .Where(p => staffProfileIds.Contains(p.UserId))
                        .ToList();
                }

                // ── Verification Count (Grade-wise) ────────────────
                var verificationFollowUps = staffFollowUps
                    .Where(f => f.FollowUpType == FollowUpType.ProfileVerification
                        && (
                            timelineStaffFollowUpIds.Contains(f.Id)
                            || (f.CreatedOn >= dateFrom && f.CreatedOn <= dateTo)
                            || (f.ModifiedOn >= dateFrom && f.ModifiedOn <= dateTo)
                        ))
                    .ToList();

                int gradeA = verificationFollowUps.Count(f => f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved);
                int gradeB = verificationFollowUps.Count(f => f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved);
                int gradeC = verificationFollowUps.Count(f => f.LatestProfileVerificationStatus == ProfileVerificationStatus.Started
                    || f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerifyRequest
                    || (f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify && f.LatestAdminApprovalStatus != AdminApprovalStatus.Approved));
                int gradeD = verificationFollowUps.Count(f => f.LatestProfileVerificationStatus == ProfileVerificationStatus.Pending
                    || f.LatestProfileVerificationStatus == ProfileVerificationStatus.Hold
                    || (f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify && f.LatestAdminApprovalStatus != AdminApprovalStatus.Approved));

                // Apply verification grade filter
                if (!string.IsNullOrEmpty(verificationGrade))
                {
                    bool hasGrade = verificationGrade switch
                    {
                        "A" => gradeA > 0,
                        "B" => gradeB > 0,
                        "C" => gradeC > 0,
                        "D" => gradeD > 0,
                        _ => true
                    };
                    if (!hasGrade) continue;
                }

                // ── Salary Config & Eligibility ────────────────────
                var staffSalaryConfig = salaryConfigs.FirstOrDefault(c => c.StaffId == staffId);
                bool isIncentiveEligible = staffSalaryConfig?.IncentiveEligibility ?? true;

                // ── Incentive Calculation ──────────────────────────
                var staffIncentiveConfig = incentiveConfigs.FirstOrDefault(c => c.StaffId == staffId);

                decimal premiumIncentiveBoys = 0;
                decimal premiumIncentiveGirls = 0;
                decimal verifIncentive = 0;

                int maleVerifications = verificationFollowUps.Count(f => f.Profile != null 
                    && (f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify || f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify)
                    && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                    && string.Equals(f.Profile.Gender, "Male", StringComparison.OrdinalIgnoreCase));

                int femaleNormalVerifs = verificationFollowUps.Count(f => f.Profile != null 
                    && f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify
                    && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                    && string.Equals(f.Profile.Gender, "Female", StringComparison.OrdinalIgnoreCase));

                int femaleDocVerifs = verificationFollowUps.Count(f => f.Profile != null 
                    && f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify
                    && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                    && string.Equals(f.Profile.Gender, "Female", StringComparison.OrdinalIgnoreCase));

                int femaleVerifications = femaleNormalVerifs + femaleDocVerifs;

                if (isIncentiveEligible && staffIncentiveConfig != null)
                {
                    // 1. Male Profile Verification Incentive
                    if (string.Equals(staffIncentiveConfig.MaleVerificationType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        verifIncentive += maleVerifications * staffIncentiveConfig.MaleVerificationAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.MaleVerificationTarget ?? 0;
                        if (maleVerifications > baseTarget)
                        {
                            verifIncentive += (maleVerifications - baseTarget) * staffIncentiveConfig.MaleVerificationAmount;
                        }
                    }

                    // 2. Female Normal Profile Verification Incentive
                    if (string.Equals(staffIncentiveConfig.FemaleVerificationType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        verifIncentive += femaleNormalVerifs * staffIncentiveConfig.FemaleVerificationAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.FemaleVerificationTarget ?? 0;
                        if (femaleNormalVerifs > baseTarget)
                        {
                            verifIncentive += (femaleNormalVerifs - baseTarget) * staffIncentiveConfig.FemaleVerificationAmount;
                        }
                    }

                    // 3. Female Document Profile Verification Incentive
                    if (string.Equals(staffIncentiveConfig.FemaleDocVerificationType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        verifIncentive += femaleDocVerifs * staffIncentiveConfig.FemaleDocVerificationAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.FemaleDocVerificationTarget ?? 0;
                        if (femaleDocVerifs > baseTarget)
                        {
                            verifIncentive += (femaleDocVerifs - baseTarget) * staffIncentiveConfig.FemaleDocVerificationAmount;
                        }
                    }

                    // 4. Male Premium Conversion Incentive
                    if (string.Equals(staffIncentiveConfig.MaleConversionType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        premiumIncentiveBoys = convBoys * staffIncentiveConfig.MaleConversionAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.MaleConversionTarget ?? 0;
                        if (convBoys > baseTarget)
                        {
                            premiumIncentiveBoys = (convBoys - baseTarget) * staffIncentiveConfig.MaleConversionAmount;
                        }
                    }

                    // 5. Female Premium Conversion Incentive
                    if (string.Equals(staffIncentiveConfig.FemaleConversionType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        premiumIncentiveGirls = convGirls * staffIncentiveConfig.FemaleConversionAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.FemaleConversionTarget ?? 0;
                        if (convGirls > baseTarget)
                        {
                            premiumIncentiveGirls = (convGirls - baseTarget) * staffIncentiveConfig.FemaleConversionAmount;
                        }
                    }
                }

                // ── Daily Target Achievement & Incentive ──────────
                int totalConversions = convBoys + convGirls;
                decimal totalCollection = collectionBoys + collectionGirls;

                var perfTargetConfig = performanceTargetConfigs.FirstOrDefault(c => c.StaffId == staffId);

                decimal dailyTargetPercent = 0;
                if (perfTargetConfig != null)
                {
                    bool isDaily = string.Equals(period, "Day", StringComparison.OrdinalIgnoreCase);
                    bool isWeekly = string.Equals(period, "Week", StringComparison.OrdinalIgnoreCase);
                    bool isYearly = string.Equals(period, "Year", StringComparison.OrdinalIgnoreCase);

                    int totalTarget;
                    if (isDaily)
                    {
                        totalTarget = perfTargetConfig.MaleVerificationDailyTarget + perfTargetConfig.FemaleVerificationDailyTarget +
                                      perfTargetConfig.MaleConversionDailyTarget + perfTargetConfig.FemaleConversionDailyTarget;
                    }
                    else if (isWeekly)
                    {
                        totalTarget = (perfTargetConfig.MaleVerificationDailyTarget + perfTargetConfig.FemaleVerificationDailyTarget +
                                       perfTargetConfig.MaleConversionDailyTarget + perfTargetConfig.FemaleConversionDailyTarget) * 7;
                    }
                    else if (isYearly)
                    {
                        totalTarget = (perfTargetConfig.MaleVerificationMonthlyTarget + perfTargetConfig.FemaleVerificationMonthlyTarget +
                                       perfTargetConfig.MaleConversionMonthlyTarget + perfTargetConfig.FemaleConversionMonthlyTarget) * 12;
                    }
                    else
                    {
                        totalTarget = perfTargetConfig.MaleVerificationMonthlyTarget + perfTargetConfig.FemaleVerificationMonthlyTarget +
                                      perfTargetConfig.MaleConversionMonthlyTarget + perfTargetConfig.FemaleConversionMonthlyTarget;
                    }

                    int totalAchieved = maleVerifications + femaleVerifications + convBoys + convGirls;
                    if (totalTarget > 0)
                    {
                        dailyTargetPercent = Math.Min(100, Math.Round((decimal)totalAchieved / totalTarget * 100, 1));
                    }
                }

                decimal totalIncentive = premiumIncentiveBoys + premiumIncentiveGirls + verifIncentive;

                // ── Leave Deductions (Admin-Approved Unpaid Leaves) ────────
                var staffLeaves = leaveRecords.Where(l => l.StaffId == staffId && l.IsApproved).ToList();
                decimal perDaySalary = staffSalaryConfig?.PerDaySalary > 0 
                    ? staffSalaryConfig.PerDaySalary 
                    : (staffSalaryConfig != null && staffSalaryConfig.BasicMonthlySalary > 0 
                        ? Math.Round(staffSalaryConfig.BasicMonthlySalary / DateTime.DaysInMonth(filterYear, filterMonth), 2) 
                        : 0);

                decimal leaveDeduction = 0;
                foreach (var leave in staffLeaves)
                {
                    if (!leave.IsPaid)
                    {
                        leaveDeduction += leave.LeaveType switch
                        {
                            "HalfDay" => Math.Round(perDaySalary / 2, 2),
                            "FullDay" => perDaySalary,
                            "Late" => Math.Round(perDaySalary / 4, 2),
                            _ => perDaySalary
                        };
                    }
                }

                // ── Complaint Deductions ──────────────────────────
                var staffComplaints = complaintRecords.Where(c => c.StaffId == staffId && c.IsApproved).ToList();
                decimal complaintDeduction = staffComplaints.Sum(c => c.DeductionAmount);

                // ── Performance Score (composite) ──────────────────
                decimal perfScore = (totalConversions * 10) + (totalCollection / 1000) +
                    (gradeA * 4 + gradeB * 3 + gradeC * 2 + gradeD) + dailyTargetPercent;

                var row = new StaffComparisonRow
                {
                    StaffId = staffId,
                    StaffName = staff.NormalizedUserName ?? staff.UserName ?? "Unknown",
                    Department = staffDetail?.Department,
                    IsActive = staff.LockoutEnd == null || staff.LockoutEnd <= DateTimeOffset.UtcNow,
                    PremiumConversionsBoys = convBoys,
                    PremiumConversionsGirls = convGirls,
                    PremiumCollectionBoys = collectionBoys,
                    PremiumCollectionGirls = collectionGirls,
                    DailyTargetAchievementPercent = dailyTargetPercent,
                    VerificationGradeA = gradeA,
                    VerificationGradeB = gradeB,
                    VerificationGradeC = gradeC,
                    VerificationGradeD = gradeD,
                    PremiumIncentiveBoys = premiumIncentiveBoys,
                    PremiumIncentiveGirls = premiumIncentiveGirls,
                    VerificationIncentive = verifIncentive,
                    DailyTargetIncentive = 0,
                    IncentivePayable = totalIncentive,
                    LeaveDeductions = leaveDeduction,
                    ComplaintDeductions = complaintDeduction,
                    PerformanceScore = perfScore
                };

                model.StaffRows.Add(row);
            }

            // ── Org-Wide Aggregates ────────────────────────────────
            model.OrgAggregates = new OrgAggregatesModel
            {
                TotalPremiumCollectionBoys = model.StaffRows.Sum(r => r.PremiumCollectionBoys),
                TotalPremiumCollectionGirls = model.StaffRows.Sum(r => r.PremiumCollectionGirls),
                TotalIncentivePayable = model.StaffRows.Sum(r => r.IncentivePayable),
                OrgVerificationGradeA = model.StaffRows.Sum(r => r.VerificationGradeA),
                OrgVerificationGradeB = model.StaffRows.Sum(r => r.VerificationGradeB),
                OrgVerificationGradeC = model.StaffRows.Sum(r => r.VerificationGradeC),
                OrgVerificationGradeD = model.StaffRows.Sum(r => r.VerificationGradeD),
                DailyTargetAchievementRate = model.StaffRows.Any()
                    ? Math.Round(model.StaffRows.Average(r => r.DailyTargetAchievementPercent), 1)
                    : 0
            };

            // Payroll totals
            model.OrgAggregates.TotalEstimatedPayroll = payrollRecords.Sum(p => p.EstimatedPayroll);
            model.OrgAggregates.TotalApprovedPayroll = payrollRecords.Sum(p => p.ApprovedPayroll);

            // ── Rankings ──────────────────────────────────────────
            var sortedByPerformance = model.StaffRows.OrderByDescending(r => r.PerformanceScore).ToList();
            int topCount = Math.Min(5, sortedByPerformance.Count);
            for (int i = 0; i < topCount; i++)
            {
                model.TopPerformers.Add(new StaffRankingRow
                {
                    Rank = i + 1,
                    StaffId = sortedByPerformance[i].StaffId,
                    StaffName = sortedByPerformance[i].StaffName,
                    Department = sortedByPerformance[i].Department,
                    PerformanceScore = sortedByPerformance[i].PerformanceScore,
                    TotalConversions = sortedByPerformance[i].TotalPremiumConversions,
                    TotalCollection = sortedByPerformance[i].TotalPremiumCollection,
                    TotalVerifications = sortedByPerformance[i].TotalVerifications,
                    DailyTargetPercent = sortedByPerformance[i].DailyTargetAchievementPercent
                });
            }

            var bottomSorted = sortedByPerformance.AsEnumerable().Reverse().ToList();
            int lowCount = Math.Min(5, bottomSorted.Count);
            for (int i = 0; i < lowCount; i++)
            {
                model.LowPerformers.Add(new StaffRankingRow
                {
                    Rank = i + 1,
                    StaffId = bottomSorted[i].StaffId,
                    StaffName = bottomSorted[i].StaffName,
                    Department = bottomSorted[i].Department,
                    PerformanceScore = bottomSorted[i].PerformanceScore,
                    TotalConversions = bottomSorted[i].TotalPremiumConversions,
                    TotalCollection = bottomSorted[i].TotalPremiumCollection,
                    TotalVerifications = bottomSorted[i].TotalVerifications,
                    DailyTargetPercent = bottomSorted[i].DailyTargetAchievementPercent
                });
            }

            // ── Payroll Control ───────────────────────────────────
            // Auto-generate draft payroll records if not exist for current month
            foreach (var staff in staffUsers)
            {
                var existingPayroll = payrollRecords.FirstOrDefault(p => p.StaffId == staff.Id);
                if (existingPayroll == null)
                {
                    var staffRow = model.StaffRows.FirstOrDefault(r => r.StaffId == staff.Id);
                    var staffSalConfig = salaryConfigs.FirstOrDefault(c => c.StaffId == staff.Id);
                    decimal basicSalary = staffSalConfig?.BasicMonthlySalary ?? 0;
                    decimal totalInc = staffRow?.IncentivePayable ?? 0;
                    decimal totalDed = (staffRow?.LeaveDeductions ?? 0) + (staffRow?.ComplaintDeductions ?? 0);
                    decimal estimated = basicSalary + totalInc - totalDed;

                    var newPayroll = new StaffPayroll
                    {
                        StaffId = staff.Id,
                        Year = filterYear,
                        Month = filterMonth,
                        BasicSalary = basicSalary,
                        PremiumIncentiveBoys = staffRow?.PremiumIncentiveBoys ?? 0,
                        PremiumIncentiveGirls = staffRow?.PremiumIncentiveGirls ?? 0,
                        VerificationIncentive = staffRow?.VerificationIncentive ?? 0,
                        DailyTargetIncentive = staffRow?.DailyTargetIncentive ?? 0,
                        AdminIncentive = 0,
                        AdminIncentiveRemarks = null,
                        TotalIncentive = totalInc,
                        LeaveDeduction = staffRow?.LeaveDeductions ?? 0,
                        ComplaintDeduction = staffRow?.ComplaintDeductions ?? 0,
                        TotalDeduction = totalDed,
                        EstimatedPayroll = estimated,
                        ApprovedPayroll = 0,
                        Status = "Draft"
                    };
                    _dbContext.StaffPayrolls.Add(newPayroll);
                    payrollRecords.Add(newPayroll);
                }
                else if (existingPayroll.Status == "Draft")
                {
                    // Auto-refresh Draft payroll with latest calculated values
                    var staffRow = model.StaffRows.FirstOrDefault(r => r.StaffId == staff.Id);
                    var staffSalConfig = salaryConfigs.FirstOrDefault(c => c.StaffId == staff.Id);
                    decimal basicSalary = staffSalConfig?.BasicMonthlySalary ?? 0;
                    decimal adminInc = existingPayroll.AdminIncentive;
                    decimal totalInc = (staffRow?.IncentivePayable ?? 0) + adminInc;
                    decimal totalDed = (staffRow?.LeaveDeductions ?? 0) + (staffRow?.ComplaintDeductions ?? 0);

                    existingPayroll.BasicSalary = basicSalary;
                    existingPayroll.PremiumIncentiveBoys = staffRow?.PremiumIncentiveBoys ?? 0;
                    existingPayroll.PremiumIncentiveGirls = staffRow?.PremiumIncentiveGirls ?? 0;
                    existingPayroll.VerificationIncentive = staffRow?.VerificationIncentive ?? 0;
                    existingPayroll.DailyTargetIncentive = staffRow?.DailyTargetIncentive ?? 0;
                    existingPayroll.AdminIncentive = adminInc;
                    existingPayroll.TotalIncentive = totalInc;
                    existingPayroll.LeaveDeduction = staffRow?.LeaveDeductions ?? 0;
                    existingPayroll.ComplaintDeduction = staffRow?.ComplaintDeductions ?? 0;
                    existingPayroll.TotalDeduction = totalDed;
                    existingPayroll.EstimatedPayroll = basicSalary + totalInc - totalDed;
                }
            }
            await _dbContext.SaveChangesAsync();

            // Apply salary status filter
            var filteredPayrolls = payrollRecords.AsEnumerable();
            if (!string.IsNullOrEmpty(salaryStatus))
            {
                filteredPayrolls = filteredPayrolls.Where(p => p.Status == salaryStatus);
            }

            // Load admin incentive items for all payrolls in one query
            var payrollIds = filteredPayrolls.Select(p => p.Id).ToList();
            var adminIncentiveItemsMap = await _dbContext.StaffPayrollAdminIncentiveItems
                .Where(i => payrollIds.Contains(i.StaffPayrollId) && !i.IsDeleted)
                .GroupBy(i => i.StaffPayrollId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            foreach (var payroll in filteredPayrolls)
            {
                var staff = staffUsers.FirstOrDefault(u => u.Id == payroll.StaffId);
                staffDetailsMap.TryGetValue(payroll.StaffId, out var sd);

                adminIncentiveItemsMap.TryGetValue(payroll.Id, out var items);

                model.PayrollRows.Add(new StaffPayrollRow
                {
                    PayrollId = payroll.Id,
                    StaffId = payroll.StaffId,
                    StaffName = staff?.NormalizedUserName ?? staff?.UserName ?? "Unknown",
                    Department = sd?.Department,
                    Year = payroll.Year,
                    Month = payroll.Month,
                    BasicSalary = payroll.BasicSalary,
                    AdminIncentive = payroll.AdminIncentive,
                    AdminIncentiveRemarks = payroll.AdminIncentiveRemarks,
                    AdminIncentiveItems = items?.Select(i => new AdminIncentiveItemRow
                    {
                        Id = i.Id,
                        Amount = i.Amount,
                        Label = i.Label,
                        CreatedOn = i.CreatedOn
                    }).OrderBy(i => i.CreatedOn).ToList() ?? new List<AdminIncentiveItemRow>(),
                    TotalIncentive = payroll.TotalIncentive,
                    TotalDeduction = payroll.TotalDeduction,
                    EstimatedPayroll = payroll.EstimatedPayroll,
                    ApprovedPayroll = payroll.ApprovedPayroll,
                    Status = payroll.Status,
                    ApprovedBy = payroll.ApprovedBy,
                    ApprovedDate = payroll.ApprovedDate
                });
            }

            return View(model);
        }

        // ─── Bulk Approve ─────────────────────────────────────────
        [HttpPost("/admin/staff/cross-comparison/payroll/bulk-approve")]
        public async Task<IActionResult> BulkApprove([FromBody] List<long> payrollIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var payrolls = await _dbContext.StaffPayrolls
                .Where(p => payrollIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            foreach (var payroll in payrolls)
            {
                if (payroll.Status == "Locked") continue; // Cannot approve locked records

                string previousStatus = payroll.Status;
                payroll.Status = "Approved";
                payroll.ApprovedBy = currentUser?.UserName ?? "Admin";
                payroll.ApprovedDate = DateTime.UtcNow;
                payroll.ApprovedPayroll = payroll.EstimatedPayroll;

                _dbContext.StaffPayrollAuditLogs.Add(new StaffPayrollAuditLog
                {
                    StaffPayrollId = payroll.Id,
                    Action = "StatusChanged",
                    PreviousStatus = previousStatus,
                    NewStatus = "Approved",
                    PerformedBy = currentUser?.UserName ?? "Admin",
                    Reason = "Bulk Approve"
                });
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = $"{payrolls.Count} payroll(s) approved." });
        }

        // ─── Bulk Lock ────────────────────────────────────────────
        [HttpPost("/admin/staff/cross-comparison/payroll/bulk-lock")]
        public async Task<IActionResult> BulkLock([FromBody] List<long> payrollIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var payrolls = await _dbContext.StaffPayrolls
                .Where(p => payrollIds.Contains(p.Id) && !p.IsDeleted
                    && (p.Status == "Approved" || p.Status == "Paid"))
                .ToListAsync();

            foreach (var payroll in payrolls)
            {
                string previousStatus = payroll.Status;
                payroll.Status = "Locked";
                payroll.LockedBy = currentUser?.UserName ?? "Admin";
                payroll.LockedDate = DateTime.UtcNow;

                _dbContext.StaffPayrollAuditLogs.Add(new StaffPayrollAuditLog
                {
                    StaffPayrollId = payroll.Id,
                    Action = "Locked",
                    PreviousStatus = previousStatus,
                    NewStatus = "Locked",
                    PerformedBy = currentUser?.UserName ?? "Admin",
                    Reason = "Bulk Lock"
                });
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = $"{payrolls.Count} payroll(s) locked." });
        }

        // ─── Reopen ───────────────────────────────────────────────
        [HttpPost("/admin/staff/cross-comparison/payroll/reopen")]
        public async Task<IActionResult> Reopen([FromBody] ReopenPayrollModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var payroll = await _dbContext.StaffPayrolls
                .FirstOrDefaultAsync(p => p.Id == model.PayrollId && !p.IsDeleted);

            if (payroll == null)
                return Json(new { success = false, message = "Payroll not found." });

            string previousStatus = payroll.Status;
            payroll.Status = "Reopened";

            _dbContext.StaffPayrollAuditLogs.Add(new StaffPayrollAuditLog
            {
                StaffPayrollId = payroll.Id,
                Action = "Reopened",
                PreviousStatus = previousStatus,
                NewStatus = "Reopened",
                PerformedBy = currentUser?.UserName ?? "Admin",
                Reason = model.Reason
            });

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = "Payroll reopened." });
        }

        // ─── Add Admin Incentive Item ──────────────────────────────
        [HttpPost("/admin/staff/cross-comparison/payroll/update-admin-incentive")]
        public async Task<IActionResult> UpdateAdminIncentive([FromBody] UpdateAdminIncentiveModel model)
        {
            if (model == null || model.PayrollId <= 0)
                return Json(new { success = false, message = "Invalid payroll record." });

            if (model.Amount <= 0)
                return Json(new { success = false, message = "Incentive amount must be greater than zero." });

            var currentUser = await _userManager.GetUserAsync(User);
            var payroll = await _dbContext.StaffPayrolls
                .FirstOrDefaultAsync(p => p.Id == model.PayrollId && !p.IsDeleted);

            if (payroll == null)
                return Json(new { success = false, message = "Payroll record not found." });

            if (payroll.Status == "Locked")
                return Json(new { success = false, message = "Cannot edit incentive for a locked payroll. Please reopen it first." });

            // Add new incentive item
            var newItem = new StaffPayrollAdminIncentiveItem
            {
                StaffPayrollId = payroll.Id,
                Amount = model.Amount,
                Label = model.Label
            };
            _dbContext.StaffPayrollAdminIncentiveItems.Add(newItem);

            // Recalculate aggregates from all active items
            var allItems = await _dbContext.StaffPayrollAdminIncentiveItems
                .Where(i => i.StaffPayrollId == payroll.Id && !i.IsDeleted)
                .ToListAsync();
            decimal totalAdminIncentive = allItems.Sum(i => i.Amount) + model.Amount;

            decimal oldAdminIncentive = payroll.AdminIncentive;
            decimal oldEstimated = payroll.EstimatedPayroll;

            payroll.AdminIncentive = totalAdminIncentive;
            payroll.AdminIncentiveRemarks = string.Join(", ",
                allItems.Where(i => !string.IsNullOrEmpty(i.Label)).Select(i => i.Label)
                .Concat(string.IsNullOrEmpty(model.Label) ? Array.Empty<string>() : new[] { model.Label }));

            // Recalculate total incentive and estimated payroll
            payroll.TotalIncentive = payroll.PremiumIncentiveBoys + payroll.PremiumIncentiveGirls +
                                    payroll.VerificationIncentive + payroll.DailyTargetIncentive +
                                    payroll.AdminIncentive;
            payroll.EstimatedPayroll = payroll.BasicSalary + payroll.TotalIncentive - payroll.TotalDeduction;

            // If already approved, update approved payroll as well
            if (payroll.Status == "Approved")
            {
                payroll.ApprovedPayroll = payroll.EstimatedPayroll;
            }

            _dbContext.StaffPayrollAuditLogs.Add(new StaffPayrollAuditLog
            {
                StaffPayrollId = payroll.Id,
                Action = "AdminIncentiveAdded",
                PreviousAmount = oldEstimated,
                NewAmount = payroll.EstimatedPayroll,
                PerformedBy = currentUser?.UserName ?? "Admin",
                Reason = $"Admin Incentive item added: ₹{model.Amount:N0} ({model.Label ?? "No label"}). Total admin incentive: ₹{totalAdminIncentive:N0}"
            });

            await _dbContext.SaveChangesAsync();

            // Fetch all items to return
            var updatedItems = await _dbContext.StaffPayrollAdminIncentiveItems
                .Where(i => i.StaffPayrollId == payroll.Id && !i.IsDeleted)
                .OrderBy(i => i.CreatedOn)
                .Select(i => new { id = i.Id, amount = i.Amount, label = i.Label ?? "", createdOn = i.CreatedOn })
                .ToListAsync();

            return Json(new
            {
                success = true,
                message = "Admin incentive added successfully.",
                adminIncentive = payroll.AdminIncentive,
                adminIncentiveRemarks = payroll.AdminIncentiveRemarks,
                totalIncentive = payroll.TotalIncentive,
                estimatedPayroll = payroll.EstimatedPayroll,
                approvedPayroll = payroll.ApprovedPayroll,
                items = updatedItems
            });
        }

        // ─── Delete Admin Incentive Item ──────────────────────────────
        [HttpPost("/admin/staff/cross-comparison/payroll/delete-admin-incentive-item")]
        public async Task<IActionResult> DeleteAdminIncentiveItem([FromBody] DeleteAdminIncentiveItemModel model)
        {
            if (model == null || model.ItemId <= 0 || model.PayrollId <= 0)
                return Json(new { success = false, message = "Invalid request." });

            var currentUser = await _userManager.GetUserAsync(User);
            var payroll = await _dbContext.StaffPayrolls
                .FirstOrDefaultAsync(p => p.Id == model.PayrollId && !p.IsDeleted);

            if (payroll == null)
                return Json(new { success = false, message = "Payroll record not found." });

            if (payroll.Status == "Locked")
                return Json(new { success = false, message = "Cannot edit incentive for a locked payroll. Please reopen it first." });

            var item = await _dbContext.StaffPayrollAdminIncentiveItems
                .FirstOrDefaultAsync(i => i.Id == model.ItemId && i.StaffPayrollId == model.PayrollId && !i.IsDeleted);

            if (item == null)
                return Json(new { success = false, message = "Incentive item not found." });

            // Soft delete the item
            item.IsDeleted = true;

            // Recalculate aggregates
            var remainingItems = await _dbContext.StaffPayrollAdminIncentiveItems
                .Where(i => i.StaffPayrollId == payroll.Id && !i.IsDeleted && i.Id != model.ItemId)
                .ToListAsync();

            decimal oldEstimated = payroll.EstimatedPayroll;
            decimal oldAdminIncentive = payroll.AdminIncentive;

            payroll.AdminIncentive = remainingItems.Sum(i => i.Amount);
            payroll.AdminIncentiveRemarks = remainingItems.Any()
                ? string.Join(", ", remainingItems.Where(i => !string.IsNullOrEmpty(i.Label)).Select(i => i.Label))
                : null;

            payroll.TotalIncentive = payroll.PremiumIncentiveBoys + payroll.PremiumIncentiveGirls +
                                    payroll.VerificationIncentive + payroll.DailyTargetIncentive +
                                    payroll.AdminIncentive;
            payroll.EstimatedPayroll = payroll.BasicSalary + payroll.TotalIncentive - payroll.TotalDeduction;

            if (payroll.Status == "Approved")
            {
                payroll.ApprovedPayroll = payroll.EstimatedPayroll;
            }

            _dbContext.StaffPayrollAuditLogs.Add(new StaffPayrollAuditLog
            {
                StaffPayrollId = payroll.Id,
                Action = "AdminIncentiveRemoved",
                PreviousAmount = oldEstimated,
                NewAmount = payroll.EstimatedPayroll,
                PerformedBy = currentUser?.UserName ?? "Admin",
                Reason = $"Admin Incentive item removed: ₹{item.Amount:N0} ({item.Label ?? "No label"}). Total admin incentive: ₹{payroll.AdminIncentive:N0}"
            });

            await _dbContext.SaveChangesAsync();

            var updatedItems = await _dbContext.StaffPayrollAdminIncentiveItems
                .Where(i => i.StaffPayrollId == payroll.Id && !i.IsDeleted)
                .OrderBy(i => i.CreatedOn)
                .Select(i => new { id = i.Id, amount = i.Amount, label = i.Label ?? "", createdOn = i.CreatedOn })
                .ToListAsync();

            return Json(new
            {
                success = true,
                message = "Incentive item removed.",
                adminIncentive = payroll.AdminIncentive,
                adminIncentiveRemarks = payroll.AdminIncentiveRemarks,
                totalIncentive = payroll.TotalIncentive,
                estimatedPayroll = payroll.EstimatedPayroll,
                approvedPayroll = payroll.ApprovedPayroll,
                items = updatedItems
            });
        }

        // ─── Audit Log ───────────────────────────────────────────
        [HttpGet("/admin/staff/cross-comparison/payroll/audit-log/{payrollId:long}")]
        public async Task<IActionResult> AuditLog(long payrollId)
        {
            var logs = await _dbContext.StaffPayrollAuditLogs
                .Where(l => l.StaffPayrollId == payrollId && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedOn)
                .Select(l => new PayrollAuditLogRow
                {
                    Id = l.Id,
                    Action = l.Action,
                    PreviousStatus = l.PreviousStatus,
                    NewStatus = l.NewStatus,
                    PreviousAmount = l.PreviousAmount,
                    NewAmount = l.NewAmount,
                    PerformedBy = l.PerformedBy,
                    Reason = l.Reason,
                    PerformedOn = l.CreatedOn
                })
                .ToListAsync();

            return Json(logs);
        }

        // ─── Export Salary Slip Data (JSON for client-side PDF) ──
        [HttpGet("/admin/staff/cross-comparison/export/salary-slip/{payrollId:long}")]
        public async Task<IActionResult> ExportSalarySlip(long payrollId)
        {
            var payroll = await _dbContext.StaffPayrolls
                .FirstOrDefaultAsync(p => p.Id == payrollId && !p.IsDeleted);

            if (payroll == null)
                return NotFound();

            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staff = staffUsers.FirstOrDefault(u => u.Id == payroll.StaffId);
            var staffDetail = await _dbContext.StaffDetails
                .FirstOrDefaultAsync(s => s.UserId == payroll.StaffId && !s.IsDeleted);

            // Load admin incentive items
            var adminIncentiveItems = await _dbContext.StaffPayrollAdminIncentiveItems
                .Where(i => i.StaffPayrollId == payroll.Id && !i.IsDeleted)
                .OrderBy(i => i.CreatedOn)
                .ToListAsync();

            var slipData = new
            {
                staffName = staff?.NormalizedUserName ?? staff?.UserName ?? "Unknown",
                department = staffDetail?.Department ?? "N/A",
                designation = staffDetail?.Designation ?? "N/A",
                month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.Month),
                year = payroll.Year,
                basicSalary = payroll.BasicSalary,
                premiumIncentiveBoys = payroll.PremiumIncentiveBoys,
                premiumIncentiveGirls = payroll.PremiumIncentiveGirls,
                verificationIncentive = payroll.VerificationIncentive,
                dailyTargetIncentive = payroll.DailyTargetIncentive,
                adminIncentive = payroll.AdminIncentive,
                adminIncentiveRemarks = payroll.AdminIncentiveRemarks ?? "",
                adminIncentiveItems = adminIncentiveItems.Select(i => new { id = i.Id, amount = i.Amount, label = i.Label ?? "" }).ToList(),
                totalIncentive = payroll.TotalIncentive,
                leaveDeduction = payroll.LeaveDeduction,
                complaintDeduction = payroll.ComplaintDeduction,
                totalDeduction = payroll.TotalDeduction,
                estimatedPayroll = payroll.EstimatedPayroll,
                approvedPayroll = payroll.ApprovedPayroll,
                netPayable = payroll.ApprovedPayroll > 0 ? payroll.ApprovedPayroll : payroll.EstimatedPayroll,
                status = payroll.Status
            };

            return Json(slipData);
        }

        // ─── Export Payroll Register Data (JSON for client-side Excel) ──
        [HttpGet("/admin/staff/cross-comparison/export/payroll-register")]
        public async Task<IActionResult> ExportPayrollRegister(int year, int month)
        {
            var payrolls = await _dbContext.StaffPayrolls
                .Where(p => !p.IsDeleted && p.Year == year && p.Month == month)
                .ToListAsync();

            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staffDetails = await _dbContext.StaffDetails
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            var register = payrolls.Select(p =>
            {
                var staff = staffUsers.FirstOrDefault(u => u.Id == p.StaffId);
                var detail = staffDetails.FirstOrDefault(d => d.UserId == p.StaffId);
                return new
                {
                    staffName = staff?.NormalizedUserName ?? staff?.UserName ?? "Unknown",
                    department = detail?.Department ?? "N/A",
                    designation = detail?.Designation ?? "N/A",
                    basicSalary = p.BasicSalary,
                    adminIncentive = p.AdminIncentive,
                    totalIncentive = p.TotalIncentive,
                    totalDeduction = p.TotalDeduction,
                    estimatedPayroll = p.EstimatedPayroll,
                    approvedPayroll = p.ApprovedPayroll,
                    netPayable = p.ApprovedPayroll > 0 ? p.ApprovedPayroll : p.EstimatedPayroll,
                    status = p.Status
                };
            }).ToList();

            return Json(register);
        }

        // ─── Chart Data (AJAX) ───────────────────────────────────
        [HttpGet("/admin/staff/cross-comparison/chart-data")]
        public async Task<IActionResult> GetChartData(int year, int month)
        {
            // Return the same data as Index but in JSON format for chart rendering
            // This is a lightweight endpoint that returns just the comparison data
            var payrolls = await _dbContext.StaffPayrolls
                .Where(p => !p.IsDeleted && p.Year == year && p.Month == month)
                .ToListAsync();

            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staffDetails = await _dbContext.StaffDetails
                .Where(s => !s.IsDeleted)
                .ToDictionaryAsync(s => s.UserId);

            var chartData = new
            {
                labels = staffUsers.Select(s => s.NormalizedUserName ?? s.UserName ?? "Unknown").ToArray(),
                payrollData = staffUsers.Select(s =>
                {
                    var p = payrolls.FirstOrDefault(pr => pr.StaffId == s.Id);
                    return new
                    {
                        basicSalary = p?.BasicSalary ?? 0,
                        totalIncentive = p?.TotalIncentive ?? 0,
                        totalDeduction = p?.TotalDeduction ?? 0,
                        estimatedPayroll = p?.EstimatedPayroll ?? 0,
                        approvedPayroll = p?.ApprovedPayroll ?? 0
                    };
                }).ToArray()
            };

            return Json(chartData);
        }

        // ─── Helper: Compute Date Range ──────────────────────────
        private void ComputeDateRange(string? period, int year, int month, out DateTime dateFrom, out DateTime dateTo)
        {
            string cleanPeriod = (period ?? "Month").Trim();

            if (cleanPeriod.Equals("Day", StringComparison.OrdinalIgnoreCase))
            {
                var localTodayStartUtc = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local).ToUniversalTime();
                dateFrom = localTodayStartUtc < DateTime.UtcNow.Date ? localTodayStartUtc : DateTime.UtcNow.Date;
                var localTodayEnd = DateTime.Today.AddDays(1).AddTicks(-1);
                var utcTodayEnd = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
                dateTo = localTodayEnd > utcTodayEnd ? localTodayEnd : utcTodayEnd;
            }
            else if (cleanPeriod.Equals("Week", StringComparison.OrdinalIgnoreCase))
            {
                var today = DateTime.UtcNow.Date;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                dateFrom = today.AddDays(-diff);
                dateTo = dateFrom.AddDays(7).AddTicks(-1);
            }
            else if (cleanPeriod.Equals("Year", StringComparison.OrdinalIgnoreCase))
            {
                dateFrom = new DateTime(year, 1, 1);
                dateTo = new DateTime(year, 12, 31, 23, 59, 59);
            }
            else // Month (default)
            {
                dateFrom = new DateTime(year, month, 1);
                dateTo = dateFrom.AddMonths(1).AddTicks(-1);
            }
        }
    }

    // ─── Request Models ──────────────────────────────────────────
    public class ReopenPayrollModel
    {
        public long PayrollId { get; set; }
        public string? Reason { get; set; }
    }
}
