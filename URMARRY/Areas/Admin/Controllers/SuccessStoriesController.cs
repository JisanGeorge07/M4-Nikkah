using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class SuccessStoriesController : Controller
    {
        private readonly IRepository<SuccessStory> _repo;
        private readonly IRepository<Registration> _userRepository;
        private readonly IRepository<MatchStatusUpdate> _matchStatusRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public SuccessStoriesController(
            IRepository<SuccessStory> repo,
            IRepository<Registration> userRepository,
            IRepository<MatchStatusUpdate> matchStatusRepo,
            IRepository<Notification> notificationRepo,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _repo = repo;
            _userRepository = userRepository;
            _matchStatusRepo = matchStatusRepo;
            _notificationRepo = notificationRepo;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet("/admin/success-stories")]
        public async Task<IActionResult> GetAll()
        {
            ViewBag.users = _mapper.Map<List<RegistrationDto>>(await _userRepository.GetAllWithDeleted());
            var stories = (await _repo.GetAll()).OrderByDescending(x => x.CreatedOn).ToList();
            return View(_mapper.Map<List<SuccessStoryDto>>(stories));
        }

        [HttpGet("/admin/success-stories/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            ViewBag.users = _mapper.Map<List<RegistrationDto>>(await _userRepository.GetAllWithDeleted());

            SuccessStoryDto? dto = id > 0
                ? _mapper.Map<SuccessStoryDto>(await _repo.Get(id))
                : new SuccessStoryDto
                {
                    IsActive = true,
                };

            if (dto != null && !string.IsNullOrWhiteSpace(dto.ReviewedBy))
            {
                ApplicationUser? user = await _userManager.FindByIdAsync(dto.ReviewedBy);
                if (user != null)
                {
                    dto.ReviewedByUsername = user.UserName;
                }
            }

            // Load submitter and partner details
            if (dto != null)
            {
                var submitter = await _userRepository.GetWithDeleted(dto.SubmittedByUserId);
                var partner = await _userRepository.GetWithDeleted(dto.PartnerUserId);
                ViewBag.SubmitterName = submitter?.Name ?? "Unknown";
                ViewBag.SubmitterRegNumber = submitter?.RegisterNumber ?? "";
                ViewBag.SubmitterId = submitter?.Id ?? 0;
                ViewBag.SubmitterIsDeleted = submitter?.IsDeleted ?? false;
                ViewBag.PartnerName = partner?.Name ?? "Unknown";
                ViewBag.PartnerRegNumber = partner?.RegisterNumber ?? "";
                ViewBag.PartnerId = partner?.Id ?? 0;
                ViewBag.PartnerIsDeleted = partner?.IsDeleted ?? false;
            }

            return View(dto);
        }

        [HttpPost("/admin/success-stories")]
        public async Task<IActionResult> Post(SuccessStoryDto model)
        {
            SuccessStory entity;
            SuccessStoryStatus? oldStatus = null;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<SuccessStory>(model);
                if (model.Status != SuccessStoryStatus.PendingApproval)
                {
                    entity.ReviewedBy = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    entity.ReviewedOn = DateTime.UtcNow;
                }
                await _repo.Add(entity);
            }
            else
            {
                entity = (await _repo.Get(model.Id))!;
                oldStatus = entity.Status;

                if (model.Status == SuccessStoryStatus.PendingApproval)
                {
                    model.ReviewedOn = null;
                    model.ReviewedBy = null;
                }
                else if (model.Status != entity.Status)
                {
                    model.ReviewedBy = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    model.ReviewedOn = DateTime.UtcNow;
                }
                else
                {
                    model.ReviewedBy = entity.ReviewedBy;
                    model.ReviewedOn = entity.ReviewedOn;
                }

                // Preserve fields that admin shouldn't overwrite
                model.SubmittedByUserId = entity.SubmittedByUserId;
                model.PartnerUserId = entity.PartnerUserId;
                model.MatchStatusUpdateId = entity.MatchStatusUpdateId;
                model.BrideDisplayName = entity.BrideDisplayName;
                model.GroomDisplayName = entity.GroomDisplayName;
                model.MarriageDate = entity.MarriageDate;
                model.Rating = entity.Rating;
                model.CouplePhotoPath = entity.CouplePhotoPath;
                model.ConsentToPublish = entity.ConsentToPublish;
                model.HideFullName = model.HideFullName; // Admin can toggle this

                // Preserve original story but allow admin-edited version
                if (string.IsNullOrWhiteSpace(model.AdminEditedStory))
                {
                    model.AdminEditedStory = entity.AdminEditedStory;
                }

                // Keep original story intact
                model.Story = entity.Story;

                _mapper.Map(model, entity);
                await _repo.Update(entity);
            }

            await _repo.SaveChanges();

            // Create notification for the submitted user based on admin's action
            if (oldStatus == null || oldStatus.Value != entity.Status)
            {
                var partner = await _userRepository.Get(entity.PartnerUserId);
                var partnerName = partner?.Name ?? "your match";
                string message = "";

                if (entity.Status == SuccessStoryStatus.Published || entity.Status == SuccessStoryStatus.Approved)
                {
                    message = $"Your success story with {partnerName} has been approved and published!";
                }
                else if (entity.Status == SuccessStoryStatus.Rejected)
                {
                    message = $"Your success story with {partnerName} was not approved.";
                    if (!string.IsNullOrWhiteSpace(entity.AdminNotes))
                    {
                        message += $" Reason: {entity.AdminNotes}";
                    }
                }

                if (!string.IsNullOrEmpty(message))
                {
                    var notification = new Notification
                    {
                        UserId = entity.PartnerUserId, // Shows partner's avatar
                        Notify_Id = entity.SubmittedByUserId, // Recipient is the submitter
                        Notify_Message = message,
                        CreatedOn = DateTime.UtcNow,
                        ModifiedOn = DateTime.UtcNow,
                        Is_Read = 0
                    };
                    await _notificationRepo.Add(notification);
                    await _notificationRepo.SaveChanges();
                }
            }

            return RedirectToAction(nameof(Get), new { id = entity.Id });
        }

        [HttpPost("/admin/success-stories/delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _repo.Get(id);
            if (entity != null)
            {
                await _repo.SoftDelete(entity);
                await _repo.SaveChanges();
            }
            return RedirectToAction(nameof(GetAll));
        }
    }
}
