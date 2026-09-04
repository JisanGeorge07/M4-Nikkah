using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using URMARRY.Services;

namespace URMARRY.Controllers.Api
{
    [ApiController]
    [Route("api/testimonial")]
    public class TestimonialApiController : ControllerBase
    {
        private readonly IRepository<UserFavouriteProfile> _favouriteRepo;
        private readonly IRepository<MatchStatusUpdate> _matchStatusRepo;
        private readonly IRepository<SuccessStory> _successStoryRepo;
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;
        private readonly IRepository<TestimonialSetting> _testimonialSettingRepo;

        public TestimonialApiController(
            IRepository<UserFavouriteProfile> favouriteRepo,
            IRepository<MatchStatusUpdate> matchStatusRepo,
            IRepository<SuccessStory> successStoryRepo,
            IRepository<Registration> registrationRepo,
            IRepository<Notification> notificationRepo,
            IFileService fileService,
            IMapper mapper,
            IRepository<TestimonialSetting> testimonialSettingRepo)
        {
            _favouriteRepo = favouriteRepo;
            _matchStatusRepo = matchStatusRepo;
            _successStoryRepo = successStoryRepo;
            _registrationRepo = registrationRepo;
            _notificationRepo = notificationRepo;
            _fileService = fileService;
            _mapper = mapper;
            _testimonialSettingRepo = testimonialSettingRepo;
        }

        /// <summary>
        /// Returns accepted interests that haven't had a status update yet.
        /// Checks date (older than 7 days) OR if manual trigger notification is active.
        /// Used by user dashboard and notification section.
        /// </summary>
        [HttpGet("pending-status-prompts")]
        public async Task<IActionResult> GetPendingStatusPrompts(long userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { success = false, message = "Invalid userId" });

                var settings = await _testimonialSettingRepo.First();
                var firstPromptDays = settings?.FirstPromptDays ?? 7;
                var followUpPromptDays = settings?.FollowUpPromptDays ?? 30;

                var sevenDaysAgo = DateTime.UtcNow.AddDays(-firstPromptDays);

                // Get all accepted interests involving this user (all accepted)
                var acceptedInterests = (await _favouriteRepo.WhereActive(x =>
                    (x.UserId == userId || x.LikedId == userId) &&
                    x.Status == InterestStatus.Accepted
                )).ToList();

                if (!acceptedInterests.Any())
                    return Ok(new { success = true, data = new List<object>() });

                // Get existing status updates involving this user (either as UserId or PartnerId)
                var existingUpdates = await _matchStatusRepo.WhereActive(x => x.UserId == userId || x.PartnerId == userId);

                // Exclude matches that are finalized (Married, Rejected, PreferNotToSay)
                var finalizedMatchIds = existingUpdates
                    .Where(u =>
                        u.Status == MatchRelationshipStatus.Married ||
                        u.Status == MatchRelationshipStatus.Rejected ||
                        u.Status == MatchRelationshipStatus.PreferNotToSay
                    )
                    .Select(u => u.UserFavouriteProfileId)
                    .ToHashSet();

                // Exclude matches that were updated recently (within the last X days)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-followUpPromptDays);
                var recentUpdateMatchIds = existingUpdates
                    .Where(u =>
                        (u.Status == MatchRelationshipStatus.Engaged || u.Status == MatchRelationshipStatus.StillCommunicating) &&
                        (u.StatusUpdatedOn.HasValue && u.StatusUpdatedOn.Value > thirtyDaysAgo)
                    )
                    .Select(u => u.UserFavouriteProfileId)
                    .ToHashSet();

                var promptPrefix = RelationshipStatusNotificationService.StatusUpdatePromptPrefix;
                var existingPromptNotifs = (await _notificationRepo.Where(x =>
                    x.Notify_Id == userId &&
                    x.Notify_Message.StartsWith(promptPrefix) &&
                    !x.IsDeleted)).ToList();

                var pendingPrompts = acceptedInterests
                    .Where(x => {
                        if (finalizedMatchIds.Contains(x.Id))
                            return false;

                        var partnerId = x.UserId == userId ? x.LikedId : x.UserId;
                        bool hasNotification = existingPromptNotifs.Any(n => n.UserId == partnerId);

                        // If manually triggered by admin, prompt immediately regardless of recent update or match age
                        if (hasNotification)
                            return true;

                        // Default behavior: prompt if older than 7 days and has not been updated in the last 30 days
                        bool isOlderThan7Days = x.ModifiedOn <= sevenDaysAgo;
                        bool isRecentUpdate = recentUpdateMatchIds.Contains(x.Id);

                        return isOlderThan7Days && !isRecentUpdate;
                    })
                    .ToList();

