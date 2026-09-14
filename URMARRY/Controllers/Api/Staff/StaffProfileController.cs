using Application.Interfaces.Persistence;
using Application.Interfaces.Infrastructure;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Application.Helpers;
using Application.Models;
using Application.Constants;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using URMARRY.Services;
using URMARRY.Controllers;

namespace URMARRY.Controllers.Api.Staff
{
    [ApiController]
    [Route("api/staff")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Staff")]
    public class StaffProfileController : ControllerBase
    {
        private readonly IRepository<StaffProfileAssignment> _assignmentRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StaffProfileController> _logger;
        private readonly IRepository<FollowUp> _followUpRepo;
        private readonly IRepository<PlanPurchase> _planPurchaseRepo;
        private readonly Persistence.AppDbContext _dbContext;
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly EmailNotificationHelper _emailNotificationHelper;
        private readonly IRepository<FollowUpTimeline> _followUpTimelineRepo;
        private readonly IRepository<ProfileFor> _profileForRepo;
        private readonly IRepository<Nationality> _nationalityRepo;
        private readonly IRepository<MaritalStatus> _maritalStatusRepo;
        private readonly IRepository<BodyFeatures> _bodyFeaturesRepo;
        private readonly IRepository<Profession> _professionRepo;
        private readonly IRepository<MotherTongue> _motherTongueRepo;
        private readonly IRepository<ReligionCaste> _religionCasteRepo;
        private readonly IRepository<Community> _communityRepo;
        private readonly IRepository<Religiousness> _religiousnessRepo;
        private readonly IRepository<FinancialStatus> _financialStatus;
        private readonly IFileService _fileService;
        private readonly CookieHelper _cookieHelper;
        private readonly IRepository<ColdLead> _coldLeadRepo;
        private readonly IConfiguration _configuration;
        private readonly Guid key = new Guid("F7AD797A-416C-4C56-8749-7017FBC90427");

        public StaffProfileController(
            IRepository<StaffProfileAssignment> assignmentRepo,
            UserManager<ApplicationUser> userManager,
            ILogger<StaffProfileController> logger,
            IRepository<FollowUp> followUpRepo,
            IRepository<PlanPurchase> planPurchaseRepo,
            Persistence.AppDbContext dbContext,
            IRepository<Registration> registrationRepo,
            IMapper mapper,
            IEmailService emailService,
            EmailNotificationHelper emailNotificationHelper,
            IRepository<FollowUpTimeline> followUpTimelineRepo,
            IRepository<ProfileFor> profileForRepo,
            IRepository<Nationality> nationalityRepo,
            IRepository<MaritalStatus> maritalStatusRepo,
            IRepository<BodyFeatures> bodyFeaturesRepo,
            IRepository<Profession> professionRepo,
            IRepository<MotherTongue> motherTongueRepo,
            IRepository<ReligionCaste> religionCasteRepo,
            IRepository<Community> communityRepo,
            IRepository<Religiousness> religiousnessRepo,
            IRepository<FinancialStatus> financialStatus,
            IFileService fileService,
            CookieHelper cookieHelper,
            IRepository<ColdLead> coldLeadRepo,
            IConfiguration configuration)
        {
            _assignmentRepo = assignmentRepo;
            _userManager = userManager;
            _logger = logger;
            _followUpRepo = followUpRepo;
            _planPurchaseRepo = planPurchaseRepo;
            _dbContext = dbContext;
            _registrationRepo = registrationRepo;
            _mapper = mapper;
            _emailService = emailService;
            _emailNotificationHelper = emailNotificationHelper;
            _followUpTimelineRepo = followUpTimelineRepo;
            _profileForRepo = profileForRepo;
            _nationalityRepo = nationalityRepo;
            _maritalStatusRepo = maritalStatusRepo;
            _bodyFeaturesRepo = bodyFeaturesRepo;
            _professionRepo = professionRepo;
            _motherTongueRepo = motherTongueRepo;
            _religionCasteRepo = religionCasteRepo;
            _communityRepo = communityRepo;
            _religiousnessRepo = religiousnessRepo;
            _financialStatus = financialStatus;
            _fileService = fileService;
            _cookieHelper = cookieHelper;
            _coldLeadRepo = coldLeadRepo;
            _configuration = configuration;
        }


        /// <summary>
        /// Fetches all profiles that are assigned to any staff member.
        /// </summary>
        [HttpGet("assigned-profiles")]
        public async Task<IActionResult> GetAssignedProfiles()
        {
            try
            {
                // 1. Fetch active assignments with their profile details
                var assignments = await _assignmentRepo.GetQueryable()
                    .Where(a => !a.IsDeleted)
                    .Include(a => a.Profile)
                    .ToListAsync();

                // 2. Fetch all staff members to get their names
                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = staffUsers.ToDictionary(u => u.Id, u => u.NormalizedUserName ?? u.UserName);

                var profileIds = assignments.Where(a => a.Profile != null && !a.Profile.IsDeleted).Select(a => a.Profile!.Id).Distinct().ToList();
                var docsList = await _dbContext.VerificationDocuments
                    .Where(d => profileIds.Contains(d.ProfileId) && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToListAsync();

                var docsDict = docsList
                    .GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    }).ToList());

                var planPurchases = await _planPurchaseRepo.GetQueryable()
                    .Where(p => !p.IsDeleted && profileIds.Contains(p.UserId))
                    .ToListAsync();

                // 3. Project and map assignments to profile list
                var data = assignments
                    .Where(a => a.Profile != null && !a.Profile.IsDeleted)
                    .OrderByDescending(a => a.CreatedOn)
                    .ThenByDescending(a => a.Profile!.CreatedOn)
                    .ThenByDescending(a => a.Id)
                    .Select(a =>
                    {
                        var profile = a.Profile!;
                        
                        // Resolve staff name from dictionary
                        staffDict.TryGetValue(a.StaffId, out var staffName);
                        if (string.IsNullOrEmpty(staffName))
                        {
                            staffName = "Unknown Staff";
                        }

                        // Resolve location parts
                        var locationParts = new List<string>();
                        if (!string.IsNullOrEmpty(profile.Village)) locationParts.Add(profile.Village);
                        if (!string.IsNullOrEmpty(profile.District)) locationParts.Add(profile.District);
                        if (!string.IsNullOrEmpty(profile.State)) locationParts.Add(profile.State);
                        var locationStr = string.Join(", ", locationParts);

                        var userPlans = planPurchases.Where(p => p.UserId == profile.Id).ToList();
                        var activePlans = userPlans.Where(p => p.ExpiresAt > DateTime.UtcNow).ToList();
                        var activePlan = activePlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();
                        var latestPlan = userPlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();

                        int? remainingCredits = activePlans.Count > 0 
                            ? activePlans.Sum(p => Math.Max(0, p.ViewCreditsPurchased - p.ViewCreditsUsed)) 
                            : (int?)null;

                        string membershipStatus;
                        string? expiryDate = null;
                        int? daysLeft = null;

                        if (activePlan != null)
                        {
                            expiryDate = activePlan.ExpiresAt.ToString("yyyy-MM-dd");
                            daysLeft = (int)Math.Ceiling((activePlan.ExpiresAt - DateTime.UtcNow).TotalDays);
                            if (daysLeft < 0) daysLeft = 0;

                            bool isExpiringSoon = activePlan.ExpiresAt <= DateTime.UtcNow.AddDays(10);
                            bool isLowCredits = (remainingCredits ?? 0) <= 5;

                            if (isExpiringSoon || isLowCredits)
                            {
                                membershipStatus = "Yet to Expire";
                            }
                            else
                            {
                                membershipStatus = "Active Plan";
                            }
                        }
                        else if (latestPlan != null)
                        {
                            membershipStatus = "Expired";
                            expiryDate = latestPlan.ExpiresAt.ToString("yyyy-MM-dd");
                        }
                        else if (profile.IsPremiumMember)
                        {
                            membershipStatus = "Active Plan";
                            expiryDate = "Active";
                        }
                        else
                        {
                            membershipStatus = "No Plan";
                            expiryDate = null;
                        }

                        docsDict.TryGetValue(profile.Id, out var userDocs);

                        return new
                        {
                            profileId = profile.Id,
                            registerNumber = profile.RegisterNumber ?? string.Empty,
                            name = profile.Name ?? string.Empty,
                            gender = profile.Gender ?? string.Empty,
                            age = CalculateAge(profile.DOB),
                            phone = profile.Phone ?? string.Empty,
                            email = profile.Email ?? string.Empty,
                            imagePath = profile.ImagePath ?? string.Empty,
                            location = locationStr,
                            isPremiumMember = profile.IsPremiumMember,
                            isComplete = profile.IsComplete,
                            staffId = a.StaffId,
                            staffName = staffName,
                            createdOn = profile.CreatedOn,
                            documentVerificationEnabled = profile.DocumentVerificationEnabled,
                            verificationDocumentUrl = profile.VerificationDocumentUrl ?? userDocs?.FirstOrDefault()?.documentUrl,
                            verificationDocuments = userDocs != null ? (object)userDocs : Array.Empty<object>(),
                            documentVerificationComplete = profile.DocumentVerificationComplete,
                            documentVerificationRejected = profile.DocumentVerificationRejected,
                            membershipStatus = membershipStatus,
                            expiryDate = expiryDate,
                            remainingCredits = remainingCredits,
                            daysLeft = daysLeft
                        };
                    })
                    .ToList();

                _logger.LogInformation("Fetched all staff-assigned profiles. Total count: {Count}", data.Count);

                return Ok(new
                {
                    success = true,
                    data = data,
                    message = "Staff assigned profiles fetched successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all staff-assigned profiles.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        /// <summary>
        /// Fetches profiles that are assigned to a specific staff member.
        /// </summary>
        [HttpGet("assigned-profiles/{staffId:long}")]
        public async Task<IActionResult> GetProfilesByStaffId(long staffId)
        {
            try
            {
                // 1. Fetch active assignments for the specific staff member
                var assignments = await _assignmentRepo.GetQueryable()
                    .Where(a => !a.IsDeleted && a.StaffId == staffId)
                    .Include(a => a.Profile)
                    .ToListAsync();

                // 2. Fetch the specific staff member's info
                var staffUser = await _userManager.FindByIdAsync(staffId.ToString());
                string staffName = staffUser != null ? (staffUser.NormalizedUserName ?? staffUser.UserName) : "Unknown Staff";

                var profileIds = assignments.Where(a => a.Profile != null && !a.Profile.IsDeleted).Select(a => a.Profile!.Id).Distinct().ToList();
                var docsList = await _dbContext.VerificationDocuments
                    .Where(d => profileIds.Contains(d.ProfileId) && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToListAsync();

                var docsDict = docsList
                    .GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    }).ToList());

                var planPurchases = await _planPurchaseRepo.GetQueryable()
                    .Where(p => !p.IsDeleted && profileIds.Contains(p.UserId))
                    .ToListAsync();

                // 3. Project and map assignments to profile list
                var data = assignments
                    .Where(a => a.Profile != null && !a.Profile.IsDeleted)
                    .OrderByDescending(a => a.CreatedOn)
                    .ThenByDescending(a => a.Profile!.CreatedOn)
                    .ThenByDescending(a => a.Id)
                    .Select(a =>
                    {
                        var profile = a.Profile!;

                        // Resolve location parts
                        var locationParts = new List<string>();
                        if (!string.IsNullOrEmpty(profile.Village)) locationParts.Add(profile.Village);
                        if (!string.IsNullOrEmpty(profile.District)) locationParts.Add(profile.District);
                        if (!string.IsNullOrEmpty(profile.State)) locationParts.Add(profile.State);
                        var locationStr = string.Join(", ", locationParts);

                        var userPlans = planPurchases.Where(p => p.UserId == profile.Id).ToList();
                        var activePlans = userPlans.Where(p => p.ExpiresAt > DateTime.UtcNow).ToList();
                        var activePlan = activePlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();
                        var latestPlan = userPlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();

                        int? remainingCredits = activePlans.Count > 0 
                            ? activePlans.Sum(p => Math.Max(0, p.ViewCreditsPurchased - p.ViewCreditsUsed)) 
                            : (int?)null;

                        string membershipStatus;
                        string? expiryDate = null;
                        int? daysLeft = null;

                        if (activePlan != null)
                        {
                            expiryDate = activePlan.ExpiresAt.ToString("yyyy-MM-dd");
                            daysLeft = (int)Math.Ceiling((activePlan.ExpiresAt - DateTime.UtcNow).TotalDays);
                            if (daysLeft < 0) daysLeft = 0;

                            bool isExpiringSoon = activePlan.ExpiresAt <= DateTime.UtcNow.AddDays(10);
                            bool isLowCredits = (remainingCredits ?? 0) <= 5;

                            if (isExpiringSoon || isLowCredits)
                            {
                                membershipStatus = "Yet to Expire";
                            }
                            else
                            {
                                membershipStatus = "Active Plan";
                            }
                        }
                        else if (latestPlan != null)
                        {
                            membershipStatus = "Expired";
                            expiryDate = latestPlan.ExpiresAt.ToString("yyyy-MM-dd");
                        }
                        else if (profile.IsPremiumMember)
                        {
                            membershipStatus = "Active Plan";
                            expiryDate = "Active";
                        }
                        else
                        {
                            membershipStatus = "No Plan";
                            expiryDate = null;
                        }

                        docsDict.TryGetValue(profile.Id, out var userDocs);

                        return new
                        {
                            profileId = profile.Id,
                            registerNumber = profile.RegisterNumber ?? string.Empty,
                            name = profile.Name ?? string.Empty,
                            gender = profile.Gender ?? string.Empty,
                            age = CalculateAge(profile.DOB),
                            phone = profile.Phone ?? string.Empty,
                            email = profile.Email ?? string.Empty,
                            imagePath = profile.ImagePath ?? string.Empty,
                            location = locationStr,
                            isPremiumMember = profile.IsPremiumMember,
                            isComplete = profile.IsComplete,
                            staffId = staffId,
                            staffName = staffName,
                            createdOn = profile.CreatedOn,
                            documentVerificationEnabled = profile.DocumentVerificationEnabled,
                            verificationDocumentUrl = profile.VerificationDocumentUrl ?? userDocs?.FirstOrDefault()?.documentUrl,
                            verificationDocuments = userDocs != null ? (object)userDocs : Array.Empty<object>(),
                            documentVerificationComplete = profile.DocumentVerificationComplete,
                            documentVerificationRejected = profile.DocumentVerificationRejected,
                            membershipStatus = membershipStatus,
                            expiryDate = expiryDate,
                            remainingCredits = remainingCredits,
                            daysLeft = daysLeft
                        }; 
                    })
                    .ToList();

                _logger.LogInformation("Fetched profiles assigned to staff member (ID: {StaffId}). Total count: {Count}", staffId, data.Count);

                return Ok(new
                {
                    success = true,
                    data = data,
                    message = $"Profiles assigned to staff ID {staffId} fetched successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching profiles for staff ID: {StaffId}", staffId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        #region Helper Methods

        private int CalculateAge(string? dobString)
        {
            if (string.IsNullOrEmpty(dobString)) return 0;
            try
            {
                var parts = dobString.Replace("-", "/").Split('/');
                if (parts.Length < 3) return 0;

                string day = parts[0].PadLeft(2, '0');
                string month = parts[1].PadLeft(2, '0');
                string year = parts[2];

                if (day.Length > 2)
                {
                    var temp = day;
                    day = year;
                    year = temp;
                }

                var parsedDate = DateTime.ParseExact($"{day}/{month}/{year}", "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var today = DateTime.UtcNow;
                int age = today.Year - parsedDate.Year;
                if (parsedDate > today.AddYears(-age))
                {
                    age--;
                }
                return age;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        /// <summary>
        /// Fetches all follow-ups for a specific profile.
        /// </summary>
        [HttpGet("profile-followups/{profileId:long}")]
        public async Task<IActionResult> GetProfileFollowUps(long profileId)
        {
            try
            {
                var profile = await _registrationRepo.Get(profileId);
                if (profile == null || profile.IsDeleted)
                {
                    return Ok(new
                    {
                        success = true,
                        data = Array.Empty<object>(),
                        message = "Profile not found or deleted."
                    });
                }

                var followUps = await _followUpRepo.GetQueryable()
                    .Where(f => f.ProfileId == profileId && !f.IsDeleted && f.Profile != null && !f.Profile.IsDeleted)
                    .Include(f => f.Timelines)
                    .Include(f => f.Profile)
                    .OrderByDescending(f => f.CreatedOn)
                    .ThenByDescending(f => f.Id)
                    .ToListAsync();

                var docs = await _dbContext.VerificationDocuments
                    .Where(d => d.ProfileId == profileId && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    })
                    .ToListAsync();

                var data = followUps.Select(f => new
                {
                    followUpId = f.Id,
                    followUpType = f.FollowUpType.ToString(),
                    followUpTypeVal = (int)f.FollowUpType,
                    createdOn = f.CreatedOn.ToString("dd-MMM-yyyy"),
                    createdOnFull = f.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                    latestContactType = f.LatestContactType?.ToString() ?? "N/A",
                    latestCallStatus = f.LatestCallStatus?.ToString() ?? "N/A",
                    latestPremiumInterestStatus = f.LatestInterestStatus?.ToString() ?? "N/A",
                    latestProfileVerificationStatus = f.LatestProfileVerificationStatus?.ToString() ?? "N/A",
                    latestRenewalInterestStatus = f.LatestRenewalInterestStatus?.ToString() ?? "N/A",
                    latestRemarks = f.LatestRemarks ?? string.Empty,
                    nextFollowUpDate = f.NextFollowUpDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    adminApprovalStatus = f.LatestAdminApprovalStatus?.ToString() ?? "None",
                    isAdminApproved = f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved,
                    verificationDocumentUrl = f.Profile?.VerificationDocumentUrl ?? docs.FirstOrDefault()?.documentUrl,
                    verificationDocuments = docs,
                    documentVerificationEnabled = f.Profile?.DocumentVerificationEnabled ?? false,
                    documentVerificationComplete = f.Profile?.DocumentVerificationComplete ?? false,
                    timelines = f.Timelines
                        .Where(t => !t.IsDeleted)
                        .OrderByDescending(t => t.CreatedOn)
                        .Select(t => new
                        {
                            timelineId = t.Id,
                            createdOn = t.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                            staffId = t.StaffId,
                            staffName = t.StaffName ?? "Unknown Staff",
                            contactType = t.ContactType?.ToString() ?? "N/A",
                            callStatus = t.CallStatus?.ToString() ?? "N/A",
                            premiumInterestStatus = t.InterestStatus?.ToString() ?? "N/A",
                            profileVerificationStatus = t.ProfileVerificationStatus?.ToString() ?? "N/A",
                            renewalInterestStatus = t.RenewalInterestStatus?.ToString() ?? "N/A",
                            remarks = t.Remarks ?? string.Empty,
                            nextFollowUpDate = t.NextFollowUpDate?.ToString("yyyy-MM-dd") ?? string.Empty
                        }).ToList()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = data,
                    message = "Follow-ups fetched successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching follow-ups for profile ID: {ProfileId}", profileId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        /// <summary>
        /// Fetches follow-ups assigned to a specific staff member, optionally filtered by FollowUpType.
        /// </summary>
        [HttpGet("followups/{staffId:long}")]
        public async Task<IActionResult> GetFollowUpsByStaff(long staffId, [FromQuery] FollowUpType? type)
        {
            try
            {
                IQueryable<FollowUp> query = _followUpRepo.GetQueryable()
                    .Where(f => f.AssignedStaffId == staffId && !f.IsDeleted && f.Profile != null && !f.Profile.IsDeleted)
                    .Include(f => f.Profile);

                if (type.HasValue)
                {
                    query = query.Where(f => f.FollowUpType == type.Value);
                }

                query = query.OrderByDescending(f => f.CreatedOn).ThenByDescending(f => f.Id);

                var followUps = await query.ToListAsync();

                // Defensive in-memory check to ensure deleted profiles are excluded
                followUps = followUps.Where(f => f.Profile != null && !f.Profile.IsDeleted).ToList();

                // Batch fetch latest Transaction and PlanPurchase details for these profiles
                var profileIds = followUps.Select(f => f.ProfileId).Distinct().ToList();

                var transactions = await _dbContext.Transaction
                    .Where(t => profileIds.Contains(t.userId) && t.Status == "success")
                    .ToListAsync();

                var plans = await _planPurchaseRepo.GetQueryable()
                    .Where(p => profileIds.Contains(p.UserId) && !p.IsDeleted)
                    .ToListAsync();

                var allDocs = await _dbContext.VerificationDocuments
                    .Where(d => profileIds.Contains(d.ProfileId) && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .Select(d => new
                    {
                        d.ProfileId,
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    })
                    .ToListAsync();

                var docsDict = allDocs.GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        id = d.id,
                        documentUrl = d.documentUrl,
                        originalFileName = d.originalFileName,
                        displayOrder = d.displayOrder
                    }).ToList());

                // Smart sorting for Renewal Follow-ups:
                // 1st Priority: Expired plans (most recently expired first)
                // 2nd Priority: Active plans expiring soonest (earliest expiry date first)
                // 3rd Priority: Active premium without explicit plan date
                // 4th Priority: No plan / others
                if (type == FollowUpType.RenewalFollowUp)
                {
                    var now = DateTime.UtcNow;
                    followUps = followUps
                        .OrderBy(f =>
                        {
                            var userPlan = plans
                                .Where(p => p.UserId == f.ProfileId)
                                .OrderByDescending(p => p.ExpiresAt)
                                .ThenByDescending(p => p.CreatedOn)
                                .FirstOrDefault();

                            if (userPlan != null)
                            {
                                return userPlan.ExpiresAt <= now ? 1 : 2;
                            }

                            if (f.Profile != null && f.Profile.IsPremiumMember)
                            {
                                return 3;
                            }

                            return 4;
                        })
                        .ThenBy(f =>
                        {
                            var userPlan = plans
                                .Where(p => p.UserId == f.ProfileId)
                                .OrderByDescending(p => p.ExpiresAt)
                                .ThenByDescending(p => p.CreatedOn)
                                .FirstOrDefault();

                            if (userPlan != null)
                            {
                                if (userPlan.ExpiresAt <= now)
                                {
                                    // Expired: Most recently expired first (smallest elapsed time from now)
                                    return (now - userPlan.ExpiresAt).TotalSeconds;
                                }
                                else
                                {
                                    // Active: Soonest expiring first (smallest remaining time from now)
                                    return (userPlan.ExpiresAt - now).TotalSeconds;
                                }
                            }

                            return double.MaxValue;
                        })
                        .ThenByDescending(f => f.CreatedOn)
                        .ThenByDescending(f => f.Id)
                        .ToList();
                }

                var data = followUps.Select(f =>
                {
                    var locationParts = new List<string>();
                    if (f.Profile != null)
                    {
                        if (!string.IsNullOrEmpty(f.Profile.Village)) locationParts.Add(f.Profile.Village);
                        if (!string.IsNullOrEmpty(f.Profile.District)) locationParts.Add(f.Profile.District);
                        if (!string.IsNullOrEmpty(f.Profile.State)) locationParts.Add(f.Profile.State);
                    }
                    var locationStr = string.Join(", ", locationParts);

                    var latestTxn = transactions
                        .Where(t => t.userId == f.ProfileId)
                        .OrderByDescending(t => t.CreatedOn)
                        .FirstOrDefault();

                    var userPlans = plans.Where(p => p.UserId == f.ProfileId).ToList();
                    var activePlans = userPlans.Where(p => p.ExpiresAt > DateTime.UtcNow).ToList();
                    var activePlan = activePlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();
                    var latestPlan = userPlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();

                    int? remainingCredits = activePlans.Count > 0 
                        ? activePlans.Sum(p => Math.Max(0, p.ViewCreditsPurchased - p.ViewCreditsUsed)) 
                        : (int?)null;

                    string membershipStatus;
                    string? expiryDate = null;
                    int? daysLeft = null;

                    if (activePlan != null)
                    {
                        expiryDate = activePlan.ExpiresAt.ToString("yyyy-MM-dd");
                        daysLeft = (int)Math.Ceiling((activePlan.ExpiresAt - DateTime.UtcNow).TotalDays);
                        if (daysLeft < 0) daysLeft = 0;

                        bool isExpiringSoon = activePlan.ExpiresAt <= DateTime.UtcNow.AddDays(10);
                        bool isLowCredits = (remainingCredits ?? 0) <= 5;

                        if (isExpiringSoon || isLowCredits)
                        {
                            membershipStatus = "Yet to Expire";
                        }
                        else
                        {
                            membershipStatus = "Active Plan";
                        }
                    }
                    else if (latestPlan != null)
                    {
                        membershipStatus = "Expired";
                        expiryDate = latestPlan.ExpiresAt.ToString("yyyy-MM-dd");
                    }
                    else if (f.Profile != null && f.Profile.IsPremiumMember)
                    {
                        membershipStatus = "Active Plan";
                        expiryDate = "Active";
                    }
                    else
                    {
                        membershipStatus = "No Plan";
                        expiryDate = null;
                    }

                    var packageName = (f.Profile != null && f.Profile.IsPremiumMember) ? "Premium" : "Non Premium";

                    docsDict.TryGetValue(f.ProfileId, out var userDocs);

                    return new
                    {
                        followUpId = f.Id,
                        profileId = f.ProfileId,
                        registerNumber = f.Profile?.RegisterNumber ?? "N/A",
                        customerName = f.Profile?.Name ?? "N/A",
                        gender = f.Profile?.Gender ?? "N/A",
                        age = CalculateAge(f.Profile?.DOB),
                        location = string.IsNullOrEmpty(locationStr) ? "N/A" : locationStr,
                        phone = f.Profile?.Phone ?? "N/A",
                        registrationDate = f.Profile?.CreatedOn.ToString("yyyy-MM-dd") ?? "N/A",
                        status = f.Profile != null ? (f.Profile.IsPremiumMember ? "Premium" : (f.Profile.IsComplete ? "Active" : "Pending")) : "N/A",
                        followUpType = f.FollowUpType.ToString(),
                        followUpTypeVal = (int)f.FollowUpType,
                        latestContactType = f.LatestContactType?.ToString() ?? "None",
                        latestCallStatus = f.LatestCallStatus?.ToString() ?? "None",
                        latestPremiumInterestStatus = f.LatestInterestStatus?.ToString() ?? "None",
                        latestProfileVerificationStatus = f.LatestProfileVerificationStatus?.ToString() ?? "None",
                        latestRenewalInterestStatus = f.LatestRenewalInterestStatus?.ToString() ?? "None",
                        latestRemarks = f.LatestRemarks ?? string.Empty,
                        nextFollowUpDate = f.NextFollowUpDate?.ToString("yyyy-MM-dd") ?? "N/A",
                        lastUpdateDate = f.ModifiedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                        adminApprovalStatus = f.LatestAdminApprovalStatus?.ToString() ?? "None",
                        isAdminApproved = f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved,
                        previousPackage = packageName,
                        membershipStatus = membershipStatus,
                        expiryDate = expiryDate ?? "N/A",
                        remainingCredits = remainingCredits,
                        daysLeft = daysLeft,
                        documentVerificationEnabled = f.Profile?.DocumentVerificationEnabled ?? false,
                        verificationDocumentUrl = f.Profile?.VerificationDocumentUrl,
                        verificationDocuments = userDocs != null ? (object)userDocs : Array.Empty<object>(),
                        documentVerificationComplete = f.Profile?.DocumentVerificationComplete ?? false,
                        documentVerificationRejected = f.Profile?.DocumentVerificationRejected ?? false
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = data,
                    message = "Follow-ups fetched successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching follow-ups for staff ID: {StaffId}", staffId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        /// <summary>
        /// Registers a new profile by a staff member (Step 1 and verification setup).
        /// </summary>
        [HttpPost("register-profile")]
        public async Task<IActionResult> RegisterProfile([FromBody] RegistrationDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input details." });
                }

                if (string.IsNullOrEmpty(model.Email))
                {
                    return BadRequest(new { success = false, message = "Please provide an email address." });
                }

                // 1. Validate email and phone uniqueness
                var emailValidation = await _registrationRepo.WhereActive(x => x.Email == model.Email && x.Email != null && x.IsVerified);
                if (emailValidation.Any())
                {
                    return BadRequest(new { success = false, message = "Email address already exists." });
                }

                var phoneValidation = await _registrationRepo.WhereActive(x => x.Phone == model.Phone && x.Phone != null && x.IsVerified);
                if (phoneValidation.Any())
                {
                    return BadRequest(new { success = false, message = "Phone number already exists." });
                }

                // 2. Set defaults
                model.DOB = !string.IsNullOrEmpty(model.DOB) ? model.DOB : "01/01/1990";
                model.BodyTypeId = 0;
                model.CasteId = 0;
                model.CommunityId = 0;
                model.ComplexionId = 0;
                model.FinancialStatusId = 0;
                model.HeightId = 0;
                model.MaritalStatusId = 0;
                model.MotherTongueId = 0;
                model.NationalityId = 0;
                model.ProfessionId = 0;
                model.ReligionId = 0;
                model.IsPhysicallyChallenged = false;
                model.IsComplete = false;
                model.IsSpecialRequest = false;
                model.IsActive = true;
                model.IsVerified = false;
                model.IsPremiumMember = false;
                model.ShowOnHomePage = false;
                model.Source = "StaffWebsite";
                model.StaffCreated = true;

                // Resolve staff ID from claims if logged in, otherwise from request body
                var staffIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (long.TryParse(staffIdClaim, out long parsedStaffId))
                {
                    model.StaffId = parsedStaffId;
                }

                // 3. Generate verification OTP
                Random rdm = new Random();
                string pin = rdm.Next(111111, 999999).ToString();
                // string pin = "123456";
                model.VerificationCode = pin;
                model.OtpGeneratedAt = DateTime.Now;
                model.OtpResendCount = 0;


                // 4. Encrypt/Hash Password
                RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
                if (!string.IsNullOrEmpty(model.Password))
                {
                    model.PasswordHash = encryption.CreateSalt();
                    model.Password = encryption.EncryptRijndael(model.Password, model.PasswordHash);
                }

                var entity = _mapper.Map<Registration>(model);
                await _registrationRepo.Add(entity);
                await _registrationRepo.SaveChanges();

                // 5. Build/Save RegisterNumber (e.g. M400...)
                if (entity.Id.ToString().Length > 0)
                {
                    if (6 - entity.Id.ToString().Length == 5)
                    {
                        entity.RegisterNumber = "M400000" + (entity.Id).ToString();
                    }
                    else if (6 - entity.Id.ToString().Length == 4)
                    {
                        entity.RegisterNumber = "M40000" + (entity.Id).ToString();
                    }
                    else if (6 - entity.Id.ToString().Length == 3)
                    {
                        entity.RegisterNumber = "M4000" + (entity.Id).ToString();
                    }
                    else if (6 - entity.Id.ToString().Length == 2)
                    {
                        entity.RegisterNumber = "M400" + (entity.Id).ToString();
                    }
                    else if (6 - entity.Id.ToString().Length == 1)
                    {
                        entity.RegisterNumber = "M40" + (entity.Id).ToString();
                    }
                    await _registrationRepo.Update(entity);
                    await _registrationRepo.SaveChanges();
                }

                // 6. Send OTP (SMS and Email)
                await _emailService.SendSmsAsync(entity.VerificationCode, entity.Phone);
                try
                {
                    var htmlContent = $"Dear Customer, <br/><br/>{pin} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
                    var htmlContentView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                    _emailNotificationHelper.SendEmail(entity.Email, htmlContentView, "Your OTP for M4nikah");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email verification OTP for staff-created profile {Email}", entity.Email);
                }



                return Ok(new
                {
                    success = true,
                    message = "Profile registered successfully. Verification code sent.",
                    data = new
                    {
                        id = entity.Id,
                        registrationNo = entity.RegisterNumber,
                        email = entity.Email,
                        phone = entity.Phone
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during staff profile registration.");
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request.", error = ex.Message });
            }
        }

        /// <summary>
        /// Registers or updates a profile completely by a staff member (all steps). 
        /// </summary>
        [HttpPost("api-register")]
        public async Task<IActionResult> ApiRegister([FromForm] UserController.MobileProfileDto mobileModel)
        {
            _logger.LogWarning("ApiRegister (Staff) called. Content-Type: {ContentType}", Request.ContentType);

            // Support both JSON (from mobile app/web API) and Form Data
            if (Request.ContentType != null && Request.ContentType.Contains("application/json"))
            {
                try
                {
                    using var reader = new System.IO.StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    _logger.LogWarning("ApiRegister (Staff) JSON Body: {Body}", body);

                    var jsonModel = System.Text.Json.JsonSerializer.Deserialize<UserController.MobileProfileDto>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (jsonModel != null)
                    {
                        mobileModel = jsonModel;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize JSON in ApiRegister (Staff)");
                }
            }
            else
            {
                // Log Form Data keys
                try
                {
                    var formKeys = string.Join(", ", Request.Form.Keys.Select(k => $"{k}={Request.Form[k]}"));
                    _logger.LogWarning("ApiRegister (Staff) Form Data: {FormKeys}", formKeys);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read form data in ApiRegister (Staff)");
                }
            }

            if (mobileModel == null)
            {
                return Ok(new { success = false, error = "Please provide valid registration details." });
            }

            // Resolve staff ID from claims
            var staffIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            long? parsedStaffId = null;
            if (long.TryParse(staffIdClaim, out long staffIdVal))
            {
                parsedStaffId = staffIdVal;
            }

            if (mobileModel.Id <= 0)
            {
                if (string.IsNullOrEmpty(mobileModel.Email))
                {
                    return Ok(new { success = false, error = "Please provide valid registration details." });
                }

                try
                {
                    var existing = await _registrationRepo.WhereActive(x => x.Email == mobileModel.Email && x.Email != null && x.IsVerified);
                    if (existing.Count() > 0)
                    {
                        return Ok(new { success = false, error = "Email address already exists" });
                    }

                    var existingPhone = await _registrationRepo.WhereActive(x => x.Phone == mobileModel.Phone && x.Phone != null && x.IsVerified);
                    if (existingPhone.Count() > 0)
                    {
                        return Ok(new { success = false, error = "Phone number already exists" });
                    }

                    // Map to RegistrationDto
                    var model = MapMobileProfileToRegistrationDto(mobileModel);
                    model.Source = "StaffWebsite";
                    model.StaffCreated = true;
                    model.StaffId = parsedStaffId;

                    // Fetch lookups to populate the string descriptions (just like PostEnquiry on the website)
                    var ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive());
                    var Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive());
                    var MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive());
                    var BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive());
                    var Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive());
                    var MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive());
                    var Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0));
                    var Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId != 0));
                    var Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive());
                    var Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive());
                    var FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive());

                    model.ProfileFor = model.ProfileForId > 0 ? ProfileFor.FirstOrDefault(x => x.Id == model.ProfileForId)?.Title ?? "" : "";
                    model.Nationality = model.NationalityId > 0 ? Nationalities.FirstOrDefault(x => x.Id == model.NationalityId)?.Title ?? "" : "";
                    model.MaritalStatus = model.MaritalStatusId > 0 ? MaritalStatuses.FirstOrDefault(x => x.Id == model.MaritalStatusId)?.Title ?? "" : "";
                    model.Height = model.HeightId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.HeightId)?.Title ?? "" : "";
                    model.Weight = model.WeightId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.WeightId)?.Title ?? "" : "";
                    model.Complexion = model.ComplexionId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.ComplexionId)?.Title ?? "" : "";
                    model.BodyType = model.BodyTypeId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.BodyTypeId)?.Title ?? "" : "";
                    model.MotherTongue = model.MotherTongueId > 0 ? MotherTongues.FirstOrDefault(x => x.Id == model.MotherTongueId)?.Title ?? "" : "";
                    model.Religion = model.ReligionId > 0 ? Religions.FirstOrDefault(x => x.Id == model.ReligionId)?.Title ?? "" : "";
                    model.Caste = model.CasteId > 0 ? Castes.FirstOrDefault(x => x.Id == model.CasteId)?.Title ?? "" : "";
                    model.Community = model.CommunityId > 0 ? Communities.FirstOrDefault(x => x.Id == model.CommunityId)?.Title ?? "" : "";
                    model.Religiousness = model.ReligiousnessId > 0 ? Religiousnesses.FirstOrDefault(x => x.Id == model.ReligiousnessId)?.Title ?? "" : "";
                    model.FinancialStatus = model.FinancialStatusId > 0 ? FinancialStatuses.FirstOrDefault(x => x.Id == model.FinancialStatusId)?.Title ?? "" : "";

                    RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);

                    // Default initial registration setup
                    model.DOB = !string.IsNullOrEmpty(model.DOB) ? model.DOB : "01/01/1990";
                    model.IsPhysicallyChallenged = mobileModel.Is_Physically_Challenged == 1 || mobileModel.IsPhysicallyChallenged == 1;
                    model.IsComplete = false;
                    model.IsVerified = false;
                    model.IsActive = true;
                    model.IsPremiumMember = false;
                    model.ShowOnHomePage = false;

                    // Generate OTP Verification Code (Testing purpose: hardcoded to 123456)
                    Random rdm = new Random();
                    string pin = rdm.Next(111111, 999999).ToString();
                     //string pin = "123456";
                    model.VerificationCode = pin;
                    model.OtpGeneratedAt = DateTime.Now;
                    model.OtpResendCount = 0;

                    // Password Encryption & Hashing
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        model.PasswordHash = encryption.CreateSalt();
                        model.Password = encryption.EncryptRijndael(model.Password, model.PasswordHash);
                    }

                    // Map to Registration database entity
                    var entity = _mapper.Map<Registration>(model);
                    
                    // Save profile images if any (handling file uploads is supported by _fileService)
                    await _fileService.SaveAllFiles(entity, model, "Uploads/Registration");
                    
                    await _registrationRepo.Add(entity);
                    await _registrationRepo.SaveChanges();

                    // Format generated registration number (e.g. M400...)
                    if (entity.Id.ToString().Length > 0)
                    {
                        if (6 - entity.Id.ToString().Length == 5)
                        {
                            entity.RegisterNumber = "M400000" + (entity.Id).ToString();
                        }
                        else if (6 - entity.Id.ToString().Length == 4)
                        {
                            entity.RegisterNumber = "M40000" + (entity.Id).ToString();
                        }
                        else if (6 - entity.Id.ToString().Length == 3)
                        {
                            entity.RegisterNumber = "M4000" + (entity.Id).ToString();
                        }
                        else if (6 - entity.Id.ToString().Length == 2)
                        {
                            entity.RegisterNumber = "M400" + (entity.Id).ToString();
                        }
                        else if (6 - entity.Id.ToString().Length == 1)
                        {
                            entity.RegisterNumber = "M40" + (entity.Id).ToString();
                        }
                        await _registrationRepo.Update(entity);
                        await _registrationRepo.SaveChanges();
                    }

                    // Send OTP via SMS and Email immediately (just like Step 1)
                    await _emailService.SendSmsAsync(entity.VerificationCode, entity.Phone);
                    try
                    {
                        var htmlContent = $"Dear Customer, <br/><br/>{pin} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
                        var htmlContentView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                        _logger.LogWarning("API (Staff) sent OTP Via Email Starts for ID: {Id}", entity.Id);
                        _emailNotificationHelper.SendEmail(entity.Email, htmlContentView, "Your OTP for M4nikah");
                        _logger.LogWarning("API (Staff) sent OTP Via Email Ends for ID: {Id}", entity.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"API (Staff) Email OTP send failed: {ex.Message}");
                    }


                    // Generate Auth JWT Token for plug-and-play login capability
                    var token = _cookieHelper.GenerateJwtToken(entity.Id, entity.Email);
                    
                    // Map the saved data back to mobile profile format
                    model.Id = entity.Id;
                    model.RegisterNumber = entity.RegisterNumber;
                    var savedMobileProfile = MapRegistrationDtoToMobileProfile(model);

                    // Add token inside the data object for integration
                    var dataObj = new
                    {
                        token = token,
                        id = entity.Id,
                        email = entity.Email,
                        name = entity.Name,
                        register_Number = entity.RegisterNumber,
                        profile = savedMobileProfile
                    };

                    return Ok(new
                    {
                        success = true,
                        status = "Success",
                        token = token,
                        userId = entity.Id,
                        registrationNo = entity.RegisterNumber,
                        verificationCode = entity.VerificationCode,
                        data = dataObj
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API (Staff) registration save failed");
                    return Ok(new { success = false, error = "An error occurred during registration: " + ex.Message });
                }
            }
            else
            {
                try
                {
                    var entity = await _registrationRepo.Get(mobileModel.Id);
                    if (entity == null)
                    {
                        return Ok(new { success = false, error = "Registration session not found." });
                    }

                    // Only check email and phone uniqueness if on Step-1 (where these fields are entered/modified) or if completed step is not specified.
                    if (mobileModel.CompletedStep == "Step-1" || mobileModel.Completed_Step == "Step-1" || string.IsNullOrEmpty(mobileModel.CompletedStep))
                    {
                        var existing = await _registrationRepo.WhereActive(x => x.Email == mobileModel.Email && x.Email != null && x.Id != entity.Id && x.IsVerified);
                        if (existing.Count() > 0)
                        {
                            return Ok(new { success = false, error = "Email address already exists" });
                        }

                        var existingPhone = await _registrationRepo.WhereActive(x => x.Phone == mobileModel.Phone && x.Phone != null && x.Id != entity.Id && x.IsVerified);
                        if (existingPhone.Count() > 0)
                        {
                            return Ok(new { success = false, error = "Phone number already exists" });
                        }
                    }

                    // Map to RegistrationDto
                    var model = MapMobileProfileToRegistrationDto(mobileModel);
                    model.Source = string.IsNullOrEmpty(entity.Source) ? "StaffWebsite" : entity.Source;
                    model.StaffCreated = true;
                    if (!model.StaffId.HasValue || model.StaffId <= 0)
                    {
                        model.StaffId = entity.StaffId ?? parsedStaffId;
                    }

                    RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);

                    if (model.CompletedStep == "Step-1")
                    {
                        _logger.LogWarning("API (Staff) re-entering Step-1 for user with ID: {Id}", model.Id);
                        Random rdm = new Random();
                        string pin = rdm.Next(111111, 999999).ToString();
                        // string pin = "123456";
                        model.VerificationCode = pin;
                        model.OtpGeneratedAt = DateTime.Now;
                        model.OtpResendCount = 0;

                        // Send OTP via SMS
                        await _emailService.SendSmsAsync(model.VerificationCode, model.Phone);

                        // Send OTP via Email
                        try
                        {
                            var htmlContent = $"Dear Customer, <br/><br/>{pin} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
                            var htmlContentView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                            _emailNotificationHelper.SendEmail(model.Email, htmlContentView, "Your OTP for M4nikah");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "API (Staff) OTP send via Email failed");
                        }

                        // Re-hash Password if changed
                        if (!string.IsNullOrEmpty(model.Password) && model.Password != entity.Password)
                        {
                            model.PasswordHash = encryption.CreateSalt();
                            model.Password = encryption.EncryptRijndael(model.Password, model.PasswordHash);
                        }
                        else
                        {
                            model.Password = entity.Password;
                            model.PasswordHash = entity.PasswordHash;
                        }
                    }
                    else
                    {
                        model.VerificationCode = entity.VerificationCode;
                        model.OtpGeneratedAt = entity.OtpGeneratedAt;
                        model.OtpResendCount = entity.OtpResendCount;
                        model.Password = entity.Password;
                        model.PasswordHash = entity.PasswordHash;
                    }

                    // Fetch lookups to populate the string descriptions (just like PostEnquiry on the website)
                    var ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive());
                    var Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive());
                    var MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive());
                    var BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive());
                    var Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive());
                    var MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive());
                    var Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0));
                    var Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId != 0));
                    var Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive());
                    var Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive());
                    var FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive());

                    // Correct lookup IDs if they are not provided (use existing values from entity)
                    model.Phone = string.IsNullOrEmpty(model.Phone) ? entity.Phone : model.Phone;
                    model.Email = string.IsNullOrEmpty(model.Email) ? entity.Email : model.Email;
                    model.Name = string.IsNullOrEmpty(model.Name) ? entity.Name : model.Name;
                    model.Gender = string.IsNullOrEmpty(model.Gender) ? entity.Gender : model.Gender;
                    model.ProfileForId = model.ProfileForId <= 0 ? entity.ProfileForId : model.ProfileForId;
                    model.NationalityId = model.NationalityId <= 0 ? entity.NationalityId : model.NationalityId;
                    model.MaritalStatusId = model.MaritalStatusId <= 0 ? entity.MaritalStatusId : model.MaritalStatusId;
                    model.HeightId = model.HeightId <= 0 ? entity.HeightId : model.HeightId;
                    model.WeightId = model.WeightId <= 0 ? entity.WeightId : model.WeightId;
                    model.ComplexionId = model.ComplexionId <= 0 ? entity.ComplexionId : model.ComplexionId;
                    model.BodyTypeId = model.BodyTypeId <= 0 ? entity.BodyTypeId : model.BodyTypeId;
                    model.ProfessionId = model.ProfessionId <= 0 ? entity.ProfessionId : model.ProfessionId;
                    model.MotherTongueId = model.MotherTongueId <= 0 ? entity.MotherTongueId : model.MotherTongueId;
                    model.ReligionId = model.ReligionId <= 0 ? entity.ReligionId : model.ReligionId;
                    model.CasteId = model.CasteId <= 0 ? entity.CasteId : model.CasteId;
                    model.CommunityId = model.CommunityId <= 0 ? entity.CommunityId : model.CommunityId;
                    model.ReligiousnessId = model.ReligiousnessId <= 0 ? entity.ReligiousnessId : model.ReligiousnessId;
                    model.FinancialStatusId = model.FinancialStatusId <= 0 ? entity.FinancialStatusId : model.FinancialStatusId;

                    // Correct string fields if they are not provided (use existing values from entity)
                    model.Name = !string.IsNullOrEmpty(model.Name) ? model.Name : entity.Name;
                    model.Gender = !string.IsNullOrEmpty(model.Gender) ? model.Gender : entity.Gender;
                    model.DOB = !string.IsNullOrEmpty(mobileModel.DOB) ? mobileModel.DOB : entity.DOB;
                    model.Phone = !string.IsNullOrEmpty(model.Phone) ? model.Phone : entity.Phone;
                    model.LandlineNumber = !string.IsNullOrEmpty(model.LandlineNumber) ? model.LandlineNumber : entity.LandlineNumber;
                    model.HighestEducation = !string.IsNullOrEmpty(model.HighestEducation) ? model.HighestEducation : entity.HighestEducation;
                    model.ProfessionType = !string.IsNullOrEmpty(model.ProfessionType) ? model.ProfessionType : entity.ProfessionType;
                    model.FamilyName = !string.IsNullOrEmpty(model.FamilyName) ? model.FamilyName : entity.FamilyName;
                    model.FatherName = !string.IsNullOrEmpty(model.FatherName) ? model.FatherName : entity.FatherName;
                    model.Post = !string.IsNullOrEmpty(model.Post) ? model.Post : entity.Post;
                    model.Village = !string.IsNullOrEmpty(model.Village) ? model.Village : entity.Village;
                    model.PinCode = !string.IsNullOrEmpty(model.PinCode) ? model.PinCode : entity.PinCode;
                    model.Country = !string.IsNullOrEmpty(model.Country) ? model.Country : entity.Country;
                    model.State = !string.IsNullOrEmpty(model.State) ? model.State : entity.State;
                    model.District = !string.IsNullOrEmpty(model.District) ? model.District : entity.District;
                    model.PresentCountry = !string.IsNullOrEmpty(model.PresentCountry) ? model.PresentCountry : entity.PresentCountry;
                    model.PresentState = !string.IsNullOrEmpty(model.PresentState) ? model.PresentState : entity.PresentState;
                    model.PresentDistrict = !string.IsNullOrEmpty(model.PresentDistrict) ? model.PresentDistrict : entity.PresentDistrict;
                    model.PresentCity = !string.IsNullOrEmpty(model.PresentCity) ? model.PresentCity : entity.PresentCity;
                    model.ImagePath = !string.IsNullOrEmpty(model.ImagePath) ? model.ImagePath : entity.ImagePath;
                    model.About = !string.IsNullOrEmpty(model.About) ? model.About : entity.About;
                    model.EducationType = !string.IsNullOrEmpty(model.EducationType) ? model.EducationType : entity.EducationType;
                    model.Email = !string.IsNullOrEmpty(model.Email) ? model.Email : entity.Email;
                    model.CompletedStep = !string.IsNullOrEmpty(model.CompletedStep) ? model.CompletedStep : entity.CompletedStep;
                    model.CountryCode = !string.IsNullOrEmpty(model.CountryCode) ? model.CountryCode : entity.CountryCode;
                    model.SecondaryCountryCode = !string.IsNullOrEmpty(model.SecondaryCountryCode) ? model.SecondaryCountryCode : entity.SecondaryCountryCode;
                    model.PhysicallyChallengedDetail = !string.IsNullOrEmpty(model.PhysicallyChallengedDetail) ? model.PhysicallyChallengedDetail : entity.PhysicallyChallengedDetail;

                    // Correct nullable fields
                    model.NumberOfChildrens = mobileModel.NumberOfChildrens ?? entity.NumberOfChildrens;

                    var physChallenged = mobileModel.Is_Physically_Challenged ?? mobileModel.IsPhysicallyChallenged;
                    if (physChallenged.HasValue)
                    {
                        model.IsPhysicallyChallenged = physChallenged.Value == 1;
                    }
                    else
                    {
                        model.IsPhysicallyChallenged = entity.IsPhysicallyChallenged;
                    }

                    // Correct photo privacy toggles
                    var photoAll = mobileModel.PhotoVisibleToAll ?? mobileModel.Photo_Visible_To_All;
                    if (photoAll.HasValue)
                    {
                        model.PhotoVisibleToAll = photoAll.Value;
                    }
                    else
                    {
                        model.PhotoVisibleToAll = entity.PhotoVisibleToAll;
                    }

                    var photoPremium = mobileModel.PhotoVisibleToPremium ?? mobileModel.Photo_Visible_To_Premium;
                    if (photoPremium.HasValue)
                    {
                        model.PhotoVisibleToPremium = photoPremium.Value;
                    }
                    else
                    {
                        model.PhotoVisibleToPremium = entity.PhotoVisibleToPremium;
                    }

                    var photoAccepted = mobileModel.PhotoVisibleToAccepted ?? mobileModel.Photo_Visible_To_Accepted;
                    if (photoAccepted.HasValue)
                    {
                        model.PhotoVisibleToAccepted = photoAccepted.Value;
                    }
                    else
                    {
                        model.PhotoVisibleToAccepted = entity.PhotoVisibleToAccepted;
                    }

                    // Map textual lookup strings
                    model.ProfileFor = model.ProfileForId > 0 ? ProfileFor.FirstOrDefault(x => x.Id == model.ProfileForId)?.Title ?? "" : "";
                    model.Nationality = model.NationalityId > 0 ? Nationalities.FirstOrDefault(x => x.Id == model.NationalityId)?.Title ?? "" : "";
                    model.MaritalStatus = model.MaritalStatusId > 0 ? MaritalStatuses.FirstOrDefault(x => x.Id == model.MaritalStatusId)?.Title ?? "" : "";
                    model.Height = model.HeightId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.HeightId)?.Title ?? "" : "";
                    model.Weight = model.WeightId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.WeightId)?.Title ?? "" : "";
                    model.Complexion = model.ComplexionId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.ComplexionId)?.Title ?? "" : "";
                    model.BodyType = model.BodyTypeId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.BodyTypeId)?.Title ?? "" : "";
                    model.MotherTongue = model.MotherTongueId > 0 ? MotherTongues.FirstOrDefault(x => x.Id == model.MotherTongueId)?.Title ?? "" : "";
                    model.Religion = model.ReligionId > 0 ? Religions.FirstOrDefault(x => x.Id == model.ReligionId)?.Title ?? "" : "";
                    model.Caste = model.CasteId > 0 ? Castes.FirstOrDefault(x => x.Id == model.CasteId)?.Title ?? "" : "";
                    model.Community = model.CommunityId > 0 ? Communities.FirstOrDefault(x => x.Id == model.CommunityId)?.Title ?? "" : "";
                    model.Religiousness = model.ReligiousnessId > 0 ? Religiousnesses.FirstOrDefault(x => x.Id == model.ReligiousnessId)?.Title ?? "" : "";
                    model.FinancialStatus = model.FinancialStatusId > 0 ? FinancialStatuses.FirstOrDefault(x => x.Id == model.FinancialStatusId)?.Title ?? "" : "";

                    model.RegisterNumber = entity.RegisterNumber;
                    model.IsActive = true;
                    model.IsPremiumMember = false;
                    model.ShowOnHomePage = false;

                    // Set verify status based on current completed step
                    model.IsVerified = (model.CompletedStep == "Step-OTP-Verify") || (model.CompletedStep == "Step-5") || entity.IsVerified;
                    model.IsComplete = (model.CompletedStep == "Step-5") || entity.IsComplete;

                    _mapper.Map(model, entity);
                    await _fileService.SaveAllFiles(entity, model, "Uploads/Registration");
                    await _registrationRepo.Update(entity);
                    await _registrationRepo.SaveChanges();

                    if ((model.CompletedStep == "Step-5") && (model.IsVerified == true))
                    {
                        try
                        {
                            await SendRegistrationSuccessEmail(model.Email, model.Name, model.RegisterNumber);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "API (Staff) registration success email sending failed");
                        }
                    }
                    if ((model.CompletedStep == "Step-5") && (model.IsVerified == false))
                    {
                        try
                        {
                            await SendRegistrationUnsuccessfulEmail(model.Email, model.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "API (Staff) registration unsuccess email sending failed");
                        }
                    }

                    // Ensure StaffProfileAssignment and Follow-ups are created ONLY IF the profile is verified
                    var targetStaffId = parsedStaffId ?? entity.StaffId;
                    if (targetStaffId.HasValue && entity.IsVerified)
                    {
                        if (!entity.StaffId.HasValue)
                        {
                            entity.StaffId = targetStaffId;
                            await _registrationRepo.Update(entity);
                            await _registrationRepo.SaveChanges();
                        }
                        var existingAssignment = await _assignmentRepo.FirstOrDefaultActive(a => a.ProfileId == entity.Id && a.StaffId == targetStaffId.Value);
                        if (existingAssignment == null)
                        {
                            var assignment = new StaffProfileAssignment
                            {
                                StaffId = targetStaffId.Value,
                                ProfileId = entity.Id
                            };
                            await _assignmentRepo.Add(assignment);
                            await _assignmentRepo.SaveChanges();
                        }

                        // Auto-generate Profile Verification and Premium Follow-ups
                        await CreateOrUpdateStaffFollowUpsAsync(entity.Id, targetStaffId.Value);
                    }

                    // Generate Auth JWT Token for plug-and-play login capability
                    var token = _cookieHelper.GenerateJwtToken(entity.Id, entity.Email);
                    
                    // Map the saved data back to mobile profile format
                    model.Id = entity.Id;
                    model.RegisterNumber = entity.RegisterNumber;
                    var savedMobileProfile = MapRegistrationDtoToMobileProfile(model);

                    // Add token inside the data object for integration
                    var dataObj = new
                    {
                        token = token,
                        id = entity.Id,
                        email = entity.Email,
                        name = entity.Name,
                        register_Number = entity.RegisterNumber,
                        profile = savedMobileProfile
                    };

                    return Ok(new
                    {
                        success = true,
                        status = "Success",
                        token = token,
                        userId = entity.Id,
                        registrationNo = entity.RegisterNumber,
                        verificationCode = entity.VerificationCode,
                        data = dataObj
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API (Staff) registration update failed");
                    return Ok(new { success = false, error = "An error occurred during registration update: " + ex.Message });
                }
            }
        }

        /// <summary>
        /// Verifies registration OTP for a profile, marks it as verified, assigns to staff, and creates follow-ups.
        /// </summary>
        [HttpPost("verify-registration-otp")]
        public async Task<IActionResult> VerifyRegistrationOtp([FromBody] VerifyRegistrationOtpRequest model)
        {
            try
            {
                if (model == null || model.ProfileId <= 0 || string.IsNullOrWhiteSpace(model.Otp))
                {
                    return BadRequest(new { success = false, message = "Invalid profile ID or OTP." });
                }

                var entity = await _registrationRepo.Get(model.ProfileId);
                if (entity == null || entity.IsDeleted)
                {
                    return NotFound(new { success = false, message = "Registration profile not found." });
                }

                if (entity.OtpGeneratedAt.HasValue)
                {
                    var expiryTime = entity.OtpGeneratedAt.Value.AddMinutes(10);
                    if (DateTime.Now > expiryTime)
                    {
                        return BadRequest(new { success = false, message = "OTP has expired. Please request a new code." });
                    }
                }

                if (entity.VerificationCode != model.Otp.Trim())
                {
                    return BadRequest(new { success = false, message = "Invalid verification code. Please try again." });
                }

                var staffIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                long? parsedStaffId = null;
                if (long.TryParse(staffIdClaim, out long staffIdVal))
                {
                    parsedStaffId = staffIdVal;
                }
                var targetStaffId = parsedStaffId ?? entity.StaffId;

                entity.IsVerified = true;
                entity.CompletedStep = "Step-OTP-Verify";
                if (targetStaffId.HasValue && !entity.StaffId.HasValue)
                {
                    entity.StaffId = targetStaffId;
                }
                await _registrationRepo.Update(entity);
                await _registrationRepo.SaveChanges();

                if (targetStaffId.HasValue)
                {
                    var existingAssignment = await _assignmentRepo.FirstOrDefaultActive(a => a.ProfileId == entity.Id && a.StaffId == targetStaffId.Value);
                    if (existingAssignment == null)
                    {
                        var assignment = new StaffProfileAssignment
                        {
                            StaffId = targetStaffId.Value,
                            ProfileId = entity.Id
                        };
                        await _assignmentRepo.Add(assignment);
                        await _assignmentRepo.SaveChanges();
                    }

                    await CreateOrUpdateStaffFollowUpsAsync(entity.Id, targetStaffId.Value);
                }

                return Ok(new
                {
                    success = true,
                    message = "OTP verified successfully. Profile assigned and follow-ups initialized.",
                    data = new
                    {
                        profileId = entity.Id,
                        isVerified = entity.IsVerified,
                        staffId = targetStaffId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying registration OTP for profile ID: {ProfileId}", model?.ProfileId);
                return StatusCode(500, new { success = false, message = "An error occurred while verifying OTP." });
            }
        }

        /// <summary>
        /// Fetches profiles that were created by a specific staff member.
        /// </summary>
        [HttpGet("created-profiles/{staffId:long}")]
        public async Task<IActionResult> GetCreatedProfilesByStaffId(long staffId)
        {
            try
            {
                var profiles = await _registrationRepo.WhereActive(p => p.StaffCreated == true && p.StaffId == staffId && p.IsVerified);

                var profileIds = profiles.Select(p => p.Id).Distinct().ToList();
                var docsList = await _dbContext.VerificationDocuments
                    .Where(d => profileIds.Contains(d.ProfileId) && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToListAsync();

                var docsDict = docsList
                    .GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    }).ToList());

                var data = profiles
                    .OrderByDescending(p => p.CreatedOn)
                    .ThenByDescending(p => p.Id)
                    .Select(p =>
                    {
                        var locationParts = new List<string>();
                        if (!string.IsNullOrEmpty(p.Village)) locationParts.Add(p.Village);
                        if (!string.IsNullOrEmpty(p.District)) locationParts.Add(p.District);
                        if (!string.IsNullOrEmpty(p.State)) locationParts.Add(p.State);
                        var locationStr = string.Join(", ", locationParts);

                        docsDict.TryGetValue(p.Id, out var userDocs);

                        return new
                        {
                            profileId = p.Id,
                            registerNumber = p.RegisterNumber ?? string.Empty,
                            name = p.Name ?? string.Empty,
                            gender = p.Gender ?? string.Empty,
                            age = CalculateAge(p.DOB),
                            phone = p.Phone ?? string.Empty,
                            email = p.Email ?? string.Empty,
                            imagePath = p.ImagePath ?? string.Empty,
                            location = locationStr,
                            isPremiumMember = p.IsPremiumMember,
                            isComplete = p.IsComplete,
                            isVerified = p.IsVerified,
                            createdOn = p.CreatedOn,
                            documentVerificationEnabled = p.DocumentVerificationEnabled,
                            verificationDocumentUrl = p.VerificationDocumentUrl ?? userDocs?.FirstOrDefault()?.documentUrl,
                            verificationDocuments = userDocs != null ? (object)userDocs : Array.Empty<object>(),
                            documentVerificationComplete = p.DocumentVerificationComplete,
                            documentVerificationRejected = p.DocumentVerificationRejected
                        };
                    })
                    .ToList();

                _logger.LogInformation("Fetched profiles created by staff member (ID: {StaffId}). Total count: {Count}", staffId, data.Count);

                return Ok(new
                {
                    success = true,
                    data = data,
                    message = $"Profiles created by staff ID {staffId} fetched successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching profiles created by staff ID: {StaffId}", staffId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        /// <summary>
        /// Checks if a transaction ID is already used in Transactions or pending FollowUps.
        /// </summary>
        [HttpGet("check-txnid-availability")]
        public async Task<IActionResult> CheckTxnIdAvailability([FromQuery] string txnId, [FromQuery] long? followUpId = null)
        {
            if (string.IsNullOrWhiteSpace(txnId))
            {
                return BadRequest(new { available = false, message = "Transaction ID cannot be empty." });
            }

            var cleanTxnId = txnId.Trim();
            bool isUsedInTxn = await _dbContext.Transaction.AnyAsync(t => t.TxnId != null && t.TxnId.ToLower() == cleanTxnId.ToLower());
            if (isUsedInTxn)
            {
                return Ok(new { available = false, message = "This Transaction ID is already used for another transaction." });
            }

            var followUpQuery = _dbContext.FollowUps.Where(f => !f.IsDeleted && f.TransactionId != null && f.TransactionId.ToLower() == cleanTxnId.ToLower());
            if (followUpId.HasValue && followUpId.Value > 0)
            {
                followUpQuery = followUpQuery.Where(f => f.Id != followUpId.Value);
            }

            bool isUsedInFollowUp = await followUpQuery.AnyAsync();
            if (isUsedInFollowUp)
            {
                return Ok(new { available = false, message = "This Transaction ID is already submitted in a staff follow-up payment." });
            }

            return Ok(new { available = true, message = "Transaction ID is available." });
        }

        /// <summary>
        /// Logs / updates a follow-up call/interaction.
        /// </summary>
        [HttpPost("update-followup")]
        public async Task<IActionResult> UpdateFollowUp([FromBody] UpdateFollowUpModel model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Invalid data." });
            }

            try
            {
                var followUp = await _followUpRepo.Get(model.FollowUpId);
                if (followUp == null || followUp.IsDeleted)
                {
                    return NotFound(new { success = false, message = "Follow-up not found." });
                }

                // Get currently logged-in staff member
                long staffId = 0;
                string staffName = "Unknown Staff";

                if (model.StaffId.HasValue && model.StaffId.Value > 0)
                {
                    staffId = model.StaffId.Value;
                    var staffUser = await _userManager.FindByIdAsync(staffId.ToString());
                    if (staffUser != null)
                    {
                        staffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Unknown Staff";
                    }
                }
                else
                {
                    var loggedInUser = await _userManager.GetUserAsync(User);
                    staffId = loggedInUser?.Id ?? 0;
                    staffName = loggedInUser?.NormalizedUserName ?? loggedInUser?.UserName ?? "Admin";
                }

                // Create new timeline interaction entry
                var timeline = new FollowUpTimeline
                {
                    FollowUpId = followUp.Id,
                    StaffId = staffId,
                    StaffName = staffName,
                    ContactType = model.ContactType,
                    CallStatus = model.CallStatus,
                    Remarks = model.Remarks,
                    NextFollowUpDate = model.NextFollowUpDate,
                    IsActive = true
                };

                // Update latest follow-up fields
                followUp.LatestContactType = model.ContactType;
                followUp.LatestCallStatus = model.CallStatus;
                followUp.LatestRemarks = model.Remarks;
                followUp.NextFollowUpDate = model.NextFollowUpDate;

                bool hasReachedEndStatus = false;

                // Map type-specific statuses
                if (followUp.FollowUpType == FollowUpType.ProfileVerification)
                {
                    if (model.ProfileVerificationStatus == null)
                    {
                        return BadRequest(new { success = false, message = "Profile verification status is required." });
                    }
                    timeline.ProfileVerificationStatus = model.ProfileVerificationStatus;
                    followUp.LatestProfileVerificationStatus = model.ProfileVerificationStatus;

                    if (model.ProfileVerificationStatus == ProfileVerificationStatus.DetailedVerifyRequest)
                    {
                        var userProfile = await _registrationRepo.Get(followUp.ProfileId);
                        if (userProfile != null)
                        {
                            userProfile.DocumentVerificationEnabled = true;
                            await _registrationRepo.Update(userProfile);
                            await _registrationRepo.SaveChanges();
                        }
                        hasReachedEndStatus = false;
                    }
                    else if (model.ProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify)
                    {
                        var userProfile = await _registrationRepo.Get(followUp.ProfileId);
                        if (userProfile != null)
                        {
                            userProfile.DocumentVerificationEnabled = true;
                            if (model.DocumentVerificationComplete.HasValue)
                            {
                                userProfile.DocumentVerificationComplete = model.DocumentVerificationComplete.Value;
                                userProfile.DocumentVerificationRejected = !model.DocumentVerificationComplete.Value;
                                hasReachedEndStatus = true;
                            }
                            await _registrationRepo.Update(userProfile);
                            await _registrationRepo.SaveChanges();
                        }
                    }
                    else if (model.ProfileVerificationStatus == ProfileVerificationStatus.Verify ||
                        model.ProfileVerificationStatus == ProfileVerificationStatus.Dismissed ||
                        model.ProfileVerificationStatus == ProfileVerificationStatus.Suspended)
                    {
                        hasReachedEndStatus = true;
                    }
                }
                else if (followUp.FollowUpType == FollowUpType.PremiumFollowUp)
                {
                    if (model.PremiumInterestStatus == null)
                    {
                        return BadRequest(new { success = false, message = "Premium interest status is required." });
                    }
                    timeline.InterestStatus = model.PremiumInterestStatus;
                    followUp.LatestInterestStatus = model.PremiumInterestStatus;

                    if (model.PremiumInterestStatus == PremiumInterestStatus.Converted)
                    {
                        if (string.Equals(model.PaymentMode, "Offline", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(model.TransactionId))
                            {
                                return BadRequest(new { success = false, message = "Transaction ID is required for offline payment." });
                            }

                            var cleanTxnId = model.TransactionId.Trim();
                            bool isUsedInTxn = await _dbContext.Transaction.AnyAsync(t => t.TxnId != null && t.TxnId.ToLower() == cleanTxnId.ToLower());
                            bool isUsedInFollowUp = await _dbContext.FollowUps.AnyAsync(f => !f.IsDeleted && f.Id != followUp.Id && f.TransactionId != null && f.TransactionId.ToLower() == cleanTxnId.ToLower());

                            if (isUsedInTxn || isUsedInFollowUp)
                            {
                                return BadRequest(new { success = false, message = "This Transaction ID is already used for another transaction. Please enter a unique Transaction ID." });
                            }

                            followUp.PaymentMode = "Offline";
                            followUp.OfflinePaymentType = model.OfflinePaymentType;
                            followUp.TransactionId = cleanTxnId;
                            followUp.PaymentAmount = model.PaymentAmount;
                            followUp.PaymentCompleted = false;
                            followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Pending;

                            var adminApproval = new FollowUpAdminApproval
                            {
                                FollowUpId = followUp.Id,
                                Status = AdminApprovalStatus.Pending,
                                Remarks = $"Pending admin review for offline payment ({staffName}). TxnId: {cleanTxnId}, Type: {model.OfflinePaymentType}, Amount: {model.PaymentAmount}. Remarks: {model.Remarks}",
                                ActionDate = null,
                                ActionBy = null,
                                IsActive = true
                            };
                            await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                            await _dbContext.SaveChangesAsync();

                            timeline.Remarks = string.IsNullOrEmpty(timeline.Remarks)
                                ? $"Offline payment details submitted (Txn: {cleanTxnId}, Amount: {model.PaymentAmount})"
                                : $"{timeline.Remarks} [Offline Payment: {cleanTxnId}, Amount: {model.PaymentAmount}, Type: {model.OfflinePaymentType}]";
                        }
                        else
                        {
                            // Online Payment (Default)
                            followUp.PaymentMode = "Online";
                            followUp.PaymentLinkSent = true;
                            followUp.PaymentLinkSentAt = DateTime.UtcNow;
                            followUp.PaymentCompleted = false;
                            followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;

                            var adminApproval = new FollowUpAdminApproval
                            {
                                FollowUpId = followUp.Id,
                                Status = AdminApprovalStatus.Approved,
                                Remarks = $"Auto-approved on payment link generation ({staffName}). Remarks: {model.Remarks}",
                                ActionDate = DateTime.UtcNow,
                                ActionBy = staffName,
                                IsActive = true
                            };
                            await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                            await _dbContext.SaveChangesAsync();

                            var userProfile = await _registrationRepo.Get(followUp.ProfileId);
                            if (userProfile != null && !userProfile.IsDeleted)
                            {
                                bool sendEmail = model.SendEmail;
                                bool sendMessage = model.SendMessage;
                                bool sendWhatsApp = model.SendWhatsApp;

                                if (!sendEmail && !sendMessage && !sendWhatsApp)
                                {
                                    sendEmail = true;
                                    sendMessage = true;
                                }

                                string? whatsappUrl = await SendPaymentLinkNotification(userProfile, sendEmail, sendMessage, sendWhatsApp);

                                var sentChannels = new List<string>();
                                if (sendEmail) sentChannels.Add("Email");
                                if (sendMessage) sentChannels.Add("SMS");
                                if (sendWhatsApp) sentChannels.Add("WhatsApp");

                                timeline.Remarks = string.IsNullOrEmpty(timeline.Remarks)
                                    ? $"Online payment link sent via {string.Join(", ", sentChannels)}"
                                    : $"{timeline.Remarks} [Online Payment Link Sent via {string.Join(", ", sentChannels)}]";

                                // Save changes
                                await _followUpTimelineRepo.Add(timeline);
                                await _followUpTimelineRepo.SaveChanges();

                                await _followUpRepo.Update(followUp);
                                await _followUpRepo.SaveChanges();

                                return Ok(new
                                {
                                    success = true,
                                    message = $"Follow-up converted successfully and payment link sent via {string.Join(", ", sentChannels)}.",
                                    whatsappUrl = whatsappUrl
                                });
                            }
                        }
                    }
                    else if (model.PremiumInterestStatus == PremiumInterestStatus.NotInterested)
                    {
                        hasReachedEndStatus = true;
                    }
                    else
                    {
                        followUp.LatestAdminApprovalStatus = null;
                    }
                }
                else if (followUp.FollowUpType == FollowUpType.RenewalFollowUp)
                {
                    if (model.RenewalInterestStatus == null)
                    {
                        return BadRequest(new { success = false, message = "Renewal interest status is required." });
                    }
                    timeline.RenewalInterestStatus = model.RenewalInterestStatus;
                    followUp.LatestRenewalInterestStatus = model.RenewalInterestStatus;

                    if (model.RenewalInterestStatus == RenewalInterestStatus.Renewed)
                    {
                        if (string.Equals(model.PaymentMode, "Offline", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(model.TransactionId))
                            {
                                return BadRequest(new { success = false, message = "Transaction ID is required for offline renewal payment." });
                            }

                            var cleanTxnId = model.TransactionId.Trim();
                            bool isUsedInTxn = await _dbContext.Transaction.AnyAsync(t => t.TxnId != null && t.TxnId.ToLower() == cleanTxnId.ToLower());
                            bool isUsedInFollowUp = await _dbContext.FollowUps.AnyAsync(f => !f.IsDeleted && f.Id != followUp.Id && f.TransactionId != null && f.TransactionId.ToLower() == cleanTxnId.ToLower());

                            if (isUsedInTxn || isUsedInFollowUp)
                            {
                                return BadRequest(new { success = false, message = "This Transaction ID is already used for another transaction. Please enter a unique Transaction ID." });
                            }

                            followUp.PaymentMode = "Offline";
                            followUp.OfflinePaymentType = model.OfflinePaymentType;
                            followUp.TransactionId = cleanTxnId;
                            followUp.PaymentAmount = model.PaymentAmount;
                            followUp.PaymentCompleted = false;
                            followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Pending;

                            var adminApproval = new FollowUpAdminApproval
                            {
                                FollowUpId = followUp.Id,
                                Status = AdminApprovalStatus.Pending,
                                Remarks = $"Pending admin review for offline renewal payment ({staffName}). TxnId: {cleanTxnId}, Type: {model.OfflinePaymentType}, Amount: {model.PaymentAmount}. Remarks: {model.Remarks}",
                                ActionDate = null,
                                ActionBy = null,
                                IsActive = true
                            };
                            await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                            await _dbContext.SaveChangesAsync();

                            timeline.Remarks = string.IsNullOrEmpty(timeline.Remarks)
                                ? $"Offline payment details submitted (Txn: {cleanTxnId}, Amount: {model.PaymentAmount})"
                                : $"{timeline.Remarks} [Offline Payment: {cleanTxnId}, Amount: {model.PaymentAmount}, Type: {model.OfflinePaymentType}]";
                        }
                        else
                        {
                            // Online Payment (Default)
                            followUp.PaymentMode = "Online";
                            followUp.PaymentLinkSent = true;
                            followUp.PaymentLinkSentAt = DateTime.UtcNow;
                            followUp.PaymentCompleted = false;
                            followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;

                            var adminApproval = new FollowUpAdminApproval
                            {
                                FollowUpId = followUp.Id,
                                Status = AdminApprovalStatus.Approved,
                                Remarks = $"Auto-approved on payment link generation ({staffName}). Remarks: {model.Remarks}",
                                ActionDate = DateTime.UtcNow,
                                ActionBy = staffName,
                                IsActive = true
                            };
                            await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                            await _dbContext.SaveChangesAsync();

                            var userProfile = await _registrationRepo.Get(followUp.ProfileId);
                            if (userProfile != null && !userProfile.IsDeleted)
                            {
                                bool sendEmail = model.SendEmail;
                                bool sendMessage = model.SendMessage;
                                bool sendWhatsApp = model.SendWhatsApp;

                                if (!sendEmail && !sendMessage && !sendWhatsApp)
                                {
                                    sendEmail = true;
                                    sendMessage = true;
                                }

                                string? whatsappUrl = await SendPaymentLinkNotification(userProfile, sendEmail, sendMessage, sendWhatsApp);

                                var sentChannels = new List<string>();
                                if (sendEmail) sentChannels.Add("Email");
                                if (sendMessage) sentChannels.Add("SMS");
                                if (sendWhatsApp) sentChannels.Add("WhatsApp");

                                timeline.Remarks = string.IsNullOrEmpty(timeline.Remarks)
                                    ? $"Online payment link sent via {string.Join(", ", sentChannels)}"
                                    : $"{timeline.Remarks} [Online Payment Link Sent via {string.Join(", ", sentChannels)}]";

                                // Save changes
                                await _followUpTimelineRepo.Add(timeline);
                                await _followUpTimelineRepo.SaveChanges();

                                await _followUpRepo.Update(followUp);
                                await _followUpRepo.SaveChanges();

                                return Ok(new
                                {
                                    success = true,
                                    message = $"Follow-up renewed successfully and payment link sent via {string.Join(", ", sentChannels)}.",
                                    whatsappUrl = whatsappUrl
                                });
                            }
                        }
                    }
                    else if (model.RenewalInterestStatus == RenewalInterestStatus.NotInterested)
                    {
                        hasReachedEndStatus = true;
                    }
                    else
                    {
                        followUp.LatestAdminApprovalStatus = null;
                    }
                }

                if (hasReachedEndStatus)
                {
                    followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Pending;

                    var adminApproval = new FollowUpAdminApproval
                    {
                        FollowUpId = followUp.Id,
                        Status = AdminApprovalStatus.Pending,
                        Remarks = $"Pending admin review ({staffName}). Remarks: {model.Remarks}",
                        ActionDate = null,
                        ActionBy = null,
                        IsActive = true
                    };
                    await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                    await _dbContext.SaveChangesAsync();
                }

                // Save changes to repositories
                await _followUpTimelineRepo.Add(timeline);
                await _followUpTimelineRepo.SaveChanges();

                await _followUpRepo.Update(followUp);
                await _followUpRepo.SaveChanges();

                return Ok(new { success = true, message = "Follow-up interaction logged successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating follow-up via API.");
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        private async Task<string?> SendPaymentLinkNotification(Registration userProfile, bool sendEmail, bool sendMessage, bool sendWhatsApp)
        {
            string? whatsappUrl = null;
            try
            {
                string secretKey = _configuration.GetValue<string>("PaymentLinkSettings:SecretKey") ?? "M4NikkahPaymentSecretKey#2026";
                int expirationMinutes = _configuration.GetValue<int?>("PaymentLinkSettings:ExpirationMinutes") ?? 15;

                DateTime expiryTime = DateTime.UtcNow.AddMinutes(expirationMinutes);
                string payload = $"{userProfile.Id}|{expiryTime:o}";
                
                var crypt = new RijndaelCrypt(secretKey);
                string token = crypt.Encrypt(payload);
                string paymentLink = $"{Request.Scheme}://{Request.Host}/Transaction/MakePaymentDirect?token={Uri.EscapeDataString(token)}";

                // Send email
                if (sendEmail && !string.IsNullOrEmpty(userProfile.Email))
                {
                    string emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 8px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #7209b7, #3f37c9); color: white; padding: 25px; text-align: center; }}
        .content {{ padding: 30px; }}
        .footer {{ background-color: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #777; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: #7209b7; color: white !important; text-decoration: none; border-radius: 5px; margin-top: 20px; font-weight: bold; }}
        .expiry-notice {{ color: #d9534f; font-weight: bold; margin-top: 15px; padding: 10px; background-color: #fdf7f7; border-left: 4px solid #d9534f; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin:0; color: white;'>M4Nikah Matrimony</h2>
        </div>
        <div class='content'>
            <p>Dear <strong>{userProfile.Name}</strong>,</p>
            <p>We are pleased to inform you that your request has been reviewed and approved by our team.</p>
            <p>To activate or renew your premium benefits, please complete your payment using the link below:</p>
            <div class='expiry-notice'>
                ⏰ <strong>Note:</strong> This payment link is valid for <strong>{expirationMinutes} minutes</strong> only.
            </div>
            <center>
                <a href='{paymentLink}' class='button' style='color: white !important; text-decoration: none;'>Proceed to Payment</a>
            </center>
            <p style='margin-top: 25px;'>If the button doesn't work, you can copy and paste the following URL into your browser:</p>
            <p style='word-break: break-all;'><a href='{paymentLink}'>{paymentLink}</a></p>
            <br/>
            <p>Best Regards,<br/><strong>The M4Nikah Team</strong></p>
        </div>
        <div class='footer'>
            <p>Contact Us: info@m4nikah.com | +91 6238785898</p>
            <p>&copy; {DateTime.Now.Year} M4Nikah. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                    var htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                    _emailNotificationHelper.SendEmail(userProfile.Email, htmlView, "Complete Your Premium Payment - M4Nikah");
                }

                // Send SMS
                if (sendMessage && !string.IsNullOrEmpty(userProfile.Phone))
                {
                    await _emailService.SendSmsPaymentLinkAsync(paymentLink, userProfile.Phone);
                }

                // Send WhatsApp
                if (sendWhatsApp && !string.IsNullOrEmpty(userProfile.Phone))
                {
                    string rawCountryCode = userProfile.CountryCode ?? userProfile.Country;
                    string cleanCode = !string.IsNullOrEmpty(rawCountryCode) ? CountryCodeHelper.GetCountryCode(rawCountryCode).Replace("+", "").Replace(" ", "").Trim() : "91";
                    if (string.IsNullOrEmpty(cleanCode)) cleanCode = "91";
                    string cleanPhone = userProfile.Phone.Replace(" ", "").Replace("+", "").Trim();
                    string fullPhone = cleanPhone.StartsWith(cleanCode) ? cleanPhone : cleanCode + cleanPhone;
                    string waMessage = $" *M4Nikah Matrimony*\n\nDear *{userProfile.Name}*,\n\nGreat news! Your membership request has been *approved*.\n\n Click the secure link below to complete your payment:\n{paymentLink}\n\n *Note:* This payment link is active for 15 minutes.\n\nBest Regards,\n*The M4Nikah Team*";
                    whatsappUrl = $"https://wa.me/{fullPhone}?text={Uri.EscapeDataString(waMessage)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment link notification via API.");
            }

            return whatsappUrl;
        }

        /// <summary>
        /// Resends an expired or pending online payment link.
        /// </summary>
        [HttpPost("resend-payment-link")]
        public async Task<IActionResult> ResendPaymentLink([FromBody] ResendPaymentLinkModel model)
        {
            if (model == null || model.FollowUpId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid request." });
            }

            try
            {
                var followUp = await _followUpRepo.Get(model.FollowUpId);
                if (followUp == null || followUp.IsDeleted)
                {
                    return NotFound(new { success = false, message = "Follow-up not found." });
                }

                var userProfile = await _registrationRepo.Get(followUp.ProfileId);
                if (userProfile == null || userProfile.IsDeleted)
                {
                    return NotFound(new { success = false, message = "User profile not found." });
                }

                var loggedInUser = await _userManager.GetUserAsync(User);
                string staffName = loggedInUser?.NormalizedUserName ?? loggedInUser?.UserName ?? "Staff";

                bool sendEmail = model.SendEmail;
                bool sendMessage = model.SendMessage;
                bool sendWhatsApp = model.SendWhatsApp;

                if (!sendEmail && !sendMessage && !sendWhatsApp)
                {
                    sendEmail = true;
                    sendMessage = true;
                }

                string? whatsappUrl = await SendPaymentLinkNotification(userProfile, sendEmail, sendMessage, sendWhatsApp);

                followUp.PaymentMode = "Online";
                followUp.PaymentLinkSent = true;
                followUp.PaymentLinkSentAt = DateTime.UtcNow;
                followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;

                var sentChannels = new List<string>();
                if (sendEmail) sentChannels.Add("Email");
                if (sendMessage) sentChannels.Add("SMS");
                if (sendWhatsApp) sentChannels.Add("WhatsApp");

                var timeline = new FollowUpTimeline
                {
                    FollowUpId = followUp.Id,
                    StaffId = loggedInUser?.Id ?? 0,
                    StaffName = staffName,
                    ContactType = followUp.LatestContactType,
                    CallStatus = followUp.LatestCallStatus,
                    Remarks = $"Payment link resent via {string.Join(", ", sentChannels)} by {staffName}.",
                    NextFollowUpDate = followUp.NextFollowUpDate,
                    InterestStatus = followUp.LatestInterestStatus,
                    RenewalInterestStatus = followUp.LatestRenewalInterestStatus,
                    IsActive = true
                };

                await _followUpTimelineRepo.Add(timeline);
                await _followUpTimelineRepo.SaveChanges();

                await _followUpRepo.Update(followUp);
                await _followUpRepo.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = $"Payment link resent successfully via {string.Join(", ", sentChannels)}.",
                    whatsappUrl = whatsappUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending payment link via API.");
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        /// <summary>
        /// Suspends or restores a profile.
        /// </summary>
        [HttpPost("recycle/{id:long}")]
        public async Task<IActionResult> RecycleProfile(long id, [FromQuery] bool restore = false)
        {
            try
            {
                var entity = await _registrationRepo.Get(id);
                if (entity == null || entity.IsDeleted)
                {
                    return NotFound(new { success = false, message = "Profile not found." });
                }

                entity.IsActive = restore;
                if (restore)
                {
                    entity.DisabledReason = DisabledReason.Deactivated;
                    entity.IsVisible = true;
                }
                else
                {
                    entity.DisabledReason = DisabledReason.Recycled;
                    entity.IsVisible = false;
                }

                await _registrationRepo.Update(entity);
                await _registrationRepo.SaveChanges();

                return Ok(new { success = true, message = restore ? "Profile restored successfully." : "Profile suspended successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while recycling/restoring profile via API.");
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        /// <summary>
        /// Dismisses (soft deletes) a profile.
        /// </summary>
        [HttpPost("delete/{id:long}")]
        public async Task<IActionResult> DeleteProfile(long id)
        {
            try
            {
                var entity = await _registrationRepo.Get(id);
                if (entity == null || entity.IsDeleted)
                {
                    return NotFound(new { success = false, message = "Profile not found." });
                }

                // Remove reports associated with this profile
                var reports = await _dbContext.UserReports.Where(x => x.ReportedUserId == id && !x.IsDeleted).ToListAsync();
                foreach (var report in reports)
                {
                    report.IsDeleted = true;
                }
                await _dbContext.SaveChangesAsync();

                await _registrationRepo.SoftDelete(entity);
                await _registrationRepo.SaveChanges();

                return Ok(new { success = true, message = "Profile dismissed/deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting profile via API.");
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        /// <summary>
        /// Fetches complete user registration details by user ID for staff editing.
        /// </summary>
        [HttpGet("user-details/{id:long}")]
        public async Task<IActionResult> GetUserDetails(long id)
        {
            try
            {
                var userEntity = await _registrationRepo.Get(id);
                if (userEntity == null || userEntity.IsDeleted)
                {
                    return NotFound(new { success = false, message = "User profile not found." });
                }

                var userDto = _mapper.Map<RegistrationDto>(userEntity);

                var docs = await _dbContext.VerificationDocuments
                    .Where(d => d.ProfileId == id && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToListAsync();

                userDto.VerificationDocuments = docs.Select(d => _mapper.Map<VerificationDocumentDto>(d)).ToList();

                // Decrypt password if present so staff can view/edit
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    try
                    {
                        RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
                        userDto.Password = encryption.DecryptRijndael(userDto.Password, userDto.PasswordHash);
                    }
                    catch
                    {
                        userDto.Password = "";
                    }
                }
                else
                {
                    userDto.Password = "";
                }

                return Ok(new
                {
                    success = true,
                    data = userDto,
                    message = "User details fetched successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user details for ID: {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        /// <summary>
        /// Edits user registration details by a staff member.
        /// Supports both Form Data (including file upload) and JSON payload.
        /// </summary>
        [HttpPost("edit-user")]
        public async Task<IActionResult> EditUser([FromForm] RegistrationDto model)
        {
            try
            {
                // Support JSON body fallback if Content-Type is application/json
                if (Request.ContentType != null && Request.ContentType.Contains("application/json"))
                {
                    try
                    {
                        using var reader = new System.IO.StreamReader(Request.Body);
                        var body = await reader.ReadToEndAsync();
                        var jsonModel = System.Text.Json.JsonSerializer.Deserialize<RegistrationDto>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (jsonModel != null)
                        {
                            model = jsonModel;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize JSON in EditUser (Staff)");
                    }
                }

                if (model == null || model.Id <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user details or User ID." });
                }

                var user = await _registrationRepo.Get(model.Id);
                if (user == null || user.IsDeleted)
                {
                    return NotFound(new { success = false, message = "User profile not found." });
                }

                // Check email uniqueness if email changed
                if (!string.IsNullOrEmpty(model.Email) && model.Email != user.Email)
                {
                    var existingEmail = await _registrationRepo.WhereActive(x => x.Email == model.Email && x.Id != user.Id && x.IsVerified);
                    if (existingEmail.Any())
                    {
                        return BadRequest(new { success = false, message = "Email address already exists." });
                    }
                    user.Email = model.Email;
                }

                // Check phone uniqueness if phone changed
                if (!string.IsNullOrEmpty(model.Phone) && model.Phone != user.Phone)
                {
                    var existingPhone = await _registrationRepo.WhereActive(x => x.Phone == model.Phone && x.Id != user.Id && x.IsVerified);
                    if (existingPhone.Any())
                    {
                        return BadRequest(new { success = false, message = "Phone number already exists." });
                    }
                    user.Phone = model.Phone;
                }

                // Map updated fields
                if (!string.IsNullOrEmpty(model.Name)) user.Name = model.Name;
                if (!string.IsNullOrEmpty(model.Gender)) user.Gender = model.Gender;
                if (!string.IsNullOrEmpty(model.CountryCode)) user.CountryCode = model.CountryCode;
                if (model.SecondaryCountryCode != null) user.SecondaryCountryCode = model.SecondaryCountryCode;
                if (model.LandlineNumber != null) user.LandlineNumber = model.LandlineNumber;

                if (!string.IsNullOrEmpty(model.DOB))
                {
                    user.DOB = model.DOB;
                }

                if (model.ProfileForId > 0) user.ProfileForId = model.ProfileForId;
                if (model.NationalityId > 0) user.NationalityId = model.NationalityId;
                if (model.MaritalStatusId > 0) user.MaritalStatusId = model.MaritalStatusId;
                user.NumberOfChildrens = (model.MaritalStatusId == 1) ? null : model.NumberOfChildrens;

                if (model.HeightId > 0) user.HeightId = model.HeightId;
                if (model.WeightId > 0) user.WeightId = model.WeightId;
                if (model.ComplexionId > 0) user.ComplexionId = model.ComplexionId;
                if (model.BodyTypeId > 0) user.BodyTypeId = model.BodyTypeId;

                user.IsPhysicallyChallenged = model.IsPhysicallyChallenged;
                user.PhysicallyChallengedDetail = model.IsPhysicallyChallenged ? model.PhysicallyChallengedDetail : null;

                if (model.HighestEducation != null) user.HighestEducation = model.HighestEducation;
                if (model.EducationType != null) user.EducationType = model.EducationType;
                if (model.ProfessionId > 0) user.ProfessionId = model.ProfessionId;
                if (model.ProfessionType != null) user.ProfessionType = model.ProfessionType;

                if (model.MotherTongueId > 0) user.MotherTongueId = model.MotherTongueId;
                if (model.ReligionId > 0) user.ReligionId = model.ReligionId;
                if (model.CasteId > 0) user.CasteId = model.CasteId;
                if (model.CommunityId > 0) user.CommunityId = model.CommunityId;
                if (model.ReligiousnessId > 0) user.ReligiousnessId = model.ReligiousnessId;
                if (model.FinancialStatusId > 0) user.FinancialStatusId = model.FinancialStatusId;

                if (model.FamilyName != null) user.FamilyName = model.FamilyName;
                if (model.FatherName != null) user.FatherName = model.FatherName;
                if (model.Post != null) user.Post = model.Post;
                if (model.Village != null) user.Village = model.Village;
                if (model.PinCode != null) user.PinCode = model.PinCode;
                if (model.Country != null) user.Country = model.Country;
                if (model.State != null) user.State = model.State;
                if (model.District != null) user.District = model.District;
                if (model.PresentCountry != null) user.PresentCountry = model.PresentCountry;
                if (model.PresentState != null) user.PresentState = model.PresentState;
                if (model.PresentDistrict != null) user.PresentDistrict = model.PresentDistrict;
                if (model.PresentCity != null) user.PresentCity = model.PresentCity;
                if (model.About != null) user.About = model.About;

                user.IsSpecialRequest = model.IsSpecialRequest;
                user.ShowOnHomePage = model.ShowOnHomePage;
                user.IsActive = model.IsActive;
                user.IsVerified = model.IsVerified;
                user.IsComplete = model.IsComplete;
                user.IsVisible = model.IsVisible;

                user.PhotoVisibleToAll = model.PhotoVisibleToAll;
                user.PhotoVisibleToPremium = model.PhotoVisibleToPremium;
                user.PhotoVisibleToAccepted = model.PhotoVisibleToAccepted;

                // Handle password update if specified
                if (!string.IsNullOrEmpty(model.Password))
                {
                    RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
                    user.PasswordHash = encryption.CreateSalt();
                    user.Password = encryption.EncryptRijndael(model.Password, user.PasswordHash);
                }

                // Handle profile photo upload if file attached
                if (model.Image != null)
                {
                    await _fileService.SaveAllFiles(user, model, "Uploads/Registration");
                }

                await _registrationRepo.Update(user);
                await _registrationRepo.SaveChanges();

                var updatedDto = _mapper.Map<RegistrationDto>(user);

                return Ok(new
                {
                    success = true,
                    message = "User details updated successfully.",
                    data = updatedDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user details for ID: {UserId}", model?.Id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating user details: " + ex.Message });
            }
        }

        private RegistrationDto MapMobileProfileToRegistrationDto(UserController.MobileProfileDto mobile)
        {
            return new RegistrationDto
            {
                Id = mobile.Id,
                ProfileForId = mobile.Profile_For_Id ?? mobile.ProfileForId ?? 0,
                NationalityId = mobile.Nationality_Id > 0 ? mobile.Nationality_Id : (mobile.NationalityId > 0 ? mobile.NationalityId : 0),
                CountryCode = mobile.Country_Code ?? mobile.CountryCode,
                SecondaryCountryCode = mobile.Secondary_Country_Code ?? mobile.SecondaryCountryCode,
                MaritalStatusId = mobile.Marital_Status_Id > 0 ? mobile.Marital_Status_Id : (mobile.MaritalStatusId > 0 ? mobile.MaritalStatusId : 0),
                HeightId = mobile.Height_Id > 0 ? mobile.Height_Id : (mobile.HeightId > 0 ? mobile.HeightId : 0),
                WeightId = mobile.Weight_Id > 0 ? mobile.Weight_Id : (mobile.WeightId > 0 ? mobile.WeightId : 0),
                ComplexionId = mobile.Complexion_Id > 0 ? mobile.Complexion_Id : (mobile.ComplexionId > 0 ? mobile.ComplexionId : 0),
                BodyTypeId = mobile.Body_Type_Id > 0 ? mobile.Body_Type_Id : (mobile.BodyTypeId > 0 ? mobile.BodyTypeId : 0),
                IsPhysicallyChallenged = (mobile.Is_Physically_Challenged ?? mobile.IsPhysicallyChallenged) == 1,
                PhysicallyChallengedDetail = mobile.Physically_Challenged_Detail ?? mobile.PhysicallyChallengedDetail,
                Name = mobile.Name,
                Gender = mobile.Gender,
                DOB = mobile.DOB,
                Phone = mobile.Phone,
                LandlineNumber = mobile.Landline_Number ?? mobile.LandlineNumber,
                HighestEducation = mobile.Highest_Education ?? mobile.HighestEducation,
                ProfessionId = mobile.Profession_Id > 0 ? mobile.Profession_Id : (mobile.ProfessionId > 0 ? mobile.ProfessionId : 0),
                MotherTongueId = mobile.Mother_Tongue_Id > 0 ? mobile.Mother_Tongue_Id : (mobile.MotherTongueId > 0 ? mobile.MotherTongueId : 0),
                ReligionId = mobile.Religion_Id > 0 ? mobile.Religion_Id : (mobile.ReligionId > 0 ? mobile.ReligionId : 0),
                CasteId = mobile.Caste_Id > 0 ? mobile.Caste_Id : (mobile.CasteId > 0 ? mobile.CasteId : 0),
                CommunityId = mobile.Community_Id > 0 ? mobile.Community_Id : (mobile.CommunityId > 0 ? mobile.CommunityId : 0),
                ReligiousnessId = mobile.Religiousness_Id > 0 ? mobile.Religiousness_Id : (mobile.ReligiousnessId > 0 ? mobile.ReligiousnessId : 0),
                ProfessionType = mobile.Profession_Type ?? mobile.ProfessionType,
                FinancialStatusId = mobile.financial_status_id > 0 ? mobile.financial_status_id : (mobile.FinancialStatusId > 0 ? mobile.FinancialStatusId : 0),
                FamilyName = mobile.Family_Name ?? mobile.FamilyName,
                FatherName = mobile.Father_name ?? mobile.FatherName,
                Post = mobile.Post,
                Village = mobile.Village,
                PinCode = mobile.Pin_Code ?? mobile.PinCode,
                Country = mobile.Country,
                State = mobile.State,
                District = mobile.District,
                PresentCountry = mobile.Present_Country ?? mobile.PresentCountry,
                PresentState = mobile.Present_State ?? mobile.PresentState,
                PresentDistrict = mobile.Present_District ?? mobile.PresentDistrict,
                PresentCity = mobile.Present_City ?? mobile.PresentCity,
                ImagePath = mobile.Image_Path ?? mobile.ImagePath,
                Image = mobile.Image,
                About = mobile.About,
                EducationType = mobile.Education_Type ?? mobile.EducationType,
                Email = mobile.Email,
                Password = mobile.Password,
                RegisterNumber = mobile.Register_Number ?? mobile.RegisterNumber,
                NumberOfChildrens = mobile.NumberOfChildrens,
                PhotoVisibleToAll = mobile.PhotoVisibleToAll ?? mobile.Photo_Visible_To_All ?? true,
                PhotoVisibleToPremium = mobile.PhotoVisibleToPremium ?? mobile.Photo_Visible_To_Premium ?? false,
                PhotoVisibleToAccepted = mobile.PhotoVisibleToAccepted ?? mobile.Photo_Visible_To_Accepted ?? false,
                CompletedStep = mobile.CompletedStep ?? mobile.Completed_Step,
                Source = mobile.Source,
                DocumentVerificationEnabled = mobile.DocumentVerificationEnabled || (mobile.Document_Verification_Enabled ?? false),
                VerificationDocumentUrl = mobile.Verification_Document_Url ?? mobile.VerificationDocumentUrl,
                DocumentVerificationComplete = mobile.DocumentVerificationComplete || (mobile.Document_Verification_Complete ?? false),
                DocumentVerificationRejected = mobile.DocumentVerificationRejected || (mobile.Document_Verification_Rejected ?? false)
            };
        }

        private UserController.MobileProfileDto MapRegistrationDtoToMobileProfile(RegistrationDto reg)
        {
            return new UserController.MobileProfileDto
            {
                Id = reg.Id,
                Profile_For_Id = reg.ProfileForId,
                ProfileForId = reg.ProfileForId,
                Nationality_Id = reg.NationalityId,
                NationalityId = reg.NationalityId,
                Country_Code = reg.CountryCode,
                CountryCode = reg.CountryCode,
                Secondary_Country_Code = reg.SecondaryCountryCode,
                SecondaryCountryCode = reg.SecondaryCountryCode,
                Marital_Status_Id = reg.MaritalStatusId,
                MaritalStatusId = reg.MaritalStatusId,
                Height_Id = reg.HeightId,
                HeightId = reg.HeightId,
                Weight_Id = reg.WeightId,
                WeightId = reg.WeightId,
                Complexion_Id = reg.ComplexionId,
                ComplexionId = reg.ComplexionId,
                Body_Type_Id = reg.BodyTypeId,
                BodyTypeId = reg.BodyTypeId,
                Is_Physically_Challenged = reg.IsPhysicallyChallenged ? 1 : 0,
                IsPhysicallyChallenged = reg.IsPhysicallyChallenged ? 1 : 0,
                Physically_Challenged_Detail = reg.PhysicallyChallengedDetail,
                PhysicallyChallengedDetail = reg.PhysicallyChallengedDetail,
                Name = reg.Name,
                Gender = reg.Gender,
                DOB = reg.DOB,
                Phone = reg.Phone,
                Landline_Number = reg.LandlineNumber,
                LandlineNumber = reg.LandlineNumber,
                Highest_Education = reg.HighestEducation,
                HighestEducation = reg.HighestEducation,
                Profession_Id = reg.ProfessionId,
                ProfessionId = reg.ProfessionId,
                Mother_Tongue_Id = reg.MotherTongueId,
                MotherTongueId = reg.MotherTongueId,
                Religion_Id = reg.ReligionId,
                ReligionId = reg.ReligionId,
                Caste_Id = reg.CasteId,
                CasteId = reg.CasteId,
                Community_Id = reg.CommunityId,
                CommunityId = reg.CommunityId,
                Religiousness_Id = reg.ReligiousnessId,
                ReligiousnessId = reg.ReligiousnessId,
                Profession_Type = reg.ProfessionType,
                ProfessionType = reg.ProfessionType,
                financial_status_id = reg.FinancialStatusId,
                FinancialStatusId = reg.FinancialStatusId,
                Family_Name = reg.FamilyName,
                FamilyName = reg.FamilyName,
                Father_name = reg.FatherName,
                FatherName = reg.FatherName,
                Post = reg.Post,
                Village = reg.Village,
                Pin_Code = reg.PinCode,
                PinCode = reg.PinCode,
                Country = reg.Country,
                State = reg.State,
                District = reg.District,
                Present_Country = reg.PresentCountry,
                PresentCountry = reg.PresentCountry,
                Present_State = reg.PresentState,
                PresentState = reg.PresentState,
                Present_District = reg.PresentDistrict,
                PresentDistrict = reg.PresentDistrict,
                Present_City = reg.PresentCity,
                PresentCity = reg.PresentCity,
                Image_Path = reg.ImagePath,
                ImagePath = reg.ImagePath,
                About = reg.About,
                Education_Type = reg.EducationType,
                EducationType = reg.EducationType,
                Email = reg.Email,
                Password = reg.Password,
                Register_Number = reg.RegisterNumber,
                RegisterNumber = reg.RegisterNumber,
                NumberOfChildrens = reg.NumberOfChildrens,
                PhotoVisibleToAll = reg.PhotoVisibleToAll,
                Photo_Visible_To_All = reg.PhotoVisibleToAll,
                PhotoVisibleToPremium = reg.PhotoVisibleToPremium,
                Photo_Visible_To_Premium = reg.PhotoVisibleToPremium,
                PhotoVisibleToAccepted = reg.PhotoVisibleToAccepted,
                Photo_Visible_To_Accepted = reg.PhotoVisibleToAccepted,
                CompletedStep = reg.CompletedStep,
                Completed_Step = reg.CompletedStep,
                Source = reg.Source,
                DocumentVerificationEnabled = reg.DocumentVerificationEnabled,
                Document_Verification_Enabled = reg.DocumentVerificationEnabled,
                VerificationDocumentUrl = reg.VerificationDocumentUrl,
                Verification_Document_Url = reg.VerificationDocumentUrl,
                DocumentVerificationComplete = reg.DocumentVerificationComplete,
                Document_Verification_Complete = reg.DocumentVerificationComplete,
                DocumentVerificationRejected = reg.DocumentVerificationRejected,
                Document_Verification_Rejected = reg.DocumentVerificationRejected,
                VerificationDocuments = reg.VerificationDocuments
            };
        }

        private async Task SendRegistrationSuccessEmail(string email, string name, string registerNumber)
        {
            string templatePath = "Templates/Mail/template-registrationsuccessful.html";
            string emailContent = System.IO.File.ReadAllText(templatePath);
            emailContent = emailContent.Replace("[User's Name]", name);
            emailContent = emailContent.Replace("[Profile ID]", registerNumber);

            var htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
            _emailNotificationHelper.SendEmail(email, htmlView, "Welcome to M4Nikah - Your Profile Registration is Complete!");
        }

        private async Task SendRegistrationUnsuccessfulEmail(string email, string name)
        {
            string templatePath = "Templates/Mail/template-registrationunsuccessful.html";
            string emailContent = System.IO.File.ReadAllText(templatePath);
            emailContent = emailContent.Replace("[User's Name]", name);
            emailContent = emailContent.Replace("[Support Email]", "support@m4nikkah.com");
            emailContent = emailContent.Replace("[Support Phone Number]", "123-456-7890");

            var htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
            _emailNotificationHelper.SendEmail(email, htmlView, "M4Nikkah Registration Unsuccessful");
        }

        /// <summary>
        /// Fetches cold leads assigned to a specific staff member.
        /// </summary>
        [HttpGet("cold-leads/{staffId:long}")]
        public async Task<IActionResult> GetColdLeadsByStaff(long staffId, [FromQuery] ColdLeadStatus? status)
        {
            try
            {
                var query = _coldLeadRepo.GetQueryable()
                    .Where(c => c.AssignedStaffId == staffId && !c.IsDeleted);

                if (status.HasValue)
                {
                    query = query.Where(c => c.Status == status.Value);
                }

                var list = await query.OrderByDescending(c => c.CreatedOn).ThenByDescending(c => c.Id).ToListAsync();

                var dtos = list.Select(c => new ColdLeadDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    AssignedStaffId = c.AssignedStaffId,
                    Status = c.Status,
                    Remarks = c.Remarks,
                    CreatedOn = c.CreatedOn
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Cold leads fetched successfully.",
                    data = dtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cold leads for staff ID: {StaffId}", staffId);
                return StatusCode(500, new { status = false, message = "Failed to fetch cold leads." });
            }
        }

        /// <summary>
        /// Staff updates the status and remarks of an assigned cold lead.
        /// </summary>
        [HttpPost("cold-leads/update-status")]
        public async Task<IActionResult> UpdateColdLeadStatus([FromBody] UpdateColdLeadStatusRequest request)
        {
            try
            {
                if (request == null || request.Id <= 0)
                {
                    return BadRequest(new { status = false, message = "Invalid request parameter." });
                }

                var coldLead = await _coldLeadRepo.Get(request.Id);
                if (coldLead == null || coldLead.IsDeleted)
                {
                    return NotFound(new { status = false, message = "Cold lead record not found." });
                }

                coldLead.Status = request.Status;
                if (!string.IsNullOrWhiteSpace(request.Remarks))
                {
                    coldLead.Remarks = request.Remarks.Trim();
                }

                await _coldLeadRepo.Update(coldLead);
                await _coldLeadRepo.SaveChanges();

                return Ok(new
                {
                    status = true,
                    message = "Cold lead status updated successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cold lead status for ID: {Id}", request?.Id);
                return StatusCode(500, new { status = false, message = "Failed to update cold lead status." });
            }
        }

        /// <summary>
        /// Fetches a comprehensive performance report for a specific staff ID.
        /// Includes assignments, call activity logs, premium conversions, revenue collections, verification counts, 
        /// targets, incentive earnings, attendance deductions, net estimated payroll, and overall performance rating.
        /// Header requires standard Staff JWT bearer token.
        /// </summary>
        [HttpGet("performance-report/{staffId:long}")]
        public async Task<IActionResult> GetStaffPerformanceReport(
            long staffId,
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] string? period = "Month",
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                // 1. Resolve Staff ID
                long targetStaffId = staffId;
                if (targetStaffId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid staff ID provided." });
                }

                // 2. Resolve Staff User & Staff Detail
                var staffUser = await _userManager.FindByIdAsync(targetStaffId.ToString());
                if (staffUser == null)
                {
                    return NotFound(new { success = false, message = "Staff member not found." });
                }
                string staffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Staff Member";
                string staffEmail = staffUser.Email ?? string.Empty;

                var staffDetail = await _dbContext.StaffDetails
                    .FirstOrDefaultAsync(s => s.UserId == targetStaffId && !s.IsDeleted);
                string department = staffDetail?.Department ?? "General Operations";

                // 3. Resolve Date Filters
                int filterYear = year ?? DateTime.UtcNow.Year;
                int filterMonth = month ?? DateTime.UtcNow.Month;
                DateTime startDate;
                DateTime endDate;

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    startDate = dateFrom.Value.Date;
                    endDate = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                }
                else
                {
                    string cleanPeriod = (period ?? "Month").Trim();
                    if (cleanPeriod.Equals("Day", StringComparison.OrdinalIgnoreCase))
                    {
                        var localTodayStartUtc = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local).ToUniversalTime();
                        startDate = localTodayStartUtc < DateTime.UtcNow.Date ? localTodayStartUtc : DateTime.UtcNow.Date;
                        var localTodayEnd = DateTime.Today.AddDays(1).AddTicks(-1);
                        var utcTodayEnd = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
                        endDate = localTodayEnd > utcTodayEnd ? localTodayEnd : utcTodayEnd;
                    }
                    else if (cleanPeriod.Equals("Week", StringComparison.OrdinalIgnoreCase))
                    {
                        int diff = (int)DateTime.UtcNow.DayOfWeek - (int)DayOfWeek.Monday;
                        if (diff < 0) diff += 7;
                        startDate = DateTime.UtcNow.Date.AddDays(-1 * diff);
                        endDate = startDate.AddDays(7).AddTicks(-1);
                    }
                    else if (cleanPeriod.Equals("Year", StringComparison.OrdinalIgnoreCase))
                    {
                        startDate = new DateTime(filterYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        endDate = new DateTime(filterYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                    }
                    else // Default Month
                    {
                        startDate = new DateTime(filterYear, filterMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                        int daysInMonth = DateTime.DaysInMonth(filterYear, filterMonth);
                        endDate = new DateTime(filterYear, filterMonth, daysInMonth, 23, 59, 59, DateTimeKind.Utc);
                    }
                }

                // 4. Fetch Profile Assignments
                var staffAssignments = await _assignmentRepo.GetQueryable()
                    .Where(a => a.StaffId == targetStaffId && !a.IsDeleted)
                    .Include(a => a.Profile)
                    .ToListAsync();

                // Fetch follow-ups and timelines first for resilient profile linking
                var staffFollowUps = await _followUpRepo.GetQueryable()
                    .Where(f => f.AssignedStaffId == targetStaffId && !f.IsDeleted)
                    .Include(f => f.Profile)
                    .ToListAsync();

                var staffTimelines = await _followUpTimelineRepo.GetQueryable()
                    .Where(t => t.StaffId == targetStaffId && !t.IsDeleted && t.CreatedOn >= startDate && t.CreatedOn <= endDate)
                    .ToListAsync();

                var additionalFollowUpIds = staffTimelines
                    .Select(t => t.FollowUpId)
                    .Where(fId => !staffFollowUps.Any(f => f.Id == fId))
                    .Distinct()
                    .ToList();

                if (additionalFollowUpIds.Any())
                {
                    var extraFollowUps = await _followUpRepo.GetQueryable()
                        .Where(f => additionalFollowUpIds.Contains(f.Id) && !f.IsDeleted)
                        .Include(f => f.Profile)
                        .ToListAsync();
                    staffFollowUps.AddRange(extraFollowUps);
                }

                var timelineStaffProfileIds = staffTimelines
                    .Select(t => staffFollowUps.FirstOrDefault(f => f.Id == t.FollowUpId)?.ProfileId ?? 0)
                    .Where(pid => pid > 0);

                var staffProfileIds = staffAssignments.Select(a => a.ProfileId)
                    .Concat(staffFollowUps.Select(f => f.ProfileId))
                    .Concat(timelineStaffProfileIds)
                    .Distinct()
                    .ToList();

                int totalAssignedProfiles = staffAssignments.Count;
                int activeAssignedProfiles = staffAssignments.Count(a => a.Profile != null && a.Profile.IsComplete && !a.Profile.IsDeleted);
                int premiumAssignedProfiles = staffAssignments.Count(a => a.Profile != null && a.Profile.IsPremiumMember && !a.Profile.IsDeleted);
                int unverifiedAssignedProfiles = staffAssignments.Count(a => a.Profile != null && !a.Profile.DocumentVerificationComplete && !a.Profile.IsDeleted);

                // Period-Specific Assignments (Assigned within startDate and endDate)
                var periodAssignments = staffAssignments
                    .Where(a => a.CreatedOn >= startDate && a.CreatedOn <= endDate)
                    .ToList();

                int newlyAssignedInPeriod = periodAssignments.Count;
                int newlyAssignedActive = periodAssignments.Count(a => a.Profile != null && a.Profile.IsComplete && !a.Profile.IsDeleted);
                int newlyAssignedPremium = periodAssignments.Count(a => a.Profile != null && a.Profile.IsPremiumMember && !a.Profile.IsDeleted);
                int newlyAssignedUnverified = periodAssignments.Count(a => a.Profile != null && !a.Profile.DocumentVerificationComplete && !a.Profile.IsDeleted);

                // 5. Fetch Follow-Ups & Timeline Logged Activities
                int totalFollowUpsHandled = staffFollowUps.Count;
                int totalCallsMade = staffTimelines.Count;

                // Timeline-based follow-up IDs for date-range filtering
                var timelineStaffFollowUpIds = staffTimelines
                    .Select(t => t.FollowUpId)
                    .Distinct()
                    .ToHashSet();
                bool hasTimelinesInPeriod = timelineStaffFollowUpIds.Any();

                // Call Status Breakdown
                var callStatusBreakdown = new
                {
                    connected = staffTimelines.Count(t => t.CallStatus == CallStatus.Connected),
                    notConnected = staffTimelines.Count(t => t.CallStatus == CallStatus.NotConnected),
                    busy = staffTimelines.Count(t => t.CallStatus == CallStatus.Busy),
                    switchedOff = staffTimelines.Count(t => t.CallStatus == CallStatus.SwitchedOff),
                    noResponse = staffTimelines.Count(t => t.CallStatus == CallStatus.NoResponse)
                };

                // Contact Type Breakdown
                var contactTypeBreakdown = new
                {
                    whatsApp = staffTimelines.Count(t => t.ContactType == ContactType.WhatsApp),
                    phoneCall = staffTimelines.Count(t => t.ContactType == ContactType.PhoneCall),
                    directCall = staffTimelines.Count(t => t.ContactType == ContactType.DirectCall),
                    email = staffTimelines.Count(t => t.ContactType == ContactType.Email)
                };

                // 6. Conversions & Premium Collections (Premium + Renewal, date-filtered)
                // Load successful transactions first — needed for payment verification
                var transactions = await _dbContext.Transaction
                    .Where(t => staffProfileIds.Contains(t.userId) && t.Status == "success" && t.CreatedOn >= startDate && t.CreatedOn <= endDate)
                    .ToListAsync();

                var convertedFollowUps = staffFollowUps
                    .Where(f => 
                        (
                            timelineStaffFollowUpIds.Contains(f.Id)
                            || (f.ModifiedOn >= startDate && f.ModifiedOn <= endDate)
                            || (f.CreatedOn >= startDate && f.CreatedOn <= endDate)
                        )
                        && (
                            (f.FollowUpType == FollowUpType.PremiumFollowUp && (f.LatestInterestStatus == PremiumInterestStatus.Converted || (f.Profile != null && f.Profile.IsPremiumMember)))
                            || (f.FollowUpType == FollowUpType.RenewalFollowUp && (f.LatestRenewalInterestStatus == RenewalInterestStatus.Renewed || (f.Profile != null && f.Profile.IsPremiumMember)))
                        )
                        && (
                            f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                            || f.PaymentCompleted
                            || transactions.Any(t => t.userId == f.ProfileId && t.CreatedOn >= f.CreatedOn)
                        ))
                    .ToList();

                // Each distinct converted follow-up (Premium conversion or Renewal conversion) counts towards conversions
                var validConvertedFollowUps = convertedFollowUps
                    .Where(fu => fu.Profile != null)
                    .DistinctBy(fu => fu.Id)
                    .ToList();

                int convBoys = 0, convGirls = 0;
                foreach (var fu in validConvertedFollowUps)
                {
                    if (fu.Profile == null) continue;
                    if (string.Equals(fu.Profile.Gender, "Male", StringComparison.OrdinalIgnoreCase)) convBoys++;
                    else convGirls++;
                }

                int totalPremiumConversions = convBoys + convGirls;

                // Group by ProfileId to avoid double-counting when summing gateway transactions
                decimal collectionBoys = 0, collectionGirls = 0;
                var distinctConvertedProfiles = convertedFollowUps
                    .Where(fu => fu.Profile != null)
                    .GroupBy(fu => fu.ProfileId)
                    .Select(g => g.First())
                    .ToList();

                foreach (var fu in distinctConvertedProfiles)
                {
                    bool isMale = string.Equals(fu.Profile!.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                    var pTxns = transactions.Where(t => t.userId == fu.ProfileId);
                    decimal profileCollection = 0;
                    foreach (var txn in pTxns)
                    {
                        if (decimal.TryParse(txn.Amount, out decimal amt) && amt > 0)
                        {
                            profileCollection += amt;
                        }
                    }

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
                decimal totalPremiumCollection = collectionBoys + collectionGirls;

                // 7. Profile Verification Metrics (Grade-Wise)
                var verificationFollowUps = staffFollowUps
                    .Where(f => f.FollowUpType == FollowUpType.ProfileVerification
                        && (
                            timelineStaffFollowUpIds.Contains(f.Id)
                            || (f.CreatedOn >= startDate && f.CreatedOn <= endDate)
                            || (f.ModifiedOn >= startDate && f.ModifiedOn <= endDate)
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
                int totalVerifications = gradeA + gradeB + gradeC + gradeD;

                int verifBoys = verificationFollowUps.Count(f => f.Profile != null 
                    && (f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify || f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify)
                    && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                    && string.Equals(f.Profile.Gender, "Male", StringComparison.OrdinalIgnoreCase));

                var approvedFemaleVerifs = verificationFollowUps.Where(f => f.Profile != null 
                    && (f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify || f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify)
                    && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                    && string.Equals(f.Profile.Gender, "Female", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                int femaleGradeAVerifs = approvedFemaleVerifs.Count(f => string.Equals(f.VerificationGrade, "A", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrEmpty(f.VerificationGrade) && f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify));

                int femaleGradeBVerifs = approvedFemaleVerifs.Count(f => string.Equals(f.VerificationGrade, "B", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrEmpty(f.VerificationGrade) && f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify));

                int femaleGradeCVerifs = approvedFemaleVerifs.Count(f => string.Equals(f.VerificationGrade, "C", StringComparison.OrdinalIgnoreCase));

                int femaleGradeDVerifs = approvedFemaleVerifs.Count(f => string.Equals(f.VerificationGrade, "D", StringComparison.OrdinalIgnoreCase));

                int verifGirls = femaleGradeAVerifs + femaleGradeBVerifs + femaleGradeCVerifs + femaleGradeDVerifs;
                int totalApprovedVerifications = verifBoys + verifGirls;

                // 8. Salary Config & Eligibility Flags
                var salaryConfig = await _dbContext.StaffSalaryConfigs.FirstOrDefaultAsync(c => c.StaffId == targetStaffId && !c.IsDeleted);
                bool isIncentiveEligible = salaryConfig?.IncentiveEligibility ?? true;

                if (salaryConfig?.IncentiveEffectiveDate.HasValue == true && DateTime.UtcNow < salaryConfig.IncentiveEffectiveDate.Value)
                {
                    isIncentiveEligible = false;
                }

                // 9. Performance Targets & Achievements (Used for performance evaluation, not incentives)
                var perfTargetConfig = await _dbContext.StaffPerformanceTargetConfigs.FirstOrDefaultAsync(c => c.StaffId == targetStaffId && !c.IsDeleted);

                bool isDaily = string.Equals((period ?? "Month").Trim(), "Day", StringComparison.OrdinalIgnoreCase);

                int maleVerifTarget = isDaily ? (perfTargetConfig?.MaleVerificationDailyTarget ?? 0) : (perfTargetConfig?.MaleVerificationMonthlyTarget ?? 0);
                int femaleVerifTarget = isDaily ? (perfTargetConfig?.FemaleVerificationDailyTarget ?? 0) : (perfTargetConfig?.FemaleVerificationMonthlyTarget ?? 0);
                int totalVerifTarget = maleVerifTarget + femaleVerifTarget;

                int maleConvTarget = isDaily ? (perfTargetConfig?.MaleConversionDailyTarget ?? 0) : (perfTargetConfig?.MaleConversionMonthlyTarget ?? 0);
                int femaleConvTarget = isDaily ? (perfTargetConfig?.FemaleConversionDailyTarget ?? 0) : (perfTargetConfig?.FemaleConversionMonthlyTarget ?? 0);
                int totalConvTarget = maleConvTarget + femaleConvTarget;

                int totalPeriodTarget = totalVerifTarget + totalConvTarget;
                int totalPeriodAchieved = totalApprovedVerifications + totalPremiumConversions;

                decimal targetAchievementPercent = totalPeriodTarget > 0 
                    ? Math.Min(100, Math.Round((decimal)totalPeriodAchieved / totalPeriodTarget * 100, 2)) 
                    : 0;

                decimal maleVerifProgress = maleVerifTarget > 0 ? Math.Min(100, Math.Round((decimal)verifBoys / maleVerifTarget * 100, 2)) : 0;
                decimal femaleVerifProgress = femaleVerifTarget > 0 ? Math.Min(100, Math.Round((decimal)verifGirls / femaleVerifTarget * 100, 2)) : 0;
                decimal totalVerifProgress = totalVerifTarget > 0 ? Math.Min(100, Math.Round((decimal)totalApprovedVerifications / totalVerifTarget * 100, 2)) : 0;

                decimal maleConvProgress = maleConvTarget > 0 ? Math.Min(100, Math.Round((decimal)convBoys / maleConvTarget * 100, 2)) : 0;
                decimal femaleConvProgress = femaleConvTarget > 0 ? Math.Min(100, Math.Round((decimal)convGirls / femaleConvTarget * 100, 2)) : 0;
                decimal totalConvProgress = totalConvTarget > 0 ? Math.Min(100, Math.Round((decimal)totalPremiumConversions / totalConvTarget * 100, 2)) : 0;

                // 10. Incentives Calculation (Conversion & Verification based only)
                var staffIncentiveConfig = await _dbContext.StaffIncentiveConfigs.FirstOrDefaultAsync(c => c.StaffId == targetStaffId && !c.IsDeleted);

                decimal verifIncentiveBoys = 0;
                decimal verifIncentiveGirlsGradeA = 0;
                decimal verifIncentiveGirlsGradeB = 0;
                decimal verifIncentiveGirlsGradeC = 0;
                decimal verifIncentiveGirlsGradeD = 0;
                decimal premiumIncentiveBoys = 0;
                decimal premiumIncentiveGirls = 0;

                if (isIncentiveEligible && staffIncentiveConfig != null)
                {
                    // Male Verification (Requires Admin Approval)
                    if (string.Equals(staffIncentiveConfig.MaleVerificationType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        verifIncentiveBoys += verifBoys * staffIncentiveConfig.MaleVerificationAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.MaleVerificationTarget ?? 0;
                        if (verifBoys > baseTarget)
                        {
                            verifIncentiveBoys += (verifBoys - baseTarget) * staffIncentiveConfig.MaleVerificationAmount;
                        }
                    }

                    // Female Grade A Verification (Requires Admin Approval)
                    if (string.Equals(staffIncentiveConfig.FemaleGradeAVerificationType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        verifIncentiveGirlsGradeA += femaleGradeAVerifs * staffIncentiveConfig.FemaleGradeAVerificationAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.FemaleGradeAVerificationTarget ?? 0;
                        if (femaleGradeAVerifs > baseTarget)
                        {
                            verifIncentiveGirlsGradeA += (femaleGradeAVerifs - baseTarget) * staffIncentiveConfig.FemaleGradeAVerificationAmount;
                        }
                    }

                    // Female Grade B Verification (Requires Admin Approval)
                    if (string.Equals(staffIncentiveConfig.FemaleGradeBVerificationType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        verifIncentiveGirlsGradeB += femaleGradeBVerifs * staffIncentiveConfig.FemaleGradeBVerificationAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.FemaleGradeBVerificationTarget ?? 0;
                        if (femaleGradeBVerifs > baseTarget)
                        {
                            verifIncentiveGirlsGradeB += (femaleGradeBVerifs - baseTarget) * staffIncentiveConfig.FemaleGradeBVerificationAmount;
                        }
                    }

                    // Female Grade C Verification (Requires Admin Approval)
                    if (string.Equals(staffIncentiveConfig.FemaleGradeCVerificationType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        verifIncentiveGirlsGradeC += femaleGradeCVerifs * staffIncentiveConfig.FemaleGradeCVerificationAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.FemaleGradeCVerificationTarget ?? 0;
                        if (femaleGradeCVerifs > baseTarget)
                        {
                            verifIncentiveGirlsGradeC += (femaleGradeCVerifs - baseTarget) * staffIncentiveConfig.FemaleGradeCVerificationAmount;
                        }
                    }

                    // Female Grade D Verification (Requires Admin Approval)
                    if (string.Equals(staffIncentiveConfig.FemaleGradeDVerificationType, "ProfileBasis", StringComparison.OrdinalIgnoreCase))
                    {
                        verifIncentiveGirlsGradeD += femaleGradeDVerifs * staffIncentiveConfig.FemaleGradeDVerificationAmount;
                    }
                    else
                    {
                        int baseTarget = staffIncentiveConfig.FemaleGradeDVerificationTarget ?? 0;
                        if (femaleGradeDVerifs > baseTarget)
                        {
                            verifIncentiveGirlsGradeD += (femaleGradeDVerifs - baseTarget) * staffIncentiveConfig.FemaleGradeDVerificationAmount;
                        }
                    }

                    // Male Premium Conversion
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

                    // Female Premium Conversion
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

                decimal verifIncentiveGirls = verifIncentiveGirlsGradeA + verifIncentiveGirlsGradeB + verifIncentiveGirlsGradeC + verifIncentiveGirlsGradeD;
                decimal totalVerifIncentive = verifIncentiveBoys + verifIncentiveGirls;
                decimal totalIncentivePayable = premiumIncentiveBoys + premiumIncentiveGirls + totalVerifIncentive;

                // 11. Leaves, Deductions & Payroll Summary
                decimal basicSalary = salaryConfig?.BasicMonthlySalary ?? 0;
                decimal perDaySalary = salaryConfig?.PerDaySalary > 0 
                    ? salaryConfig.PerDaySalary 
                    : (basicSalary > 0 ? Math.Round(basicSalary / DateTime.DaysInMonth(filterYear, filterMonth), 2) : 0);

                var leaveRecords = await _dbContext.StaffLeaveRecords
                    .Where(l => l.StaffId == targetStaffId && !l.IsDeleted && l.IsApproved && l.LeaveDate >= startDate && l.LeaveDate <= endDate)
                    .ToListAsync();

                int paidLeavesCount = 0;
                int unpaidLeavesCount = 0;
                decimal leaveDeduction = 0;

                foreach (var leave in leaveRecords)
                {
                    if (leave.IsPaid)
                    {
                        paidLeavesCount++;
                        continue;
                    }
                    unpaidLeavesCount++;
                    leaveDeduction += leave.LeaveType switch
                    {
                        "HalfDay" => Math.Round(perDaySalary / 2, 2),
                        "FullDay" => perDaySalary,
                        "Late" => Math.Round(perDaySalary / 4, 2),
                        _ => perDaySalary
                    };
                }

                var complaintRecords = await _dbContext.StaffComplaintRecords
                    .Where(c => c.StaffId == targetStaffId && !c.IsDeleted && c.IsApproved && c.ComplaintDate >= startDate && c.ComplaintDate <= endDate)
                    .ToListAsync();
                decimal complaintDeduction = complaintRecords.Sum(c => c.DeductionAmount);

                // Deleted Profile Incentive Deductions
                var verifiedFollowUps = verificationFollowUps
                    .Where(f => f.Profile != null 
                        && (f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify || f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify)
                        && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved)
                    .DistinctBy(f => f.ProfileId)
                    .ToList();

                var deletedProfileItems = new List<object>();
                decimal deletedProfileDeduction = 0;

                foreach (var vf in verifiedFollowUps)
                {
                    var profile = vf.Profile!;
                    bool isDeleted = profile.IsDeleted 
                        || profile.DisabledReason == Application.Constants.DisabledReason.Recycled 
                        || profile.DisabledReason == Application.Constants.DisabledReason.ReportedViolation 
                        || profile.DeleteReasonId != null 
                        || !profile.IsActive;

                    if (isDeleted)
                    {
                        bool isMale = string.Equals(profile.Gender, "Male", StringComparison.OrdinalIgnoreCase);
                        bool isDocVerify = vf.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify;

                        decimal unitDeduction = 0;
                        string verifType;

                        if (isMale)
                        {
                            verifType = isDocVerify ? "Male Document Verification" : "Male Verification";
                            unitDeduction = isIncentiveEligible && staffIncentiveConfig != null ? staffIncentiveConfig.MaleVerificationAmount : 0;
                        }
                        else
                        {
                            string grade = !string.IsNullOrWhiteSpace(vf.VerificationGrade) 
                                ? vf.VerificationGrade.Trim().ToUpper() 
                                : (isDocVerify ? "A" : "B");
                            verifType = $"Female Grade {grade} Verification";

                            if (isIncentiveEligible && staffIncentiveConfig != null)
                            {
                                unitDeduction = grade switch
                                {
                                    "A" => staffIncentiveConfig.FemaleGradeAVerificationAmount,
                                    "B" => staffIncentiveConfig.FemaleGradeBVerificationAmount,
                                    "C" => staffIncentiveConfig.FemaleGradeCVerificationAmount,
                                    "D" => staffIncentiveConfig.FemaleGradeDVerificationAmount,
                                    _ => staffIncentiveConfig.FemaleGradeBVerificationAmount > 0 ? staffIncentiveConfig.FemaleGradeBVerificationAmount : staffIncentiveConfig.FemaleVerificationAmount
                                };
                            }
                        }

                        string delStatus = !string.IsNullOrEmpty(profile.DeleteReasonText) 
                            ? profile.DeleteReasonText 
                            : (profile.DisabledReason == Application.Constants.DisabledReason.Recycled ? "Account Deleted / Recycled" 
                                : (profile.DisabledReason == Application.Constants.DisabledReason.ReportedViolation ? "Reported Violation" 
                                : (profile.IsDeleted ? "Profile Deleted" : "Deactivated")));

                        deletedProfileItems.Add(new
                        {
                            profileId = profile.Id,
                            registerNumber = !string.IsNullOrEmpty(profile.RegisterNumber) ? profile.RegisterNumber : ("ID #" + profile.Id),
                            profileName = !string.IsNullOrEmpty(profile.Name) ? profile.Name : "Profile #" + profile.Id,
                            gender = profile.Gender ?? "N/A",
                            verificationType = verifType,
                            verifiedDate = (vf.ModifiedOn != default ? vf.ModifiedOn : vf.CreatedOn).ToString("yyyy-MM-dd"),
                            deletionStatus = delStatus,
                            deductionAmount = Math.Round(unitDeduction, 2)
                        });

                        deletedProfileDeduction += unitDeduction;
                    }
                }

                decimal totalDeduction = leaveDeduction + complaintDeduction + deletedProfileDeduction;

                var payrollRecord = await _dbContext.StaffPayrolls
                    .FirstOrDefaultAsync(p => p.StaffId == targetStaffId && p.Year == filterYear && p.Month == filterMonth && !p.IsDeleted);

                decimal adminIncentive = payrollRecord?.AdminIncentive ?? 0;
                string? adminIncentiveRemarks = payrollRecord?.AdminIncentiveRemarks;

                // Fetch individual admin incentive items
                var adminIncentiveItems = new List<object>();
                if (payrollRecord != null)
                {
                    var items = await _dbContext.StaffPayrollAdminIncentiveItems
                        .Where(i => i.StaffPayrollId == payrollRecord.Id && !i.IsDeleted)
                        .OrderBy(i => i.CreatedOn)
                        .Select(i => new { amount = Math.Round(i.Amount, 2), label = i.Label ?? "", createdOn = i.CreatedOn })
                        .ToListAsync();
                    adminIncentiveItems.AddRange(items);
                }

                decimal totalIncentivePayableWithAdmin = totalIncentivePayable + adminIncentive;

                string payrollStatus = payrollRecord?.Status ?? "Draft";
                decimal estimatedNetSalary = payrollRecord != null && (payrollRecord.Status == "Approved" || payrollRecord.Status == "Paid")
                    ? payrollRecord.ApprovedPayroll
                    : Math.Max(0, (basicSalary + totalIncentivePayableWithAdmin - totalDeduction));

                // 11. Performance Score Calculation
                decimal conversionScore = Math.Min(40, totalPremiumConversions * 8);
                decimal verificationScore = Math.Min(30, totalVerifications * 5);
                decimal targetScore = Math.Min(30, (targetAchievementPercent / 100) * 30);
                decimal performanceScore = Math.Round(conversionScore + verificationScore + targetScore, 2);

                string performanceGrade = performanceScore switch
                {
                    >= 90 => "A+ (Excellent)",
                    >= 75 => "A (Very Good)",
                    >= 60 => "B (Good)",
                    >= 40 => "C (Average)",
                    _ => "D (Needs Improvement)"
                };

                // 12. Return Final JSON Response
                return Ok(new
                {
                    success = true,
                    message = "Staff performance report fetched successfully.",
                    data = new
                    {
                        staff = new
                        {
                            staffId = targetStaffId,
                            staffName = staffName,
                            email = staffEmail,
                            department = department
                        },
                        filter = new
                        {
                            period = period,
                            year = filterYear,
                            month = filterMonth,
                            startDate = startDate.ToString("yyyy-MM-dd"),
                            endDate = endDate.ToString("yyyy-MM-dd")
                        },
                        assignments = new
                        {
                            totalAssignedProfiles = totalAssignedProfiles,
                            activeAssignedProfiles = activeAssignedProfiles,
                            premiumAssignedProfiles = premiumAssignedProfiles,
                            unverifiedAssignedProfiles = unverifiedAssignedProfiles,
                            assignedInPeriod = new
                            {
                                totalAssignedProfiles = newlyAssignedInPeriod,
                                activeAssignedProfiles = newlyAssignedActive,
                                premiumAssignedProfiles = newlyAssignedPremium,
                                unverifiedAssignedProfiles = newlyAssignedUnverified
                            }
                        },
                        activities = new
                        {
                            totalFollowUpsHandled = totalFollowUpsHandled,
                            totalCallsMade = totalCallsMade,
                            callStatusBreakdown = callStatusBreakdown,
                            contactTypeBreakdown = contactTypeBreakdown
                        },
                        conversionsAndRevenue = new
                        {
                            premiumConversionsBoys = convBoys,
                            premiumConversionsGirls = convGirls,
                            totalPremiumConversions = totalPremiumConversions,
                            premiumCollectionBoys = Math.Round(collectionBoys, 2),
                            premiumCollectionGirls = Math.Round(collectionGirls, 2),
                            totalPremiumCollection = Math.Round(totalPremiumCollection, 2)
                        },
                        verifications = new
                        {
                            verificationBoys = verifBoys,
                            verificationGirls = verifGirls,
                            femaleGradeAVerifications = femaleGradeAVerifs,
                            femaleGradeBVerifications = femaleGradeBVerifs,
                            femaleGradeCVerifications = femaleGradeCVerifs,
                            femaleGradeDVerifications = femaleGradeDVerifs,
                            femaleNormalVerifications = femaleGradeBVerifs,
                            femaleDocVerifications = femaleGradeAVerifs,
                            totalApprovedVerifications = totalApprovedVerifications,
                            gradeA = gradeA,
                            gradeB = gradeB,
                            gradeC = gradeC,
                            gradeD = gradeD,
                            totalVerifications = totalVerifications
                        },
                        targets = new
                        {
                            targetPeriod = isDaily ? "Day" : (period ?? "Month"),
                            verificationTargets = new
                            {
                                maleTarget = maleVerifTarget,
                                maleAchieved = verifBoys,
                                maleProgressPercent = maleVerifProgress,
                                femaleTarget = femaleVerifTarget,
                                femaleAchieved = verifGirls,
                                femaleProgressPercent = femaleVerifProgress,
                                totalTarget = totalVerifTarget,
                                totalAchieved = totalApprovedVerifications,
                                totalProgressPercent = totalVerifProgress,
                                dailyTargets = new
                                {
                                    male = perfTargetConfig?.MaleVerificationDailyTarget ?? 0,
                                    female = perfTargetConfig?.FemaleVerificationDailyTarget ?? 0,
                                    total = (perfTargetConfig?.MaleVerificationDailyTarget ?? 0) + (perfTargetConfig?.FemaleVerificationDailyTarget ?? 0)
                                },
                                monthlyTargets = new
                                {
                                    male = perfTargetConfig?.MaleVerificationMonthlyTarget ?? 0,
                                    female = perfTargetConfig?.FemaleVerificationMonthlyTarget ?? 0,
                                    total = (perfTargetConfig?.MaleVerificationMonthlyTarget ?? 0) + (perfTargetConfig?.FemaleVerificationMonthlyTarget ?? 0)
                                }
                            },
                            conversionTargets = new
                            {
                                maleTarget = maleConvTarget,
                                maleAchieved = convBoys,
                                maleProgressPercent = maleConvProgress,
                                femaleTarget = femaleConvTarget,
                                femaleAchieved = convGirls,
                                femaleProgressPercent = femaleConvProgress,
                                totalTarget = totalConvTarget,
                                totalAchieved = totalPremiumConversions,
                                totalProgressPercent = totalConvProgress,
                                dailyTargets = new
                                {
                                    male = perfTargetConfig?.MaleConversionDailyTarget ?? 0,
                                    female = perfTargetConfig?.FemaleConversionDailyTarget ?? 0,
                                    total = (perfTargetConfig?.MaleConversionDailyTarget ?? 0) + (perfTargetConfig?.FemaleConversionDailyTarget ?? 0)
                                },
                                monthlyTargets = new
                                {
                                    male = perfTargetConfig?.MaleConversionMonthlyTarget ?? 0,
                                    female = perfTargetConfig?.FemaleConversionMonthlyTarget ?? 0,
                                    total = (perfTargetConfig?.MaleConversionMonthlyTarget ?? 0) + (perfTargetConfig?.FemaleConversionMonthlyTarget ?? 0)
                                }
                            },
                            overall = new
                            {
                                totalTarget = totalPeriodTarget,
                                totalAchieved = totalPeriodAchieved,
                                achievementPercent = targetAchievementPercent
                            },
                            dailyTarget = perfTargetConfig != null ? (perfTargetConfig.MaleVerificationDailyTarget + perfTargetConfig.FemaleVerificationDailyTarget + perfTargetConfig.MaleConversionDailyTarget + perfTargetConfig.FemaleConversionDailyTarget) : 0,
                            monthlyTarget = perfTargetConfig != null ? (perfTargetConfig.MaleVerificationMonthlyTarget + perfTargetConfig.FemaleVerificationMonthlyTarget + perfTargetConfig.MaleConversionMonthlyTarget + perfTargetConfig.FemaleConversionMonthlyTarget) : 0,
                            targetAchievementPercent = targetAchievementPercent,
                            dailyTargetAchievementPercent = targetAchievementPercent
                        },
                        incentives = new
                        {
                            verificationIncentiveBoys = Math.Round(verifIncentiveBoys, 2),
                            verificationIncentiveGirlsGradeA = Math.Round(verifIncentiveGirlsGradeA, 2),
                            verificationIncentiveGirlsGradeB = Math.Round(verifIncentiveGirlsGradeB, 2),
                            verificationIncentiveGirlsGradeC = Math.Round(verifIncentiveGirlsGradeC, 2),
                            verificationIncentiveGirlsGradeD = Math.Round(verifIncentiveGirlsGradeD, 2),
                            verificationIncentiveGirlsNormal = Math.Round(verifIncentiveGirlsGradeB, 2),
                            verificationIncentiveGirlsDoc = Math.Round(verifIncentiveGirlsGradeA, 2),
                            verificationIncentiveGirls = Math.Round(verifIncentiveGirls, 2),
                            totalVerificationIncentive = Math.Round(totalVerifIncentive, 2),
                            premiumIncentiveBoys = Math.Round(premiumIncentiveBoys, 2),
                            premiumIncentiveGirls = Math.Round(premiumIncentiveGirls, 2),
                            totalPremiumIncentive = Math.Round(premiumIncentiveBoys + premiumIncentiveGirls, 2),
                            adminIncentiveTotal = Math.Round(adminIncentive, 2),
                            adminIncentiveItems = adminIncentiveItems,
                            totalIncentivePayable = Math.Round(totalIncentivePayableWithAdmin, 2)
                        },
                        payroll = new
                        {
                            basicSalary = Math.Round(basicSalary, 2),
                            adminIncentiveTotal = Math.Round(adminIncentive, 2),
                            adminIncentiveItems = adminIncentiveItems,
                            totalIncentive = Math.Round(totalIncentivePayableWithAdmin, 2),
                            paidLeavesCount = paidLeavesCount,
                            unpaidLeavesCount = unpaidLeavesCount,
                            leaveDeduction = Math.Round(leaveDeduction, 2),
                            complaintDeduction = Math.Round(complaintDeduction, 2),
                            deletedProfileDeduction = Math.Round(deletedProfileDeduction, 2),
                            deletedProfileDeductions = deletedProfileItems,
                            totalDeduction = Math.Round(totalDeduction, 2),
                            estimatedNetSalary = Math.Round(estimatedNetSalary, 2),
                            payrollStatus = payrollStatus
                        },
                        rating = new
                        {
                            performanceScore = performanceScore,
                            performanceGrade = performanceGrade
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching performance report for staff");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching the staff performance report."
                });
            }
        }

        private long GetCurrentStaffId()
        {
            var claimVal = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                ?? User.Identity?.Name;
                
            if (long.TryParse(claimVal, out long id))
            {
                return id;
            }
            return 0;
        }

        #region Staff Leave Records API

        /// <summary>
        /// Get leave records for the logged-in staff member (or target staff).
        /// </summary>
        [HttpGet("leave-records")]
        public async Task<IActionResult> GetLeaveRecords([FromQuery] long? staffId, [FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                long targetStaffId = staffId ?? GetCurrentStaffId();
                if (targetStaffId <= 0) return BadRequest(new { success = false, message = "Invalid Staff ID." });

                var query = _dbContext.StaffLeaveRecords
                    .Where(l => l.StaffId == targetStaffId && !l.IsDeleted);

                if (year.HasValue && year.Value > 0)
                {
                    query = query.Where(l => l.Year == year.Value);
                }

                if (month.HasValue && month.Value > 0)
                {
                    query = query.Where(l => l.Month == month.Value);
                }

                var list = await query
                    .OrderByDescending(l => l.LeaveDate)
                    .ThenByDescending(l => l.CreatedOn)
                    .ThenByDescending(l => l.Id)
                    .Select(l => new
                    {
                        l.Id,
                        l.StaffId,
                        leaveDate = l.LeaveDate.ToString("yyyy-MM-dd"),
                        l.LeaveType,
                        l.Reason,
                        l.IsPaid,
                        l.IsApproved,
                        l.ApprovedBy,
                        l.Year,
                        l.Month,
                        createdOn = l.CreatedOn.ToString("yyyy-MM-dd HH:mm")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Leave records fetched successfully.",
                    data = list
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching leave records");
                return StatusCode(500, new { success = false, message = "An error occurred while fetching leave records." });
            }
        }

        /// <summary>
        /// Insert or update a leave record for staff.
        /// </summary>
        [HttpPost("leave-records")]
        public async Task<IActionResult> SaveLeaveRecord([FromBody] SaveLeaveRecordApiModel model)
        {
            try
            {
                if (model == null) return BadRequest(new { success = false, message = "Invalid request payload." });

                long targetStaffId = model.StaffId > 0 ? model.StaffId : GetCurrentStaffId();
                if (targetStaffId <= 0) return BadRequest(new { success = false, message = "Invalid Staff ID." });

                if (string.IsNullOrWhiteSpace(model.LeaveType))
                {
                    return BadRequest(new { success = false, message = "LeaveType is required ('FullDay', 'HalfDay', 'Late')." });
                }

                DateTime leaveDate = model.LeaveDate != default ? model.LeaveDate : DateTime.UtcNow.Date;

                if (model.Id > 0)
                {
                    var existing = await _dbContext.StaffLeaveRecords.FirstOrDefaultAsync(l => l.Id == model.Id && !l.IsDeleted);
                    if (existing == null) return NotFound(new { success = false, message = "Leave record not found." });

                    existing.LeaveDate = leaveDate;
                    existing.LeaveType = model.LeaveType;
                    existing.Reason = model.Reason;
                    existing.Year = leaveDate.Year;
                    existing.Month = leaveDate.Month;

                    _dbContext.StaffLeaveRecords.Update(existing);
                    await _dbContext.SaveChangesAsync();

                    return Ok(new { success = true, message = "Leave record updated successfully.", data = existing });
                }
                else
                {
                    var newRecord = new StaffLeaveRecord
                    {
                        StaffId = targetStaffId,
                        LeaveDate = leaveDate,
                        LeaveType = model.LeaveType,
                        Reason = model.Reason,
                        IsPaid = false, // Admin decides Paid vs Unpaid upon approval
                        IsApproved = false, // Requires Admin Approval
                        ApprovedBy = null,
                        Year = leaveDate.Year,
                        Month = leaveDate.Month
                    };

                    await _dbContext.StaffLeaveRecords.AddAsync(newRecord);
                    await _dbContext.SaveChangesAsync();

                    return Ok(new { success = true, message = "Leave record created successfully.", data = newRecord });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving leave record");
                return StatusCode(500, new { success = false, message = "An error occurred while saving leave record." });
            }
        }

        /// <summary>
        /// Soft delete a leave record.
        /// </summary>
        [HttpDelete("leave-records/{id:long}")]
        public async Task<IActionResult> DeleteLeaveRecord(long id)
        {
            try
            {
                var record = await _dbContext.StaffLeaveRecords.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
                if (record == null) return NotFound(new { success = false, message = "Leave record not found." });

                record.IsDeleted = true;
                _dbContext.StaffLeaveRecords.Update(record);
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Leave record deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting leave record");
                return StatusCode(500, new { success = false, message = "An error occurred while deleting leave record." });
            }
        }

        #endregion

        #region Staff Complaint Records API

        /// <summary>
        /// Get complaint records for staff.
        /// </summary>
        [HttpGet("complaint-records")]
        public async Task<IActionResult> GetComplaintRecords([FromQuery] long? staffId, [FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                long targetStaffId = staffId ?? GetCurrentStaffId();
                if (targetStaffId <= 0) return BadRequest(new { success = false, message = "Invalid Staff ID." });

                int filterYear = year ?? DateTime.UtcNow.Year;
                int filterMonth = month ?? DateTime.UtcNow.Month;

                var query = _dbContext.StaffComplaintRecords
                    .Where(c => c.StaffId == targetStaffId && !c.IsDeleted && c.Year == filterYear);

                if (month.HasValue && month.Value > 0)
                {
                    query = query.Where(c => c.Month == filterMonth);
                }

                var list = await query
                    .OrderByDescending(c => c.ComplaintDate)
                    .Select(c => new
                    {
                        c.Id,
                        c.StaffId,
                        complaintDate = c.ComplaintDate.ToString("yyyy-MM-dd"),
                        c.ComplaintDescription,
                        c.ComplaintLevel,
                        c.DeductionAmount,
                        c.IsApproved,
                        c.ApprovedBy,
                        c.Resolution,
                        c.Year,
                        c.Month,
                        createdOn = c.CreatedOn.ToString("yyyy-MM-dd HH:mm")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Complaint records fetched successfully.",
                    data = list
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching complaint records");
                return StatusCode(500, new { success = false, message = "An error occurred while fetching complaint records." });
            }
        }

        /// <summary>
        /// Insert or update a complaint record for staff.
        /// </summary>
        [HttpPost("complaint-records")]
        public async Task<IActionResult> SaveComplaintRecord([FromBody] SaveComplaintRecordApiModel model)
        {
            try
            {
                if (model == null) return BadRequest(new { success = false, message = "Invalid request payload." });

                long targetStaffId = model.StaffId > 0 ? model.StaffId : GetCurrentStaffId();
                if (targetStaffId <= 0) return BadRequest(new { success = false, message = "Invalid Staff ID." });

                DateTime complaintDate = model.ComplaintDate != default ? model.ComplaintDate : DateTime.UtcNow.Date;

                if (model.Id > 0)
                {
                    var existing = await _dbContext.StaffComplaintRecords.FirstOrDefaultAsync(c => c.Id == model.Id && !c.IsDeleted);
                    if (existing == null) return NotFound(new { success = false, message = "Complaint record not found." });

                    existing.ComplaintDate = complaintDate;
                    existing.ComplaintDescription = model.ComplaintDescription;
                    existing.ComplaintLevel = model.ComplaintLevel;
                    existing.DeductionAmount = model.DeductionAmount;
                    existing.IsApproved = model.IsApproved;
                    existing.Resolution = model.Resolution;
                    existing.Year = complaintDate.Year;
                    existing.Month = complaintDate.Month;

                    _dbContext.StaffComplaintRecords.Update(existing);
                    await _dbContext.SaveChangesAsync();

                    return Ok(new { success = true, message = "Complaint record updated successfully.", data = existing });
                }
                else
                {
                    var newRecord = new StaffComplaintRecord
                    {
                        StaffId = targetStaffId,
                        ComplaintDate = complaintDate,
                        ComplaintDescription = model.ComplaintDescription,
                        ComplaintLevel = model.ComplaintLevel,
                        DeductionAmount = model.DeductionAmount,
                        IsApproved = model.IsApproved,
                        ApprovedBy = User.Identity?.Name ?? "System",
                        Resolution = model.Resolution,
                        Year = complaintDate.Year,
                        Month = complaintDate.Month
                    };

                    await _dbContext.StaffComplaintRecords.AddAsync(newRecord);
                    await _dbContext.SaveChangesAsync();

                    return Ok(new { success = true, message = "Complaint record created successfully.", data = newRecord });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving complaint record");
                return StatusCode(500, new { success = false, message = "An error occurred while saving complaint record." });
            }
        }

        /// <summary>
        /// Soft delete a complaint record.
        /// </summary>
        [HttpDelete("complaint-records/{id:long}")]
        public async Task<IActionResult> DeleteComplaintRecord(long id)
        {
            try
            {
                var record = await _dbContext.StaffComplaintRecords.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (record == null) return NotFound(new { success = false, message = "Complaint record not found." });

                record.IsDeleted = true;
                _dbContext.StaffComplaintRecords.Update(record);
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Complaint record deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting complaint record");
                return StatusCode(500, new { success = false, message = "An error occurred while deleting complaint record." });
            }
        }

        /// <summary>
        /// Updates the phone number and/or email address for a registration profile in progress and resends OTP.
        /// </summary>
        [HttpPost("update-phone-or-email-and-resend-otp")]
        public async Task<IActionResult> UpdatePhoneOrEmailAndResendOtp([FromForm] UpdatePhoneOrEmailOtpRequest model)
        {
            try
            {
                // Support JSON body fallback if Content-Type is application/json
                if (Request.ContentType != null && Request.ContentType.Contains("application/json"))
                {
                    try
                    {
                        using var reader = new System.IO.StreamReader(Request.Body);
                        var body = await reader.ReadToEndAsync();
                        var jsonModel = System.Text.Json.JsonSerializer.Deserialize<UpdatePhoneOrEmailOtpRequest>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (jsonModel != null)
                        {
                            model = jsonModel;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize JSON in UpdatePhoneOrEmailAndResendOtp (Staff)");
                    }
                }

                if (model == null || model.Id <= 0)
                {
                    return BadRequest(new { success = false, message = "Registration ID is required." });
                }

                if (string.IsNullOrWhiteSpace(model.Phone) && string.IsNullOrWhiteSpace(model.Email))
                {
                    return BadRequest(new { success = false, message = "Please provide a phone number or email address to update." });
                }

                var entity = await _registrationRepo.Get(model.Id);
                if (entity == null || entity.IsDeleted)
                {
                    return NotFound(new { success = false, message = "Registration profile not found." });
                }

                bool phoneUpdated = false;
                bool emailUpdated = false;

                // Validate and update Phone if provided
                if (!string.IsNullOrWhiteSpace(model.Phone) && model.Phone.Trim() != entity.Phone)
                {
                    var cleanPhone = model.Phone.Trim();
                    if (cleanPhone.Length < 8 || cleanPhone.Length > 15 || !cleanPhone.All(char.IsDigit))
                    {
                        return BadRequest(new { success = false, message = "Please provide a valid phone number." });
                    }

                    var phoneExists = await _registrationRepo.WhereActive(x => x.Phone == cleanPhone && x.Id != model.Id && x.IsVerified);
                    if (phoneExists.Any())
                    {
                        return BadRequest(new { success = false, message = "Phone number already exists!" });
                    }

                    entity.Phone = cleanPhone;
                    if (!string.IsNullOrEmpty(model.CountryCode))
                    {
                        entity.CountryCode = model.CountryCode.Trim();
                    }
                    phoneUpdated = true;
                }

                // Validate and update Email if provided
                if (!string.IsNullOrWhiteSpace(model.Email) && model.Email.Trim() != entity.Email)
                {
                    var cleanEmail = model.Email.Trim();
                    if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(cleanEmail))
                    {
                        return BadRequest(new { success = false, message = "Please provide a valid email address." });
                    }

                    var emailExists = await _registrationRepo.WhereActive(x => x.Email == cleanEmail && x.Id != model.Id && x.IsVerified);
                    if (emailExists.Any())
                    {
                        return BadRequest(new { success = false, message = "Email address already exists!" });
                    }

                    entity.Email = cleanEmail;
                    emailUpdated = true;
                }

                // Generate fresh OTP pin
                Random rdm = new Random();
                string pin = rdm.Next(111111, 999999).ToString();
                //string pin = "123456";

                entity.VerificationCode = pin;
                entity.OtpGeneratedAt = DateTime.Now;
                entity.OtpResendCount++;

                await _registrationRepo.Update(entity);
                await _registrationRepo.SaveChanges();

                // Send OTP via SMS

                if (!string.IsNullOrEmpty(entity.Phone))
                {
                    try
                    {
                        await _emailService.SendSmsAsync(entity.VerificationCode, entity.Phone);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send SMS to {Phone}", entity.Phone);
                    }
                }

                // Send OTP via Email

                if (!string.IsNullOrEmpty(entity.Email))
                {
                    try
                    {
                        var htmlContent = $"Dear Customer, <br/><br/>{pin} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
                        var htmlContentView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                        _emailNotificationHelper.SendEmail(entity.Email, htmlContentView, "Your Resent OTP for M4nikah");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send Email to {Email}", entity.Email);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Contact details updated and OTP resent successfully.",
                    phone = entity.Phone,
                    email = entity.Email,
                    countryCode = entity.CountryCode,
                    verificationCode = entity.VerificationCode,
                    resendCount = entity.OtpResendCount,
                    phoneUpdated = phoneUpdated,
                    emailUpdated = emailUpdated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdatePhoneOrEmailAndResendOtp for ID: {Id}", model?.Id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating contact details and resending OTP." });
            }
        }

        #endregion

        #region Helper Methods

        private async Task CreateOrUpdateStaffFollowUpsAsync(long profileId, long staffId)
        {
            try
            {
                var staffUser = await _userManager.FindByIdAsync(staffId.ToString());
                string staffName = staffUser?.NormalizedUserName ?? staffUser?.UserName ?? "Staff";

                // 1. Auto-generate or update Profile Verification Follow-up
                var existingVerifFollowUp = (await _followUpRepo.Where(f => f.ProfileId == profileId && f.FollowUpType == FollowUpType.ProfileVerification && !f.IsDeleted)).FirstOrDefault();
                if (existingVerifFollowUp != null)
                {
                    if (existingVerifFollowUp.AssignedStaffId != staffId)
                    {
                        existingVerifFollowUp.AssignedStaffId = staffId;
                        await _followUpRepo.Update(existingVerifFollowUp);

                        var reassignVerifTimeline = new FollowUpTimeline
                        {
                            FollowUpId = existingVerifFollowUp.Id,
                            StaffId = staffId,
                            StaffName = staffName,
                            ContactType = existingVerifFollowUp.LatestContactType,
                            CallStatus = existingVerifFollowUp.LatestCallStatus,
                            InterestStatus = existingVerifFollowUp.LatestInterestStatus,
                            ProfileVerificationStatus = existingVerifFollowUp.LatestProfileVerificationStatus,
                            RenewalInterestStatus = existingVerifFollowUp.LatestRenewalInterestStatus,
                            Remarks = $"Reassigned to {staffName} (Auto-generated)",
                            NextFollowUpDate = existingVerifFollowUp.NextFollowUpDate,
                            IsActive = true
                        };
                        await _followUpTimelineRepo.Add(reassignVerifTimeline);
                    }
                }
                else
                {
                    var verifFollowUp = new FollowUp
                    {
                        ProfileId = profileId,
                        FollowUpType = FollowUpType.ProfileVerification,
                        LatestContactType = null,
                        LatestCallStatus = null,
                        LatestInterestStatus = null,
                        LatestProfileVerificationStatus = ProfileVerificationStatus.Pending,
                        LatestRenewalInterestStatus = null,
                        LatestRemarks = "Auto-generated on assignment",
                        NextFollowUpDate = null,
                        AssignedStaffId = staffId,
                        IsActive = true
                    };
                    await _followUpRepo.Add(verifFollowUp);
                    await _followUpRepo.SaveChanges();

                    var verifTimeline = new FollowUpTimeline
                    {
                        FollowUpId = verifFollowUp.Id,
                        StaffId = staffId,
                        StaffName = staffName,
                        ContactType = null,
                        CallStatus = null,
                        InterestStatus = null,
                        ProfileVerificationStatus = ProfileVerificationStatus.Pending,
                        RenewalInterestStatus = null,
                        Remarks = "Auto-generated on assignment",
                        NextFollowUpDate = null,
                        IsActive = true
                    };
                    await _followUpTimelineRepo.Add(verifTimeline);
                }

                // 2. Auto-generate or update Premium Follow-up
                var existingPremFollowUp = (await _followUpRepo.Where(f => f.ProfileId == profileId && f.FollowUpType == FollowUpType.PremiumFollowUp && !f.IsDeleted)).FirstOrDefault();
                if (existingPremFollowUp != null)
                {
                    if (existingPremFollowUp.AssignedStaffId != staffId)
                    {
                        existingPremFollowUp.AssignedStaffId = staffId;
                        await _followUpRepo.Update(existingPremFollowUp);

                        var reassignPremTimeline = new FollowUpTimeline
                        {
                            FollowUpId = existingPremFollowUp.Id,
                            StaffId = staffId,
                            StaffName = staffName,
                            ContactType = existingPremFollowUp.LatestContactType,
                            CallStatus = existingPremFollowUp.LatestCallStatus,
                            InterestStatus = existingPremFollowUp.LatestInterestStatus,
                            ProfileVerificationStatus = existingPremFollowUp.LatestProfileVerificationStatus,
                            RenewalInterestStatus = existingPremFollowUp.LatestRenewalInterestStatus,
                            Remarks = $"Reassigned to {staffName} (Auto-generated)",
                            NextFollowUpDate = existingPremFollowUp.NextFollowUpDate,
                            IsActive = true
                        };
                        await _followUpTimelineRepo.Add(reassignPremTimeline);
                    }
                }
                else
                {
                    var premFollowUp = new FollowUp
                    {
                        ProfileId = profileId,
                        FollowUpType = FollowUpType.PremiumFollowUp,
                        LatestContactType = null,
                        LatestCallStatus = null,
                        LatestInterestStatus = null,
                        LatestProfileVerificationStatus = null,
                        LatestRenewalInterestStatus = null,
                        LatestRemarks = "Auto-generated on assignment",
                        NextFollowUpDate = null,
                        AssignedStaffId = staffId,
                        IsActive = true
                    };
                    await _followUpRepo.Add(premFollowUp);
                    await _followUpRepo.SaveChanges();

                    var premTimeline = new FollowUpTimeline
                    {
                        FollowUpId = premFollowUp.Id,
                        StaffId = staffId,
                        StaffName = staffName,
                        ContactType = null,
                        CallStatus = null,
                        InterestStatus = null,
                        ProfileVerificationStatus = null,
                        RenewalInterestStatus = null,
                        Remarks = "Auto-generated on assignment",
                        NextFollowUpDate = null,
                        IsActive = true
                    };
                    await _followUpTimelineRepo.Add(premTimeline);
                }

                await _followUpRepo.SaveChanges();
                await _followUpTimelineRepo.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or updating follow-ups for staff assignment (ProfileId: {ProfileId}, StaffId: {StaffId})", profileId, staffId);
            }
        }

        #endregion
    }
}





