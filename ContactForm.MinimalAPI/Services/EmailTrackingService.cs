using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Interfaces;

namespace ContactForm.MinimalAPI.Services
{
    public class EmailTrackingService : IEmailTrackingService
    {
        // STORE EMAIL USAGE DATA WITH TIMESTAMP AND COUNTER
        private static readonly ConcurrentDictionary<string, EmailUsageData> _trackedEmails = new();

        // CHECK IF EMAIL IS UNIQUE OR CAN BE USED AGAIN BASED ON TIMEOUT
        public virtual Task<(bool IsAllowed, TimeSpan? TimeRemaining, int UsageCount)> IsEmailUnique(string email, int smtpIndex)
        {
            string key = $"{email.ToLower()}:{smtpIndex}";
            
            // IF EMAIL IS NOT IN DICTIONARY, IT'S ALLOWED
            if (!_trackedEmails.TryGetValue(key, out var usageData))
            {
                return Task.FromResult((true, (TimeSpan?)null, 0));
            }
            
            // CALCULATE TIMEOUT BASED ON USAGE COUNT (HOURS)
            var timeout = TimeSpan.FromHours(usageData.UsageCount);
            
            // CHECK IF TIMEOUT HAS PASSED
            var timeElapsed = DateTime.UtcNow - usageData.LastUsed;
            if (timeElapsed >= timeout)
            {
                return Task.FromResult((true, (TimeSpan?)null, usageData.UsageCount));
            }
            
            // CALCULATE REMAINING TIME
            var timeRemaining = timeout - timeElapsed;
            return Task.FromResult<(bool IsAllowed, TimeSpan? TimeRemaining, int UsageCount)>((false, timeRemaining, usageData.UsageCount));
        }

        // TRACK EMAIL USAGE
        public virtual Task TrackEmail(string email, int smtpIndex)
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
    }
    
    // CLASS TO STORE EMAIL USAGE DATA
    public class EmailUsageData
    {
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }
    }
} 
