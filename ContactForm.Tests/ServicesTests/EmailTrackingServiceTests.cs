using System.Collections.Concurrent;
using ContactForm.MinimalAPI.Services;

namespace ContactForm.Tests.ServicesTests
{
    public class EmailTrackingServiceTests
    {
        // TEST FOR FIRST USE OF EMAIL
        [Fact]
        public async Task IsEmailUnique_FirstUse_ReturnsTrue()
        {
            // ARRANGE - INIT SERVICE
            var service = new EmailTrackingService();
            var email = "test1@example.com";
            var smtpIndex = 1;

            // ACT - CHECK UNIQUE
            var (isAllowed, timeRemaining, usageCount) = await service.IsEmailUnique(email, smtpIndex);

            // ASSERT - UNIQUE TRUE
            Assert.True(isAllowed);
            Assert.Null(timeRemaining);
            Assert.Equal(0, usageCount);
        }

        // TEST FOR AFTER TRACKING EMAIL
        [Fact]
        public async Task IsEmailUnique_AfterTrackingEmail_ReturnsFalseWithTimeout()
        {
            // ARRANGE - INIT SERVICE
            var service = new EmailTrackingService();
            var email = "test2@example.com";
            var smtpIndex = 1;

            // ACT - TRACK EMAIL
            await service.TrackEmail(email, smtpIndex);
            var (isAllowed, timeRemaining, usageCount) = await service.IsEmailUnique(email, smtpIndex);

            // ASSERT - VERIFY RESULT
            Assert.False(isAllowed);
            Assert.NotNull(timeRemaining);
            Assert.Equal(1, usageCount);

            // ASSERT - TIMEOUT 1 HOUR
            var expectedTimeout = TimeSpan.FromHours(1);
            var tolerance = TimeSpan.FromMinutes(1);
            Assert.True(
                Math.Abs((timeRemaining.Value - expectedTimeout).TotalMinutes) < tolerance.TotalMinutes,
                $"Timeout should be approximately {expectedTimeout} but was {timeRemaining}"
            );
        }

        // TEST FOR MULTIPLE TRACKS OF EMAIL
        [Fact]
        public async Task IsEmailUnique_MultipleTracks_IncreaseTimeout()
        {
            // ARRANGE - INIT SERVICE
            var service = new EmailTrackingService();
            var email = "test3@example.com";
            var smtpIndex = 1;

            // ACT - TRACK TWICE
            await service.TrackEmail(email, smtpIndex);
            await service.TrackEmail(email, smtpIndex);
            var (isAllowed, timeRemaining, usageCount) = await service.IsEmailUnique(email, smtpIndex);

            // ASSERT - VALIDATE RESULT
            Assert.False(isAllowed);
            Assert.NotNull(timeRemaining);
            Assert.Equal(2, usageCount);

            // ASSERT - TIMEOUT 2H
            var expectedTimeout = TimeSpan.FromHours(2);
            var tolerance = TimeSpan.FromMinutes(1);
            Assert.True(
                Math.Abs((timeRemaining.Value - expectedTimeout).TotalMinutes) < tolerance.TotalMinutes,
                $"Timeout should be approximately {expectedTimeout} but was {timeRemaining}"
            );
        }

        // TEST FOR DIFFERENT SMTP INDEXES
        [Fact]
        public async Task IsEmailUnique_DifferentSmtpIndexes_TracksSeparately()
        {
            // ARRANGE - INIT SERVICE
            var service = new EmailTrackingService();
            var email = "test4@example.com";
            var smtpIndex1 = 1;
            var smtpIndex2 = 2;

            // ACT - TRACK EMAIL, CHECK BOTH INDEXES
            await service.TrackEmail(email, smtpIndex1);
            var (isAllowed1, _, _) = await service.IsEmailUnique(email, smtpIndex1);
            var (isAllowed2, _, _) = await service.IsEmailUnique(email, smtpIndex2);

            // ASSERT - FIRST BLOCKED, SECOND ALLOWED
            Assert.False(isAllowed1);
            Assert.True(isAllowed2);
        }

