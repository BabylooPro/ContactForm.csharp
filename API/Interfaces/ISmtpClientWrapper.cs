using MailKit.Security;
using MimeKit;

namespace API.Interfaces
{
    public interface ISmtpClientWrapper : IDisposable
    {
        bool IsConnected { get; }
        Task ConnectWithTokenAsync(string host, int port, SecureSocketOptions options, CancellationToken cancellationToken);
        Task AuthenticateWithTokenAsync(string username, string password, CancellationToken cancellationToken);
        Task<string> SendWithTokenAsync(MimeMessage message, CancellationToken cancellationToken);
        Task DisconnectWithTokenAsync(bool quit, CancellationToken cancellationToken);
    }
}
