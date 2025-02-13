using MailKit;
using MailKit.Security;
using MimeKit;
using System.Threading;
using System.Threading.Tasks;

namespace ContactForm.MinimalAPI.Interfaces
{
    public interface ISmtpClientWrapper
    {
        bool IsConnected { get; }
        Task ConnectWithTokenAsync(string host, int port, SecureSocketOptions options, CancellationToken cancellationToken);
        Task AuthenticateWithTokenAsync(string username, string password, CancellationToken cancellationToken);
        Task<string> SendWithTokenAsync(MimeMessage message, CancellationToken cancellationToken);
        Task DisconnectWithTokenAsync(bool quit, CancellationToken cancellationToken);
    }
} 
