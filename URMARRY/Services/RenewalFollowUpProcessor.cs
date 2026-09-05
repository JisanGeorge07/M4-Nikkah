using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Persistence;

namespace URMARRY.Services
{
    public class RenewalFollowUpProcessor : IRenewalFollowUpProcessor
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RenewalFollowUpProcessor> _logger;

        public RenewalFollowUpProcessor(
            AppDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<RenewalFollowUpProcessor> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RenewalFollowUpProcessResult> ProcessAutoRenewalFollowUpsAsync(CancellationToken cancellationToken = default)
        {
            var result = new RenewalFollowUpProcessResult();

            bool enabled = _configuration.GetValue<bool>("AutoRenewalFollowUp:Enabled", true);
            if (!enabled)
            {
                result.Success = true;
                result.Message = "AutoRenewalFollowUp is disabled via configuration.";
                return result;
            }

            int daysBeforeExpiry = _configuration.GetValue<int>("AutoRenewalFollowUp:DaysBeforeExpiry", 10);
            int creditsThreshold = _configuration.GetValue<int>("AutoRenewalFollowUp:CreditsThreshold", 5);
            int maxRecentExpiredDays = _configuration.GetValue<int>("AutoRenewalFollowUp:MaxRecentExpiredDays", 30);

            var now = DateTime.UtcNow;
            var expiryThreshold = now.AddDays(daysBeforeExpiry);
            var recentExpiredThreshold = now.AddDays(-maxRecentExpiredDays);

            _logger.LogInformation(
                "Starting Auto Renewal Follow-up evaluation. DaysBeforeExpiry: {Days}, CreditsThreshold: {Credits}, MaxRecentExpiredDays: {MaxExpired}",
                daysBeforeExpiry, creditsThreshold, maxRecentExpiredDays);

            // 1. Fetch active staff assignments (only profiles assigned to active staff)
            var activeAssignments = await _dbContext.StaffProfileAssignments
                .Where(a => !a.IsDeleted && a.IsActive && a.StaffId > 0)
                .ToListAsync(cancellationToken);

            if (!activeAssignments.Any())
            {
                result.Success = true;
                result.Message = "No active staff profile assignments found.";
                _logger.LogInformation(result.Message);
                return result;
            }

            // Group assignments by ProfileId (take the latest active assignment per profile)
            var assignmentDict = activeAssignments
                .GroupBy(a => a.ProfileId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ModifiedOn).ThenByDescending(a => a.Id).First());

            var assignedProfileIds = assignmentDict.Keys.ToList();

            // 2. Fetch active and non-deleted customer registrations
            var activeProfiles = await _dbContext.Registration
                .Where(p => assignedProfileIds.Contains(p.Id) && !p.IsDeleted && p.IsActive)
                .Select(p => new { p.Id, p.Name, p.RegisterNumber, p.Phone })
                .ToListAsync(cancellationToken);

            var activeProfileIds = activeProfiles.Select(p => p.Id).ToHashSet();

            // 3. Fetch plan purchases for candidate profiles
            var planPurchases = await _dbContext.PlanPurchases
                .Where(p => activeProfileIds.Contains(p.UserId) && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            var plansByUser = planPurchases
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 4. Fetch Staff directory for human-readable staff names in timelines
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staffDict = new Dictionary<long, string>();
            foreach (var u in staffUsers)
            {
                staffDict[u.Id] = u.NormalizedUserName ?? u.UserName ?? "Staff";
            }

            // 5. Fetch existing renewal follow-ups for candidate profiles
            var existingFollowUps = await _dbContext.FollowUps
                .Where(f => activeProfileIds.Contains(f.ProfileId) && f.FollowUpType == FollowUpType.RenewalFollowUp && !f.IsDeleted)
                .ToListAsync(cancellationToken);

            var existingFollowUpsGroup = existingFollowUps
                .GroupBy(f => f.ProfileId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.CreatedOn).ToList());

            // Helper to get staff name with fallback
            async Task<string> ResolveStaffName(long staffId)
            {
                if (staffDict.TryGetValue(staffId, out var name))
                {
                    return name;
                }

                var user = await _userManager.FindByIdAsync(staffId.ToString());
                if (user != null)
                {
                    var resolvedName = user.NormalizedUserName ?? user.UserName ?? "Staff";
                    staffDict[staffId] = resolvedName;
                    return resolvedName;
                }

                return "Staff";
            }

            // 6. Evaluate each active assigned profile
            foreach (var profile in activeProfiles)
            {
                long profileId = profile.Id;
                if (!assignmentDict.TryGetValue(profileId, out var assignment))
                {
                    continue;
                }

                long staffId = assignment.StaffId;

                // Profile must have purchase history to be eligible for renewal
                if (!plansByUser.TryGetValue(profileId, out var userPlans) || userPlans.Count == 0)
                {
                    result.SkippedNotEligibleCount++;
                    continue;
                }

                var activePlans = userPlans.Where(p => p.ExpiresAt > now).ToList();
                var activePlan = activePlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();
                int remainingCredits = activePlans.Sum(p => Math.Max(0, p.ViewCreditsPurchased - p.ViewCreditsUsed));

                bool isEligible = false;
                string triggerReason = string.Empty;

                if (activePlan != null)
                {
                    bool isExpiringSoon = activePlan.ExpiresAt <= expiryThreshold;
                    bool isLowCredits = remainingCredits <= creditsThreshold;

                    if (isExpiringSoon && isLowCredits)
                    {
                        isEligible = true;
                        triggerReason = $"Plan expiring in <= {daysBeforeExpiry} days ({activePlan.ExpiresAt:dd-MMM-yyyy}) & low credits ({remainingCredits} left)";
                    }
                    else if (isExpiringSoon)
                    {
                        isEligible = true;
                        triggerReason = $"Plan expiring in <= {daysBeforeExpiry} days ({activePlan.ExpiresAt:dd-MMM-yyyy})";
                    }
                    else if (isLowCredits)
                    {
                        isEligible = true;
                        triggerReason = $"Low credits remaining ({remainingCredits} credits left, <= {creditsThreshold})";
                    }
                }
                else
                {
                    // No active plan: check if plan recently expired within maxRecentExpiredDays
                    var latestExpiredPlan = userPlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();
                    if (latestExpiredPlan != null && latestExpiredPlan.ExpiresAt >= recentExpiredThreshold)
                    {
                        isEligible = true;
                        triggerReason = $"Plan recently expired on {latestExpiredPlan.ExpiresAt:dd-MMM-yyyy}";
                    }
                }

                if (!isEligible)
                {
                    result.SkippedNotEligibleCount++;
                    continue;
                }

                result.EligibleCount++;
                string staffName = await ResolveStaffName(staffId);

                // Fetch all previous renewal follow-ups for this profile
                existingFollowUpsGroup.TryGetValue(profileId, out var userRenewals);
                userRenewals ??= new List<FollowUp>();

                // CHECK 1: Is there ANY active/open renewal follow-up in progress? (Pending / Interested)
                var activeOpenRenewal = userRenewals.FirstOrDefault(f =>
                    f.LatestRenewalInterestStatus == RenewalInterestStatus.Pending ||
                    f.LatestRenewalInterestStatus == RenewalInterestStatus.Interested);

                if (activeOpenRenewal != null)
                {
                    // Staff is already actively following up. Keep staff assignment synced if reassigned.
                    if (activeOpenRenewal.AssignedStaffId != staffId)
                    {
                        activeOpenRenewal.AssignedStaffId = staffId;
                    }
                    result.SkippedActiveCount++;
                    continue; // DO NOT create redundant duplicate
                }

                // CHECK 2: Respect customer rejections (NotInterested within last 30 days)
                var latestRenewal = userRenewals.FirstOrDefault();
                if (latestRenewal != null &&
                    latestRenewal.LatestRenewalInterestStatus == RenewalInterestStatus.NotInterested &&
                    latestRenewal.ModifiedOn >= now.AddDays(-30))
                {
                    result.SkippedNotInterestedCount++;
                    continue;
                }

                // CHECK 3: CREATE A BRAND NEW DISTINCT RENEWAL FOLLOW-UP RECORD!
                // Past renewal follow-ups and their approvals/incentives remain 100% untouched.
                var newRenewal = new FollowUp
                {
                    ProfileId = profileId,
                    FollowUpType = FollowUpType.RenewalFollowUp,
                    LatestContactType = null,
                    LatestCallStatus = null,
                    LatestInterestStatus = null,
                    LatestProfileVerificationStatus = null,
                    LatestRenewalInterestStatus = RenewalInterestStatus.Pending,
                    LatestRemarks = $"Auto-created renewal follow-up: {triggerReason}",
                    NextFollowUpDate = null,
                    AssignedStaffId = staffId,
                    IsActive = true
                };

                await _dbContext.FollowUps.AddAsync(newRenewal, cancellationToken);
                await _dbContext.SaveChangesAsync("SYSTEM");

                var timeline = new FollowUpTimeline
                {
                    FollowUpId = newRenewal.Id,
                    StaffId = staffId,
                    StaffName = staffName,
                    ContactType = null,
                    CallStatus = null,
                    InterestStatus = null,
                    ProfileVerificationStatus = null,
                    RenewalInterestStatus = RenewalInterestStatus.Pending,
                    Remarks = $"Renewal follow-up cycle created and assigned to {staffName} ({triggerReason})",
                    NextFollowUpDate = null,
                    IsActive = true
                };

                await _dbContext.FollowUpTimelines.AddAsync(timeline, cancellationToken);
                result.AddedCount++;
                _logger.LogInformation("Created new Renewal Follow-up cycle for Profile {ProfileId} assigned to Staff {StaffName} ({StaffId}). Reason: {Reason}",
                    profileId, staffName, staffId, triggerReason);
            }

            await _dbContext.SaveChangesAsync("SYSTEM");

            result.Success = true;
            result.Message = $"Auto-renewal evaluation complete. Eligible: {result.EligibleCount} | Added: {result.AddedCount} | Reopened: {result.ReopenedCount} | In-Progress: {result.SkippedActiveCount} | Not Interested: {result.SkippedNotInterestedCount}";
            _logger.LogInformation(result.Message);

            return result;
        }
    }
}
