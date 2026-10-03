using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistence;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace URMARRY.Services
{
    /// <summary>
    /// Background service that scans the local call backup folder (Uploads/backups/) periodically.
    /// Deletes any audio/video backup files older than 3 days (72 hours) and clears the recording metadata in CallLogs.
    /// </summary>
    public class BackupCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackupCleanupService> _logger;

        public BackupCleanupService(
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<BackupCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackupCleanupService has started. Monitoring 3-day call backup retention.");

            int retentionDays = _configuration.GetValue<int>("CallBackupSettings:RetentionDays", 3);
            int intervalHours = _configuration.GetValue<int>("CallBackupSettings:CleanupIntervalHours", 1);
            if (intervalHours < 1) intervalHours = 1;
            var checkInterval = TimeSpan.FromHours(intervalHours);

            // Run initial check after a short 1-minute startup delay
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                await PerformCleanupSweepAsync(retentionDays, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial BackupCleanupService sweep.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(checkInterval, stoppingToken);
                    await PerformCleanupSweepAsync(retentionDays, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during call backup cleanup sweep.");
                }
            }

            _logger.LogInformation("BackupCleanupService is stopping.");
        }

        public async Task PerformCleanupSweepAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            string backupFolderConfig = _configuration["CallBackupSettings:BackupFolder"] ?? "Uploads/backups";
            string backupFolderPath = Path.Combine(_env.WebRootPath, backupFolderConfig.Replace('/', Path.DirectorySeparatorChar));

            if (!Directory.Exists(backupFolderPath))
            {
                return;
            }

            DateTime thresholdUtc = DateTime.UtcNow.AddDays(-retentionDays);
            int deletedFilesCount = 0;
            long freedBytes = 0;

            try
            {
                var directoryInfo = new DirectoryInfo(backupFolderPath);
                var files = directoryInfo.GetFiles();

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    // Check file age (based on creation/last write time in UTC)
                    var fileAgeUtc = file.CreationTimeUtc < file.LastWriteTimeUtc ? file.CreationTimeUtc : file.LastWriteTimeUtc;

                    if (fileAgeUtc <= thresholdUtc)
                    {
                        try
                        {
                            long fileLength = file.Length;
                            string fileName = file.Name;

                            // Delete the physical file
                            file.Delete();
                            deletedFilesCount++;
                            freedBytes += fileLength;
                            _logger.LogInformation("Deleted expired call backup file: {FileName}, size: {Size} bytes, age: {AgeDays:F1} days",
                                fileName, fileLength, (DateTime.UtcNow - fileAgeUtc).TotalDays);

                            // Clean matching database records
                            var matchingLogs = await db.CallLogs
                                .Where(c => c.RecordingFilePath != null && c.RecordingFilePath.Contains(fileName))
                                .ToListAsync(cancellationToken);

                            foreach (var log in matchingLogs)
                            {
                                log.RecordingFilePath = null;
                                log.RecordingUrl = null;
                                log.RecordingFileSize = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete expired backup file: {FilePath}", file.FullName);
                        }
                    }
                }

                // Also check for any DB records marked expired whose file might not have matched
                var expiredLogs = await db.CallLogs
                    .Where(c => !c.IsDeleted && c.RecordingExpiresAt.HasValue && c.RecordingExpiresAt.Value <= DateTime.UtcNow && (c.RecordingFilePath != null || c.RecordingUrl != null))
                    .ToListAsync(cancellationToken);

                foreach (var log in expiredLogs)
                {
                    if (!string.IsNullOrEmpty(log.RecordingFilePath) && File.Exists(log.RecordingFilePath))
                    {
                        try
                        {
                            File.Delete(log.RecordingFilePath);
                            deletedFilesCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not delete expired file for call {CallLogId}", log.Id);
                        }
                    }

                    log.RecordingFilePath = null;
                    log.RecordingUrl = null;
                    log.RecordingFileSize = null;
                }

                await db.SaveChangesAsync(cancellationToken);

                if (deletedFilesCount > 0)
                {
                    _logger.LogInformation("Call backup cleanup completed: {Count} files deleted, {FreedMB:F2} MB freed.",
                        deletedFilesCount, freedBytes / (1024.0 * 1024.0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sweeping call backups in folder: {BackupFolderPath}", backupFolderPath);
            }
        }
    }
}
