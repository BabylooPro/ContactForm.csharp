using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Services;
using Xunit;

namespace ContactForm.Tests.ServicesTests
{
    public class EmailTrackingServiceTests
    {
        // TEST FOR FIRST USE OF EMAIL
        [Fact]
        public async Task IsEmailUnique_FirstUse_ReturnsTrue()
        {
            // ARRANGE - CREATE NEW INSTANCE OF EMAIL TRACKING SERVICE
            var service = new EmailTrackingService();
            var email = "test1@example.com"; // UNIQUE EMAIL FOR THIS TEST
            var smtpIndex = 1;

            // ACT - CHECK IF EMAIL IS UNIQUE
            var result = await service.IsEmailUnique(email, smtpIndex);

            // ASSERT - EMAIL SHOULD BE UNIQUE
            Assert.True(result.IsAllowed);
            Assert.Null(result.TimeRemaining);
            Assert.Equal(0, result.UsageCount);
        }

        // TEST FOR AFTER TRACKING EMAIL
        [Fact]
        public async Task IsEmailUnique_AfterTrackingEmail_ReturnsFalseWithTimeout()
        {
            // ARRANGE - CREATE NEW INSTANCE OF EMAIL TRACKING SERVICE
            var service = new EmailTrackingService();
            var email = "test2@example.com"; // UNIQUE EMAIL FOR THIS TEST
            var smtpIndex = 1;

            // ACT - FIRST TRACK THE EMAIL
            await service.TrackEmail(email, smtpIndex);
            var result = await service.IsEmailUnique(email, smtpIndex);

            // ASSERT - EMAIL SHOULD NOT BE UNIQUE
            Assert.False(result.IsAllowed);
            Assert.NotNull(result.TimeRemaining);
            Assert.Equal(1, result.UsageCount);
            
            // VERIFY THE TIMEOUT IS APPROXIMATELY 1 HOUR
            var expectedTimeout = TimeSpan.FromHours(1);
            var tolerance = TimeSpan.FromMinutes(1); // ALLOW 1 MINUTE TOLERANCE
            
            Assert.True(
                Math.Abs((result.TimeRemaining.Value - expectedTimeout).TotalMinutes) < tolerance.TotalMinutes,
                $"Timeout should be approximately {expectedTimeout} but was {result.TimeRemaining}"
            );
        }

        // TEST FOR MULTIPLE TRACKS OF EMAIL
        [Fact]
        public async Task IsEmailUnique_MultipleTracks_IncreaseTimeout()
        {
            // ARRANGE - CREATE NEW INSTANCE OF EMAIL TRACKING SERVICE
            var service = new EmailTrackingService();
            var email = "test3@example.com"; // UNIQUE EMAIL FOR THIS TEST
            var smtpIndex = 1;

            // ACT - TRACK EMAIL MULTIPLE TIMES
            await service.TrackEmail(email, smtpIndex); // FIRST USAGE - 1 HOUR TIMEOUT
            await service.TrackEmail(email, smtpIndex); // SECOND USAGE - 2 HOURS TIMEOUT
            var result = await service.IsEmailUnique(email, smtpIndex);

            // ASSERT - EMAIL SHOULD NOT BE UNIQUE
            Assert.False(result.IsAllowed);
            Assert.NotNull(result.TimeRemaining);
            Assert.Equal(2, result.UsageCount);
            
            // VERIFY THE TIMEOUT IS APPROXIMATELY 2 HOURS
            var expectedTimeout = TimeSpan.FromHours(2);
            var tolerance = TimeSpan.FromMinutes(1); // ALLOW 1 MINUTE TOLERANCE
            
            Assert.True(
                Math.Abs((result.TimeRemaining.Value - expectedTimeout).TotalMinutes) < tolerance.TotalMinutes,
                $"Timeout should be approximately {expectedTimeout} but was {result.TimeRemaining}"
            );
        }

        // TEST FOR DIFFERENT SMTP INDEXES
        [Fact]
        public async Task IsEmailUnique_DifferentSmtpIndexes_TracksSeparately()
        {
            // ARRANGE - CREATE NEW INSTANCE OF EMAIL TRACKING SERVICE
            var service = new EmailTrackingService();
            var email = "test4@example.com"; // UNIQUE EMAIL FOR THIS TEST
            var smtpIndex1 = 1;
            var smtpIndex2 = 2;

            // ACT - TRACK EMAIL FOR ONE SMTP INDEX
            await service.TrackEmail(email, smtpIndex1);
            
            // CHECK FIRST SMTP INDEX
            var result1 = await service.IsEmailUnique(email, smtpIndex1);
            
            // CHECK SECOND SMTP INDEX (UNUSED)
            var result2 = await service.IsEmailUnique(email, smtpIndex2);

            // ASSERT - EMAIL SHOULD NOT BE UNIQUE FOR FIRST SMTP
            Assert.False(result1.IsAllowed); // SHOULD BE BLOCKED FOR FIRST SMTP
            Assert.True(result2.IsAllowed);  // SHOULD BE ALLOWED FOR SECOND SMTP
        }

        // TEST FOR AFTER TIMEOUT EXPIRES
        [Fact]
        public async Task IsEmailUnique_AfterTimeoutExpires_ReturnsTrue()
        {
            // ARRANGE - CREATE NEW INSTANCE OF EMAIL TRACKING SERVICE
            var testService = new TestableEmailTrackingService();
            var email = "test5@example.com"; // UNIQUE EMAIL FOR THIS TEST
            var smtpIndex = 1;

            // ACT - TRACK EMAIL ONCE
            await testService.TrackEmail(email, smtpIndex);
            
            // SIMULATE TIME PASSING (SET LAST USED TO 2 HOURS AGO)
            testService.SimulateTimePassing(email, smtpIndex, TimeSpan.FromHours(2));
            
            // CHECK IF EMAIL CAN BE USED NOW
            var result = await testService.IsEmailUnique(email, smtpIndex);

            // ASSERT - EMAIL SHOULD BE UNIQUE
            Assert.True(result.IsAllowed);
            Assert.Null(result.TimeRemaining);
            Assert.Equal(1, result.UsageCount); // USAGE COUNT STILL SHOWS 1
        }
    }

    // TESTABLE VERSION OF THE EMAIL TRACKING SERVICE THAT ALLOWS MANIPULATING TIME
    public class TestableEmailTrackingService : EmailTrackingService
    {
        // EXPOSE THE TRACKED EMAILS DICTIONARY FOR TESTING
        private static readonly ConcurrentDictionary<string, EmailUsageData> _trackedEmails = new();

        public void SimulateTimePassing(string email, int smtpIndex, TimeSpan timePassed)
        {
            string key = $"{email.ToLower()}:{smtpIndex}";
            
            if (_trackedEmails.TryGetValue(key, out var data))
            {
                // SIMULATE TIME PASSING BY BACKDATING THE LAST USED TIMESTAMP
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