                if (!pendingPrompts.Any())
                    return Ok(new { success = true, data = new List<object>() });

                // Get partner details
                var partnerIds = pendingPrompts
                    .Select(x => x.UserId == userId ? x.LikedId : x.UserId)
                    .Distinct()
                    .ToList();

                var partners = await _registrationRepo.GetAllByIds(partnerIds);
                var partnerDict = partners.ToDictionary(x => x.Id);
                var existingUpdatesDict = existingUpdates.ToDictionary(x => x.UserFavouriteProfileId);

                bool notificationAdded = false;
                foreach (var interest in pendingPrompts)
                {
                    var partnerId = interest.UserId == userId ? interest.LikedId : interest.UserId;
                    var alreadyExists = existingPromptNotifs.Any(n => n.UserId == partnerId);
                    if (!alreadyExists)
                    {
                        partnerDict.TryGetValue(partnerId, out var partner);
                        var partnerName = partner?.Name ?? "your match";

                        var notification = new Notification
                        {
                            UserId = partnerId,
                            Notify_Id = userId,
                            Notify_Message = $"{promptPrefix} Update your relationship status with {partnerName}",
                            CreatedOn = DateTime.UtcNow,
                            ModifiedOn = DateTime.UtcNow,
                            Is_Read = 0
                        };

                        await _notificationRepo.Add(notification);
                        notificationAdded = true;
                    }
                }

                if (notificationAdded)
                {
                    await _notificationRepo.SaveChanges();
                }

