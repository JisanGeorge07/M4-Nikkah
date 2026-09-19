using Application.Models.Call;
using Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Persistence
{
    public interface ICallService
    {
        /// <summary>
        /// Validates whether the caller has plan/premium permission to initiate audio/video calls with the receiver.
        /// </summary>
        Task<CallPermissionResult> CheckCallPermissionAsync(long callerId, long receiverId, UserCallType callType);

        /// <summary>
        /// Initiates a call session, generates room ID, creates CallLog in Ringing state.
        /// </summary>
        Task<CallLogDto> InitiateCallAsync(long callerId, InitiateCallRequest request);

        /// <summary>
        /// Marks call as Connected, sets ConnectedAt timestamp, and starts the session.
        /// </summary>
        Task<CallLogDto> AnswerCallAsync(long callLogId, long receiverId);

        /// <summary>
        /// Rejects incoming call and updates status to Rejected.
        /// </summary>
        Task<CallLogDto> RejectCallAsync(long callLogId, long receiverId, string? reason);

        /// <summary>
        /// Ends active call session, computes duration, and updates status to Ended.
        /// </summary>
        Task<CallLogDto> EndCallAsync(long callLogId, long userId, string? reason);

        /// <summary>
        /// Returns paginated call history for a user with caller and receiver details.
        /// </summary>
        Task<PaginatedCallLogsDto> GetCallHistoryAsync(long userId, int page = 1, int pageSize = 20);

        /// <summary>
        /// Retrieves single call log details by ID with permission checks.
        /// </summary>
        Task<CallLogDto?> GetCallLogByIdAsync(long callLogId, long userId);

        /// <summary>
        /// Generates WebRTC ICE server configurations (Google STUN + Coturn TURN dynamic HMAC-SHA1 tokens).
        /// </summary>
        IceServerConfigResponse GetIceServerConfiguration(long userId);

        /// <summary>
        /// Saves 14-day security call recording metadata (S3 key, URL, duration, expiry date).
        /// </summary>
        Task<bool> SaveCallRecordingMetadataAsync(long callLogId, string s3Key, string recordingUrl, long fileSize, int durationSeconds);
    }
}
