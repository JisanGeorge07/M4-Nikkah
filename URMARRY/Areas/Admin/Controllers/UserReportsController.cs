using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Application.Constants;
using Application.Helpers;
using Application.Interfaces.Infrastructure;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using URMARRY.Services;
using System.Net.Mail;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class UserReportsController : Controller
    {
        private readonly IRepository<Registration> _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<UserReport> _repo;
        private readonly IRepository<UserReportReason> _userReportReasonRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly EmailNotificationHelper _emailNotificationHelper;
        private readonly IRepository<Notification> _notificationRepo;

        public UserReportsController(IRepository<UserReport> repo, IRepository<Registration> userRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, IRepository<UserReportReason> userReportReasonRepository, IUserService userService, EmailNotificationHelper emailNotificationHelper, IRepository<Notification> notificationRepo)
        {
            _userRepository = userRepository;
            _repo = repo;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _userReportReasonRepository = userReportReasonRepository;
            _userService = userService;
            _emailNotificationHelper = emailNotificationHelper;
            _notificationRepo = notificationRepo;
        }

        [HttpGet("/admin/user-reports")]
        public async Task<IActionResult> GetAll()
        {
            ViewBag.users = _mapper.Map<List<RegistrationDto>>(await _userRepository.GetAll());
            return View(_mapper.Map<List<UserReportDto>>((await _repo.GetAll()).OrderByDescending(x => x.CreatedOn)));
        }

        [HttpGet("/admin/user-reports/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            ViewBag.users = _mapper.Map<List<RegistrationDto>>(await _userRepository.GetAll());
            ViewBag.reasons = _mapper.Map<List<UserReportReasonDto>>(await _userReportReasonRepository.GetAllActive());
            UserReportDto? userReportDto = id > 0
                ? _mapper.Map<UserReportDto>(await _repo.Get(id))
                : new UserReportDto
                {
                    IsActive = true,
                };
            if (!string.IsNullOrWhiteSpace(userReportDto.ActionedBy))
            {
                ApplicationUser? user = await _userManager.FindByIdAsync(userReportDto.ActionedBy);
                if (user != null)
                {
                    userReportDto.ActionedByUsername = user.UserName;
                }
            }
            return View(userReportDto);
        }

        [HttpPost("/admin/user-reports")]
        public async Task<IActionResult> Post(UserReportDto model)
        {
            UserReport entity;
            UserReportStatus? oldStatus = null;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<UserReport>(model);
                if (model.Status != UserReportStatus.Pending)
                {
                    entity.ActionedBy = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    entity.ActionedOn = DateTime.UtcNow;
                }
                await _repo.Add(entity);
            }
            else
            {
                entity = (await _repo.Get(model.Id))!;
                oldStatus = entity.Status;
                if (model.Status == UserReportStatus.Pending)
                {
                    model.ActionedOn = null;
                }
                else if (model.Status != entity.Status)
                {
                    model.ActionedBy = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    model.ActionedOn = DateTime.UtcNow;
                }
                else
                {
                    model.ActionedBy = entity.ActionedBy;
                    model.ActionedOn = entity.ActionedOn;
                }
                _mapper.Map(model, entity);
                await _repo.Update(entity);
            }

            await _repo.SaveChanges();

            // Handle automation if status changed from Pending (or any other status) to Actioned/Dismissed
            if (model.Status != oldStatus && model.Status != UserReportStatus.Pending)
            {
                await HandleReportAutomation(entity);
            }

            return RedirectToAction(nameof(Get), new { id = entity.Id });
        }

        private async Task HandleReportAutomation(UserReport report)
        {
            var reporter = await _userRepository.Get(report.ReporterUserId);
            var reportedUser = await _userRepository.Get(report.ReportedUserId);

            if (reporter == null || reportedUser == null) return;

            // Check if reporter had unlocked the contact details of the reported user
            bool isUnlocked = await _userService.AreUserContactDetailsUnlocked(report.ReporterUserId, report.ReportedUserId);

            // 1. Perform Actions based on Status
            string actionDetails = "";
            if (report.Status == UserReportStatus.Actioned)
            {
                // Disable reported user
                reportedUser.IsVisible = false;
                // reportedUser.IsActive = false;
                reportedUser.DisabledReason = DisabledReason.ReportedViolation;
                await _userRepository.Update(reportedUser);
                await _userRepository.SaveChanges(); 

                actionDetails = "Based on our investigation, we have taken action against the reported profile. ";

                // Refund credit to reporter ONLY if they had unlocked the contact
                if (isUnlocked)
                {
                    bool refunded = await _userService.RefundContactViewCredit(report.ReporterUserId);
                    if (refunded)
                    {
                        actionDetails += "As a token of our appreciation for keeping the community safe, we have refunded 1 contact view credit to your account (since you had unlocked this profile's contact details).";
                    }
                }
            }
            else if (report.Status == UserReportStatus.Dismissed)
            {
                actionDetails = "After careful review, we did not find sufficient evidence of a policy violation at this time. No further action has been taken.";
            }

            // 2. Send Notification
            var notification = new Notification
            {
                Notify_Id = report.ReporterUserId,
                UserId = report.ReportedUserId, // System/Admin
                Notify_Message = $"Your report regarding {reportedUser.Name} has been {report.Status}. Check your email for details.",
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };
            await _notificationRepo.Add(notification);
            await _notificationRepo.SaveChanges();

            // 3. Send Email
            try
            {
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates/Mail/UserReport/Resolution.html");
                if (System.IO.File.Exists(templatePath))
                {
                    string emailContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    emailContent = emailContent.Replace("[ReporterName]", reporter.Name)
                                             .Replace("[ReportedUserName]", reportedUser.Name)
                                             .Replace("[Status]", report.Status.ToString())
                                             .Replace("[StatusClass]", report.Status == UserReportStatus.Actioned ? "status-actioned" : "status-dismissed")
                                             .Replace("[Reason]", report.Reason)
                                             .Replace("[ModeratorNotes]", report.ModeratorNotes ?? "No additional notes provided.")
                                             .Replace("[ActionDetails]", actionDetails);

                    var subject = $"Report Update: {report.Status} - M4Nikah";
                    var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                    _emailNotificationHelper.SendEmail(reporter.Email!, htmlView, subject);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't break the flow
                Console.WriteLine($"Error sending report resolution email: {ex.Message}");
            }
        }

        [HttpGet("/admin/user-report-reasons")]
        public async Task<IActionResult> UserReportReasonGetAll()
        {
            return View(_mapper.Map<List<UserReportReasonDto>>((await _userReportReasonRepository.GetAll()).OrderBy(x => x.DisplayOrder)));
        }

        [HttpGet("/admin/user-report-reasons/{id:long}")]
        public async Task<IActionResult> UserReportReasonGet(long id)
        {
            UserReportReasonDto? userReportReasonDto = id > 0
                ? _mapper.Map<UserReportReasonDto>(await _userReportReasonRepository.Get(id))
                : new UserReportReasonDto
                {
                    IsActive = true,
                    Id = 0
                };
            return View(userReportReasonDto);
        }

        [HttpPost("/admin/user-report-reasons/post")]
        public async Task<IActionResult> PostReason(UserReportReasonDto model)
        {
            UserReportReason entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<UserReportReason>(model);
                var userReportReasons = await _userReportReasonRepository.GetAll();
                entity.DisplayOrder = userReportReasons.Any() ? userReportReasons.Max(x => x.DisplayOrder) + 1 : 1;
                await _userReportReasonRepository.Add(entity);
            }
            else
            {
                entity = (await _userReportReasonRepository.Get(model.Id))!;
                _mapper.Map(model, entity);
                await _userReportReasonRepository.Update(entity);
            }

            await _userReportReasonRepository.SaveChanges();

            return RedirectToAction(nameof(UserReportReasonGetAll));
        }

        [HttpPost("/admin/user-report-reasons/delete/{id:long}")]
        public async Task<IActionResult> DeleteReason(long id)
        {
            var entity = await _userReportReasonRepository.Get(id);
            if (entity != null)
            {
                await _userReportReasonRepository.SoftDelete(entity);
                await _userReportReasonRepository.SaveChanges();
            }
            return RedirectToAction(nameof(UserReportReasonGetAll));
        }
    }
}
