using API.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace API.Services
{
    public class SmtpClientWrapper : ISmtpClientWrapper, IDisposable
    {
        private readonly SmtpClient _smtpClient;
        private bool _disposed;

        // CONSTRUCTOR
        public SmtpClientWrapper()
        {
            _smtpClient = new SmtpClient();
        }

        // CHECK IF CONNECTED
        public bool IsConnected => _smtpClient.IsConnected;

        // CONNECT TO SMTP
        public Task ConnectWithTokenAsync(string host, int port, SecureSocketOptions options, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _smtpClient.ConnectAsync(host, port, options, cancellationToken);
        }

        // AUTHENTICATE WITH PASSWORD
        public Task AuthenticateWithTokenAsync(string username, string password, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _smtpClient.AuthenticateAsync(username, password, cancellationToken);
        }

        // SEND MESSAGE
        public Task<string> SendWithTokenAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _smtpClient.SendAsync(message, cancellationToken);
        }

        // DISCONNECT FROM SMTP
        public Task DisconnectWithTokenAsync(bool quit, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _smtpClient.DisconnectAsync(quit, cancellationToken);
        }

        // THROW ERROR IF DISPOSED
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        // DISPOSE
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_smtpClient.IsConnected)
                {
                    _smtpClient.Disconnect(true);
                }
                _smtpClient.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
} 
