using Application.Constants;
using Application.Helpers;
using Application.Interfaces.Persistence;
using Application.Models.Call;
using AutoMapper;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using URMARRY.Areas.Admin.Models;
using URMARRY.Services;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class CallReportsController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IRepository<CallReport> _callReportRepo;
        private readonly IRepository<Registration> _userRepository;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IWebHostEnvironment _env;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailNotificationHelper _emailNotificationHelper;
        private readonly ILogger<CallReportsController> _logger;

        public CallReportsController(
            AppDbContext dbContext,
            IRepository<CallReport> callReportRepo,
            IRepository<Registration> userRepository,
            IRepository<Notification> notificationRepo,
            IWebHostEnvironment env,
            IMapper mapper,
            IUserService userService,
            UserManager<ApplicationUser> userManager,
            EmailNotificationHelper emailNotificationHelper,
            ILogger<CallReportsController> logger)
        {
            _dbContext = dbContext;
            _callReportRepo = callReportRepo;
            _userRepository = userRepository;
            _notificationRepo = notificationRepo;
            _env = env;
            _mapper = mapper;
            _userService = userService;
            _userManager = userManager;
            _emailNotificationHelper = emailNotificationHelper;
            _logger = logger;
        }

        [HttpGet("/admin/call-reports")]
        public async Task<IActionResult> Index([FromQuery] string? status = null)
        {
            try
            {
                var query = _dbContext.CallReports
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.CreatedOn);

                var allReports = await query.ToListAsync();

                var userIds = allReports.Select(r => r.ReporterUserId)
                    .Concat(allReports.Select(r => r.ReportedUserId))
                    .Distinct()
                    .ToList();

                var users = await _dbContext.Registration
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                var callLogIds = allReports.Where(r => r.CallLogId.HasValue).Select(r => r.CallLogId!.Value).Distinct().ToList();
                var callLogs = await _dbContext.CallLogs
                    .AsNoTracking()
                    .Where(c => callLogIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);

                var dtoList = allReports.Select(r =>
                {
                    users.TryGetValue(r.ReporterUserId, out var reporter);
                    users.TryGetValue(r.ReportedUserId, out var reported);

                    CallLog? log = null;
                    if (r.CallLogId.HasValue) callLogs.TryGetValue(r.CallLogId.Value, out log);

                    bool hasBackup = false;
                    string? recordingUrl = log?.RecordingUrl;
                    string? filePath = log?.RecordingFilePath;

                    if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                    {
                        hasBackup = true;
                    }
                    else if (!string.IsNullOrEmpty(recordingUrl))
                    {
                        string localPath = Path.Combine(_env.WebRootPath, recordingUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(localPath))
                        {
                            hasBackup = true;
                        }
                    }

                    return new CallReportDto
                    {
                        Id = r.Id,
                        ReporterUserId = r.ReporterUserId,
                        ReporterName = reporter?.Name ?? "Deleted Member",
                        ReporterRegisterNumber = reporter?.RegisterNumber,
                        ReporterPhotoUrl = reporter?.ImagePath,
                        ReporterPhone = reporter?.Phone,

                        ReportedUserId = r.ReportedUserId,
                        ReportedUserName = reported?.Name ?? "Deleted Member",
                        ReportedUserRegisterNumber = reported?.RegisterNumber,
                        ReportedUserPhotoUrl = reported?.ImagePath,
                        ReportedUserPhone = reported?.Phone,

                        CallLogId = r.CallLogId,
                        CallType = r.CallType ?? log?.CallType ?? UserCallType.Voice,
                        CallDurationSeconds = r.CallDurationSeconds ?? log?.DurationSeconds ?? 0,
                        Reason = r.Reason,
                        Comments = r.Comments,
                        HasRecording = hasBackup,
                        RecordingUrl = recordingUrl,
                        RecordingFilePath = filePath,

                        Status = r.Status,
                        ModeratorNotes = r.ModeratorNotes,
                        ActionedBy = r.ActionedBy,
                        ActionedOn = r.ActionedOn,
                        CreatedOn = r.CreatedOn
                    };
                }).ToList();

                var model = new CallReportListViewModel
                {
                    TotalCount = dtoList.Count,
                    PendingCount = dtoList.Count(d => d.Status == UserReportStatus.Pending),
                    ActionedCount = dtoList.Count(d => d.Status == UserReportStatus.Actioned),
                    DismissedCount = dtoList.Count(d => d.Status == UserReportStatus.Dismissed),
                    SelectedStatus = status
                };

                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserReportStatus>(status, true, out var filterStatus))
                {
                    model.Reports = dtoList.Where(d => d.Status == filterStatus).ToList();
                }
                else
                {
                    model.Reports = dtoList;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading call reports list in admin panel.");
                return View(new CallReportListViewModel());
            }
        }

        [HttpGet("/admin/call-reports/{id:long}")]
        public async Task<IActionResult> Details(long id)
        {
            var report = await _dbContext.CallReports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (report == null)
            {
                return NotFound("Call report not found.");
            }

            var reporter = await _dbContext.Registration.AsNoTracking().FirstOrDefaultAsync(u => u.Id == report.ReporterUserId);
            var reported = await _dbContext.Registration.AsNoTracking().FirstOrDefaultAsync(u => u.Id == report.ReportedUserId);

            CallLog? callLog = null;
            if (report.CallLogId.HasValue)
            {
                callLog = await _dbContext.CallLogs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == report.CallLogId.Value);
            }

            bool backupExists = false;
            string? playbackUrl = callLog?.RecordingUrl;

            if (!string.IsNullOrEmpty(callLog?.RecordingFilePath) && System.IO.File.Exists(callLog.RecordingFilePath))
            {
                backupExists = true;
            }
            else if (!string.IsNullOrEmpty(playbackUrl))
            {
                string localPath = Path.Combine(_env.WebRootPath, playbackUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(localPath))
                {
                    backupExists = true;
                }
            }

            string? actionedByName = null;
            if (!string.IsNullOrEmpty(report.ActionedBy))
            {
                var adminUser = await _userManager.FindByIdAsync(report.ActionedBy);
                actionedByName = adminUser?.UserName ?? adminUser?.Email ?? "Admin";
            }

            var dto = new CallReportDto
            {
                Id = report.Id,
                ReporterUserId = report.ReporterUserId,
                ReporterName = reporter?.Name ?? "Member",
                ReporterRegisterNumber = reporter?.RegisterNumber,
                ReporterPhotoUrl = reporter?.ImagePath,
                ReporterPhone = reporter?.Phone,

                ReportedUserId = report.ReportedUserId,
                ReportedUserName = reported?.Name ?? "Member",
                ReportedUserRegisterNumber = reported?.RegisterNumber,
                ReportedUserPhotoUrl = reported?.ImagePath,
                ReportedUserPhone = reported?.Phone,

                CallLogId = report.CallLogId,
                CallType = report.CallType ?? callLog?.CallType ?? UserCallType.Voice,
                CallDurationSeconds = report.CallDurationSeconds ?? callLog?.DurationSeconds ?? 0,
                Reason = report.Reason,
                Comments = report.Comments,
                HasRecording = backupExists,
                RecordingUrl = playbackUrl,

                Status = report.Status,
                ModeratorNotes = report.ModeratorNotes,
                ActionedBy = report.ActionedBy,
                ActionedByUsername = actionedByName,
                ActionedOn = report.ActionedOn,
                CreatedOn = report.CreatedOn
            };

            var viewModel = new CallReportDetailViewModel
            {
                Report = dto,
                Reporter = reporter,
                ReportedUser = reported,
                CallLog = callLog,
                BackupFileExists = backupExists,
                PlaybackUrl = playbackUrl
            };

            return View(viewModel);
        }

        [HttpPost("/admin/call-reports/post-action")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostAction(long id, UserReportStatus status, string? moderatorNotes)
        {
            try
            {
                var report = await _dbContext.CallReports.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (report == null)
                {
                    TempData["Error"] = "Call report not found.";
                    return RedirectToAction(nameof(Index));
                }

                var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var oldStatus = report.Status;

                report.Status = status;
                report.ModeratorNotes = moderatorNotes;

                if (status != UserReportStatus.Pending)
                {
                    report.ActionedBy = currentAdminId;
                    report.ActionedOn = DateTime.UtcNow;
                }
                else
                {
                    report.ActionedBy = null;
                    report.ActionedOn = null;
                }

                await _dbContext.SaveChangesAsync();

                // If status changed from Pending to Actioned or Dismissed, perform automation like normal reporting
                if (status != oldStatus && status != UserReportStatus.Pending)
                {
                    var reporter = await _dbContext.Registration.FirstOrDefaultAsync(u => u.Id == report.ReporterUserId);
                    var reportedUser = await _dbContext.Registration.FirstOrDefaultAsync(u => u.Id == report.ReportedUserId);

                    string actionDetails = "";

                    if (status == UserReportStatus.Actioned)
                    {
                        if (reportedUser != null)
                        {
                            // Automatically disable and restrict reported profile
                            reportedUser.IsVisible = false;
                            reportedUser.DisabledReason = DisabledReason.ReportedViolation;
                            _dbContext.Registration.Update(reportedUser);
                            await _dbContext.SaveChangesAsync();
                            _logger.LogInformation("Profile {UserId} restricted after call report {ReportId} was Actioned.", reportedUser.Id, report.Id);

                            // Send notification to reported user
                            var reportedUserNotification = new Notification
                            {
                                Notify_Id = reportedUser.Id,
                                UserId = reporter?.Id ?? 0,
                                Notify_Message = $"Your profile has been restricted following a report regarding a call violation ({report.Reason}).",
                                CreatedOn = DateTime.UtcNow,
                                ModifiedOn = DateTime.UtcNow
                            };
                            await _dbContext.Notification.AddAsync(reportedUserNotification);
                        }

                        actionDetails = "Based on our investigation into the reported call, we have taken action and restricted the reported profile.";

                        // Refund credit to reporter if they had unlocked contact details
                        if (reporter != null && reportedUser != null)
                        {
                            bool isUnlocked = await _userService.AreUserContactDetailsUnlocked(report.ReporterUserId, report.ReportedUserId);
                            if (isUnlocked)
                            {
                                bool refunded = await _userService.RefundContactViewCredit(report.ReporterUserId);
                                if (refunded)
                                {
                                    actionDetails += " As a token of appreciation for keeping our community safe, 1 contact view credit has been refunded to your account.";
                                }
                            }
                        }

                        // Send notification to reporter
                        if (reporter != null)
                        {
                            var reporterNotification = new Notification
                            {
                                Notify_Id = report.ReporterUserId,
                                UserId = reportedUser?.Id ?? 0,
                                Notify_Message = $"Your call report regarding {reportedUser?.Name ?? "the member"} has been Actioned. Necessary actions have been taken against the profile.",
                                CreatedOn = DateTime.UtcNow,
                                ModifiedOn = DateTime.UtcNow
                            };
                            await _dbContext.Notification.AddAsync(reporterNotification);
                        }

                        await _dbContext.SaveChangesAsync();
                    }
                    else if (status == UserReportStatus.Dismissed)
                    {
                        actionDetails = "After careful review of the call report, we did not find sufficient evidence of a policy violation at this time. No further action has been taken.";

                        // Send notification to reporter
                        if (reporter != null)
                        {
                            var reporterNotification = new Notification
                            {
                                Notify_Id = report.ReporterUserId,
                                UserId = reportedUser?.Id ?? 0,
                                Notify_Message = $"Your call report regarding {reportedUser?.Name ?? "the member"} has been Dismissed after review.",
                                CreatedOn = DateTime.UtcNow,
                                ModifiedOn = DateTime.UtcNow
                            };
                            await _dbContext.Notification.AddAsync(reporterNotification);
                            await _dbContext.SaveChangesAsync();
                        }
                    }

                    // Send email notification to reporter (same as normal UserReport)
                    if (reporter != null && !string.IsNullOrEmpty(reporter.Email))
                    {
                        try
                        {
                            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates/Mail/UserReport/Resolution.html");
                            if (System.IO.File.Exists(templatePath))
                            {
                                string emailContent = await System.IO.File.ReadAllTextAsync(templatePath);
                                emailContent = emailContent.Replace("[ReporterName]", reporter.Name)
                                                         .Replace("[ReportedUserName]", reportedUser?.Name ?? "Reported Member")
                                                         .Replace("[Status]", report.Status.ToString())
                                                         .Replace("[StatusClass]", status == UserReportStatus.Actioned ? "status-actioned" : "status-dismissed")
                                                         .Replace("[Reason]", report.Reason)
                                                         .Replace("[ModeratorNotes]", report.ModeratorNotes ?? "No additional notes provided.")
                                                         .Replace("[ActionDetails]", actionDetails);

                                var subject = $"Call Report Update: {report.Status} - M4Nikah";
                                var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                                _emailNotificationHelper.SendEmail(reporter.Email, htmlView, subject);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending call report resolution email to reporter {Email}", reporter.Email);
                        }
                    }
                }

                TempData["Success"] = $"Call report status updated to {status} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating call report action for report {ReportId}", id);
                TempData["Error"] = "An error occurred while updating the report status: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
