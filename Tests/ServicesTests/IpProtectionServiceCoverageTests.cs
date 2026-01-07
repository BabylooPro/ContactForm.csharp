using System.Reflection;
using API.Services;
using Moq;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR IP PROTECTION SERVICE
    public class IpProtectionServiceCoverageTests
    {
        // TEST FOR TRACKREQUEST IGNORING UNKNOWN IP
        [Fact]
        public void TrackRequest_WhenIpIsUnknown_ReturnsImmediately()
        {
            // ARRANGE - INIT SERVICE
            var logger = new Mock<ILogger<IpProtectionService>>().Object;
            using var service = new IpProtectionService(logger);

            // ACT - TRACK REQUEST
            service.TrackRequest("unknown", "/test", "ua");

            // ASSERT - IP IS NOT BLOCKED
            Assert.False(service.IsIpBlocked("unknown"));
        }

        // TEST FOR TRACKREQUEST IGNORING EMPTY IP
        [Fact]
        public void TrackRequest_WhenIpIsEmpty_ReturnsImmediately()
        {
            // ARRANGE - INIT SERVICE
            var logger = new Mock<ILogger<IpProtectionService>>().Object;
            using var service = new IpProtectionService(logger);

            // ACT - TRACK REQUEST
            service.TrackRequest(string.Empty, "/test", "ua");
        }

        // TEST FOR TRACKREQUEST BLOCKING WHEN TOTAL THRESHOLD IS EXCEEDED
        [Fact]
        public void TrackRequest_WhenTotalThresholdExceeded_BlocksForSixHours()
        {
            // ARRANGE - INIT SERVICE
            var logger = new Mock<ILogger<IpProtectionService>>().Object;
            using var service = new IpProtectionService(logger);

            // ARRANGE - IP ADDRESS
            var ip = "1.2.3.4";

            // ARRANGE - PRELOAD TRACKER WITH > 100 REQUESTS IN LAST 10 MINUTES BUT NONE IN LAST 5 SECONDS
            var tracker = new RequestTracker();
            var now = DateTime.UtcNow;
            for (var i = 0; i < 101; i++) tracker.AddRequest(now.AddSeconds(-6));
            var ipTrackingField = typeof(IpProtectionService).GetField("_ipTracking", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(ipTrackingField);
            var ipTracking = (System.Collections.Concurrent.ConcurrentDictionary<string, RequestTracker>)ipTrackingField!.GetValue(service)!;
            ipTracking[ip] = tracker;

            // ACT - TRACK REQUEST (SHOULD TRIGGER BLOCK)
            service.TrackRequest(ip, "/test", "ua");

            // ASSERT - IP IS BLOCKED
            Assert.True(service.IsIpBlocked(ip));
        }

        // TEST FOR CLEANUPEXPIREDENTRIES REMOVING EXPIRED BLOCKS AND EMPTY TRACKERS
        [Fact]
        public void CleanupExpiredEntries_RemovesExpiredBlocksAndEmptyTrackers()
        {
            // ARRANGE - INIT SERVICE
            var logger = new Mock<ILogger<IpProtectionService>>().Object;
            using var service = new IpProtectionService(logger);

            // ARRANGE - GET PRIVATE FIELDS/METHOD VIA REFLECTION
            var blockedIpsField = typeof(IpProtectionService).GetField("_blockedIps", BindingFlags.NonPublic | BindingFlags.Instance);
            var ipTrackingField = typeof(IpProtectionService).GetField("_ipTracking", BindingFlags.NonPublic | BindingFlags.Instance);
            var cleanupMethod = typeof(IpProtectionService).GetMethod("CleanupExpiredEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(blockedIpsField);
            Assert.NotNull(ipTrackingField);
            Assert.NotNull(cleanupMethod);

            // ARRANGE - EXPIRED AND ACTIVE BLOCKS
            var blockedIps = (System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>)blockedIpsField!.GetValue(service)!;
            var now = DateTime.UtcNow;
            blockedIps["expired"] = now.AddMinutes(-1);
            blockedIps["active"] = now.AddMinutes(10);

            // ARRANGE - TRACKER THAT BECOMES EMPTY AFTER PRUNE (ALL ENTRIES OLD)
            var ipTracking = (System.Collections.Concurrent.ConcurrentDictionary<string, RequestTracker>)ipTrackingField!.GetValue(service)!;
            var emptyAfterPrune = new RequestTracker();
            emptyAfterPrune.AddRequest(now.AddHours(-1));
            ipTracking["old"] = emptyAfterPrune;

            // ARRANGE - TRACKER THAT REMAINS (RECENT ENTRY)
            var keep = new RequestTracker();
            keep.AddRequest(now);
            ipTracking["keep"] = keep;

            // ACT - RUN CLEANUP
            cleanupMethod!.Invoke(service, [null]);

            // ASSERT - EXPIRED BLOCK REMOVED, ACTIVE BLOCK KEPT
            Assert.False(blockedIps.ContainsKey("expired"));
            Assert.True(blockedIps.ContainsKey("active"));

            // ASSERT - EMPTY TRACKER REMOVED, NON-EMPTY TRACKER KEPT
            Assert.False(ipTracking.ContainsKey("old"));
            Assert.True(ipTracking.ContainsKey("keep"));
        }
    }
}
