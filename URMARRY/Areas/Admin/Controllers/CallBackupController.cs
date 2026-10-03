using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Persistence;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using URMARRY.Areas.Admin.Models;
using URMARRY.Services;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class CallBackupController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallBackupController> _logger;

        public CallBackupController(
            AppDbContext dbContext,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<CallBackupController> logger)
        {
            _dbContext = dbContext;
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("/admin/call-backups")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Query all call logs so admin has full visibility of calls and their backup status
                var callLogs = await _dbContext.CallLogs
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .OrderByDescending(c => c.StartedAt)
                    .ToListAsync();

                var userIds = callLogs.Select(c => c.CallerId)
                    .Concat(callLogs.Select(c => c.ReceiverId))
                    .Distinct()
                    .ToList();

                var users = await _dbContext.Registration
                    .AsNoTracking()
                    .Where(r => userIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id);

                var backupItems = callLogs.Select(log =>
                {
                    users.TryGetValue(log.CallerId, out var caller);
                    users.TryGetValue(log.ReceiverId, out var receiver);

                    // Check physical file existence and size
                    bool exists = false;
                    long fileSize = log.RecordingFileSize ?? 0;
                    string? fileName = null;

                    if (!string.IsNullOrEmpty(log.RecordingFilePath))
                    {
                        if (System.IO.File.Exists(log.RecordingFilePath))
                        {
                            exists = true;
                            var fi = new FileInfo(log.RecordingFilePath);
                            fileSize = fi.Length;
                            fileName = fi.Name;
                        }
                        else
                        {
                            fileName = Path.GetFileName(log.RecordingFilePath);
                        }
                    }
                    else if (!string.IsNullOrEmpty(log.RecordingUrl))
                    {
                        fileName = Path.GetFileName(log.RecordingUrl);
                        string fullPath = Path.Combine(_env.WebRootPath, log.RecordingUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(fullPath))
                        {
                            exists = true;
                            var fi = new FileInfo(fullPath);
                            fileSize = fi.Length;
                        }
                    }

                    return new CallBackupItemViewModel
                    {
                        CallLogId = log.Id,
                        FileName = fileName ?? $"call_{log.Id}",
                        CallType = log.CallType,
                        Status = log.Status,
                        RoomId = log.RoomId,
                        CallerId = log.CallerId,
                        CallerName = caller?.Name ?? "Deleted / Unknown",
                        CallerRegisterNumber = caller?.RegisterNumber,
                        CallerPhone = caller?.Phone,
                        CallerPhotoUrl = caller?.ImagePath,
                        ReceiverId = log.ReceiverId,
                        ReceiverName = receiver?.Name ?? "Deleted / Unknown",
                        ReceiverRegisterNumber = receiver?.RegisterNumber,
                        ReceiverPhone = receiver?.Phone,
                        ReceiverPhotoUrl = receiver?.ImagePath,
                        FileSizeBytes = fileSize,
                        RecordingUrl = log.RecordingUrl,
                        RecordingFilePath = log.RecordingFilePath,
                        FileExistsOnDisk = exists,
                        StartedAt = log.StartedAt,
                        ConnectedAt = log.ConnectedAt,
                        EndedAt = log.EndedAt,
                        DurationSeconds = log.DurationSeconds,
                        ExpiresAt = log.RecordingExpiresAt,
                        EndReason = log.EndReason
                    };
                }).ToList();

                var model = new CallBackupListViewModel
                {
                    Backups = backupItems,
                    TotalCallsCount = backupItems.Count,
                    BackupsStoredCount = backupItems.Count(b => b.FileExistsOnDisk),
                    TotalSizeBytes = backupItems.Where(b => b.FileExistsOnDisk).Sum(b => b.FileSizeBytes),
                    AudioBackupsCount = backupItems.Count(b => b.FileExistsOnDisk && b.CallType == UserCallType.Voice),
                    VideoBackupsCount = backupItems.Count(b => b.FileExistsOnDisk && b.CallType == UserCallType.Video)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading call backups for admin panel.");
                var fallbackModel = new CallBackupListViewModel();
                return View(fallbackModel);
            }
        }

        [HttpGet("/admin/call-backups/download/{callLogId:long}")]
        public async Task<IActionResult> Download(long callLogId)
        {
            var callLog = await _dbContext.CallLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == callLogId && !c.IsDeleted);

            if (callLog == null)
            {
                return NotFound("Call record not found.");
            }

            string? filePath = callLog.RecordingFilePath;
            if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(callLog.RecordingUrl))
            {
                filePath = Path.Combine(_env.WebRootPath, callLog.RecordingUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }

            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return NotFound("The requested recording backup file was not found on disk or has already been expired and deleted.");
            }

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string contentType = ext switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".ogg" => "audio/ogg",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                _ => "application/octet-stream"
            };

            string downloadFileName = $"Call_{callLog.CallType}_{callLog.Id}_{callLog.StartedAt:yyyyMMdd_HHmmss}{ext}";
            return PhysicalFile(filePath, contentType, downloadFileName);
        }

        [HttpPost("/admin/call-backups/delete/{callLogId:long}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long callLogId)
        {
            try
            {
                var callLog = await _dbContext.CallLogs
                    .FirstOrDefaultAsync(c => c.Id == callLogId && !c.IsDeleted);

                if (callLog == null)
                {
                    TempData["Error"] = "Call log not found.";
                    return RedirectToAction("Index");
                }

                string? filePath = callLog.RecordingFilePath;
                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(callLog.RecordingUrl))
                {
                    filePath = Path.Combine(_env.WebRootPath, callLog.RecordingUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                }

                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Admin deleted call backup file: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting physical backup file {FilePath}", filePath);
                    }
                }

                callLog.RecordingFilePath = null;
                callLog.RecordingUrl = null;
                callLog.RecordingFileSize = null;

                await _dbContext.SaveChangesAsync();

                TempData["Success"] = "Call backup file deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting call backup for call {CallLogId}", callLogId);
                TempData["Error"] = "An error occurred while deleting the call backup.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost("/admin/call-backups/sweep")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SweepNow([FromServices] BackupCleanupService cleanupService)
        {
            try
            {
                int retentionDays = _configuration.GetValue<int>("CallBackupSettings:RetentionDays", 3);
                await cleanupService.PerformCleanupSweepAsync(retentionDays);
                TempData["Success"] = "3-day retention cleanup sweep completed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running manual backup cleanup sweep.");
                TempData["Error"] = "Error running cleanup sweep: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
