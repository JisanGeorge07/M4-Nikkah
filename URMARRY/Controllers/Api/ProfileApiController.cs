using Application.Constants;
using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using URMARRY.Services;
using URMARRY.Models;
using Application.Models.Transactions;
using Application.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Text.Json.Serialization;

namespace URMARRY.Controllers.Api
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileApiController : ControllerBase
    {
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IMatchingProfileRepo _matchingRepo;
        private readonly IMapper _mapper;
        private readonly IRepository<BodyFeatures> _bodyFeaturesRepo;
        private readonly IRepository<MaritalStatus> _maritalStatusRepo;
        private readonly IRepository<Profession> _professionRepo;
        private readonly IRepository<Community> _communityRepo;
        private readonly IRepository<ReligionCaste> _religionCasteRepo;
        private readonly IProfileService _profileService;
        private readonly IRepository<UserContactView> _contactViewRepo;
        private readonly IRepository<UserReport> _userReportRepo;
        private readonly IRepository<UserNotLikeProfile> _notLikeRepo;
        private readonly IRepository<UserFavouriteProfile> _favouriteRepo;
        private readonly IUserService _userService;
        private readonly IRepository<PhotoUnlockRequest> _photoUnlockRequestRepo;
        private readonly IRepository<Images> _imagesRepo;
        private readonly IRepository<UserStarProfile> _userStarProfileRepo;
        private readonly INotifcationRepository _notificationPageRepo;
        private readonly IRepository<MatchStatusUpdate> _matchStatusUpdateRepo;
        private readonly IRepository<SuccessStory> _successStoryRepo;
        private readonly IFileService _fileService;
        private readonly IRepository<ProfileFor> _profileForRepo;
        private readonly IRepository<Nationality> _nationalityRepo;
        private readonly IRepository<MotherTongue> _motherTongueRepo;
        private readonly IRepository<Religiousness> _religiousnessRepo;
        private readonly IRepository<FinancialStatus> _financialStatusRepo;
        private readonly IRepository<State> _stateRepo;
        private readonly IRepository<UserReportReason> _userReportReasonRepo;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IRepository<PlanPurchase> _planPurchaseRepo;
        private readonly PayUSettings _payUSettings;
        private readonly IWebHostEnvironment _env;
        private readonly EmailNotificationHelper _emailNotificationHelper;
        private readonly IRepository<VerificationDocument> _verificationDocRepo;
        private readonly IRepository<FollowUp> _followUpRepo;
        private readonly IRepository<FollowUpTimeline> _followUpTimelineRepo;
        private readonly PresenceTracker _presenceTracker;

        public ProfileApiController(
            IRepository<Registration> registrationRepo,
            IMatchingProfileRepo matchingRepo,
            IMapper mapper,
            IRepository<BodyFeatures> bodyFeaturesRepo,
            IRepository<MaritalStatus> maritalStatusRepo,
            IRepository<Profession> professionRepo,
            IRepository<Community> communityRepo,
            IRepository<ReligionCaste> religionCasteRepo,
            IProfileService profileService,
            IRepository<UserContactView> contactViewRepo,
            IRepository<UserReport> userReportRepo,
            IRepository<UserNotLikeProfile> notLikeRepo,
            IRepository<UserFavouriteProfile> favouriteRepo,
            IUserService userService,
            IRepository<PhotoUnlockRequest> photoUnlockRequestRepo,
            IRepository<Images> imagesRepo,
            IRepository<UserStarProfile> userStarProfileRepo,
            INotifcationRepository notificationPageRepo,
            IRepository<MatchStatusUpdate> matchStatusUpdateRepo,
            IRepository<SuccessStory> successStoryRepo,
            IFileService fileService,
            IRepository<ProfileFor> profileForRepo,
            IRepository<Nationality> nationalityRepo,
            IRepository<MotherTongue> motherTongueRepo,
            IRepository<Religiousness> religiousnessRepo,
            IRepository<FinancialStatus> financialStatusRepo,
            IRepository<State> stateRepo,
            IRepository<UserReportReason> userReportReasonRepo,
            ITransactionRepository transactionRepository,
            IRepository<PlanPurchase> planPurchaseRepo,
            IOptions<PayUSettings> payUSettings,
            IWebHostEnvironment env,
            EmailNotificationHelper emailNotificationHelper,
            IRepository<VerificationDocument> verificationDocRepo,
            IRepository<FollowUp> followUpRepo,
            IRepository<FollowUpTimeline> followUpTimelineRepo,
            PresenceTracker presenceTracker)
        {
            _registrationRepo = registrationRepo;
            _matchingRepo = matchingRepo;
            _mapper = mapper;
            _bodyFeaturesRepo = bodyFeaturesRepo;
            _maritalStatusRepo = maritalStatusRepo;
            _professionRepo = professionRepo;
            _communityRepo = communityRepo;
            _religionCasteRepo = religionCasteRepo;
            _profileService = profileService;
            _contactViewRepo = contactViewRepo;
            _userReportRepo = userReportRepo;
            _notLikeRepo = notLikeRepo;
            _favouriteRepo = favouriteRepo;
            _userService = userService;
            _photoUnlockRequestRepo = photoUnlockRequestRepo;
            _imagesRepo = imagesRepo;
            _userStarProfileRepo = userStarProfileRepo;
            _notificationPageRepo = notificationPageRepo;
            _matchStatusUpdateRepo = matchStatusUpdateRepo;
            _successStoryRepo = successStoryRepo;
            _fileService = fileService;
            _profileForRepo = profileForRepo;
            _nationalityRepo = nationalityRepo;
            _motherTongueRepo = motherTongueRepo;
            _religiousnessRepo = religiousnessRepo;
            _financialStatusRepo = financialStatusRepo;
            _stateRepo = stateRepo;
            _userReportReasonRepo = userReportReasonRepo;
            _transactionRepository = transactionRepository;
            _planPurchaseRepo = planPurchaseRepo;
            _payUSettings = payUSettings.Value;
            _env = env;
            _emailNotificationHelper = emailNotificationHelper;
            _verificationDocRepo = verificationDocRepo;
            _followUpRepo = followUpRepo;
            _followUpTimelineRepo = followUpTimelineRepo;
            _presenceTracker = presenceTracker;
        }

        [HttpPost("notlike")]
        public async Task<IActionResult> NotLikeProfile([FromForm] long userId, [FromForm] long notLikedId)
        {
            try
            {
                var result = await _profileService.NotLikeProfileAsync(userId, notLikedId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("homeprofiles")]
        public async Task<IActionResult> GetHomeProfiles(
    long userId,
    string type,
    int page = 1,
    int pageSize = 30)
        {
            try
            {
                int skip = (page - 1) * pageSize;

                var loginUser = await _registrationRepo.Get(userId);

                if (loginUser == null)
                    return NotFound("User not found");

                bool viewerIsPremium = loginUser.IsPremiumMember || await _userService.IsPremiumUser(userId);

                var allUnlockRequests = await _photoUnlockRequestRepo.WhereActive(x => x.RequesterId == userId);
                var approvedUnlockRequests = allUnlockRequests
                    .Where(x => x.Status == PhotoUnlockStatus.Approved)
                    .Select(x => x.OwnerId)
                    .ToHashSet();
                var unlockRequestDict = allUnlockRequests
                    .GroupBy(x => x.OwnerId)
                    .ToDictionary(g => g.Key, g => g.First().Status);

                var acceptedInterestIds = (await _favouriteRepo.WhereActive(x =>
                    (x.UserId == userId || x.LikedId == userId) && x.Status == Domain.InterestStatus.Accepted
                ))
                .Select(x => x.UserId == userId ? x.LikedId : x.UserId)
                .ToHashSet();

                // Get not-liked profile IDs to exclude from all tabs
                var notLikedIds = await _profileService.GetNotLikedProfileIdsAsync(userId);
                // Get viewed profile IDs to exclude
                var viewedIds = (await _contactViewRepo.WhereActive(x => x.ViewerUserId == userId))
                    .Select(x => x.ViewedUserId)
                    .Distinct()
                    .ToHashSet();

                var list = new List<Registration>();

                // Get all matching profiles for the user
                var matches = await _matchingRepo.GetMatchingUsersWithPercentage((int)userId);
                var matchingProfilesDict = matches
                    .GroupBy(mp => mp.Id)
                    .ToDictionary(g => g.Key, g => g.Last());

                // Combined exclusion set (not-liked + viewed + shortlisted)
                var excludeIds = new HashSet<long>(notLikedIds);
                foreach (var vid in viewedIds) excludeIds.Add(vid);
                foreach (var m in matches)
                {
                    if (m.IsStarred) excludeIds.Add(m.Id);
                }

                var matchIds = matches.Select(x => x.Id).Distinct().ToHashSet();

                switch ((type ?? "").ToLower())
                {
                    case "mymatching":
                        // Fetch all non-excluded matching IDs in order
                        var allMatchingIds = matches
                            .Where(m => !excludeIds.Contains(m.Id))
                            .Select(m => m.Id)
                            .ToList();

                        // Fetch registrations, filter by visibility, and restore matching order
                        var registrations = await _registrationRepo.GetAllByIds(allMatchingIds);
                        var visibleRegs = registrations.Where(x => x.IsVisible).ToList();
                        var regDict = visibleRegs.ToDictionary(x => x.Id);
                        var orderedVisibleRegs = allMatchingIds
                            .Where(id => regDict.ContainsKey(id))
                            .Select(id => regDict[id])
                            .ToList();

                        // Order online users first, preserving match score order within online and offline groups
                        var onlineMatchIds = _presenceTracker.GetOnlineUserIds(orderedVisibleRegs.Select(x => x.Id));
                        var sortedVisibleRegs = orderedVisibleRegs
                            .OrderByDescending(x => onlineMatchIds.Contains(x.Id))
                            .ToList();

                        // Paginate the fully filtered and ordered list
                        list = sortedVisibleRegs
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();
                        break;

                    case "newmatching":
                        list = (await _registrationRepo.WhereActive(x =>
                            matchIds.Contains(x.Id) &&
                            x.CreatedOn >= DateTime.UtcNow.AddMonths(-1) &&
                            x.IsVisible &&
                            !excludeIds.Contains(x.Id)))
                            .OrderByDescending(x => x.CreatedOn)
                            .Skip(skip).Take(pageSize).ToList();
                        break;

                    case "mylocation":
                        list = (await _registrationRepo.WhereActive(x =>
                            matchIds.Contains(x.Id) &&
                            x.District == loginUser.District &&
                            x.IsVisible &&
                            !excludeIds.Contains(x.Id)))
                            .OrderByDescending(x => x.CreatedOn)
                            .Skip(skip).Take(pageSize).ToList();
                        break;

                    case "samefamilystatus":
                        list = (await _registrationRepo.WhereActive(x =>
                            matchIds.Contains(x.Id) &&
                            x.FinancialStatusId == loginUser.FinancialStatusId &&
                            x.IsVisible &&
                            !excludeIds.Contains(x.Id)))
                            .OrderByDescending(x => x.CreatedOn)
                            .Skip(skip).Take(pageSize).ToList();
                        break;

                    case "sameeducation":
                        list = (await _registrationRepo.WhereActive(x =>
                            matchIds.Contains(x.Id) &&
                            x.HighestEducation == loginUser.HighestEducation &&
                            x.IsVisible &&
                            !excludeIds.Contains(x.Id)))
                            .OrderByDescending(x => x.CreatedOn)
                            .Skip(skip).Take(pageSize).ToList();
                        break;

                    case "sameprofession":
                        list = (await _registrationRepo.WhereActive(x =>
                            matchIds.Contains(x.Id) &&
                            x.ProfessionId == loginUser.ProfessionId &&
                            x.IsVisible &&
                            !excludeIds.Contains(x.Id)))
                            .OrderByDescending(x => x.CreatedOn)
                            .Skip(skip).Take(pageSize).ToList();
                        break;

                    case "physicallychallenged":
                        list = (await _registrationRepo.WhereActive(x =>
                            matchIds.Contains(x.Id) &&
                            x.IsPhysicallyChallenged == true &&
                            x.IsVisible &&
                            !excludeIds.Contains(x.Id)))
                            .OrderByDescending(x => x.CreatedOn)
                            .Skip(skip).Take(pageSize).ToList();
                        break;

                    case "onlineprofiles":
                    case "online":
                        var allOnlineUserIds = _presenceTracker.GetAllOnlineUserIds();
                        var onlineMatchingIds = matches
                            .Where(m => allOnlineUserIds.Contains(m.Id) && !excludeIds.Contains(m.Id))
                            .OrderByDescending(m => m.total_matching_score)
                            .Select(m => m.Id)
                            .ToList();

                        var onlineRegs = await _registrationRepo.GetAllByIds(onlineMatchingIds);
                        var visibleOnlineRegs = onlineRegs.Where(x => x.IsVisible).ToList();
                        var onlineRegDict = visibleOnlineRegs.ToDictionary(x => x.Id);
                        var orderedOnlineRegs = onlineMatchingIds
                            .Where(id => onlineRegDict.ContainsKey(id))
                            .Select(id => onlineRegDict[id])
                            .ToList();

                        list = orderedOnlineRegs
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();
                        break;
                }

                // Load lookup data for resolving IDs to display text
                var allBodyFeaturesList = await _bodyFeaturesRepo.GetAllActive();
                var heights = allBodyFeaturesList
                    .Where(x => x.Type == BodyFeature.Height)
                    .ToDictionary(x => x.Id, x => x.Title);
                var weights = allBodyFeaturesList
                    .Where(x => x.Type == BodyFeature.Weight)
                    .ToDictionary(x => x.Id, x => x.Title);
                var maritalStatuses = (await _maritalStatusRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);
                var professions = (await _professionRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);
                var communities = (await _communityRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);
                var religions = (await _religionCasteRepo.WhereActive(x => x.ParentId == 0))
                    .ToDictionary(x => x.Id, x => x.Title);

                var userFavourites = await _favouriteRepo.WhereActive(x => x.UserId == userId);
                var favouriteStatusDict = userFavourites.GroupBy(x => x.LikedId).ToDictionary(g => g.Key, g => g.First().Status.ToString());

                var profileIds = list.Select(x => x.Id).Distinct().ToList();
                var onlineUserIds = _presenceTracker.GetOnlineUserIds(profileIds);
                var docsList = await _verificationDocRepo.WhereActive(d => profileIds.Contains(d.ProfileId));
                var docsDict = docsList
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    }).ToList());

                // Project to a slim response with resolved display text
                var profileData = list.Select(item =>
                {
                    // Calculate age
                    int age = 0;
                    try
                    {
                        string dtb = !string.IsNullOrEmpty(item.DOB) ? item.DOB : "01/01/1990";
                        string[] parts = dtb.Replace("-", "/").Trim().Split('/');
                        string day = parts[0];
                        string month = parts[1];
                        string year = parts[2];
                        if (day.Length < 2) day = "0" + day;
                        if (month.Length < 2) month = "0" + month;
                        if (day.Length > 2) { var temp = year; year = day; day = temp; }
                        string[] formats = { "d/M/yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd" };
                        string dobStr = day.Trim() + "/" + month.Trim() + "/" + year.Trim();
                        DateTime birthDate = DateTime.ParseExact(dobStr, formats,
                            System.Globalization.CultureInfo.InvariantCulture);
                        DateTime today = DateTime.UtcNow;
                        age = today.Year - birthDate.Year;
                        if (birthDate > today.AddYears(-age)) age--;
                    }
                    catch { }

                    // Resolve lookup values
                    string? heightText = heights.GetValueOrDefault(item.HeightId);
                    string? weightText = weights.GetValueOrDefault(item.WeightId);
                    string? maritalStatusText = maritalStatuses.GetValueOrDefault(item.MaritalStatusId);
                    string? professionText = professions.GetValueOrDefault(item.ProfessionId);
                    string? communityText = communities.GetValueOrDefault(item.CommunityId);
                    string? religionText = religions.GetValueOrDefault(item.ReligionId);

                    // Get starred/liked for mymatching
                    bool isStarred = false;
                    bool isLiked = false;
                    int totalMatchingScore = 0;
                    if (matchingProfilesDict.TryGetValue(item.Id, out var mp))
                    {
                        isStarred = mp.IsStarred;
                        isLiked = mp.IsLiked;
                        totalMatchingScore = mp.total_matching_score;
                    }

                    string interestStatus = favouriteStatusDict.TryGetValue(item.Id, out var statusStr) ? statusStr : "None";

                    // Photo Visibility calculation
                    bool photosUnlocked = false;
                    if (userId == item.Id)
                    {
                        photosUnlocked = true;
                    }
                    else
                    {
                        if (item.PhotoVisibleToAll)
                        {
                            photosUnlocked = true;
                        }
                        else
                        {
                            if (item.PhotoVisibleToPremium && viewerIsPremium)
                            {
                                photosUnlocked = true;
                            }
                            if (!photosUnlocked && item.PhotoVisibleToAccepted && acceptedInterestIds.Contains(item.Id))
                            {
                                photosUnlocked = true;
                            }
                            if (!photosUnlocked && approvedUnlockRequests.Contains(item.Id))
                            {
                                photosUnlocked = true;
                            }
                        }
                    }

                    docsDict.TryGetValue(item.Id, out var itemDocs);

                    return new
                    {
                        id = item.Id,
                        name = item.Name,
                        imagePath = item.ImagePath,
                        registerNumber = item.RegisterNumber,
                        age,
                        height = heightText,
                        weight = weightText,
                        maritalStatus = maritalStatusText,
                        educationType = item.EducationType,
                        higherEducation = item.HighestEducation,
                        profession = professionText,
                        community = communityText,
                        religion = religionText,
                        village = item.Village,
                        state = item.State,
                        district = item.District,
                        isStarred,
                        isLiked,
                        totalMatchingScore,
                        isOnline = onlineUserIds.Contains(item.Id),
                        lastSeenAt = _presenceTracker.GetLastSeen(item.Id) ?? item.LastSeenAt,
                        isPremiumMember = item.IsPremiumMember,
                        gender = item.Gender,
                        interestStatus,
                        photoVisibleToAll = item.PhotoVisibleToAll,
                        photoVisibleToPremium = item.PhotoVisibleToPremium,
                        photoVisibleToAccepted = item.PhotoVisibleToAccepted,
                        photosUnlocked = photosUnlocked,
                        photoUnlockRequestStatus = unlockRequestDict.TryGetValue(item.Id, out var uStat) ? (int)uStat : -1,
                        documentVerificationEnabled = item.DocumentVerificationEnabled,
                        verificationDocumentUrl = item.VerificationDocumentUrl ?? itemDocs?.FirstOrDefault()?.documentUrl,
                        verificationDocuments = itemDocs != null ? (object)itemDocs : Array.Empty<object>(),
                        documentVerificationComplete = item.DocumentVerificationComplete,
                        documentVerificationRejected = item.DocumentVerificationRejected,
                        documentVerificationFollowupApproved = item.DocumentVerificationFollowupApproved
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    type,
                    page,
                    pageSize,
                    hasMore = list.Count == pageSize,
                    data = profileData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("exploreprofiles")]
        public async Task<IActionResult> GetExploreProfiles(
            long userId,
            string type,
            UserReportStatus? status = null,
            InterestStatus? interestStatus = null,
            int page = 1,
            int pageSize = 30)
        {
            try
            {
                int skip = (page - 1) * pageSize;
                var loginUser = await _registrationRepo.Get(userId);
                if (loginUser == null) return NotFound("User not found");

                bool viewerIsPremium = loginUser.IsPremiumMember || await _userService.IsPremiumUser(userId);

                var allUnlockRequests = await _photoUnlockRequestRepo.WhereActive(x => x.RequesterId == userId);
                var approvedUnlockRequests = allUnlockRequests
                    .Where(x => x.Status == PhotoUnlockStatus.Approved)
                    .Select(x => x.OwnerId)
                    .ToHashSet();
                var unlockRequestDict = allUnlockRequests
                    .GroupBy(x => x.OwnerId)
                    .ToDictionary(g => g.Key, g => g.First().Status);

                var acceptedInterestIds = (await _favouriteRepo.WhereActive(x =>
                    (x.UserId == userId || x.LikedId == userId) && x.Status == Domain.InterestStatus.Accepted
                ))
                .Select(x => x.UserId == userId ? x.LikedId : x.UserId)
                .ToHashSet();

                var notLikedIds = await _profileService.GetNotLikedProfileIdsAsync(userId);
                var notLikedSet = new HashSet<long>(notLikedIds);

                var list = new List<Registration>();
                var matches = await _matchingRepo.GetMatchingUsersWithPercentage((int)userId);
                var matchingProfilesDict = matches
                    .GroupBy(mp => mp.Id)
                    .ToDictionary(g => g.Key, g => g.Last());

                switch ((type ?? "").ToLower())
                {
                    case "favorited":
                        var allStarredIds = matches
                            .Where(m => m.IsStarred && !notLikedSet.Contains(m.Id))
                            .OrderByDescending(m => m.Starred_Created_On)
                            .Select(m => m.Id)
                            .ToList();

                        var starredRegs = await _registrationRepo.GetAllByIds(allStarredIds);
                        var visibleStarredRegs = starredRegs.Where(x => x.IsVisible).ToList();
                        var starredDict = visibleStarredRegs.ToDictionary(x => x.Id);
                        var orderedStarred = allStarredIds
                            .Where(id => starredDict.ContainsKey(id))
                            .Select(id => starredDict[id])
                            .ToList();

                        list = orderedStarred
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();
                        break;

                    case "liked":
                        var likedQuery = (await _favouriteRepo.WhereActive(x => x.UserId == userId)).AsEnumerable();
                        if (interestStatus != null)
                        {
                            likedQuery = likedQuery.Where(x => x.Status == interestStatus.Value);
                        }
                        var allLikedIds = likedQuery
                            .OrderByDescending(x => x.CreatedOn)
                            .Select(x => x.LikedId)
                            .Distinct()
                            .ToList();

                        var likedRegs = await _registrationRepo.GetAllByIds(allLikedIds);
                        var visibleLikedRegs = likedRegs.Where(x => x.IsVisible).ToList();
                        var likedDict = visibleLikedRegs.ToDictionary(x => x.Id);
                        var orderedLiked = allLikedIds
                            .Where(id => likedDict.ContainsKey(id))
                            .Select(id => likedDict[id])
                            .ToList();

                        list = orderedLiked
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();
                        break;

                    case "likedby":
                        var likedByQuery = (await _favouriteRepo.WhereActive(x => x.LikedId == userId)).AsEnumerable();
                        if (interestStatus != null)
                        {
                            likedByQuery = likedByQuery.Where(x => x.Status == interestStatus.Value);
                        }
                        var allLikedByIds = likedByQuery
                            .OrderByDescending(x => x.CreatedOn)
                            .Select(x => x.UserId) // Select the user who liked us
                            .Distinct()
                            .ToList();

                        var likedByRegs = await _registrationRepo.GetAllByIds(allLikedByIds);
                        var visibleLikedByRegs = likedByRegs.Where(x => x.IsVisible).ToList();
                        var likedByDict = visibleLikedByRegs.ToDictionary(x => x.Id);
                        var orderedLikedBy = allLikedByIds
                            .Where(id => likedByDict.ContainsKey(id))
                            .Select(id => likedByDict[id])
                            .ToList();

                        list = orderedLikedBy
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();
                        break;


                    case "contactviewed":
                        var allViewedIds = (await _contactViewRepo.WhereActive(x => x.ViewerUserId == userId))
                            .OrderByDescending(x => x.CreatedOn)
                            .Select(x => x.ViewedUserId)
                            .Distinct()
                            .ToList();

                        var viewedRegs = await _registrationRepo.GetAllByIds(allViewedIds);
                        var visibleViewedRegs = viewedRegs.Where(x => x.IsVisible).ToList();
                        var viewedDict = visibleViewedRegs.ToDictionary(x => x.Id);
                        var orderedViewed = allViewedIds
                            .Where(id => viewedDict.ContainsKey(id))
                            .Select(id => viewedDict[id])
                            .ToList();

                        list = orderedViewed
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();
                        break;

                    case "reported":
                        if (status == null) status = UserReportStatus.Actioned;
                        var reportedQuery = (await _userReportRepo.WhereActive(x => x.ReporterUserId == userId))
                            .Where(x => x.Status == status.Value);

                        var allReportedIds = reportedQuery
                            .OrderByDescending(x => x.CreatedOn)
                            .Select(x => x.ReportedUserId)
                            .Distinct()
                            .ToList();

                        var reportedRegs = await _registrationRepo.GetAllByIds(allReportedIds);
                        var reportedDict = reportedRegs.ToDictionary(x => x.Id);
                        var orderedReported = allReportedIds
                            .Where(id => reportedDict.ContainsKey(id))
                            .Select(id => reportedDict[id])
                            .ToList();

                        list = orderedReported
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();
                        break;

                    case "closeprofiles":
                        var allClosedIds = (await _notLikeRepo.WhereActive(x => x.UserId == userId))
                            .OrderByDescending(x => x.CreatedOn)
                            .Select(x => x.NotLikedId)
                            .Distinct()
                            .ToList();

                        var closedRegs = await _registrationRepo.GetAllByIds(allClosedIds);
                        var visibleClosedRegs = closedRegs.Where(x => x.IsVisible).ToList();
                        var closedDict = visibleClosedRegs.ToDictionary(x => x.Id);
                        var orderedClosed = allClosedIds
                            .Where(id => closedDict.ContainsKey(id))
                            .Select(id => closedDict[id])
                            .ToList();

                        list = orderedClosed
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();
                        break;
                }

                // Resolve lookup values
                var allBodyFeaturesListExplore = await _bodyFeaturesRepo.GetAllActive();
                var heights = allBodyFeaturesListExplore
                    .Where(x => x.Type == BodyFeature.Height)
                    .ToDictionary(x => x.Id, x => x.Title);
                var weights = allBodyFeaturesListExplore
                    .Where(x => x.Type == BodyFeature.Weight)
                    .ToDictionary(x => x.Id, x => x.Title);
                var maritalStatuses = (await _maritalStatusRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);
                var professions = (await _professionRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);
                var communities = (await _communityRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);
                var religions = (await _religionCasteRepo.WhereActive(x => x.ParentId == 0))
                    .ToDictionary(x => x.Id, x => x.Title);

                var userFavourites = await _favouriteRepo.WhereActive(x => x.UserId == userId || x.LikedId == userId);
                var sentFavDict = userFavourites
                    .Where(x => x.UserId == userId)
                    .GroupBy(x => x.LikedId)
                    .ToDictionary(g => g.Key, g => g.First().Status.ToString());
                var receivedFavDict = userFavourites
                    .Where(x => x.LikedId == userId)
                    .GroupBy(x => x.UserId)
                    .ToDictionary(g => g.Key, g => g.First().Status.ToString());

                var profileIdsExplore = list.Select(x => x.Id).Distinct().ToList();
                var onlineUserIdsExplore = _presenceTracker.GetOnlineUserIds(profileIdsExplore);
                var docsListExplore = await _verificationDocRepo.WhereActive(d => profileIdsExplore.Contains(d.ProfileId));
                var docsDictExplore = docsListExplore
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    }).ToList());

                var profileData = list.Select(item =>
                {
                    int age = 0;
                    try
                    {
                        string dtb = !string.IsNullOrEmpty(item.DOB) ? item.DOB : "01/01/1990";
                        string[] parts = dtb.Replace("-", "/").Trim().Split('/');
                        string day = parts[0].PadLeft(2, '0');
                        string month = parts[1].PadLeft(2, '0');
                        string year = parts[2];
                        if (day.Length > 2) { var temp = year; year = day; day = temp; }
                        string dobStr = $"{day}/{month}/{year}";
                        DateTime birthDate = DateTime.ParseExact(dobStr, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        DateTime today = DateTime.UtcNow;
                        age = today.Year - birthDate.Year;
                        if (birthDate > today.AddYears(-age)) age--;
                    }
                    catch { }

                    bool isStarred = false;
                    bool isLiked = false;
                    int totalMatchingScore = 0;
                    if (matchingProfilesDict.TryGetValue(item.Id, out var mp))
                    {
                        isStarred = mp.IsStarred;
                        isLiked = mp.IsLiked;
                        totalMatchingScore = mp.total_matching_score;
                    }

                    string interestStatus = "None";
                    if (type == "likedby")
                    {
                        interestStatus = receivedFavDict.TryGetValue(item.Id, out var statusStr) ? statusStr : "None";
                    }
                    else
                    {
                        interestStatus = sentFavDict.TryGetValue(item.Id, out var statusStr) ? statusStr : "None";
                    }

                    // Photo Visibility calculation
                    bool photosUnlocked = false;
                    if (userId == item.Id)
                    {
                        photosUnlocked = true;
                    }
                    else
                    {
                        if (item.PhotoVisibleToAll)
                        {
                            photosUnlocked = true;
                        }
                        else
                        {
                            if (item.PhotoVisibleToPremium && viewerIsPremium)
                            {
                                photosUnlocked = true;
                            }
                            if (!photosUnlocked && item.PhotoVisibleToAccepted && acceptedInterestIds.Contains(item.Id))
                            {
                                photosUnlocked = true;
                            }
                            if (!photosUnlocked && approvedUnlockRequests.Contains(item.Id))
                            {
                                photosUnlocked = true;
                            }
                        }
                    }

                    docsDictExplore.TryGetValue(item.Id, out var itemDocs);

                    return new
                    {
                        id = item.Id,
                        name = item.Name,
                        imagePath = item.ImagePath,
                        registerNumber = item.RegisterNumber,
                        age,
                        height = heights.GetValueOrDefault(item.HeightId),
                        weight = weights.GetValueOrDefault(item.WeightId),
                        maritalStatus = maritalStatuses.GetValueOrDefault(item.MaritalStatusId),
                        educationType = item.EducationType,
                        higherEducation = item.HighestEducation,
                        profession = professions.GetValueOrDefault(item.ProfessionId),
                        community = communities.GetValueOrDefault(item.CommunityId),
                        religion = religions.GetValueOrDefault(item.ReligionId),
                        village = item.Village,
                        state = item.State,
                        district = item.District,
                        isStarred,
                        isLiked,
                        totalMatchingScore,
                        isOnline = onlineUserIdsExplore.Contains(item.Id),
                        lastSeenAt = _presenceTracker.GetLastSeen(item.Id) ?? item.LastSeenAt,
                        isPremiumMember = item.IsPremiumMember,
                        gender = item.Gender,
                        interestStatus,
                        photoVisibleToAll = item.PhotoVisibleToAll,
                        photoVisibleToPremium = item.PhotoVisibleToPremium,
                        photoVisibleToAccepted = item.PhotoVisibleToAccepted,
                        photosUnlocked = photosUnlocked,
                        photoUnlockRequestStatus = unlockRequestDict.TryGetValue(item.Id, out var uStat) ? (int)uStat : -1,
                        documentVerificationEnabled = item.DocumentVerificationEnabled,
                        verificationDocumentUrl = item.VerificationDocumentUrl ?? itemDocs?.FirstOrDefault()?.documentUrl,
                        verificationDocuments = itemDocs != null ? (object)itemDocs : Array.Empty<object>(),
                        documentVerificationComplete = item.DocumentVerificationComplete,
                        documentVerificationRejected = item.DocumentVerificationRejected,
                        documentVerificationFollowupApproved = item.DocumentVerificationFollowupApproved
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    type,
                    page,
                    pageSize,
                    hasMore = list.Count == pageSize,
                    data = profileData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProfiles(
            [FromQuery] long userId,
            [FromQuery] string? searchQuery,
            [FromQuery] int? ageFrom,
            [FromQuery] int? ageTo,
            [FromQuery] int? heightFrom,
            [FromQuery] int? heightTo,
            [FromQuery] long? maritalStatusId,
            [FromQuery] long? communityId,
            [FromQuery] string? highestEducationTitle,
            [FromQuery] string? district,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            try
            {
                var loginUser = await _registrationRepo.Get(userId);
                if (loginUser == null)
                    return NotFound(new { success = false, message = "User not found" });

                bool viewerIsPremium = loginUser.IsPremiumMember || await _userService.IsPremiumUser(userId);

                // Fetch opposing gender profiles that are visible and complete
                var allActiveProfiles = await _registrationRepo.WhereActive(x =>
                    x.IsComplete == true &&
                    x.Gender != loginUser.Gender &&
                    x.IsVisible == true);

                var allBodyFeatures = await _bodyFeaturesRepo.GetAllActive();

                var allUnlockRequests = await _photoUnlockRequestRepo.WhereActive(x => x.RequesterId == userId);
                var approvedUnlockRequests = allUnlockRequests
                    .Where(x => x.Status == PhotoUnlockStatus.Approved)
                    .Select(x => x.OwnerId)
                    .ToHashSet();
                var unlockRequestDict = allUnlockRequests
                    .GroupBy(x => x.OwnerId)
                    .ToDictionary(g => g.Key, g => g.First().Status);

                var acceptedInterestIds = (await _favouriteRepo.WhereActive(x =>
                    (x.UserId == userId || x.LikedId == userId) && x.Status == Domain.InterestStatus.Accepted
                ))
                .Select(x => x.UserId == userId ? x.LikedId : x.UserId)
                .ToHashSet();

                var userFavourites = await _favouriteRepo.WhereActive(x => x.UserId == userId);
                var favouriteStatusDict = userFavourites
                    .GroupBy(x => x.LikedId)
                    .ToDictionary(g => g.Key, g => g.First().Status.ToString());

                var matchingProfiles = await _matchingRepo.GetMatchingUsersWithPercentage((int)userId);
                var matchingProfilesDict = matchingProfiles
                    .GroupBy(mp => mp.Id)
                    .ToDictionary(g => g.Key, g => g.Last());

                var heights = allBodyFeatures
                    .Where(x => x.Type == BodyFeature.Height)
                    .ToDictionary(x => x.Id, x => x.Title);
                var weights = allBodyFeatures
                    .Where(x => x.Type == BodyFeature.Weight)
                    .ToDictionary(x => x.Id, x => x.Title);
                var maritalStatuses = (await _maritalStatusRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);
                var communities = (await _communityRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);
                var religions = (await _religionCasteRepo.WhereActive(x => x.ParentId == 0))
                    .ToDictionary(x => x.Id, x => x.Title);
                var professions = (await _professionRepo.GetAllActive())
                    .ToDictionary(x => x.Id, x => x.Title);

                var filteredList = new List<Registration>();

                foreach (var item in allActiveProfiles)
                {
                    bool matches = true;

                    // 1. SearchQuery (RegisterNumber or Name)
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        var q = searchQuery.ToLower().Trim();
                        if (!((item.RegisterNumber != null && item.RegisterNumber.ToLower().Contains(q)) ||
                              (item.Name != null && item.Name.ToLower().Contains(q))))
                        {
                            matches = false;
                        }
                    }

                    // Calculate Age
                    int age = 0;
                    try
                    {
                        string dtb = !string.IsNullOrEmpty(item.DOB) ? item.DOB : "01/01/1990";
                        string[] parts = dtb.Replace("-", "/").Trim().Split('/');
                        string day = parts[0];
                        string month = parts[1];
                        string year = parts[2];
                        if (day.Length < 2) day = "0" + day;
                        if (month.Length < 2) month = "0" + month;
                        if (day.Length > 2) { var temp = year; year = day; day = temp; }
                        string[] formats = { "d/M/yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd" };
                        string dobStr = day.Trim() + "/" + month.Trim() + "/" + year.Trim();
                        DateTime birthDate = DateTime.ParseExact(dobStr, formats, System.Globalization.CultureInfo.InvariantCulture);
                        DateTime today = DateTime.UtcNow;
                        age = today.Year - birthDate.Year;
                        if (birthDate > today.AddYears(-age)) age--;
                    }
                    catch { }

                    // 2. Age Filter
                    if (ageFrom.HasValue && age <= ageFrom.Value)
                    {
                        matches = false;
                    }
                    if (ageTo.HasValue && age >= ageTo.Value)
                    {
                        matches = false;
                    }

                    // Resolve height in numeric cm
                    int partnerHeight = 0;
                    string? heightText = heights.GetValueOrDefault(item.HeightId);
                    if (!string.IsNullOrWhiteSpace(heightText))
                    {
                        try
                        {
                            var numericPart = new string(heightText.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
                            if (!string.IsNullOrWhiteSpace(numericPart) && int.TryParse(numericPart, out int heightInCm))
                            {
                                partnerHeight = heightInCm;
                            }
                        }
                        catch { }
                    }

                    // 3. Height Filter
                    if (heightFrom.HasValue && partnerHeight <= heightFrom.Value)
                    {
                        matches = false;
                    }
                    if (heightTo.HasValue && partnerHeight >= heightTo.Value)
                    {
                        matches = false;
                    }

                    // 4. Marital Status Filter
                    if (maritalStatusId.HasValue && item.MaritalStatusId != maritalStatusId.Value)
                    {
                        matches = false;
                    }

                    // 5. Community Filter
                    if (communityId.HasValue && item.CommunityId != communityId.Value)
                    {
                        matches = false;
                    }

                    // 6. Education Filter
                    if (!string.IsNullOrWhiteSpace(highestEducationTitle) &&
                        (item.HighestEducation == null || item.HighestEducation.ToLower().Trim() != highestEducationTitle.ToLower().Trim()))
                    {
                        matches = false;
                    }

                    // 7. District Filter
                    if (!string.IsNullOrWhiteSpace(district) &&
                        (item.District == null || item.District.ToLower().Trim() != district.ToLower().Trim()))
                    {
                        matches = false;
                    }

                    if (matches)
                    {
                        filteredList.Add(item);
                    }
                }

                // Relevance-based sorting if a search query was provided
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var q = searchQuery.ToLower().Trim();
                    filteredList = filteredList.OrderByDescending(p =>
                    {
                        var name = p.Name?.ToLower() ?? "";
                        var reg = p.RegisterNumber?.ToLower() ?? "";

                        if (reg == q) return 100;
                        if (reg.StartsWith(q)) return 90;
                        if (name == q) return 80;
                        if (name.StartsWith(q)) return 70;
                        if (name.Contains(" " + q)) return 60;
                        return 50;
                    }).ToList();
                }

                // Apply pagination
                int skip = (page - 1) * pageSize;
                var paginatedList = filteredList.Skip(skip).Take(pageSize).ToList();

                var profileIdsSearch = paginatedList.Select(x => x.Id).Distinct().ToList();
                var onlineUserIdsSearch = _presenceTracker.GetOnlineUserIds(profileIdsSearch);
                var docsListSearch = await _verificationDocRepo.WhereActive(d => profileIdsSearch.Contains(d.ProfileId));
                var docsDictSearch = docsListSearch
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    }).ToList());

                // Project to DTO responses
                var profileData = paginatedList.Select(item =>
                {
                    int age = 0;
                    try
                    {
                        string dtb = !string.IsNullOrEmpty(item.DOB) ? item.DOB : "01/01/1990";
                        string[] parts = dtb.Replace("-", "/").Trim().Split('/');
                        string day = parts[0];
                        string month = parts[1];
                        string year = parts[2];
                        if (day.Length < 2) day = "0" + day;
                        if (month.Length < 2) month = "0" + month;
                        if (day.Length > 2) { var temp = year; year = day; day = temp; }
                        string[] formats = { "d/M/yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd" };
                        string dobStr = day.Trim() + "/" + month.Trim() + "/" + year.Trim();
                        DateTime birthDate = DateTime.ParseExact(dobStr, formats, System.Globalization.CultureInfo.InvariantCulture);
                        DateTime today = DateTime.UtcNow;
                        age = today.Year - birthDate.Year;
                        if (birthDate > today.AddYears(-age)) age--;
                    }
                    catch { }

                    string? heightText = heights.GetValueOrDefault(item.HeightId);
                    string? weightText = weights.GetValueOrDefault(item.WeightId);
                    string? maritalStatusText = maritalStatuses.GetValueOrDefault(item.MaritalStatusId);
                    string? communityText = communities.GetValueOrDefault(item.CommunityId);
                    string? religionText = religions.GetValueOrDefault(item.ReligionId);
                    string? professionText = professions.GetValueOrDefault(item.ProfessionId);

                    bool isStarred = false;
                    bool isLiked = false;
                    int totalMatchingScore = 0;
                    if (matchingProfilesDict.TryGetValue(item.Id, out var mp))
                    {
                        isStarred = mp.IsStarred;
                        isLiked = mp.IsLiked;
                        totalMatchingScore = mp.total_matching_score;
                    }

                    string interestStatus = favouriteStatusDict.TryGetValue(item.Id, out var statusStr) ? statusStr : "None";

                    bool photosUnlocked = false;
                    if (userId == item.Id)
                    {
                        photosUnlocked = true;
                    }
                    else
                    {
                        if (item.PhotoVisibleToAll)
                        {
                            photosUnlocked = true;
                        }
                        else
                        {
                            if (item.PhotoVisibleToPremium && viewerIsPremium)
                            {
                                photosUnlocked = true;
                            }
                            if (!photosUnlocked && item.PhotoVisibleToAccepted && acceptedInterestIds.Contains(item.Id))
                            {
                                photosUnlocked = true;
                            }
                            if (!photosUnlocked && approvedUnlockRequests.Contains(item.Id))
                            {
                                photosUnlocked = true;
                            }
                        }
                    }

                    docsDictSearch.TryGetValue(item.Id, out var itemDocs);

                    return new
                    {
                        id = item.Id,
                        name = item.Name,
                        imagePath = item.ImagePath,
                        registerNumber = item.RegisterNumber,
                        age,
                        height = heightText,
                        weight = weightText,
                        maritalStatus = maritalStatusText,
                        educationType = item.EducationType,
                        higherEducation = item.HighestEducation,
                        profession = professionText,
                        community = communityText,
                        religion = religionText,
                        village = item.Village,
                        state = item.State,
                        district = item.District,
                        isStarred,
                        isLiked,
                        totalMatchingScore,
                        isOnline = onlineUserIdsSearch.Contains(item.Id),
                        isPremiumMember = item.IsPremiumMember,
                        gender = item.Gender,
                        interestStatus,
                        photoVisibleToAll = item.PhotoVisibleToAll,
                        photoVisibleToPremium = item.PhotoVisibleToPremium,
                        photoVisibleToAccepted = item.PhotoVisibleToAccepted,
                        photosUnlocked = photosUnlocked,
                        photoUnlockRequestStatus = unlockRequestDict.TryGetValue(item.Id, out var uStat) ? (int)uStat : -1,
                        documentVerificationEnabled = item.DocumentVerificationEnabled,
                        verificationDocumentUrl = item.VerificationDocumentUrl ?? itemDocs?.FirstOrDefault()?.documentUrl,
                        verificationDocuments = itemDocs != null ? (object)itemDocs : Array.Empty<object>(),
                        documentVerificationComplete = item.DocumentVerificationComplete,
                        documentVerificationRejected = item.DocumentVerificationRejected,
                        documentVerificationFollowupApproved = item.DocumentVerificationFollowupApproved
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    page,
                    pageSize,
                    hasMore = filteredList.Count > skip + paginatedList.Count,
                    totalCount = filteredList.Count,
                    data = profileData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("activeplan")]
        public async Task<IActionResult> GetActivePlanPurchase(long userId)
        {
            try
            {
                var user = await _registrationRepo.Get(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var activePlan = await _userService.GetActivePlanPurchase(userId);
                var isPremiumUser = await _userService.IsPremiumUser(userId);
                var remainingCredits = await _userService.GetRemainingContactViewCredits(userId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        hasPlan = activePlan != null,
                        isPremiumUser,
                        viewCreditsPurchased = activePlan?.ViewCreditsPurchased ?? 0,
                        viewCreditsUsed = activePlan?.ViewCreditsUsed ?? 0,
                        remainingCredits,
                        expiresAt = activePlan?.ExpiresAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfileDetails(long userId)
        {
            try
            {
                // 1. Fetch the user profile
                var registration = await _registrationRepo.Get(userId);
                if (registration == null || !registration.IsActive)
                {
                    return NotFound(new { success = false, message = "User profile not found or inactive" });
                }

                // 2. Map profile details to Dto
                var profileDto = _mapper.Map<RegistrationDto>(registration);
                // Clear sensitive fields
                profileDto.Password = null;
                profileDto.PasswordHash = null;
                profileDto.VerificationCode = null;

                var myDocs = (await _verificationDocRepo.WhereActive(d => d.ProfileId == userId))
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToList();
                profileDto.VerificationDocuments = myDocs.Select(d => _mapper.Map<VerificationDocumentDto>(d)).ToList();

                // 3. Calculate Age
                int age = 0;
                try
                {
                    string dtb = !string.IsNullOrEmpty(registration.DOB) ? registration.DOB : "01/01/1990";
                    string[] parts = dtb.Replace("-", "/").Trim().Split('/');
                    string day = parts[0].PadLeft(2, '0');
                    string month = parts[1].PadLeft(2, '0');
                    string year = parts[2];
                    if (day.Length > 2) { var temp = year; year = day; day = temp; }
                    string dobStr = $"{day}/{month}/{year}";
                    DateTime birthDate = DateTime.ParseExact(dobStr, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    DateTime today = DateTime.UtcNow;
                    age = today.Year - birthDate.Year;
                    if (birthDate > today.AddYears(-age)) age--;
                }
                catch { }

                // 4. Fetch associated images (gallery)
                var images = await _imagesRepo.FirstOrDefaultActive(x => x.UserId == userId);

                // 5. Fetch active plan details
                var activePlan = await _userService.GetActivePlanPurchase(userId);
                var isPremiumUser = await _userService.IsPremiumUser(userId);
                var remainingCredits = await _userService.GetRemainingContactViewCredits(userId);

                // Return all the structured data!
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        profile = profileDto,
                        age = age,
                        images = new
                        {
                            image1Path = images?.Image1Path,
                            image2Path = images?.Image2Path,
                            image3Path = images?.Image3Path,
                            image4Path = images?.Image4Path,
                            image5Path = images?.Image5Path
                        },
                        plan = new
                        {
                            hasPlan = activePlan != null,
                            isPremiumUser,
                            viewCreditsPurchased = activePlan?.ViewCreditsPurchased ?? 0,
                            viewCreditsUsed = activePlan?.ViewCreditsUsed ?? 0,
                            remainingCredits,
                            expiresAt = activePlan?.ExpiresAt
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching user profile: " + ex.Message });
            }
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetProfileDetails(long userId, long profileId)
        {
            try
            {
                // 1. Fetch the target profile (owner)
                var registration = await _registrationRepo.Get(profileId);
                if (registration == null || !registration.IsActive)
                {
                    return NotFound(new { success = false, message = "Profile not found or inactive" });
                }

                // 2. Fetch currently logged in user details (viewer) to determine permissions
                var viewer = await _registrationRepo.Get(userId);
                if (viewer == null)
                {
                    return NotFound(new { success = false, message = "Viewer user not found" });
                }

                // 3. Map profile details to Dto
                var profileDto = _mapper.Map<RegistrationDto>(registration);

                var profileDocs = (await _verificationDocRepo.WhereActive(d => d.ProfileId == profileId))
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToList();
                profileDto.VerificationDocuments = profileDocs.Select(d => _mapper.Map<VerificationDocumentDto>(d)).ToList();

                // 4. Calculate Age
                int age = 0;
                try
                {
                    string dtb = !string.IsNullOrEmpty(registration.DOB) ? registration.DOB : "01/01/1990";
                    string[] parts = dtb.Replace("-", "/").Trim().Split('/');
                    string day = parts[0].PadLeft(2, '0');
                    string month = parts[1].PadLeft(2, '0');
                    string year = parts[2];
                    if (day.Length > 2) { var temp = year; year = day; day = temp; }
                    string dobStr = $"{day}/{month}/{year}";
                    DateTime birthDate = DateTime.ParseExact(dobStr, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    DateTime today = DateTime.UtcNow;
                    age = today.Year - birthDate.Year;
                    if (birthDate > today.AddYears(-age)) age--;
                }
                catch { }

                // 5. Fetch associated images
                var images = await _imagesRepo.FirstOrDefaultActive(x => x.UserId == profileId);

                // 6. Photo Unlock and Visibility status
                bool viewerIsPremium = viewer.IsPremiumMember || await _userService.IsPremiumUser(userId);

                var allUnlockRequests = await _photoUnlockRequestRepo.WhereActive(x => x.RequesterId == userId && x.OwnerId == profileId);
                var approvedUnlock = allUnlockRequests.Any(x => x.Status == PhotoUnlockStatus.Approved);
                var unlockRequestStatus = allUnlockRequests.Any() ? (int)allUnlockRequests.First().Status : -1;

                var acceptedInterestIds = (await _favouriteRepo.WhereActive(x =>
                    (x.UserId == userId || x.LikedId == userId) && x.Status == Domain.InterestStatus.Accepted
                ))
                .Select(x => x.UserId == userId ? x.LikedId : x.UserId)
                .ToHashSet();

                bool photosUnlocked = false;
                if (userId == profileId)
                {
                    photosUnlocked = true;
                }
                else
                {
                    if (registration.PhotoVisibleToAll)
                    {
                        photosUnlocked = true;
                    }
                    else
                    {
                        if (registration.PhotoVisibleToPremium && viewerIsPremium)
                        {
                            photosUnlocked = true;
                        }
                        if (!photosUnlocked && registration.PhotoVisibleToAccepted && acceptedInterestIds.Contains(profileId))
                        {
                            photosUnlocked = true;
                        }
                        if (!photosUnlocked && approvedUnlock)
                        {
                            photosUnlocked = true;
                        }
                    }
                }

                // 7. Check if they have viewed contact details previously
                var contactRecord = await _contactViewRepo.FirstOrDefaultActive(x => x.ViewerUserId == userId && x.ViewedUserId == profileId);
                bool hasViewedContact = contactRecord != null;

                // 8. Connection status
                var starRecord = await _userStarProfileRepo.FirstOrDefaultActive(x => x.UserId == userId && x.StarId == profileId);
                bool isStarred = starRecord != null;

                var favouriteRecord = await _favouriteRepo.FirstOrDefaultActive(x => x.UserId == userId && x.LikedId == profileId);
                bool isLiked = favouriteRecord != null;
                string interestStatus = favouriteRecord != null ? favouriteRecord.Status.ToString() : "None";

                var likedByRecord = await _favouriteRepo.FirstOrDefaultActive(x => x.UserId == profileId && x.LikedId == userId);
                string interestReceivedStatus = likedByRecord != null ? likedByRecord.Status.ToString() : "None";

                // 9. Check if they have already reported this profile
                var existingReport = await _userReportRepo.FirstOrDefaultActive(x => x.ReporterUserId == userId && x.ReportedUserId == profileId);
                bool alreadyReported = existingReport != null;

                // Return all the structured data!
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        profile = profileDto,
                        age = age,
                        images = new
                        {
                            image1Path = images?.Image1Path,
                            image2Path = images?.Image2Path,
                            image3Path = images?.Image3Path,
                            image4Path = images?.Image4Path,
                            image5Path = images?.Image5Path
                        },
                        connection = new
                        {
                            isOnline = _presenceTracker.IsOnline(profileId),
                            lastSeenAt = _presenceTracker.GetLastSeen(profileId) ?? registration.LastSeenAt,
                            isStarred,
                            isLiked,
                            interestStatus,
                            interestReceivedStatus,
                            hasViewedContact,
                            alreadyReported
                        },
                        photoPrivacy = new
                        {
                            photoVisibleToAll = registration.PhotoVisibleToAll,
                            photoVisibleToPremium = registration.PhotoVisibleToPremium,
                            photoVisibleToAccepted = registration.PhotoVisibleToAccepted,
                            photosUnlocked = photosUnlocked,
                            photoUnlockRequestStatus = unlockRequestStatus
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching profile details: " + ex.Message });
            }
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditProfile([FromBody] MobileProfileEditDto model)
        {
            if (model == null || model.UserId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid profile update parameters or user ID." });
            }

            try
            {
                var user = await _registrationRepo.FirstOrDefaultActive(x => x.Id == model.UserId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                string section = model.Section ?? "";

                if (string.IsNullOrEmpty(section))
                {
                    // Fallback to updating everything if no section is specified
                    user.HeightId = model.HeightId;
                    user.WeightId = model.WeightId;
                    user.ComplexionId = model.ComplexionId;
                    user.BodyTypeId = model.BodyTypeId;
                    user.IsPhysicallyChallenged = model.IsPhysicallyChallenged;
                    user.HighestEducation = model.HighestEducation;
                    user.EducationType = model.EducationType;
                    user.ProfessionId = model.ProfessionId;
                    user.ProfessionType = model.ProfessionType;
                    user.MotherTongueId = model.MotherTongueId;
                    user.CommunityId = model.CommunityId;
                    user.ReligiousnessId = model.ReligiousnessId;
                    user.FinancialStatusId = model.FinancialStatusId;
                    user.LandlineNumber = model.LandlineNumber;
                    user.PresentCountry = model.PresentCountry;
                    user.About = model.About;
                    user.MaritalStatusId = model.MaritalStatusId;
                    user.NumberOfChildrens = (model.MaritalStatusId == 1) ? null : (model.NumberOfChildrens ?? model.NumberOfChildren ?? model.Number_Of_Children ?? model.Number_Of_Childrens);

                    if (!string.IsNullOrEmpty(model.Name)) user.Name = model.Name;
                    if (!string.IsNullOrEmpty(model.Phone)) user.Phone = model.Phone;
                    if (!string.IsNullOrEmpty(model.DOB)) user.DOB = model.DOB;
                    if (!string.IsNullOrEmpty(model.Gender)) user.Gender = model.Gender;
                }
                else
                {
                    switch (section.ToLower())
                    {
                        case "basic":
                            user.MaritalStatusId = model.MaritalStatusId;
                            user.NumberOfChildrens = (model.MaritalStatusId == 1) ? null : (model.NumberOfChildrens ?? model.NumberOfChildren ?? model.Number_Of_Children ?? model.Number_Of_Childrens);
                            user.MotherTongueId = model.MotherTongueId;
                            user.About = model.About;
                            if (!string.IsNullOrEmpty(model.Name)) user.Name = model.Name;
                            if (!string.IsNullOrEmpty(model.DOB)) user.DOB = model.DOB;
                            break;
                        case "physical":
                            user.HeightId = model.HeightId;
                            user.WeightId = model.WeightId;
                            user.ComplexionId = model.ComplexionId;
                            user.BodyTypeId = model.BodyTypeId;
                            user.IsPhysicallyChallenged = model.IsPhysicallyChallenged;
                            break;
                        case "education":
                            user.HighestEducation = model.HighestEducation;
                            user.EducationType = model.EducationType;
                            user.ProfessionId = model.ProfessionId;
                            user.ProfessionType = model.ProfessionType;
                            break;
                        case "religious":
                            user.CommunityId = model.CommunityId;
                            user.ReligiousnessId = model.ReligiousnessId;
                            break;
                        case "location":
                            user.PresentCountry = model.PresentCountry;
                            user.LandlineNumber = model.LandlineNumber;
                            break;
                        case "family":
                            user.FinancialStatusId = model.FinancialStatusId;
                            break;
                        default:
                            return BadRequest(new { success = false, message = "Invalid section requested." });
                    }
                }

                await _registrationRepo.Update(user);
                await _registrationRepo.SaveChanges();

                return Ok(new { success = true, message = "Profile updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating profile: " + ex.Message });
            }
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications(long userId)
        {
            try
            {
                var loggedInUserprofile = await _registrationRepo.Get(userId);
                if (loggedInUserprofile == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                var notifications = await _notificationPageRepo.GetNotificationsBasedOnNotifyId(userId);
                var profileIds = notifications.Select(m => m.UserId).Distinct().ToList();
                var UserProfiles = await _registrationRepo.GetAllByIds(profileIds);

                var userFavourites = await _favouriteRepo.WhereActive(x => x.UserId == userId || x.LikedId == userId);
                var photoUnlocks = await _photoUnlockRequestRepo.WhereActive(x => x.RequesterId == userId || x.OwnerId == userId);

                var notificationList = new List<NotificationDto>();
                foreach (var notification in notifications)
                {
                    var userProfile = UserProfiles.FirstOrDefault(profile => profile.Id == notification.UserId);
                    var favToUs = userFavourites.FirstOrDefault(x => x.UserId == notification.UserId && x.LikedId == userId);
                    var favFromUs = userFavourites.FirstOrDefault(x => x.UserId == userId && x.LikedId == notification.UserId);

                    string? interestStatus = null;
                    long? interestRecordId = null;

                    if (notification.Notify_Message != null && notification.Notify_Message.Contains("viewed your profile"))
                    {
                        // No status badge for locked photo view notifications
                    }
                    else if (notification.Notify_Message != null && notification.Notify_Message.Contains("success story", StringComparison.OrdinalIgnoreCase))
                    {
                        // No status badge for success story notifications
                    }
                    else if (notification.Notify_Message != null && notification.Notify_Message.Contains("shortlisted by"))
                    {
                        // No status badge for shortlist notifications
                    }
                    else if (notification.Notify_Message != null && (notification.Notify_Message.Contains("Photo unlock request") || notification.Notify_Message.Contains("photo unlock request")))
                    {
                        var photoReq = photoUnlocks.FirstOrDefault(x =>
                            (x.RequesterId == notification.UserId && x.OwnerId == userId) ||
                            (x.RequesterId == userId && x.OwnerId == notification.UserId)
                        );
                        if (photoReq != null)
                        {
                            interestStatus = photoReq.Status == PhotoUnlockStatus.Approved ? "Accepted" :
                                             photoReq.Status == PhotoUnlockStatus.Rejected ? "Declined" : "Pending";
                            interestRecordId = photoReq.Id;
                        }
                    }
                    else if (favToUs != null)
                    {
                        interestStatus = favToUs.Status.ToString();
                        interestRecordId = favToUs.Id;
                    }
                    else if (favFromUs != null)
                    {
                        interestStatus = favFromUs.Status.ToString();
                        interestRecordId = favFromUs.Id;
                    }

                    var dto = new NotificationDto
                    {
                        Id = notification.Id,
                        Notify_Message = notification.Notify_Message,
                        UserId = notification.UserId,
                        Notify_Id = notification.Notify_Id,
                        Is_Read = notification.Is_Read,
                        CreatedOn = notification.CreatedOn,
                        profileImageUrl = userProfile?.ImagePath,
                        likedByUserName = userProfile?.Name,
                        Gender = userProfile?.Gender,
                        InterestStatus = interestStatus,
                        InterestRecordId = interestRecordId
                    };

                    // Enrich status update prompt notifications
                    if (notification.Notify_Message != null &&
                        notification.Notify_Message.StartsWith(RelationshipStatusNotificationService.StatusUpdatePromptPrefix))
                    {
                        dto.IsStatusUpdatePrompt = true;
                        dto.StatusUpdatePartnerId = notification.UserId;
                        dto.Notify_Message = notification.Notify_Message
                            .Replace(RelationshipStatusNotificationService.StatusUpdatePromptPrefix + " ", "")
                            .Replace(RelationshipStatusNotificationService.StatusUpdatePromptPrefix, "");

                        var favRecord = userFavourites.FirstOrDefault(x =>
                            (x.UserId == userId && x.LikedId == notification.UserId) ||
                            (x.UserId == notification.UserId && x.LikedId == userId));
                        if (favRecord != null)
                        {
                            dto.StatusUpdateFavId = favRecord.Id;
                            var matchStatus = (await _matchStatusUpdateRepo.WhereActive(x =>
                                x.UserId == userId && x.UserFavouriteProfileId == favRecord.Id)).FirstOrDefault();
                            if (matchStatus != null)
                                dto.CurrentMatchStatus = (int)matchStatus.Status;
                        }

                        var existingStory = (await _successStoryRepo.WhereActive(x =>
                            (x.SubmittedByUserId == userId && x.PartnerUserId == notification.UserId) ||
                            (x.SubmittedByUserId == notification.UserId && x.PartnerUserId == userId))).FirstOrDefault();
                        dto.HasSubmittedSuccessStory = existingStory != null;
                    }

                    notificationList.Add(dto);
                }

                return Ok(new { success = true, notifications = notificationList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching notifications: " + ex.Message });
            }
        }

        [HttpGet("images/{userId}")]
        public async Task<IActionResult> GetImages(long userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID" });
                }

                var user = await _registrationRepo.Get(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var image = await _imagesRepo.FirstOrDefault(x => x.UserId == userId);
                var response = new
                {
                    success = true,
                    userId = userId,
                    imagePath = user.ImagePath,
                    images = new
                    {
                        image1Path = image?.Image1Path,
                        image2Path = image?.Image2Path,
                        image3Path = image?.Image3Path,
                        image4Path = image?.Image4Path,
                        image5Path = image?.Image5Path
                    },
                    photoVisibleToAll = user.PhotoVisibleToAll,
                    photoVisibleToPremium = user.PhotoVisibleToPremium,
                    photoVisibleToAccepted = user.PhotoVisibleToAccepted
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-images")]
        public async Task<IActionResult> UpdateImages([FromForm] UserImageEditViewModel model)
        {
            if (model == null || !model.UserId.HasValue || model.UserId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid request or missing UserId." });
            }

            try
            {
                var userId = model.UserId.Value;
                var user = await _registrationRepo.FirstOrDefaultActive(x => x.Id == userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                string folderPath = "Uploads/Registration";

                var image = await _imagesRepo.FirstOrDefault(x => x.UserId == userId);
                if (image == null)
                {
                    var newImage = new Images
                    {
                        UserId = userId,
                        Image1Path = model.Image1 != null ? await _fileService.SaveFile(model.Image1, folderPath) : null,
                        Image2Path = model.Image2 != null ? await _fileService.SaveFile(model.Image2, folderPath) : null,
                        Image3Path = model.Image3 != null ? await _fileService.SaveFile(model.Image3, folderPath) : null,
                        Image4Path = model.Image4 != null ? await _fileService.SaveFile(model.Image4, folderPath) : null,
                        Image5Path = model.Image5 != null ? await _fileService.SaveFile(model.Image5, folderPath) : null,
                        CreatedOn = DateTime.UtcNow,
                        ModifiedOn = DateTime.UtcNow,
                        IsActive = true,
                        IsDeleted = false
                    };
                    await _imagesRepo.Add(newImage);
                    await _imagesRepo.SaveChanges();
                }
                else
                {
                    image.Image1Path = model.Image1 != null ? await _fileService.SaveFile(model.Image1, folderPath) : image.Image1Path;
                    image.Image2Path = model.Image2 != null ? await _fileService.SaveFile(model.Image2, folderPath) : image.Image2Path;
                    image.Image3Path = model.Image3 != null ? await _fileService.SaveFile(model.Image3, folderPath) : image.Image3Path;
                    image.Image4Path = model.Image4 != null ? await _fileService.SaveFile(model.Image4, folderPath) : image.Image4Path;
                    image.Image5Path = model.Image5 != null ? await _fileService.SaveFile(model.Image5, folderPath) : image.Image5Path;
                    image.ModifiedOn = DateTime.UtcNow;

                    await _imagesRepo.Update(image);
                    await _imagesRepo.SaveChanges();
                }

                if (model.PhotoVisibleToAll.HasValue) user.PhotoVisibleToAll = model.PhotoVisibleToAll.Value;
                if (model.PhotoVisibleToPremium.HasValue) user.PhotoVisibleToPremium = model.PhotoVisibleToPremium.Value;
                if (model.PhotoVisibleToAccepted.HasValue) user.PhotoVisibleToAccepted = model.PhotoVisibleToAccepted.Value;

                if (model.Image != null)
                {
                    user.ImagePath = await _fileService.SaveFile(model.Image, folderPath);
                }

                await _registrationRepo.Update(user);
                await _registrationRepo.SaveChanges();

                return Ok(new { success = true, message = "Images updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // [HttpPost("update-gallery-images/{userId}")]
        // public async Task<IActionResult> UpdateGalleryImages(long userId, [FromForm] IFormFile? image1, [FromForm] IFormFile? image2, [FromForm] IFormFile? image3, [FromForm] IFormFile? image4, [FromForm] IFormFile? image5)
        // {
        //     try
        //     {
        //         if (userId <= 0)
        //         {
        //             return BadRequest(new { success = false, message = "Invalid user ID" });
        //         }

        //         var user = await _registrationRepo.FirstOrDefaultActive(x => x.Id == userId);
        //         if (user == null)
        //         {
        //             return NotFound(new { success = false, message = "User not found" });
        //         }

        //         string folderPath = "Uploads/Registration";
        //         var imageDto = new ImagesDto
        //         {
        //             UserId = userId,
        //             Image1Path = image1 != null ? await _fileService.SaveFile(image1, folderPath) : null,
        //             Image2Path = image2 != null ? await _fileService.SaveFile(image2, folderPath) : null,
        //             Image3Path = image3 != null ? await _fileService.SaveFile(image3, folderPath) : null,
        //             Image4Path = image4 != null ? await _fileService.SaveFile(image4, folderPath) : null,
        //             Image5Path = image5 != null ? await _fileService.SaveFile(image5, folderPath) : null,
        //         };

        //         var image = await _imagesRepo.FirstOrDefault(x => x.UserId == userId);
        //         if (image == null)
        //         {
        //             var newImage = new Images
        //             {
        //                 UserId = userId,
        //                 Image1Path = imageDto.Image1Path,
        //                 Image2Path = imageDto.Image2Path,
        //                 Image3Path = imageDto.Image3Path,
        //                 Image4Path = imageDto.Image4Path,
        //                 Image5Path = imageDto.Image5Path,
        //                 CreatedOn = DateTime.UtcNow,
        //                 ModifiedOn = DateTime.UtcNow,
        //                 IsActive = true,
        //                 IsDeleted = false
        //             };

        //             _imagesRepo.Add(newImage);
        //             await _imagesRepo.SaveChanges();
        //         }
        //         else
        //         {
        //             image.Image1Path = imageDto.Image1Path ?? image.Image1Path;
        //             image.Image2Path = imageDto.Image2Path ?? image.Image2Path;
        //             image.Image3Path = imageDto.Image3Path ?? image.Image3Path;
        //             image.Image4Path = imageDto.Image4Path ?? image.Image4Path;
        //             image.Image5Path = imageDto.Image5Path ?? image.Image5Path;
        //             image.ModifiedOn = DateTime.UtcNow;

        //             _imagesRepo.Update(image);
        //             await _imagesRepo.SaveChanges();
        //         }

        //         return Ok(new { success = true, message = "Gallery images updated successfully." });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { success = false, message = ex.Message });
        //     }
        // }

        [HttpDelete("delete-images/{userId}")]
        public async Task<IActionResult> DeleteImages(
            long userId,
            [FromForm] bool deleteImage1,
            [FromForm] bool deleteImage2,
            [FromForm] bool deleteImage3,
            [FromForm] bool deleteImage4,
            [FromForm] bool deleteImage5)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid user ID" });
            }

            var user = await _registrationRepo.FirstOrDefaultActive(x => x.Id == userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            var images = await _imagesRepo.GetAll();
            var userImages = images.Where(x => x.UserId == userId).ToList();

            if (userImages == null || !userImages.Any())
            {
                return NotFound(new { success = false, message = "No images found for the user" });
            }

            bool anyImageDeleted = false;

            foreach (var image in userImages)
            {
                bool deleted = false;

                if (deleteImage1 && !string.IsNullOrEmpty(image.Image1Path))
                {
                    await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", deleteImage1, false, false, false, false);
                    image.Image1Path = null;
                    deleted = true;
                }

                if (deleteImage2 && !string.IsNullOrEmpty(image.Image2Path))
                {
                    await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", false, deleteImage2, false, false, false);
                    image.Image2Path = null;
                    deleted = true;
                }

                if (deleteImage3 && !string.IsNullOrEmpty(image.Image3Path))
                {
                    await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", false, false, deleteImage3, false, false);
                    image.Image3Path = null;
                    deleted = true;
                }

                if (deleteImage4 && !string.IsNullOrEmpty(image.Image4Path))
                {
                    await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", false, false, false, deleteImage4, false);
                    image.Image4Path = null;
                    deleted = true;
                }

                if (deleteImage5 && !string.IsNullOrEmpty(image.Image5Path))
                {
                    await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", false, false, false, false, deleteImage5);
                    image.Image5Path = null;
                    deleted = true;
                }

                if (deleted)
                {
                    // Save the updated image paths to the database
                    await _imagesRepo.Update(image);
                    anyImageDeleted = true;
                }
            }

            await _imagesRepo.SaveChanges();
            return Ok(new { success = anyImageDeleted, message = anyImageDeleted ? "Images deleted successfully." : "No images were deleted." });
        }

        [HttpGet("lookups")]
        public async Task<IActionResult> GetRegistrationLookups()
        {
            try
            {
                var profileForRaw = await _profileForRepo.GetAllActive();
                var nationalitiesRaw = await _nationalityRepo.GetAllActive();
                var maritalStatusesRaw = await _maritalStatusRepo.GetAllActive();
                var professionsRaw = await _professionRepo.GetAllActive();
                var motherTonguesRaw = await _motherTongueRepo.GetAllActive();
                var religionsRaw = await _religionCasteRepo.WhereActive(x => x.ParentId == 0);
                var castesRaw = await _religionCasteRepo.WhereActive(x => x.ParentId != 0);
                var communitiesRaw = await _communityRepo.GetAllActive();
                var religiousnessesRaw = await _religiousnessRepo.GetAllActive();
                var financialStatusesRaw = await _financialStatusRepo.GetAllActive();
                var bodyFeaturesRaw = await _bodyFeaturesRepo.GetAllActive();

                var profileFor = _mapper.Map<List<ProfileForDto>>(profileForRaw);
                var nationalities = _mapper.Map<List<NationalityDto>>(nationalitiesRaw).OrderBy(x => x.Title).ToList();
                var maritalStatuses = _mapper.Map<List<MaritalStatusDto>>(maritalStatusesRaw);
                var professions = _mapper.Map<List<ProfessionDto>>(professionsRaw);
                var motherTongues = _mapper.Map<List<MotherTongueDto>>(motherTonguesRaw);
                var religions = _mapper.Map<List<ReligionCasteDto>>(religionsRaw);
                var castes = _mapper.Map<List<ReligionCasteDto>>(castesRaw);
                var communities = _mapper.Map<List<CommunityDto>>(communitiesRaw);
                var religiousnesses = _mapper.Map<List<ReligiousnessDto>>(religiousnessesRaw);
                var financialStatuses = _mapper.Map<List<FinancialStatusDto>>(financialStatusesRaw);

                var bodyFeaturesMapped = _mapper.Map<List<BodyFeaturesDto>>(bodyFeaturesRaw);

                var response = new
                {
                    success = true,
                    profileFor,
                    nationalities,
                    maritalStatuses,
                    religions,
                    castes,
                    professions,
                    motherTongues,
                    communities,
                    religiousnesses,
                    financialStatuses,
                    bodyFeatures = new
                    {
                        heights = bodyFeaturesMapped.Where(x => x.Type == BodyFeature.Height).ToList(),
                        weights = bodyFeaturesMapped.Where(x => x.Type == BodyFeature.Weight).ToList(),
                        complexions = bodyFeaturesMapped.Where(x => x.Type == BodyFeature.Complexion).ToList(),
                        bodyTypes = bodyFeaturesMapped.Where(x => x.Type == BodyFeature.BodyType).ToList(),
                        physicalDisabilities = bodyFeaturesMapped.Where(x => x.Type == BodyFeature.PhysicalDisability).ToList(),
                        educations = bodyFeaturesMapped.Where(x => x.Type == BodyFeature.Education).ToList(),
                        courses = bodyFeaturesMapped.Where(x => x.Type == BodyFeature.Course).ToList(),
                        professionTypes = bodyFeaturesMapped.Where(x => x.Type == BodyFeature.ProfessionType).ToList()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching lookups: " + ex.Message });
            }
        }

        [HttpGet("report-reasons")]
        public async Task<IActionResult> GetReportReasons()
        {
            try
            {
                var reasons = await _userReportReasonRepo.GetAllActive();
                var reasonsMapped = _mapper.Map<List<UserReportReasonDto>>(reasons.OrderBy(x => x.DisplayOrder));
                return Ok(new { success = true, data = reasonsMapped });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching report reasons: " + ex.Message });
            }
        }

        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var countriesRaw = await _stateRepo.GetCountriesAsync();
                var countries = _mapper.Map<List<NationalityDto>>(countriesRaw).OrderBy(x => x.Title).ToList();
                return Ok(new { success = true, countries });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching countries: " + ex.Message });
            }
        }

        [HttpGet("states")]
        public async Task<IActionResult> GetStates([FromQuery] long countryId)
        {
            try
            {
                var statesRaw = await _stateRepo.GetStatesByCountryIdAsync(countryId);
                var states = _mapper.Map<List<StateDto>>(statesRaw);
                return Ok(new { success = true, states });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching states: " + ex.Message });
            }
        }

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] long stateId)
        {
            try
            {
                var districtsRaw = await _stateRepo.GetDistrictsByStateIdAsync(stateId);
                var districts = _mapper.Map<List<DistrictDto>>(districtsRaw);
                return Ok(new { success = true, districts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching districts: " + ex.Message });
            }
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities([FromQuery] long districtId)
        {
            try
            {
                var citiesRaw = await _stateRepo.GetCityByDistrictIdAsync(districtId);
                var cities = _mapper.Map<List<CityDto>>(citiesRaw);
                return Ok(new { success = true, cities });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching cities: " + ex.Message });
            }
        }

        [HttpPost("transaction")]
        public async Task<IActionResult> AddTransactionAsync([FromBody] MobileTransactionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Transaction payload is null." });
                }

                if (string.IsNullOrEmpty(request.Udf1) || !long.TryParse(request.Udf1, out long userId))
                {
                    return BadRequest(new { success = false, message = "User ID (Udf1) is missing or invalid." });
                }

                var existingTxn = await _transactionRepository.GetTransactionByTxnIdAsync(request.TxnId);
                if (existingTxn != null)
                {
                    return Ok(new { success = true, data = existingTxn, message = "Transaction already processed." });
                }

                string paymentGateway = "PayU";
                string generatedBy = "System";

                if (_payUSettings != null)
                {
                    var envSettings = _env.IsDevelopment() ? _payUSettings.Development : _payUSettings.Production;
                    if (envSettings != null)
                    {
                        paymentGateway = envSettings.PaymentGateway ?? paymentGateway;
                        generatedBy = envSettings.GeneratedBy ?? generatedBy;
                    }
                }

                Transaction transaction = new Transaction
                {
                    userId = userId,
                    Udf1 = request.Udf1,
                    Status = request.Status,
                    Key = request.Key,
                    TxnId = request.TxnId,
                    Amount = request.Amount,
                    ProductInfo = request.ProductInfo,
                    FirstName = request.FirstName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Hash = request.Hash,
                    PaymentGateway = paymentGateway,
                    CreatedBy = generatedBy,
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now,
                    ModifiedBy = generatedBy,
                    PaymentType = request.PaymentType ?? "Online",
                    Source = request.Source ?? "Mobile"
                };

                await _transactionRepository.AddPaymentResultAsync(transaction);

                // If status is success, update user package and premium flag
                bool isSuccess = transaction.Status != null && transaction.Status.Equals("success", StringComparison.OrdinalIgnoreCase);
                if (isSuccess)
                {
                    var plans = await _planPurchaseRepo.Where(x => x.UserId == userId);
                    var latestPlan = plans.OrderByDescending(x => x.CreatedOn).FirstOrDefault();

                    if (latestPlan != null && (latestPlan.ViewCreditsPurchased - latestPlan.ViewCreditsUsed) > 0 && latestPlan.ExpiresAt > DateTime.UtcNow)
                    {
                        latestPlan.ViewCreditsPurchased += 50;
                        latestPlan.ExpiresAt = DateTime.UtcNow.AddDays(180);
                        latestPlan.LowCreditNotificationSent = false;
                        latestPlan.ExpiryNotificationSent = false;
                        latestPlan.ModifiedOn = DateTime.Now;
                        latestPlan.ModifiedBy = generatedBy;
                        await _planPurchaseRepo.Update(latestPlan);
                    }
                    else
                    {
                        PlanPurchase planPurchase = new PlanPurchase()
                        {
                            UserId = userId,
                            ViewCreditsPurchased = 50,
                            CreatedOn = DateTime.Now,
                            ModifiedOn = DateTime.Now,
                            CreatedBy = generatedBy,
                            ModifiedBy = generatedBy
                        };
                        await _planPurchaseRepo.Add(planPurchase);
                    }
                    await _planPurchaseRepo.SaveChanges();

                    // Update user premium status in Registration table
                    var user = await _registrationRepo.Get(userId);
                    if (user != null)
                    {
                        user.IsPremiumMember = true;
                        await _registrationRepo.Update(user);
                        await _registrationRepo.SaveChanges();
                    }

                    // Update follow-up status if an active follow-up exists for this user
                    try
                    {
                        var userFollowUps = await _followUpRepo.Where(f => f.ProfileId == userId && !f.IsDeleted && (f.FollowUpType == FollowUpType.PremiumFollowUp || f.FollowUpType == FollowUpType.RenewalFollowUp));
                        var activeFollowUp = userFollowUps.OrderByDescending(f => f.CreatedOn).FirstOrDefault();
                        if (activeFollowUp != null)
                        {
                            activeFollowUp.PaymentCompleted = true;
                            activeFollowUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;
                            activeFollowUp.PaymentMode = "Online";
                            activeFollowUp.TransactionId = transaction.TxnId;
                            if (decimal.TryParse(transaction.Amount, out decimal parsedAmt))
                            {
                                activeFollowUp.PaymentAmount = parsedAmt;
                            }
                            activeFollowUp.ModifiedOn = DateTime.Now;

                            if (activeFollowUp.FollowUpType == FollowUpType.PremiumFollowUp)
                            {
                                activeFollowUp.LatestInterestStatus = PremiumInterestStatus.Converted;
                            }
                            else if (activeFollowUp.FollowUpType == FollowUpType.RenewalFollowUp)
                            {
                                activeFollowUp.LatestRenewalInterestStatus = RenewalInterestStatus.Renewed;
                            }

                            await _followUpRepo.Update(activeFollowUp);
                            await _followUpRepo.SaveChanges();

                            var timeline = new FollowUpTimeline
                            {
                                FollowUpId = activeFollowUp.Id,
                                StaffId = activeFollowUp.AssignedStaffId ?? 0,
                                StaffName = "System (Mobile Payment Gateway)",
                                ContactType = activeFollowUp.LatestContactType,
                                CallStatus = activeFollowUp.LatestCallStatus,
                                Remarks = $"Mobile payment completed successfully. TxnId: {transaction.TxnId}, Amount: {transaction.Amount}. Membership activated.",
                                NextFollowUpDate = null,
                                InterestStatus = activeFollowUp.LatestInterestStatus,
                                RenewalInterestStatus = activeFollowUp.LatestRenewalInterestStatus,
                                ProfileVerificationStatus = activeFollowUp.LatestProfileVerificationStatus,
                                CreatedOn = DateTime.Now,
                                ModifiedOn = DateTime.Now,
                                IsActive = true
                            };
                            await _followUpTimelineRepo.Add(timeline);
                            await _followUpTimelineRepo.SaveChanges();
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore follow-up exception
                    }

                    // Send success email
                    try
                    {
                        await SendPaymentSuccessEmail(transaction.Email, transaction.FirstName, transaction.TxnId, transaction.Amount, transaction.CreatedOn);
                    }
                    catch (Exception)
                    {
                        // Log or ignore email exceptions
                    }
                }
                else
                {
                    // Send failed email
                    try
                    {
                        await SendPaymentFailedEmail(transaction.Email, transaction.FirstName, transaction.TxnId, transaction.Amount);
                    }
                    catch (Exception)
                    {
                        // Log or ignore email exceptions
                    }
                }

                return Ok(new { success = true, data = transaction });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task SendPaymentSuccessEmail(string email, string name, string txnId, string amount, DateTime dateTime)
        {
            try
            {
                string templatePath = "Templates/Payment/Paymentsuccessful.html";
                string emailContent = System.IO.File.ReadAllText(templatePath);

                // Replace placeholders with actual values
                emailContent = emailContent.Replace("[User's Name]", name);
                emailContent = emailContent.Replace("[Transaction ID]", txnId);
                emailContent = emailContent.Replace("[Payment Amount]", amount);
                emailContent = emailContent.Replace("[Payment Date]", dateTime.ToShortDateString());

                var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                _emailNotificationHelper.SendEmail(email, htmlView, "Payment Successful - Welcome to Premium Membership on M4Nikah!");
            }
            catch (Exception)
            {
                // Suppress email exceptions to ensure response is successful
            }
        }

        private async Task SendPaymentFailedEmail(string email, string name, string txnId, string amount)
        {
            try
            {
                string templatePath = "Templates/Payment/Paymentfailed.html";
                string emailContent = System.IO.File.ReadAllText(templatePath);

                // Replace placeholders with actual values
                emailContent = emailContent.Replace("[User Name]", name);
                emailContent = emailContent.Replace("[Transaction ID]", txnId);
                emailContent = emailContent.Replace("[Amount]", amount);
                emailContent = emailContent.Replace("[Support Email]", "support@m4nikkah.com");
                emailContent = emailContent.Replace("[Support Phone Number]", "123-456-7890");

                var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                _emailNotificationHelper.SendEmail(email, htmlView, "Payment Failed Notification");
            }
            catch (Exception)
            {
                // Suppress email exceptions to ensure response is successful
            }
        }

        [HttpPost("/api/profile/upload-verification-document")]
        public async Task<IActionResult> UploadVerificationDocument(
            [FromForm] long userId,
            [FromForm] List<IFormFile> verificationDocuments,
            [FromForm] string? documentType = null)
        {
            try
            {
                var filesToUpload = verificationDocuments?.Where(f => f != null && f.Length > 0).ToList() ?? new List<IFormFile>();

                if (filesToUpload.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No file uploaded. Please provide at least one document in 'verificationDocuments'." });
                }

                var user = await _registrationRepo.Get(userId);
                if (user == null || user.IsDeleted)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                var uploadedUrls = new List<string>();
                string firstUrl = string.Empty;
                var currentActiveDocs = (await _verificationDocRepo.WhereActive(d => d.ProfileId == userId)).ToList();

                for (int i = 0; i < filesToUpload.Count; i++)
                {
                    var file = filesToUpload[i];
                    string relativePath = await _fileService.UploadFile(file, "Uploads/VerificationDocuments");
                    uploadedUrls.Add(relativePath);
                    if (string.IsNullOrEmpty(firstUrl)) firstUrl = relativePath;

                    int nextIndex = currentActiveDocs.Count + 1;
                    var newDoc = new VerificationDocument
                    {
                        ProfileId = userId,
                        DocumentUrl = relativePath,
                        OriginalFileName = file.FileName,
                        DisplayOrder = nextIndex,
                        IsActive = true
                    };
                    await _verificationDocRepo.Add(newDoc);
                    currentActiveDocs.Add(newDoc);
                }
                await _verificationDocRepo.SaveChanges();

                user.VerificationDocumentUrl = firstUrl;
                user.DocumentVerificationEnabled = true;
                user.DocumentVerificationRejected = false;
                user.DocumentVerificationComplete = false;
                await _registrationRepo.Update(user);
                await _registrationRepo.SaveChanges();

                // Auto-create ProfileVerification follow-up if not exists
                var existingFollowUp = (await _followUpRepo.WhereActive(f => f.ProfileId == userId && f.FollowUpType == FollowUpType.ProfileVerification)).FirstOrDefault();
                if (existingFollowUp == null)
                {
                    var newFollowUp = new FollowUp
                    {
                        ProfileId = userId,
                        FollowUpType = FollowUpType.ProfileVerification,
                        LatestProfileVerificationStatus = ProfileVerificationStatus.DetailedVerifyRequest,
                        LatestRemarks = "User uploaded verification document(s) via API.",
                        AssignedStaffId = null,
                        IsActive = true
                    };
                    await _followUpRepo.Add(newFollowUp);
                    await _followUpRepo.SaveChanges();

                    var timeline = new FollowUpTimeline
                    {
                        FollowUpId = newFollowUp.Id,
                        StaffId = 0,
                        StaffName = "System (API Upload)",
                        ProfileVerificationStatus = ProfileVerificationStatus.DetailedVerifyRequest,
                        Remarks = "User submitted verification document(s) via API.",
                        IsActive = true
                    };
                    await _followUpTimelineRepo.Add(timeline);
                    await _followUpTimelineRepo.SaveChanges();
                }
                else
                {
                    if (existingFollowUp.LatestProfileVerificationStatus == ProfileVerificationStatus.Pending ||
                        existingFollowUp.LatestProfileVerificationStatus == ProfileVerificationStatus.Hold ||
                        existingFollowUp.LatestProfileVerificationStatus == null)
                    {
                        existingFollowUp.LatestProfileVerificationStatus = ProfileVerificationStatus.DetailedVerifyRequest;
                        existingFollowUp.LatestRemarks = "User uploaded/updated verification document(s) via API.";
                        await _followUpRepo.Update(existingFollowUp);
                        await _followUpRepo.SaveChanges();

                        var timeline = new FollowUpTimeline
                        {
                            FollowUpId = existingFollowUp.Id,
                            StaffId = existingFollowUp.AssignedStaffId ?? 0,
                            StaffName = "System (API Upload)",
                            ProfileVerificationStatus = ProfileVerificationStatus.DetailedVerifyRequest,
                            Remarks = "User updated verification document(s) via API.",
                            IsActive = true
                        };
                        await _followUpTimelineRepo.Add(timeline);
                        await _followUpTimelineRepo.SaveChanges();
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Verification document(s) uploaded successfully.",
                    verificationDocumentUrl = firstUrl,
                    verificationDocumentUrls = uploadedUrls,
                    verificationDocuments = currentActiveDocs.Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("/api/profile/delete-verification-document")]
        public async Task<IActionResult> DeleteVerificationDocument([FromBody] DeleteProfileDocRequest model)
        {
            try
            {
                if (model == null || model.DocumentId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid document ID." });
                }

                var cookieHelper = HttpContext.RequestServices.GetService<CookieHelper>();
                var currentUserId = cookieHelper?.GetUserIdFromCookie(HttpContext) ?? (model.UserId > 0 ? model.UserId : (long?)null);
                if (!currentUserId.HasValue || currentUserId.Value <= 0)
                {
                    return Unauthorized(new { success = false, message = "User session expired. Please log in again." });
                }

                long userId = currentUserId.Value;
                var doc = await _verificationDocRepo.Get(model.DocumentId);
                if (doc == null || doc.ProfileId != userId || doc.IsDeleted)
                {
                    return NotFound(new { success = false, message = "Document not found." });
                }

                await _verificationDocRepo.SoftDelete(doc);
                await _verificationDocRepo.SaveChanges();

                var remainingDocs = (await _verificationDocRepo.WhereActive(d => d.ProfileId == userId)).ToList();
                var user = await _registrationRepo.Get(userId);
                if (user != null)
                {
                    var latestDoc = remainingDocs.OrderByDescending(d => d.CreatedOn).FirstOrDefault();
                    user.VerificationDocumentUrl = latestDoc?.DocumentUrl;
                    await _registrationRepo.Update(user);
                    await _registrationRepo.SaveChanges();
                }

                return Ok(new
                {
                    success = true,
                    message = "Document deleted successfully.",
                    verificationDocumentUrl = user?.VerificationDocumentUrl,
                    verificationDocuments = remainingDocs.Select(d => new
                    {
                        id = d.Id,
                        documentUrl = d.DocumentUrl,
                        originalFileName = d.OriginalFileName,
                        displayOrder = d.DisplayOrder
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class DeleteProfileDocRequest
    {
        public long DocumentId { get; set; }
        public long? UserId { get; set; }
    }

    public class MobileProfileEditDto
    {
        public long UserId { get; set; }
        public string? Section { get; set; }
        public long MaritalStatusId { get; set; }
        public long MotherTongueId { get; set; }
        public string? About { get; set; }
        public long HeightId { get; set; }
        public long WeightId { get; set; }
        public long ComplexionId { get; set; }
        public long BodyTypeId { get; set; }
        public bool IsPhysicallyChallenged { get; set; }
        public string? HighestEducation { get; set; }
        public string? EducationType { get; set; }
        public long ProfessionId { get; set; }
        public string? ProfessionType { get; set; }
        public long CommunityId { get; set; }
        public long ReligiousnessId { get; set; }
        public string? PresentCountry { get; set; }
        public string? LandlineNumber { get; set; }
        public long FinancialStatusId { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? DOB { get; set; }
        public string? Gender { get; set; }
        public long? NumberOfChildrens { get; set; }
        public long? NumberOfChildren { get; set; }
        public long? Number_Of_Children { get; set; }
        public long? Number_Of_Childrens { get; set; }
    }
}
