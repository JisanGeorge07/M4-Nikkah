using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using URMARRY.Services;

namespace URMARRY.Hubs
{
    /// <summary>
    /// Lightweight SignalR hub for tracking active presence of web users.
    /// Tracks connect / disconnect lifecycle and notifies active users.
    /// </summary>
    public class PresenceHub : Hub
    {
        private readonly PresenceTracker _tracker;
        private readonly CookieHelper _cookieHelper;
        private readonly ILogger<PresenceHub> _logger;

        public PresenceHub(PresenceTracker tracker, CookieHelper cookieHelper, ILogger<PresenceHub> logger)
        {
            _tracker = tracker;
            _cookieHelper = cookieHelper;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = ResolveUserId();
            if (userId.HasValue && userId.Value > 0)
            {
                _tracker.WebConnected(userId.Value, Context.ConnectionId);
                _logger.LogInformation("PresenceHub: User {UserId} connected with ConnectionId {ConnectionId}", userId.Value, Context.ConnectionId);

                // Notify other active users that this user came online in real-time
                await Clients.Others.SendAsync("UserOnline", userId.Value);
            }
            else
            {
                _logger.LogWarning("PresenceHub: Connection {ConnectionId} could not resolve userId. Cookie present: {HasCookie}", 
                    Context.ConnectionId, 
                    Context.GetHttpContext()?.Request.Cookies.ContainsKey("auth_session") ?? false);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = ResolveUserId();
            if (userId.HasValue && userId.Value > 0)
            {
                _tracker.WebDisconnected(userId.Value, Context.ConnectionId);
                _logger.LogInformation("PresenceHub: User {UserId} disconnected with ConnectionId {ConnectionId}", userId.Value, Context.ConnectionId);

                // Notify other active users that this user went offline in real-time
                await Clients.Others.SendAsync("UserOffline", userId.Value);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private long? ResolveUserId()
        {
            // 1. Try decoding from cookie (standard in URMARRY via auth_session JWT cookie)
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                var cookieUserId = _cookieHelper.GetUserIdFromCookie(httpContext);
                if (cookieUserId.HasValue && cookieUserId.Value > 0)
                {
                    return cookieUserId.Value;
                }

                // 2. Query param fallback (e.g. ?userId=123) for SignalR handshake
                if (httpContext.Request.Query.TryGetValue("userId", out var qUserId) && long.TryParse(qUserId, out var parsedId) && parsedId > 0)
                {
                    return parsedId;
                }
            }

            // 3. Try Claims from authenticated principal if populated
            if (Context.User != null)
            {
                var nameClaim = Context.User.FindFirst(ClaimTypes.Name)?.Value;
                if (long.TryParse(nameClaim, out var id1)) return id1;

                var idClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(idClaim, out var id2)) return id2;

                var subClaim = Context.User.FindFirst("sub")?.Value ?? Context.User.FindFirst("UserId")?.Value;
                if (long.TryParse(subClaim, out var id3)) return id3;
            }

            return null;
        }
    }
}
