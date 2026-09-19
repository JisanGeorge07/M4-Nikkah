using Application.Interfaces.Persistence;
using Application.Models.Call;
using Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using URMARRY.Services;

namespace URMARRY.Hubs
{
    /// <summary>
    /// Real-time SignalR Hub for WebRTC Audio & Video Calling signaling,
    /// dynamic ICE candidate exchange, session state sync, and live call lifecycle.
    /// </summary>
    public class CallHub : Hub
    {
        private readonly ICallService _callService;
        private readonly PresenceTracker _presenceTracker;
        private readonly CookieHelper _cookieHelper;
        private readonly ILogger<CallHub> _logger;

        public CallHub(
            ICallService callService,
            PresenceTracker presenceTracker,
            CookieHelper cookieHelper,
            ILogger<CallHub> logger)
        {
            _callService = callService;
            _presenceTracker = presenceTracker;
            _cookieHelper = cookieHelper;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = ResolveUserId();
            if (userId.HasValue && userId.Value > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
                _logger.LogInformation("CallHub: User {UserId} connected with ConnectionId {ConnectionId}", userId.Value, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("CallHub: Unauthenticated connection attempt {ConnectionId}", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = ResolveUserId();
            if (userId.HasValue && userId.Value > 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Initiates an audio or video call, notifies the callee with an IncomingCall event.
        /// </summary>
        public async Task<CallLogDto> InitiateCall(InitiateCallRequest request)
        {
            var callerId = ResolveUserId();
            if (!callerId.HasValue || callerId.Value <= 0)
            {
                throw new HubException("Unauthorized to make calls.");
            }

            try
            {
                // Check if recipient is online
                bool isRecipientOnline = _presenceTracker.IsOnline(request.ReceiverId);
                if (!isRecipientOnline)
                {
                    _logger.LogInformation("CallHub: User {CallerId} attempted to call offline User {ReceiverId}", callerId.Value, request.ReceiverId);
                }

                var callLog = await _callService.InitiateCallAsync(callerId.Value, request);

                // Add caller to dedicated CallRoom
                await Groups.AddToGroupAsync(Context.ConnectionId, $"CallRoom_{callLog.RoomId}");

                // Notify receiver in real time
                await Clients.Group($"User_{request.ReceiverId}").SendAsync("IncomingCall", callLog);

                return callLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CallHub: Error initiating call from {CallerId} to {ReceiverId}", callerId.Value, request.ReceiverId);
                throw new HubException(ex.Message);
            }
        }

        /// <summary>
        /// Callee answers incoming call.
        /// </summary>
        public async Task<CallLogDto> AnswerCall(AnswerCallRequest request)
        {
            var calleeId = ResolveUserId();
            if (!calleeId.HasValue || calleeId.Value <= 0)
            {
                throw new HubException("Unauthorized.");
            }

            try
            {
                var callLog = await _callService.AnswerCallAsync(request.CallLogId, calleeId.Value);

                // Add callee to CallRoom
                await Groups.AddToGroupAsync(Context.ConnectionId, $"CallRoom_{callLog.RoomId}");

                // Notify caller and room that call is accepted
                await Clients.Group($"CallRoom_{callLog.RoomId}").SendAsync("CallAccepted", callLog);
                await Clients.Group($"User_{callLog.CallerId}").SendAsync("CallAccepted", callLog);

                return callLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CallHub: Error answering call {CallLogId}", request.CallLogId);
                throw new HubException(ex.Message);
            }
        }

        /// <summary>
        /// Callee or caller rejects the call.
        /// </summary>
        public async Task<CallLogDto> RejectCall(RejectCallRequest request)
        {
            var userId = ResolveUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                throw new HubException("Unauthorized.");
            }

            try
            {
                var callLog = await _callService.RejectCallAsync(request.CallLogId, userId.Value, request.Reason);

                // Notify both participants
                await Clients.Group($"CallRoom_{callLog.RoomId}").SendAsync("CallRejected", callLog);
                await Clients.Group($"User_{callLog.CallerId}").SendAsync("CallRejected", callLog);
                await Clients.Group($"User_{callLog.ReceiverId}").SendAsync("CallRejected", callLog);

                return callLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CallHub: Error rejecting call {CallLogId}", request.CallLogId);
                throw new HubException(ex.Message);
            }
        }

        /// <summary>
        /// Either participant hangs up and ends the call.
        /// </summary>
        public async Task<CallLogDto> EndCall(EndCallRequest request)
        {
            var userId = ResolveUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                throw new HubException("Unauthorized.");
            }

            try
            {
                var callLog = await _callService.EndCallAsync(request.CallLogId, userId.Value, request.Reason);

                // Broadcast termination event to room and private groups
                await Clients.Group($"CallRoom_{callLog.RoomId}").SendAsync("CallEnded", callLog);
                await Clients.Group($"User_{callLog.CallerId}").SendAsync("CallEnded", callLog);
                await Clients.Group($"User_{callLog.ReceiverId}").SendAsync("CallEnded", callLog);

                return callLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CallHub: Error ending call {CallLogId}", request.CallLogId);
                throw new HubException(ex.Message);
            }
        }

        #region WebRTC SDP & ICE Candidate Signaling

        /// <summary>
        /// Relays WebRTC SDP Offer from caller to receiver.
        /// </summary>
        public async Task SendOffer(long receiverId, string sdpOffer, string roomId)
        {
            var callerId = ResolveUserId();
            if (!callerId.HasValue || callerId.Value <= 0) return;

            var payload = new
            {
                CallerId = callerId.Value,
                Sdp = sdpOffer,
                RoomId = roomId
            };

            await Clients.Group($"User_{receiverId}").SendAsync("ReceiveOffer", payload);
            await Clients.OthersInGroup($"CallRoom_{roomId}").SendAsync("ReceiveOffer", payload);
        }

        /// <summary>
        /// Relays WebRTC SDP Answer from receiver back to caller.
        /// </summary>
        public async Task SendAnswer(long callerId, string sdpAnswer, string roomId)
        {
            var calleeId = ResolveUserId();
            if (!calleeId.HasValue || calleeId.Value <= 0) return;

            var payload = new
            {
                CalleeId = calleeId.Value,
                Sdp = sdpAnswer,
                RoomId = roomId
            };

            await Clients.Group($"User_{callerId}").SendAsync("ReceiveAnswer", payload);
            await Clients.OthersInGroup($"CallRoom_{roomId}").SendAsync("ReceiveAnswer", payload);
        }

        /// <summary>
        /// Relays WebRTC ICE Candidate between peers.
        /// </summary>
        public async Task SendIceCandidate(long targetUserId, string candidate, string roomId)
        {
            var senderId = ResolveUserId();
            if (!senderId.HasValue || senderId.Value <= 0) return;

            var payload = new
            {
                SenderId = senderId.Value,
                Candidate = candidate,
                RoomId = roomId
            };

            await Clients.Group($"User_{targetUserId}").SendAsync("ReceiveIceCandidate", payload);
            await Clients.OthersInGroup($"CallRoom_{roomId}").SendAsync("ReceiveIceCandidate", payload);
        }

        /// <summary>
        /// Relays live microphone mute / video enable toggle states.
        /// </summary>
        public async Task SendMediaState(long targetUserId, bool isAudioMuted, bool isVideoEnabled, string roomId)
        {
            var senderId = ResolveUserId();
            if (!senderId.HasValue || senderId.Value <= 0) return;

            var payload = new
            {
                SenderId = senderId.Value,
                IsAudioMuted = isAudioMuted,
                IsVideoEnabled = isVideoEnabled,
                RoomId = roomId
            };

            await Clients.Group($"User_{targetUserId}").SendAsync("UserMediaStateChanged", payload);
            await Clients.OthersInGroup($"CallRoom_{roomId}").SendAsync("UserMediaStateChanged", payload);
        }

        #endregion

        #region Private Helpers

        private long? ResolveUserId()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                var cookieUserId = _cookieHelper.GetUserIdFromCookie(httpContext);
                if (cookieUserId.HasValue && cookieUserId.Value > 0)
                {
                    return cookieUserId.Value;
                }

                if (httpContext.Request.Query.TryGetValue("userId", out var qUserId) && long.TryParse(qUserId, out var parsedId) && parsedId > 0)
                {
                    return parsedId;
                }
            }

            if (Context.User != null)
            {
                var idClaim = Context.User.FindFirst(ClaimTypes.Name)?.Value
                    ?? Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User.FindFirst("sub")?.Value
                    ?? Context.User.FindFirst("UserId")?.Value;

                if (long.TryParse(idClaim, out var id) && id > 0)
                {
                    return id;
                }
            }

            return null;
        }

        #endregion
    }
}
