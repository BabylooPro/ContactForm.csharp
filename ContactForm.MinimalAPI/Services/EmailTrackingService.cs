using System.Collections.Concurrent;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Interfaces;

namespace ContactForm.MinimalAPI.Services
{
    public class EmailTrackingService : IEmailTrackingService
    {
        // USING CONCURRENT DICTIONARY TO STORE EMAILS IN THREAD-SAFE WAY
        private static readonly ConcurrentDictionary<string, byte> _trackedEmails = new();

        // CHECK IF EMAIL IS UNIQUE
        public Task<bool> IsEmailUnique(string email)
        {
            return Task.FromResult(!_trackedEmails.ContainsKey(email.ToLower()));
        }

        // TRACK EMAIL
        public Task TrackEmail(string email)
        {
            _trackedEmails.TryAdd(email.ToLower(), 1);
            return Task.CompletedTask;
        }
    }
} 
