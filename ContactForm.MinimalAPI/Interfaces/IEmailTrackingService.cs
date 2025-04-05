using System;
using System.Threading.Tasks;

namespace ContactForm.MinimalAPI.Interfaces
{
    public interface IEmailTrackingService
    {
        Task<(bool IsAllowed, TimeSpan? TimeRemaining, int UsageCount)> IsEmailUnique(string email, int smtpIndex);
        Task TrackEmail(string email, int smtpIndex);
    }
} 
