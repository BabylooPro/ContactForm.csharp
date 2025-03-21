using System.Threading.Tasks;
using ContactForm.MinimalAPI.Models;

namespace ContactForm.MinimalAPI.Interfaces
{
    // INTERFACE FOR SENDING EMAILS
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailRequest request, int smtpId, bool useTestEmail = false);
        SmtpConfig GetSmtpConfigById(int id);
        List<SmtpConfig> GetAllSmtpConfigs();
    }
}
