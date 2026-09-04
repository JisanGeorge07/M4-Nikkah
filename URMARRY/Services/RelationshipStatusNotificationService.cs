using Domain;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace URMARRY.Services;

/// <summary>
/// Background service that periodically creates notification records
/// for users who have pending relationship status update prompts.
/// Runs every 6 hours by default.
/// </summary>
public class RelationshipStatusNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RelationshipStatusNotificationService> _logger;

    /// <summary>
    /// The marker prefix used in Notify_Message to identify status-update-prompt notifications.
    /// </summary>
    public const string StatusUpdatePromptPrefix = "[STATUS_UPDATE_PROMPT]";

    public RelationshipStatusNotificationService(
        IServiceScopeFactory scopeFactory,
        ILogger<RelationshipStatusNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RelationshipStatusNotificationService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingStatusPrompts(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing relationship status prompt notifications.");
            }

            // Run every 6 hours
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }

        _logger.LogInformation("RelationshipStatusNotificationService is stopping.");
    }

    private async Task ProcessPendingStatusPrompts(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await dbContext.TestimonialSettings.FirstOrDefaultAsync(stoppingToken)
                       ?? new TestimonialSetting { FirstPromptDays = 7, FollowUpPromptDays = 30 };
        var firstPromptDays = settings.FirstPromptDays;
        var followUpPromptDays = settings.FollowUpPromptDays;

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-firstPromptDays);
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-followUpPromptDays);

        // 1. Get all accepted interests older than dynamic days threshold
        var acceptedInterests = await dbContext.Userfavoriteprofile
            .Where(x => !x.IsDeleted && x.IsActive &&
                        x.Status == InterestStatus.Accepted &&
                        x.ModifiedOn <= sevenDaysAgo)
            .ToListAsync(stoppingToken);

        if (!acceptedInterests.Any())
        {
            _logger.LogInformation("No accepted interests older than {Days} days found. Skipping.", firstPromptDays);
            return;
        }

        // 2. Get all existing match status updates
        var allMatchStatuses = await dbContext.MatchStatusUpdates
            .Where(x => !x.IsDeleted && x.IsActive)
            .ToListAsync(stoppingToken);

        // 3. Get all existing status-prompt notifications to avoid duplicates
        var existingPromptNotifications = await dbContext.Notification
            .Where(x => !x.IsDeleted && x.Notify_Message.StartsWith(StatusUpdatePromptPrefix))
            .ToListAsync(stoppingToken);

        // 4. Get all registrations for name lookups
        var allUserIds = acceptedInterests
            .SelectMany(x => new[] { x.UserId, x.LikedId })
            .Distinct()
            .ToList();
        var registrations = await dbContext.Registration
            .Where(x => allUserIds.Contains(x.Id) && !x.IsDeleted && x.IsActive)
            .ToDictionaryAsync(x => x.Id, stoppingToken);

        int notificationsCreated = 0;
        var createdInThisRun = new HashSet<(long recipientId, long partnerId)>();

        foreach (var interest in acceptedInterests)
        {
            // Process for both users in the accepted interest
            var userIds = new[] { interest.UserId, interest.LikedId };

            foreach (var userId in userIds)
            {
                var partnerId = userId == interest.UserId ? interest.LikedId : interest.UserId;

                // Check status updates from both sides for this interest
                var existingUpdate = allMatchStatuses.FirstOrDefault(u =>
                    u.UserFavouriteProfileId == interest.Id && u.UserId == userId);
                var partnerUpdate = allMatchStatuses.FirstOrDefault(u =>
                    u.UserFavouriteProfileId == interest.Id && u.UserId == partnerId);

                bool shouldCreateNotification = false;

                if (existingUpdate == null && partnerUpdate == null)
                {
                    // No status update at all — should prompt
                    shouldCreateNotification = true;
                }
                else
                {
                    // If either side has finalized, skip prompting completely
                    var isFinalized = (existingUpdate != null && (existingUpdate.Status == MatchRelationshipStatus.Married || existingUpdate.Status == MatchRelationshipStatus.Rejected || existingUpdate.Status == MatchRelationshipStatus.PreferNotToSay))
                                   || (partnerUpdate != null && (partnerUpdate.Status == MatchRelationshipStatus.Married || partnerUpdate.Status == MatchRelationshipStatus.Rejected || partnerUpdate.Status == MatchRelationshipStatus.PreferNotToSay));

                    if (!isFinalized)
                    {
                        // Non-final status (Engaged, StillCommunicating)
                        // Use our own status update to determine re-prompt; fallback to partner's if we don't have one
                        var statusToCheck = existingUpdate ?? partnerUpdate;
                        if (statusToCheck.StatusUpdatedOn.HasValue && statusToCheck.StatusUpdatedOn.Value <= thirtyDaysAgo)
                        {
                            shouldCreateNotification = true;
                        }
                        else if (!statusToCheck.StatusUpdatedOn.HasValue)
                        {
                            shouldCreateNotification = true;
                        }
                    }
                }

                if (!shouldCreateNotification)
                    continue;

                // Check if a notification already exists for this user+partner combo
                var alreadyExists = existingPromptNotifications.Any(n =>
                    n.Notify_Id == userId && n.UserId == partnerId &&
                    n.Notify_Message.StartsWith(StatusUpdatePromptPrefix))
                    || createdInThisRun.Contains((userId, partnerId));

                if (alreadyExists)
                    continue;

                // Get partner name for the notification message
                registrations.TryGetValue(partnerId, out var partnerReg);
                var partnerName = partnerReg?.Name ?? "your match";

                // Create the notification
                // Convention: UserId = partner (whose avatar shows), Notify_Id = recipient user
                var notification = new Notification
                {
                    UserId = partnerId,
                    Notify_Id = userId,
                    Notify_Message = $"{StatusUpdatePromptPrefix} Update your relationship status with {partnerName}",
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow,
                    Is_Read = 0
                };

                dbContext.Notification.Add(notification);
                createdInThisRun.Add((userId, partnerId));
                notificationsCreated++;
            }
        }

        if (notificationsCreated > 0)
        {
            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Created {Count} relationship status prompt notifications.", notificationsCreated);
        }
        else
        {
            _logger.LogInformation("No new relationship status prompt notifications needed.");
        }
    }
}
