using System.Collections.Concurrent;

namespace URMARRY.Services
{
    /// <summary>
    /// Singleton in-memory service for tracking active online users
    /// across both web (SignalR presence) and mobile apps (heartbeat API).
    /// </summary>
    public class PresenceTracker
    {
        // Tracks web users (SignalR connection IDs per user, supporting multiple tabs/devices)
        private readonly ConcurrentDictionary<long, HashSet<string>> _webConnections = new();

        // Tracks mobile / API clients via periodic heartbeat timestamps
        private readonly ConcurrentDictionary<long, DateTime> _apiHeartbeats = new();

        // Tracks last seen timestamp per user
        private readonly ConcurrentDictionary<long, DateTime> _lastSeen = new();

        // Time after which a mobile client with no heartbeat is considered offline
        private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Registers a new active web connection (SignalR) for a user.
        /// </summary>
        public void WebConnected(long userId, string connectionId)
        {
            if (userId <= 0 || string.IsNullOrEmpty(connectionId)) return;

            _webConnections.AddOrUpdate(
                userId,
                _ => new HashSet<string> { connectionId },
                (_, existingSet) =>
                {
                    lock (existingSet)
                    {
                        existingSet.Add(connectionId);
                    }
                    return existingSet;
                });

            _lastSeen[userId] = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a web connection (SignalR) when a tab/browser disconnects.
        /// </summary>
        public void WebDisconnected(long userId, string connectionId)
        {
            if (userId <= 0 || string.IsNullOrEmpty(connectionId)) return;

            if (_webConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        _webConnections.TryRemove(userId, out _);
                    }
                }
            }

            _lastSeen[userId] = DateTime.UtcNow;
        }

        /// <summary>
        /// Explicitly marks a user as logged out, clearing all active web connections and mobile heartbeats.
        /// </summary>
        public void UserLoggedOut(long userId)
        {
            RecordExplicitOffline(userId);
        }

        /// <summary>
        /// Explicitly records that a user is offline (logout, browser unload beacon, or mobile app close).
        /// Clears all web connections and mobile heartbeats, updates last seen.
        /// </summary>
        public DateTime RecordExplicitOffline(long userId)
        {
            var now = DateTime.UtcNow;
            if (userId > 0)
            {
                _webConnections.TryRemove(userId, out _);
                _apiHeartbeats.TryRemove(userId, out _);
                _lastSeen[userId] = now;
            }
            return now;
        }

        /// <summary>
        /// Records a heartbeat from a mobile / external client.
        /// </summary>
        public void RecordHeartbeat(long userId)
        {
            RecordHeartbeatAndCheckIfNewlyOnline(userId);
        }

        /// <summary>
        /// Records a heartbeat from a mobile / external client.
        /// Returns true if the user was previously offline, so a real-time UserOnline event can be broadcast.
        /// </summary>
        public bool RecordHeartbeatAndCheckIfNewlyOnline(long userId)
        {
            if (userId <= 0) return false;
            bool wasOnline = IsOnline(userId);
            var now = DateTime.UtcNow;
            _apiHeartbeats[userId] = now;
            _lastSeen[userId] = now;
            return !wasOnline;
        }

        /// <summary>
        /// Scans for mobile heartbeats that have exceeded the timeout.
        /// If the user also has no active web connections, removes them from active heartbeats
        /// and returns them so the caller can persist LastSeenAt to DB and broadcast UserOffline over SignalR.
        /// </summary>
        public List<(long UserId, DateTime LastSeen)> SweepExpiredHeartbeats(TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? HeartbeatTimeout;
            var now = DateTime.UtcNow;
            var expired = new List<(long UserId, DateTime LastSeen)>();

            foreach (var kvp in _apiHeartbeats)
            {
                if (now - kvp.Value > effectiveTimeout)
                {
                    if (_apiHeartbeats.TryRemove(kvp.Key, out var lastHeartbeat))
                    {
                        bool hasWeb = false;
                        if (_webConnections.TryGetValue(kvp.Key, out var connections))
                        {
                            lock (connections)
                            {
                                hasWeb = connections.Count > 0;
                            }
                        }

                        if (!hasWeb)
                        {
                            _lastSeen[kvp.Key] = lastHeartbeat;
                            expired.Add((kvp.Key, lastHeartbeat));
                        }
                    }
                }
            }

            return expired;
        }

        /// <summary>
        /// Checks whether a given user is currently online (either web or mobile).
        /// </summary>
        public bool IsOnline(long userId)
        {
            if (userId <= 0) return false;

            // 1. Check web connections
            if (_webConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    if (connections.Count > 0) return true;
                }
            }

            // 2. Check mobile heartbeat
            if (_apiHeartbeats.TryGetValue(userId, out var lastHeartbeat))
            {
                if (DateTime.UtcNow - lastHeartbeat <= HeartbeatTimeout)
                {
                    return true;
                }
                // Clean up expired heartbeat entry
                _apiHeartbeats.TryRemove(userId, out _);
            }

            return false;
        }

        /// <summary>
        /// Filters an enumerable of user IDs and returns a HashSet of those who are currently online.
        /// </summary>
        public HashSet<long> GetOnlineUserIds(IEnumerable<long> userIds)
        {
            var onlineSet = new HashSet<long>();
            if (userIds == null) return onlineSet;

            var now = DateTime.UtcNow;

            foreach (var id in userIds)
            {
                if (id <= 0) continue;

                bool isOnline = false;

                if (_webConnections.TryGetValue(id, out var connections))
                {
                    lock (connections)
                    {
                        if (connections.Count > 0) isOnline = true;
                    }
                }

                if (!isOnline && _apiHeartbeats.TryGetValue(id, out var lastHeartbeat))
                {
                    if (now - lastHeartbeat <= HeartbeatTimeout)
                    {
                        isOnline = true;
                    }
                    else
                    {
                        _apiHeartbeats.TryRemove(id, out _);
                    }
                }

                if (isOnline)
                {
                    onlineSet.Add(id);
                }
            }

            return onlineSet;
        }

        /// <summary>
        /// Returns all currently online user IDs.
        /// </summary>
        public HashSet<long> GetAllOnlineUserIds()
        {
            var now = DateTime.UtcNow;
            var allOnline = new HashSet<long>();

            foreach (var kvp in _webConnections)
            {
                lock (kvp.Value)
                {
                    if (kvp.Value.Count > 0)
                    {
                        allOnline.Add(kvp.Key);
                    }
                }
            }

            foreach (var kvp in _apiHeartbeats)
            {
                if (now - kvp.Value <= HeartbeatTimeout)
                {
                    allOnline.Add(kvp.Key);
                }
                else
                {
                    _apiHeartbeats.TryRemove(kvp.Key, out _);
                }
            }

            return allOnline;
        }

        /// <summary>
        /// Gets the last seen timestamp for a user.
        /// Returns DateTime.UtcNow if the user is currently online.
        /// </summary>
        public DateTime? GetLastSeen(long userId)
        {
            if (userId <= 0) return null;
            if (IsOnline(userId)) return DateTime.UtcNow;
            if (_lastSeen.TryGetValue(userId, out var lastSeen)) return lastSeen;
            if (_apiHeartbeats.TryGetValue(userId, out var heartbeat)) return heartbeat;
            return null;
        }
    }
}
