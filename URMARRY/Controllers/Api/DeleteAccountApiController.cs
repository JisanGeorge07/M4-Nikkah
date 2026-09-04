using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using URMARRY.Services;

namespace URMARRY.Controllers.Api
{
    [ApiController]
    [Route("api/delete-account")]
    public class DeleteAccountApiController : ControllerBase
    {
        private readonly IRepository<DeleteReason> _deleteReasonRepo;
        private readonly IRepository<UserFavouriteProfile> _favouriteRepo;
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IRepository<SuccessStory> _successStoryRepo;
        private readonly IRepository<MatchStatusUpdate> _matchStatusRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;

        public DeleteAccountApiController(
            IRepository<DeleteReason> deleteReasonRepo,
            IRepository<UserFavouriteProfile> favouriteRepo,
            IRepository<Registration> registrationRepo,
            IRepository<SuccessStory> successStoryRepo,
            IRepository<MatchStatusUpdate> matchStatusRepo,
            IRepository<Notification> notificationRepo,
            IFileService fileService,
            IMapper mapper)
        {
            _deleteReasonRepo = deleteReasonRepo;
            _favouriteRepo = favouriteRepo;
            _registrationRepo = registrationRepo;
            _successStoryRepo = successStoryRepo;
            _matchStatusRepo = matchStatusRepo;
            _notificationRepo = notificationRepo;
            _fileService = fileService;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves list of all active delete reasons, ordered by DisplayOrder.
        /// </summary>
        [HttpGet("reasons")]
        public async Task<IActionResult> GetReasons()
        {
            try
            {
                var reasons = (await _deleteReasonRepo.WhereActive(x => x.IsActive))
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();

                var dtos = _mapper.Map<List<DeleteReasonDto>>(reasons);
                return Ok(new { success = true, reasons = dtos });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves potential partner profiles (accepted interest matches) for a given userId.
        /// </summary>
        [HttpGet("partners")]
        public async Task<IActionResult> GetPartners(long userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { success = false, message = "Invalid userId" });

                // Find all accepted interest records involving this user
                var acceptedInterests = (await _favouriteRepo.WhereActive(x =>
                    (x.UserId == userId || x.LikedId == userId) &&
                    x.Status == InterestStatus.Accepted
                )).ToList();

                if (!acceptedInterests.Any())
                    return Ok(new { success = true, partners = new List<object>() });

                // Get unique partner user IDs
                var partnerIds = acceptedInterests
                    .Select(x => x.UserId == userId ? x.LikedId : x.UserId)
                    .Distinct()
                    .ToList();

                var partners = await _registrationRepo.GetAllByIds(partnerIds);

                var result = partners.Select(p => new
                {
                    id = p.Id,
                    name = p.Name ?? "Unknown",
                    registerNumber = p.RegisterNumber ?? "",
                    gender = p.Gender ?? "",
                    imagePath = p.ImagePath ?? ""
                }).ToList();

                return Ok(new { success = true, partners = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Submits account deletion. If testimonial details are present, creates a success story and match status update.
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitDelete([FromForm] DeleteAccountSubmitModel model)
        {
            try
            {
                if (model.UserId <= 0 || model.DeleteReasonId <= 0)
                    return BadRequest(new { success = false, message = "UserId and DeleteReasonId are required" });

                var user = await _registrationRepo.Get(model.UserId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var reason = await _deleteReasonRepo.Get(model.DeleteReasonId);
                if (reason == null)
                    return BadRequest(new { success = false, message = "Selected delete reason is invalid" });

                // Handle testimonial/success story submission if applicable
                if (reason.ShowTestimonialPrompt && model.PartnerUserId.HasValue && model.PartnerUserId.Value > 0)
                {
                    // Verify match exists
                    var match = (await _favouriteRepo.WhereActive(x =>
                        (x.UserId == model.UserId && x.LikedId == model.PartnerUserId.Value) ||
                        (x.UserId == model.PartnerUserId.Value && x.LikedId == model.UserId)
                    )).FirstOrDefault();

                    long favoriteId = match?.Id ?? 0;

                    // 1. Create or update Match Status Update
                    var existingUpdate = await _matchStatusRepo.FirstOrDefaultActive(x =>
                        x.UserId == model.UserId && x.PartnerId == model.PartnerUserId.Value);

                    long matchStatusId = 0;
                    if (existingUpdate != null)
                    {
                        existingUpdate.Status = MatchRelationshipStatus.Married;
                        existingUpdate.StatusUpdatedOn = DateTime.UtcNow;
                        await _matchStatusRepo.Update(existingUpdate);
                        matchStatusId = existingUpdate.Id;
                    }
                    else
                    {
                        var newUpdate = new MatchStatusUpdate
                        {
                            UserId = model.UserId,
                            PartnerId = model.PartnerUserId.Value,
                            UserFavouriteProfileId = favoriteId,
                            Status = MatchRelationshipStatus.Married,
                            StatusUpdatedOn = DateTime.UtcNow,
                            CreatedOn = DateTime.UtcNow,
                            ModifiedOn = DateTime.UtcNow,
                            IsActive = true,
                            IsDeleted = false
                        };
                        await _matchStatusRepo.Add(newUpdate);
                        await _matchStatusRepo.SaveChanges(); // Save to get the ID for SuccessStory foreign key
                        matchStatusId = newUpdate.Id;
                    }

                    // 2. Create Success Story
                    var story = new SuccessStory
                    {
                        SubmittedByUserId = model.UserId,
                        PartnerUserId = model.PartnerUserId.Value,
                        MatchStatusUpdateId = matchStatusId,
                        BrideDisplayName = model.BrideDisplayName ?? string.Empty,
                        GroomDisplayName = model.GroomDisplayName ?? string.Empty,
                        MarriageDate = model.MarriageDate ?? DateTime.UtcNow,
                        Story = model.Story ?? string.Empty,
                        Rating = model.Rating ?? 5,
                        ConsentToPublish = model.ConsentToPublish,
                        HideFullName = model.HideFullName,
                        Status = SuccessStoryStatus.PendingApproval,
                        CreatedOn = DateTime.UtcNow,
                        ModifiedOn = DateTime.UtcNow,
                        IsActive = true,
                        IsDeleted = false
                    };

                    if (model.CouplePhoto != null && model.CouplePhoto.Length > 0)
                    {
                        story.CouplePhotoPath = await _fileService.UploadFile(model.CouplePhoto, "Uploads/SuccessStories");
                    }

                    await _successStoryRepo.Add(story);

                    // 3. Clear prompt notifications
                    var promptNotifications = await _notificationRepo.Where(x =>
                        ((x.Notify_Id == model.UserId && x.UserId == model.PartnerUserId.Value) ||
                         (x.Notify_Id == model.PartnerUserId.Value && x.UserId == model.UserId)) &&
                        !x.IsDeleted);

                    foreach (var notif in promptNotifications)
                    {
                        await _notificationRepo.Remove(notif);
                    }
                }

                // 4. Perform soft delete and mark recycled status on the user account
                user.IsActive = false;
                // user.IsDeleted = true;
                user.DisabledReason = Application.Constants.DisabledReason.Recycled;
                user.DeleteReasonId = model.DeleteReasonId;
                user.DeleteReasonText = reason.Reason;

                await _registrationRepo.Update(user);

                // Save all changes
                await _registrationRepo.SaveChanges();
                await _favouriteRepo.SaveChanges();
                await _successStoryRepo.SaveChanges();
                await _matchStatusRepo.SaveChanges();
                await _notificationRepo.SaveChanges();

                // 5. Clear web authentication session cookies
                if (Request.Cookies.ContainsKey("id"))
                {
                    Response.Cookies.Delete("id");
                }

                return Ok(new { success = true, message = "Your account has been deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class DeleteAccountSubmitModel
    {
        public long UserId { get; set; }
        public long DeleteReasonId { get; set; }
        public long? PartnerUserId { get; set; }
        public string? BrideDisplayName { get; set; }
        public string? GroomDisplayName { get; set; }
        public DateTime? MarriageDate { get; set; }
        public string? Story { get; set; }
        public int? Rating { get; set; }
        public IFormFile? CouplePhoto { get; set; }
        public bool ConsentToPublish { get; set; }
        public bool HideFullName { get; set; }
    }
}
