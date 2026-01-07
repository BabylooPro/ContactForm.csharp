using System.Collections.Concurrent;
using System.Reflection;
using API.Services;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR EMAIL TRACKING SERVICE
    public class EmailTrackingServiceCoverageTests
    {
        // TEST FOR ISEMAILUNIQUE RETURNING ALLOWED WHEN TIMEOUT HAS PASSED
        [Fact]
        public async Task IsEmailUnique_WhenTimeoutHasPassed_ReturnsAllowed()
        {
            // ARRANGE - INIT SERVICE AND KEY
            var service = new EmailTrackingService();
            var email = "time@example.com";
            var smtpIndex = 7;
            var key = $"{email.ToLower()}:{smtpIndex}";

            // ARRANGE - GET PRIVATE DICTIONARY VIA REFLECTION
            var field = typeof(EmailTrackingService).GetField("_trackedEmails", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);
            var dict = (ConcurrentDictionary<string, EmailUsageData>)field!.GetValue(null)!;

            try
            {
                // ARRANGE - TRACK EMAIL ONCE
                await service.TrackEmail(email, smtpIndex);

                Assert.True(dict.TryGetValue(key, out var usage));
                usage.LastUsed = DateTime.UtcNow - TimeSpan.FromHours(2);

                // ACT - CHECK UNIQUENESS AFTER TIMEOUT
                var (allowed, remaining, usageCount) = await service.IsEmailUnique(email, smtpIndex);

                // ASSERT - ALLOWED
                Assert.True(allowed);
                Assert.Null(remaining);
                Assert.Equal(usage.UsageCount, usageCount);
            }
            finally
            {
                // CLEANUP - REMOVE KEY TO AVOID CROSS-TEST POLLUTION
                dict.TryRemove(key, out _);
            }
        }
    }
}