                var result = pendingPrompts.Select(interest =>
                {
                    var partnerId = interest.UserId == userId ? interest.LikedId : interest.UserId;
                    partnerDict.TryGetValue(partnerId, out var partner);
                    
                    existingUpdatesDict.TryGetValue(interest.Id, out var existingUpdate);
                    var currentStatusValue = existingUpdate != null ? (int)existingUpdate.Status : (int?)null;

                    return new
                    {
                        userFavouriteProfileId = interest.Id,
                        partnerId,
                        partnerName = partner?.Name ?? "Unknown",
                        partnerRegNumber = partner?.RegisterNumber ?? "",
                        partnerImagePath = partner?.ImagePath ?? "",
                        acceptedOn = interest.ModifiedOn,
                        currentStatus = currentStatusValue
                    };
                }).ToList();

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Manually triggers a status update prompt notification for both users of an accepted interest.
        /// </summary>
        [HttpPost("trigger-prompt-manual")]
        public async Task<IActionResult> TriggerPromptManual([FromForm] long userFavouriteProfileId)
        {
            try
            {
                if (userFavouriteProfileId <= 0)
                    return BadRequest(new { success = false, message = "Invalid parameters" });

                var interest = await _favouriteRepo.Get(userFavouriteProfileId);
                if (interest == null)
                    return NotFound(new { success = false, message = "Interest record not found" });

                if (interest.Status != InterestStatus.Accepted)
                    return BadRequest(new { success = false, message = "Interest is not in Accepted status" });

                // Check existing status updates
                var existingUpdates = await _matchStatusRepo.WhereActive(x => x.UserFavouriteProfileId == interest.Id);

                // Check if either side has finalized
                var isFinalized = existingUpdates.Any(u =>
                    u.Status == MatchRelationshipStatus.Married ||
                    u.Status == MatchRelationshipStatus.Rejected ||
                    u.Status == MatchRelationshipStatus.PreferNotToSay);

                if (isFinalized)
                    return BadRequest(new { success = false, message = "Relationship status is already finalized for this match" });

                // Create prompt prefix
                var promptPrefix = RelationshipStatusNotificationService.StatusUpdatePromptPrefix;
                var usersToPrompt = new[] { interest.UserId, interest.LikedId };

                var userIds = usersToPrompt.ToList();
                var partners = await _registrationRepo.GetAllByIds(userIds);
                var partnerDict = partners.ToDictionary(x => x.Id);

                bool notificationAdded = false;
                foreach (var userId in usersToPrompt)
                {
                    var partnerId = userId == interest.UserId ? interest.LikedId : interest.UserId;

                    // Check if notification already exists
                    var existingNotif = await _notificationRepo.FirstOrDefault(x =>
                        x.Notify_Id == userId &&
                        x.UserId == partnerId &&
                        x.Notify_Message.StartsWith(promptPrefix) &&
                        !x.IsDeleted);

                    if (existingNotif == null)
                    {
                        partnerDict.TryGetValue(partnerId, out var partner);
                        var partnerName = partner?.Name ?? "your match";

                        var notification = new Notification
                        {
                            UserId = partnerId,
                            Notify_Id = userId,
                            Notify_Message = $"{promptPrefix} Update your relationship status with {partnerName}",
                            CreatedOn = DateTime.UtcNow,
                            ModifiedOn = DateTime.UtcNow,
                            Is_Read = 0
                        };

                        await _notificationRepo.Add(notification);
                        notificationAdded = true;
                    }
                }

                if (notificationAdded)
                {
                    await _notificationRepo.SaveChanges();
                }

                return Ok(new { success = true, message = "Testimonial status prompt triggered successfully for both users!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// User submits relationship status update (Engaged, Married, etc.)
        /// </summary>
        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromForm] long userId, [FromForm] long partnerId,
            [FromForm] long userFavouriteProfileId, [FromForm] MatchRelationshipStatus status)
        {
            try
            {
                if (userId <= 0 || partnerId <= 0 || userFavouriteProfileId <= 0)
                    return BadRequest(new { success = false, message = "Invalid parameters" });

                // Verify the interest record exists and is accepted
                var interest = await _favouriteRepo.Get(userFavouriteProfileId);
                if (interest == null || interest.Status != InterestStatus.Accepted)
                    return BadRequest(new { success = false, message = "Interest record not found or not accepted" });

                // Check if an update already exists — if so, update it (repeatable)
                var existing = await _matchStatusRepo.FirstOrDefaultActive(x =>
                    x.UserId == userId && x.UserFavouriteProfileId == userFavouriteProfileId);

                if (existing != null)
                {
                    existing.Status = status;
                    existing.StatusUpdatedOn = DateTime.UtcNow;
                    await _matchStatusRepo.Update(existing);
                }
                else
                {
                    var matchStatus = new MatchStatusUpdate
                    {
                        UserId = userId,
                        PartnerId = partnerId,
                        UserFavouriteProfileId = userFavouriteProfileId,
                        Status = status,
                        StatusUpdatedOn = DateTime.UtcNow
                    };
                    await _matchStatusRepo.Add(matchStatus);
                }

                await _matchStatusRepo.SaveChanges();

                // Clean up or refresh the STATUS_UPDATE_PROMPT notification for both users in this match
                var promptPrefix = RelationshipStatusNotificationService.StatusUpdatePromptPrefix;
                var existingPromptNotifications = await _notificationRepo.Where(x =>
                    ((x.Notify_Id == userId && x.UserId == partnerId) ||
                     (x.Notify_Id == partnerId && x.UserId == userId)) &&
                    x.Notify_Message.StartsWith(promptPrefix) &&
                    !x.IsDeleted);

                if (status == MatchRelationshipStatus.Married ||
                    status == MatchRelationshipStatus.Rejected ||
                    status == MatchRelationshipStatus.PreferNotToSay)
                {
                    // Finalized status — remove the prompt notification
                    foreach (var promptNotif in existingPromptNotifications)
                    {
                        await _notificationRepo.Remove(promptNotif);
                    }
                    await _notificationRepo.SaveChanges();
                }
                else if (status == MatchRelationshipStatus.Engaged ||
                         status == MatchRelationshipStatus.StillCommunicating)
                {
                    // Non-final status — remove current notification so the background service
                    // can re-create it after 30 days when the status update becomes stale
                    foreach (var promptNotif in existingPromptNotifications)
                    {
                        await _notificationRepo.Remove(promptNotif);
                    }
                    await _notificationRepo.SaveChanges();
                }

                return Ok(new
                {
                    success = true,
                    message = "Status updated successfully",
                    showStoryForm = status == MatchRelationshipStatus.Married
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Checks if user is eligible to submit a success story (status = Married, no existing story)
        /// </summary>
        [HttpGet("can-submit-story")]
        public async Task<IActionResult> CanSubmitStory(long userId, long partnerId)
        {
            try
            {
                // Check if there's a Married status update
                var marriedStatus = await _matchStatusRepo.FirstOrDefaultActive(x =>
                    x.UserId == userId && x.PartnerId == partnerId &&
                    x.Status == MatchRelationshipStatus.Married);

                if (marriedStatus == null)
                    return Ok(new { success = true, canSubmit = false, reason = "No married status found" });

                // Check if story already exists
                var existingStory = await _successStoryRepo.FirstOrDefaultActive(x =>
                    x.SubmittedByUserId == userId && x.PartnerUserId == partnerId);

                if (existingStory != null)
                    return Ok(new { success = true, canSubmit = false, reason = "Story already submitted", storyStatus = existingStory.Status.ToString() });

                // Get partner details for pre-filling the form
                var partner = await _registrationRepo.Get(partnerId);
                var user = await _registrationRepo.Get(userId);

                return Ok(new
                {
                    success = true,
                    canSubmit = true,
                    matchStatusUpdateId = marriedStatus.Id,
                    userName = user?.Name ?? "",
                    partnerName = partner?.Name ?? "",
                    userGender = user?.Gender ?? "",
                    partnerGender = partner?.Gender ?? ""
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// User submits a success story. Multipart form with optional photo.
        /// </summary>
        [HttpPost("submit-story")]
        public async Task<IActionResult> SubmitStory([FromForm] SuccessStoryDto model)
        {
            try
            {
                if (model.SubmittedByUserId <= 0 || model.PartnerUserId <= 0)
                    return BadRequest(new { success = false, message = "Invalid user IDs" });

                if (!model.ConsentToPublish)
                    return BadRequest(new { success = false, message = "Consent to publish is required" });

                if (string.IsNullOrWhiteSpace(model.Story))
                    return BadRequest(new { success = false, message = "Story text is required" });

                if (model.Rating < 1 || model.Rating > 5)
                    return BadRequest(new { success = false, message = "Rating must be between 1 and 5" });

                // Check for duplicate submission
                var existing = await _successStoryRepo.FirstOrDefaultActive(x =>
                    x.SubmittedByUserId == model.SubmittedByUserId && x.PartnerUserId == model.PartnerUserId);

                if (existing != null)
                    return BadRequest(new { success = false, message = "You have already submitted a story for this match" });

                var entity = _mapper.Map<SuccessStory>(model);
                entity.Status = SuccessStoryStatus.PendingApproval;

                // Handle photo upload
                if (model.CouplePhoto != null && model.CouplePhoto.Length > 0)
                {
                    entity.CouplePhotoPath = await _fileService.UploadFile(model.CouplePhoto, "Uploads/SuccessStories");
                }

                await _successStoryRepo.Add(entity);
                await _successStoryRepo.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Your success story has been submitted successfully! It will be published after admin approval."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Returns the user's own submitted stories with current approval status.
        /// </summary>
        [HttpGet("my-stories")]
        public async Task<IActionResult> GetMyStories(long userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { success = false, message = "Invalid userId" });

                var stories = (await _successStoryRepo.WhereActive(x => x.SubmittedByUserId == userId))
                    .OrderByDescending(x => x.CreatedOn)
                    .ToList();

                var result = stories.Select(s => new
                {
                    id = s.Id,
                    brideDisplayName = s.BrideDisplayName,
                    groomDisplayName = s.GroomDisplayName,
                    marriageDate = s.MarriageDate,
                    story = s.Story,
                    rating = s.Rating,
                    couplePhotoPath = s.CouplePhotoPath,
                    status = s.Status.ToString(),
                    hideFullName = s.HideFullName,
                    createdOn = s.CreatedOn
                }).ToList();

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Public endpoint — Returns all published success stories for home page display.
        /// Featured stories first, then by marriage date descending.
        /// </summary>
        [HttpGet("published")]
        public async Task<IActionResult> GetPublishedStories()
        {
            try
            {
                var stories = (await _successStoryRepo.WhereActive(x =>
                    x.Status == SuccessStoryStatus.Published && x.ConsentToPublish))
                    .OrderByDescending(x => x.IsFeatured)
                    .ThenByDescending(x => x.MarriageDate)
                    .Take(10)
                    .ToList();

                var result = stories.Select(s =>
                {
                    // Use admin-edited story if available, otherwise original
                    var displayStory = !string.IsNullOrEmpty(s.AdminEditedStory) ? s.AdminEditedStory : s.Story;
                    if (displayStory != null && displayStory.Length > 300)
                    {
                        displayStory = displayStory.Substring(0, 300) + "...";
                    }

                    // Handle name display based on privacy setting
                    string brideName = s.BrideDisplayName;
                    string groomName = s.GroomDisplayName;
                    if (s.HideFullName)
                    {
                        brideName = GetInitials(s.BrideDisplayName);
                        groomName = GetInitials(s.GroomDisplayName);
                    }

                    // Calculate "Married X Months Ago" text
                    var monthsAgo = ((DateTime.UtcNow.Year - s.MarriageDate.Year) * 12) + DateTime.UtcNow.Month - s.MarriageDate.Month;
                    string durationText;
                    if (monthsAgo < 1)
                        durationText = "Married Recently";
                    else if (monthsAgo == 1)
                        durationText = "Married 1 Month Ago";
                    else if (monthsAgo < 12)
                        durationText = $"Married {monthsAgo} Months Ago";
                    else if (monthsAgo == 12)
                        durationText = "Married 1 Year Ago";
                    else
                        durationText = $"Married {monthsAgo / 12} Year{(monthsAgo / 12 > 1 ? "s" : "")} Ago";

                    return new
                    {
                        id = s.Id,
                        brideName,
                        groomName,
                        coupleName = $"{groomName} & {brideName}",
                        marriageDate = s.MarriageDate,
                        story = displayStory,
                        rating = s.Rating,
                        couplePhotoPath = s.CouplePhotoPath,
                        isFeatured = s.IsFeatured,
                        isVerified = true,  // All published stories are verified
                        durationText,
                        hideFullName = s.HideFullName
                    };
                }).ToList();

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Returns a single published story detail.
        /// </summary>
        [HttpGet("published/{id}")]
        public async Task<IActionResult> GetPublishedStory(long id)
        {
            try
            {
                var story = await _successStoryRepo.GetActive(id);
                if (story == null || story.Status != SuccessStoryStatus.Published)
                    return NotFound(new { success = false, message = "Story not found" });

                var displayStory = !string.IsNullOrEmpty(story.AdminEditedStory) ? story.AdminEditedStory : story.Story;
                if (displayStory != null && displayStory.Length > 300)
                {
                    displayStory = displayStory.Substring(0, 300) + "...";
                }
                string brideName = story.HideFullName ? GetInitials(story.BrideDisplayName) : story.BrideDisplayName;
                string groomName = story.HideFullName ? GetInitials(story.GroomDisplayName) : story.GroomDisplayName;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = story.Id,
                        brideName,
                        groomName,
                        coupleName = $"{groomName} & {brideName}",
                        marriageDate = story.MarriageDate,
                        story = displayStory,
                        rating = story.Rating,
                        couplePhotoPath = story.CouplePhotoPath,
                        isFeatured = story.IsFeatured,
                        isVerified = true
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Returns the user's match status updates.
        /// </summary>
        [HttpGet("my-status-updates")]
        public async Task<IActionResult> GetMyStatusUpdates(long userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { success = false, message = "Invalid userId" });

                var updates = (await _matchStatusRepo.WhereActive(x => x.UserId == userId))
                    .OrderByDescending(x => x.StatusUpdatedOn)
                    .ToList();

                var partnerIds = updates.Select(x => x.PartnerId).Distinct().ToList();
                var partners = await _registrationRepo.GetAllByIds(partnerIds);
                var partnerDict = partners.ToDictionary(x => x.Id);

                var result = updates.Select(u =>
                {
                    partnerDict.TryGetValue(u.PartnerId, out var partner);
                    return new
                    {
                        id = u.Id,
                        partnerId = u.PartnerId,
                        partnerName = partner?.Name ?? "Unknown",
                        status = u.Status.ToString(),
                        statusValue = (int)u.Status,
                        statusUpdatedOn = u.StatusUpdatedOn
                    };
                }).ToList();

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Returns enum metadata for match relationship status and success story status.
        /// </summary>
        [HttpGet("metadata")]
        public IActionResult GetMetadata()
        {
            try
            {
                var relationshipStatuses = Enum.GetValues(typeof(MatchRelationshipStatus))
                    .Cast<MatchRelationshipStatus>()
                    .Select(e => new { id = (int)e, name = e.ToString() })
                    .ToList();

                var successStoryStatuses = Enum.GetValues(typeof(SuccessStoryStatus))
                    .Cast<SuccessStoryStatus>()
                    .Select(e => new { id = (int)e, name = e.ToString() })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        relationshipStatuses,
                        successStoryStatuses
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0][0].ToString().ToUpper();
            return string.Join(". ", parts.Select(p => p[0].ToString().ToUpper())) + ".";
        }
    }
}
