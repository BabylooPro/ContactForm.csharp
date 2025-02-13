using System.Threading.Tasks;

namespace ContactForm.MinimalAPI.Interfaces
{
    public interface IEmailTrackingService
    {
        Task<bool> IsEmailUnique(string email);
        Task TrackEmail(string email);
    }
} 