        // TEST FOR AFTER TIMEOUT EXPIRES
        [Fact]
        public async Task IsEmailUnique_AfterTimeoutExpires_ReturnsTrue()
        {
            // ARRANGE - INIT SERVICE
            var testService = new TestableEmailTrackingService();
            var email = "test5@example.com";
            var smtpIndex = 1;

            // ACT - TRACK EMAIL, SIMULATE TIME
            await testService.TrackEmail(email, smtpIndex);
            TestableEmailTrackingService.SimulateTimePassing(email, smtpIndex, TimeSpan.FromHours(2));
            var (isAllowed, timeRemaining, usageCount) = await testService.IsEmailUnique(email, smtpIndex);

            // ASSERT - ALLOWED, TIME NULL, USAGE 1
            Assert.True(isAllowed);
            Assert.Null(timeRemaining);
            Assert.Equal(1, usageCount);
        }
    }

    // TESTABLE VERSION OF THE EMAIL TRACKING SERVICE THAT ALLOWS MANIPULATING TIME
    public class TestableEmailTrackingService : EmailTrackingService
    {
        // EXPOSE THE TRACKED EMAILS DICTIONARY FOR TESTING
        private static readonly ConcurrentDictionary<string, EmailUsageData> _trackedEmails = new();

        public static void SimulateTimePassing(string email, int smtpIndex, TimeSpan timePassed)
        {
            string key = $"{email.ToLower()}:{smtpIndex}";
            
            // SIMULATE TIME PASSING BY BACKDATING THE LAST USED TIMESTAMP
            if (_trackedEmails.TryGetValue(key, out var data))
            {
                data.LastUsed = DateTime.UtcNow.Subtract(timePassed);
                _trackedEmails[key] = data;
            }
        }

        // OVERRIDE TRACK EMAIL TO USE OUR TEST DICTIONARY
        public override Task TrackEmail(string email, int smtpIndex)
        {
            string key = $"{email.ToLower()}:{smtpIndex}";
            
            _trackedEmails.AddOrUpdate(
                key,

                // ADD NEW ENTRY IF KEY DOESN'T EXIST
                _ => new EmailUsageData { UsageCount = 1, LastUsed = DateTime.UtcNow },

                // UPDATE EXISTING ENTRY IF KEY EXISTS
                (_, existingData) => new EmailUsageData 
                { 
                    UsageCount = existingData.UsageCount + 1, 
                    LastUsed = DateTime.UtcNow 
                }
            );
            
            return Task.CompletedTask;
        }

        // OVERRIDE IS EMAIL UNIQUE TO USE OUR TEST DICTIONARY
        public override Task<(bool IsAllowed, TimeSpan? TimeRemaining, int UsageCount)> IsEmailUnique(string email, int smtpIndex)
        {
            string key = $"{email.ToLower()}:{smtpIndex}";
            
            // IF EMAIL IS NOT IN DICTIONARY, IT'S ALLOWED
            if (!_trackedEmails.TryGetValue(key, out var usageData))
            {
                return Task.FromResult<(bool IsAllowed, TimeSpan? TimeRemaining, int UsageCount)>((true, null, 0));
            }
            
            // CALCULATE TIMEOUT BASED ON USAGE COUNT (HOURS)
            var timeout = TimeSpan.FromHours(usageData.UsageCount);
            
            // CHECK IF TIMEOUT HAS PASSED
            var timeElapsed = DateTime.UtcNow - usageData.LastUsed;
            if (timeElapsed >= timeout)
            {
                return Task.FromResult<(bool IsAllowed, TimeSpan? TimeRemaining, int UsageCount)>((true, null, usageData.UsageCount));
            }
            
            // CALCULATE REMAINING TIME
            var timeRemaining = timeout - timeElapsed;
            return Task.FromResult<(bool IsAllowed, TimeSpan? TimeRemaining, int UsageCount)>((false, timeRemaining, usageData.UsageCount));
        }
    }
} 
