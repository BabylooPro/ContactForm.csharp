using System.Collections.Concurrent;

namespace API.Services
{
    public interface IIpProtectionService
    {
        bool IsIpBlocked(string ipAddress);
        void TrackRequest(string ipAddress, string path, string userAgent);
        void BlockIp(string ipAddress, TimeSpan duration, string reason);
    }

    public class IpProtectionService : IIpProtectionService, IDisposable
    {
        private readonly ILogger<IpProtectionService> _logger;
        private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();
        private readonly ConcurrentDictionary<string, RequestTracker> _ipTracking = new();
        
        // THRESHOLDS FOR DETECTING ABUSE
        private const int BURST_THRESHOLD = 20;
        private const int BURST_WINDOW_SECONDS = 5;
        private const int TOTAL_THRESHOLD = 100;
        private const int TRACKING_WINDOW_MINUTES = 10;

        private readonly Timer _cleanupTimer;

        public IpProtectionService(ILogger<IpProtectionService> logger)
        {
            _logger = logger;
            
            // START A BACKGROUND CLEANUP TASK
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public void Dispose()
        {
            _cleanupTimer.Dispose();
            GC.SuppressFinalize(this);
        }

        public bool IsIpBlocked(string ipAddress)
        {
            if (_blockedIps.TryGetValue(ipAddress, out DateTime expirationTime))
            {
                if (DateTime.UtcNow < expirationTime) return true;
                _blockedIps.TryRemove(ipAddress, out _);
            }
            
            return false;
        }

        public void TrackRequest(string ipAddress, string path, string userAgent)
        {
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "unknown") return;

            var tracker = _ipTracking.GetOrAdd(ipAddress, _ => new RequestTracker());
            
            // TRACK THE REQUEST
            var now = DateTime.UtcNow;
            tracker.AddRequest(now);
            
            // CHECK FOR ABUSE PATTERNS
            if (tracker.IsBurstDetected(now, BURST_THRESHOLD, BURST_WINDOW_SECONDS))
            {
                BlockIp(ipAddress, TimeSpan.FromHours(1), "Burst traffic detected");
                _logger.LogWarning("IP {IpAddress} blocked for 1 hour due to traffic burst", ipAddress);
            }
            else if (tracker.TotalRequests(now, TimeSpan.FromMinutes(TRACKING_WINDOW_MINUTES)) > TOTAL_THRESHOLD)
            {
                BlockIp(ipAddress, TimeSpan.FromHours(6), "Excessive requests over time");
                _logger.LogWarning("IP {IpAddress} blocked for 6 hours due to excessive requests", ipAddress);
            }
        }

        public void BlockIp(string ipAddress, TimeSpan duration, string reason)
        {
            var expirationTime = DateTime.UtcNow.Add(duration);
            _blockedIps[ipAddress] = expirationTime;
            _logger.LogWarning("IP {IpAddress} blocked until {ExpirationTime}: {Reason}", ipAddress, expirationTime, reason);
        }
        
        private void CleanupExpiredEntries(object? state)
        {
            var now = DateTime.UtcNow;
            
            // CLEANUP EXPIRED IP BLOCKS
            foreach (var (ip, expiration) in _blockedIps.ToArray())
            {
                if (now > expiration)
                {
                    _blockedIps.TryRemove(ip, out _);
                    _logger.LogInformation("IP block for {IpAddress} expired and removed", ip);
                }
            }
            
            // CLEANUP OLD TRACKING DATA
            var cutoff = now.AddMinutes(-30); // KEEP 30 MINUTES OF DATA
            foreach (var ip in _ipTracking.Keys)
            {
                if (_ipTracking.TryGetValue(ip, out var tracker))
                {
                    tracker.PruneOldEntries(cutoff);
                    
                    // REMOVE IF EMPTY
                    if (tracker.IsEmpty())
                    {
                        _ipTracking.TryRemove(ip, out _);
                    }
                }
            }
        }
    }

    // HELPER CLASS TO TRACK REQUESTS FOR AN IP
    internal class RequestTracker
    {
        private ConcurrentBag<DateTime> _requestTimestamps = [];
        
        public void AddRequest(DateTime timestamp)
        {
            _requestTimestamps.Add(timestamp);
        }
        
        public int TotalRequests(DateTime now, TimeSpan window)
        {
            var cutoff = now.Subtract(window);
            return _requestTimestamps.Count(ts => ts >= cutoff);
        }
        
        public bool IsBurstDetected(DateTime now, int threshold, int windowSeconds)
        {
            var cutoff = now.AddSeconds(-windowSeconds);
            return _requestTimestamps.Count(ts => ts >= cutoff) >= threshold;
        }
        
        public void PruneOldEntries(DateTime cutoff)
        {
            var newBag = new ConcurrentBag<DateTime>(
                _requestTimestamps.Where(ts => ts >= cutoff));
            
            Interlocked.Exchange(ref _requestTimestamps, newBag);
        }
        
        public bool IsEmpty()
        {
            return _requestTimestamps.IsEmpty;
        }
    }
} 
