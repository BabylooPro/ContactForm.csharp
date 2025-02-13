using ContactForm.MinimalAPI.Models;
using System.Threading.Tasks;

namespace ContactForm.MinimalAPI.Interfaces
{
    // INTERFACE FOR SENDING EMAILS
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailRequest request, int smtpId);
        SmtpConfig GetSmtpConfigById(int id);
        List<SmtpConfig> GetAllSmtpConfigs();
    }
}
