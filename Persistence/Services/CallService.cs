using Application.Interfaces.Persistence;
using Application.Models.Call;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Services
{
    public class CallService : ICallService
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallService> _logger;

        public CallService(
            AppDbContext dbContext,
            IConfiguration configuration,
            ILogger<CallService> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CallPermissionResult> CheckCallPermissionAsync(long callerId, long receiverId, UserCallType callType)
        {
            if (callerId <= 0 || receiverId <= 0 || callerId == receiverId)
            {
                return new CallPermissionResult
                {
                    Allowed = false,
                    Reason = "Invalid caller or recipient identification.",
                    RequiresUpgrade = false
                };
            }

            // 1. Verify recipient exists and is active
            var receiver = await _dbContext.Registration
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == receiverId && !r.IsDeleted && r.IsActive);

            if (receiver == null)
            {
                return new CallPermissionResult
                {
                    Allowed = false,
                    Reason = "The requested profile is no longer available.",
                    RequiresUpgrade = false
                };
            }

            // 2. Check if blocked
            bool isBlocked = await _dbContext.UserNotLikeProfiles
                .AsNoTracking()
                .AnyAsync(x => !x.IsDeleted && (
                    (x.UserId == callerId && x.NotLikedId == receiverId) ||
                    (x.UserId == receiverId && x.NotLikedId == callerId)
                ));

            if (!isBlocked)
            {
                long u1 = Math.Min(callerId, receiverId);
                long u2 = Math.Max(callerId, receiverId);
                isBlocked = await _dbContext.Conversations
                    .AsNoTracking()
                    .AnyAsync(c => !c.IsDeleted && c.Type == ConversationType.UserToUser && c.User1Id == u1 && c.User2Id == u2 && c.IsBlocked);
            }

            if (isBlocked)
            {
                return new CallPermissionResult
                {
                    Allowed = false,
                    Reason = "Calling is unavailable for this contact.",
                    RequiresUpgrade = false
                };
            }

            // 3. Check caller active plan / premium status
            var caller = await _dbContext.Registration
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == callerId && !r.IsDeleted && r.IsActive);

            if (caller == null)
            {
                return new CallPermissionResult
                {
                    Allowed = false,
                    Reason = "Caller profile not found or inactive.",
                    RequiresUpgrade = false
                };
            }

            // 4. MANDATORY: Check accepted mutual interest FIRST
            bool hasAcceptedInterest = await _dbContext.Userfavoriteprofile
                .AsNoTracking()
                .AnyAsync(f => !f.IsDeleted && f.Status == InterestStatus.Accepted && (
                    (f.UserId == callerId && f.LikedId == receiverId) ||
                    (f.UserId == receiverId && f.LikedId == callerId)
                ));

            if (!hasAcceptedInterest)
            {
                return new CallPermissionResult
                {
                    Allowed = false,
                    Reason = "You need mutual interest acceptance before making calls. Send an interest request first.",
                    RequiresUpgrade = false,
                    InterestRequired = true
                };
            }

            bool isPremium = caller.IsPremiumMember;
            bool hasActivePlan = await _dbContext.PlanPurchases
                .AsNoTracking()
                .AnyAsync(p => p.UserId == callerId && !p.IsDeleted && p.ExpiresAt > DateTime.UtcNow);

            // 5. Check plan & credit availability
            if (!isPremium && !hasActivePlan)
            {
                return new CallPermissionResult
                {
                    Allowed = false,
                    Reason = "An active premium membership is required to make calls.",
                    RequiresUpgrade = true,
                    InterestRequired = false
                };
            }

            // 6. Check type-specific credits
            var creditStatus = await GetCallCreditStatusAsync(callerId);

            if (callType == UserCallType.Voice)
            {
                // Check if already called this profile (no new credit needed)
                bool alreadyCalled = await _dbContext.AudioCallContacts
                    .AnyAsync(a => a.UserId == callerId && a.ContactUserId == receiverId && !a.IsDeleted);

                if (!alreadyCalled && creditStatus.AudioCallContactsRemaining <= 0)
                {
                    return new CallPermissionResult
                    {
                        Allowed = false,
                        Reason = "You've reached the maximum audio call contacts for your current plan.",
                        RequiresUpgrade = true,
                        AudioLimitReached = true,
                        CreditStatus = creditStatus
                    };
                }
            }
            else if (callType == UserCallType.Video)
            {
                if (creditStatus.VideoCallMinutesRemaining <= 0)
                {
                    return new CallPermissionResult
                    {
                        Allowed = false,
                        Reason = "You've used all your video call minutes for the current plan period.",
                        RequiresUpgrade = true,
                        VideoLimitReached = true,
                        CreditStatus = creditStatus
                    };
                }
            }

            return new CallPermissionResult
            {
                Allowed = true,
                Reason = null,
                RequiresUpgrade = false,
                CreditStatus = creditStatus
            };
        }

        public async Task<CallLogDto> InitiateCallAsync(long callerId, InitiateCallRequest request)
        {
            var perm = await CheckCallPermissionAsync(callerId, request.ReceiverId, request.CallType);
            if (!perm.Allowed)
            {
                throw new InvalidOperationException(perm.Reason ?? "Permission denied to initiate call.");
            }

            // Spend audio call contact credit (only for voice calls, only for new unique contacts)
            if (request.CallType == UserCallType.Voice)
            {
                await SpendAudioCallContactAsync(callerId, request.ReceiverId);
            }

            string roomId = string.IsNullOrWhiteSpace(request.RoomId) ? Guid.NewGuid().ToString("N") : request.RoomId.Trim();

            var callLog = new CallLog
            {
                CallerId = callerId,
                ReceiverId = request.ReceiverId,
                CallType = request.CallType,
                Status = CallLogStatus.Ringing,
                RoomId = roomId,
                StartedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = callerId.ToString(),
                IsActive = true
            };

            _dbContext.CallLogs.Add(callLog);
            await _dbContext.SaveChangesAsync(callerId.ToString());

            var users = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => r.Id == callerId || r.Id == request.ReceiverId)
                .ToDictionaryAsync(r => r.Id);

            users.TryGetValue(callerId, out var caller);
            users.TryGetValue(request.ReceiverId, out var receiver);

            return MapToCallLogDto(callLog, callerId, caller, receiver);
        }

        public async Task<CallLogDto> AnswerCallAsync(long callLogId, long receiverId)
        {
            var callLog = await _dbContext.CallLogs
                .FirstOrDefaultAsync(c => c.Id == callLogId && !c.IsDeleted);

            if (callLog == null)
            {
                throw new KeyNotFoundException("Call session not found.");
            }

            if (callLog.ReceiverId != receiverId)
            {
                throw new UnauthorizedAccessException("Unauthorized to answer this call.");
            }

            callLog.Status = CallLogStatus.Connected;
            callLog.ConnectedAt = DateTime.UtcNow;
            callLog.RecordingExpiresAt = DateTime.UtcNow.AddDays(14); // 14-day security retention window

            await _dbContext.SaveChangesAsync(receiverId.ToString());

            var users = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => r.Id == callLog.CallerId || r.Id == callLog.ReceiverId)
                .ToDictionaryAsync(r => r.Id);

            users.TryGetValue(callLog.CallerId, out var caller);
            users.TryGetValue(callLog.ReceiverId, out var receiver);

            return MapToCallLogDto(callLog, receiverId, caller, receiver);
        }

        public async Task<CallLogDto> RejectCallAsync(long callLogId, long receiverId, string? reason)
        {
            var callLog = await _dbContext.CallLogs
                .FirstOrDefaultAsync(c => c.Id == callLogId && !c.IsDeleted);

            if (callLog == null)
            {
                throw new KeyNotFoundException("Call session not found.");
            }

            if (callLog.ReceiverId != receiverId && callLog.CallerId != receiverId)
            {
                throw new UnauthorizedAccessException("Unauthorized to reject this call.");
            }

            callLog.Status = CallLogStatus.Rejected;
            callLog.EndedAt = DateTime.UtcNow;
            callLog.EndReason = string.IsNullOrWhiteSpace(reason) ? "ReceiverRejected" : reason.Trim();

            await _dbContext.SaveChangesAsync(receiverId.ToString());

            var users = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => r.Id == callLog.CallerId || r.Id == callLog.ReceiverId)
                .ToDictionaryAsync(r => r.Id);

            users.TryGetValue(callLog.CallerId, out var caller);
            users.TryGetValue(callLog.ReceiverId, out var receiver);

            return MapToCallLogDto(callLog, receiverId, caller, receiver);
        }

        public async Task<CallLogDto> EndCallAsync(long callLogId, long userId, string? reason)
        {
            var callLog = await _dbContext.CallLogs
                .FirstOrDefaultAsync(c => c.Id == callLogId && !c.IsDeleted);

            if (callLog == null)
            {
                throw new KeyNotFoundException("Call session not found.");
            }

            if (callLog.CallerId != userId && callLog.ReceiverId != userId)
            {
                throw new UnauthorizedAccessException("Unauthorized to end this call.");
            }

            callLog.Status = CallLogStatus.Ended;
            callLog.EndedAt = DateTime.UtcNow;
            callLog.EndReason = string.IsNullOrWhiteSpace(reason) ? "NormalHangup" : reason.Trim();

            if (callLog.ConnectedAt.HasValue)
            {
                callLog.DurationSeconds = (int)Math.Max(0, (callLog.EndedAt.Value - callLog.ConnectedAt.Value).TotalSeconds);
            }
            else
            {
                callLog.DurationSeconds = 0;
            }

            // Record video call duration against credits
            if (callLog.CallType == UserCallType.Video && callLog.DurationSeconds > 0)
            {
                await RecordVideoCallDurationAsync(callLog.CallerId, callLog.DurationSeconds);
            }

            await _dbContext.SaveChangesAsync(userId.ToString());

            var users = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => r.Id == callLog.CallerId || r.Id == callLog.ReceiverId)
                .ToDictionaryAsync(r => r.Id);

            users.TryGetValue(callLog.CallerId, out var caller);
            users.TryGetValue(callLog.ReceiverId, out var receiver);

            return MapToCallLogDto(callLog, userId, caller, receiver);
        }

        public async Task<PaginatedCallLogsDto> GetCallHistoryAsync(long userId, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 20;

            var query = _dbContext.CallLogs
                .AsNoTracking()
                .Where(c => !c.IsDeleted && (
                    (c.CallerId == userId && !c.IsDeletedForCaller) ||
                    (c.ReceiverId == userId && !c.IsDeletedForReceiver)
                ));

            int totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(c => c.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = logs.Select(c => c.CallerId).Concat(logs.Select(c => c.ReceiverId)).Distinct().ToList();
            var users = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => userIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

            var dtoList = logs.Select(c =>
            {
                users.TryGetValue(c.CallerId, out var caller);
                users.TryGetValue(c.ReceiverId, out var receiver);
                return MapToCallLogDto(c, userId, caller, receiver);
            }).ToList();

            return new PaginatedCallLogsDto
            {
                Calls = dtoList,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = (page * pageSize) < totalCount
            };
        }

        public async Task<CallLogDto?> GetCallLogByIdAsync(long callLogId, long userId)
        {
            var callLog = await _dbContext.CallLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == callLogId && !c.IsDeleted && (c.CallerId == userId || c.ReceiverId == userId));

            if (callLog == null) return null;

            var users = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => r.Id == callLog.CallerId || r.Id == callLog.ReceiverId)
                .ToDictionaryAsync(r => r.Id);

            users.TryGetValue(callLog.CallerId, out var caller);
            users.TryGetValue(callLog.ReceiverId, out var receiver);

            return MapToCallLogDto(callLog, userId, caller, receiver);
        }

        public IceServerConfigResponse GetIceServerConfiguration(long userId)
        {
            var stunUrl = _configuration["TurnServerSettings:StunUrl"] ?? "stun:stun.l.google.com:19302";
            var turnUdp = _configuration["TurnServerSettings:TurnUdpUrl"] ?? "turn:YOUR_AWS_ELASTIC_IP:3478?transport=udp";
            var turnTcp = _configuration["TurnServerSettings:TurnTcpUrl"] ?? "turn:YOUR_AWS_ELASTIC_IP:3478?transport=tcp";
            var secret = _configuration["TurnServerSettings:Secret"] ?? "YOUR_SUPER_SECRET_KEY";
            int ttlHours = _configuration.GetValue<int>("TurnServerSettings:TtlHours", 2);

            long expiryTimestamp = DateTimeOffset.UtcNow.AddHours(ttlHours).ToUnixTimeSeconds();
            string username = $"{expiryTimestamp}:{userId}";
            string credential = GenerateHmacSha1(secret, username);

            var response = new IceServerConfigResponse
            {
                IceServers = new List<IceServerDto>
                {
                    new IceServerDto
                    {
                        Urls = stunUrl
                    },
                    new IceServerDto
                    {
                        Urls = new List<string> { turnUdp, turnTcp },
                        Username = username,
                        Credential = credential
                    }
                }
            };

            return response;
        }

        public async Task<bool> SaveCallRecordingMetadataAsync(long callLogId, string s3Key, string recordingUrl, long fileSize, int durationSeconds)
        {
            var callLog = await _dbContext.CallLogs
                .FirstOrDefaultAsync(c => c.Id == callLogId && !c.IsDeleted);

            if (callLog == null) return false;

            callLog.RecordingS3Key = s3Key;
            callLog.RecordingUrl = recordingUrl;
            callLog.RecordingFileSize = fileSize;
            callLog.RecordingExpiresAt = DateTime.UtcNow.AddDays(14); // 14 days retention
            if (durationSeconds > 0)
            {
                callLog.DurationSeconds = durationSeconds;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<CallCreditStatus> GetCallCreditStatusAsync(long userId)
        {
            var activePlans = await _dbContext.PlanPurchases
                .AsNoTracking()
                .Where(p => p.UserId == userId && !p.IsDeleted && p.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            if (!activePlans.Any())
            {
                return new CallCreditStatus { HasActivePlan = false };
            }

            int totalAudioPurchased = activePlans.Sum(p => (int)p.AudioCallContactsPurchased);
            int totalAudioUsed = activePlans.Sum(p => (int)p.AudioCallContactsUsed);
            int totalVideoPurchased = activePlans.Sum(p => (int)p.VideoCallMinutesPurchased);
            int totalVideoUsed = activePlans.Sum(p => (int)p.VideoCallMinutesUsed);
            DateTime maxExpiry = activePlans.Max(p => p.ExpiresAt);

            return new CallCreditStatus
            {
                AudioCallContactsPurchased = (short)totalAudioPurchased,
                AudioCallContactsUsed = (short)totalAudioUsed,
                VideoCallMinutesPurchased = (short)totalVideoPurchased,
                VideoCallMinutesUsed = (short)totalVideoUsed,
                HasActivePlan = true,
                PlanExpiresAt = maxExpiry
            };
        }

        public async Task<bool> SpendAudioCallContactAsync(long callerId, long receiverId)
        {
            var activePlans = await _dbContext.PlanPurchases
                .Where(p => p.UserId == callerId && !p.IsDeleted && p.ExpiresAt > DateTime.UtcNow)
                .OrderBy(p => p.ExpiresAt)
                .ToListAsync();

            if (!activePlans.Any()) return false;

            var planIds = activePlans.Select(p => p.Id).ToList();

            // Check if this contact was already called in this plan cycle
            bool alreadyCalled = await _dbContext.AudioCallContacts
                .AnyAsync(a => a.UserId == callerId && a.ContactUserId == receiverId 
                            && planIds.Contains(a.PlanPurchaseId) && !a.IsDeleted);

            if (alreadyCalled) return true; // No credit consumed — already contacted in current cycle

            int totalPurchased = activePlans.Sum(p => (int)p.AudioCallContactsPurchased);
            int totalUsed = activePlans.Sum(p => (int)p.AudioCallContactsUsed);

            if (totalUsed >= totalPurchased)
                return false; // Limit reached

            // Spend credit from first active plan that has available quota
            var targetPlan = activePlans.FirstOrDefault(p => p.AudioCallContactsUsed < p.AudioCallContactsPurchased) ?? activePlans.Last();
            targetPlan.AudioCallContactsUsed++;

            _dbContext.AudioCallContacts.Add(new AudioCallContact
            {
                UserId = callerId,
                ContactUserId = receiverId,
                PlanPurchaseId = targetPlan.Id,
                FirstCalledAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = callerId.ToString(),
                IsActive = true
            });

            await _dbContext.SaveChangesAsync(callerId.ToString());
            return true;
        }

        public async Task<bool> RecordVideoCallDurationAsync(long userId, int durationSeconds)
        {
            if (durationSeconds <= 0) return true;

            var activePlans = await _dbContext.PlanPurchases
                .Where(p => p.UserId == userId && !p.IsDeleted && p.ExpiresAt > DateTime.UtcNow)
                .OrderBy(p => p.ExpiresAt)
                .ToListAsync();

            if (!activePlans.Any()) return false;

            int minutesUsed = (int)Math.Ceiling(durationSeconds / 60.0);
            int remainingMinutesToDeduct = minutesUsed;

            foreach (var plan in activePlans)
            {
                int availableInPlan = plan.VideoCallMinutesPurchased - plan.VideoCallMinutesUsed;
                if (availableInPlan > 0)
                {
                    int deduct = Math.Min(remainingMinutesToDeduct, availableInPlan);
                    plan.VideoCallMinutesUsed += (short)deduct;
                    remainingMinutesToDeduct -= deduct;
                    if (remainingMinutesToDeduct <= 0) break;
                }
            }

            // If still remaining, apply to the latest plan
            if (remainingMinutesToDeduct > 0)
            {
                var latestPlan = activePlans.Last();
                latestPlan.VideoCallMinutesUsed += (short)remainingMinutesToDeduct;
            }

            await _dbContext.SaveChangesAsync(userId.ToString());
            return true;
        }

        private static string GenerateHmacSha1(string secret, string message)
        {
            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                using var hmac = new HMACSHA1(keyBytes);
                byte[] hash = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static CallLogDto MapToCallLogDto(CallLog log, long currentUserId, Registration? caller, Registration? receiver)
        {
            return new CallLogDto
            {
                Id = log.Id,
                CallerId = log.CallerId,
                CallerName = caller?.Name ?? "Member",
                CallerRegisterNumber = caller?.RegisterNumber,
                CallerPhotoUrl = caller?.ImagePath,
                ReceiverId = log.ReceiverId,
                ReceiverName = receiver?.Name ?? "Member",
                ReceiverRegisterNumber = receiver?.RegisterNumber,
                ReceiverPhotoUrl = receiver?.ImagePath,
                CallType = log.CallType,
                Status = log.Status,
                RoomId = log.RoomId,
                StartedAt = log.StartedAt,
                ConnectedAt = log.ConnectedAt,
                EndedAt = log.EndedAt,
                DurationSeconds = log.DurationSeconds,
                EndReason = log.EndReason,
                HasRecording = !string.IsNullOrEmpty(log.RecordingUrl) || !string.IsNullOrEmpty(log.RecordingS3Key),
                RecordingUrl = log.RecordingUrl,
                RecordingExpiresAt = log.RecordingExpiresAt,
                IsIncoming = log.ReceiverId == currentUserId
            };
        }
    }
}
