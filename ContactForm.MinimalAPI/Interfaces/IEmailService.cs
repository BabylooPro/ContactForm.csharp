using ContactForm.MinimalAPI.Models;
using System.Threading.Tasks;

namespace ContactForm.MinimalAPI.Services
{
    // INTERFACE FOR SENDING EMAILS
    public interface IEmailService
    {
        Task<(bool IsSuccess, IEnumerable<string> Errors)> SendEmailAsync(EmailRequest request); // METHOD FOR SENDING EMAIL
    }
}