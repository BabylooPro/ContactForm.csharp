using System.Threading.Tasks;

namespace ContactForm.MinimalAPI.Interfaces
{
    public interface ISmtpTestService
    {
        Task TestSmtpConnections();
    }
} 
