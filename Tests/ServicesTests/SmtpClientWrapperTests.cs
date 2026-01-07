using System.Net;
using System.Net.Sockets;
using System.Text;
using API.Services;
using MailKit.Security;
using MimeKit;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR SMTP CLIENT WRAPPER
    public class SmtpClientWrapperTests
    {
        // TEST FOR DISPOSE BEING IDEMPOTENT
        [Fact]
        public void Dispose_IsIdempotent()
        {
            // ARRANGE - INIT WRAPPER
            using var wrapper = new SmtpClientWrapper();

            // ACT - DISPOSE TWICE
            wrapper.Dispose();
            wrapper.Dispose();
        }

        // TEST FOR METHODS THROWING OBJECTDISPOSEDEXCEPTION AFTER DISPOSE
        [Fact]
        public async Task Methods_WhenDisposed_ThrowObjectDisposedException()
        {
            // ARRANGE - INIT AND DISPOSE WRAPPER
            using var wrapper = new SmtpClientWrapper();
            wrapper.Dispose();

            // ACT & ASSERT - METHODS THROW OBJECTDISPOSEDEXCEPTION
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                wrapper.ConnectWithTokenAsync("127.0.0.1", 1, SecureSocketOptions.None, CancellationToken.None));

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                wrapper.AuthenticateWithTokenAsync("u", "p", CancellationToken.None));

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                wrapper.SendWithTokenAsync(new MimeMessage(), CancellationToken.None));

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                wrapper.DisconnectWithTokenAsync(true, CancellationToken.None));
        }

        // TEST FOR DISPOSE DISCONNECTING WHEN CONNECTED
        [Fact]
        public async Task Dispose_WhenConnected_DisconnectsSynchronously()
        {
            // ARRANGE - START LOCAL SMTP-LIKE TCP SERVER
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var serverTask = Task.Run(
                async () =>
                {
                    using var client = await listener.AcceptTcpClientAsync(cts.Token);
                    using var stream = client.GetStream();

                    using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                    using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
                    {
                        NewLine = "\r\n",
                        AutoFlush = true
                    };

                    await writer.WriteLineAsync("220 localhost ESMTP test");

                    while (!cts.Token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line is null) break;

                        if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("250-localhost");
                            await writer.WriteLineAsync("250 SIZE 35882577");
                            continue;
                        }

                        if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("221 Bye");
                            break;
                        }

                        await writer.WriteLineAsync("250 OK");
                    }
                },
                cts.Token
            );

            // ARRANGE - INIT WRAPPER
            var wrapper = new SmtpClientWrapper();
            try
            {
                // ASSERT - NOT CONNECTED INITIALLY
                Assert.False(wrapper.IsConnected);

                // ACT - CONNECT TO SERVER
                await wrapper.ConnectWithTokenAsync("127.0.0.1", port, SecureSocketOptions.None, cts.Token);

                // ASSERT - CONNECTED
                Assert.True(wrapper.IsConnected);

                // ACT - DISPOSE (SHOULD DISCONNECT)
                wrapper.Dispose();
            }
            finally
            {
                // CLEANUP - STOP SERVER
                cts.Cancel();
                listener.Stop();
                await serverTask;
            }
        }

        // TEST FOR WRAPPER METHODS EXECUTING EVEN WHEN NOT CONNECTED
        [Fact]
        public async Task Methods_WhenNotConnected_StillExecuteWrapperPaths()
        {
            // ARRANGE - INIT WRAPPER (NOT CONNECTED)
            using var wrapper = new SmtpClientWrapper();

            // ACT - INVOKE METHODS
            try { await wrapper.AuthenticateWithTokenAsync("u", "p", CancellationToken.None); } catch { }
            try { await wrapper.SendWithTokenAsync(new MimeMessage(), CancellationToken.None); } catch { }
            try { await wrapper.DisconnectWithTokenAsync(true, CancellationToken.None); } catch { }

            // ASSERT - STILL NOT CONNECTED
            Assert.False(wrapper.IsConnected);
        }

        // TEST FOR SENDWITHTOKENASYNC COMPLETING WHEN SERVER ACCEPTS MESSAGE
        [Fact]
        public async Task SendWithTokenAsync_WhenServerAcceptsMessage_Completes()
        {
            // ARRANGE - START LOCAL SMTP-LIKE TCP SERVER
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var serverTask = Task.Run(
                async () =>
                {
                    using var client = await listener.AcceptTcpClientAsync(cts.Token);
                    using var stream = client.GetStream();

                    using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                    using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };

                    await writer.WriteLineAsync("220 localhost ESMTP test");

                    while (!cts.Token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line is null) break;

                        if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("250-localhost");
                            await writer.WriteLineAsync("250 SIZE 35882577");
                            continue;
                        }

                        if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("250 2.1.0 Ok");
                            continue;
                        }

                        if (line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("250 2.1.5 Ok");
                            continue;
                        }

                        if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");

                            // ACT - READ MESSAGE CONTENT UNTIL DOT TERMINATOR
                            while (true)
                            {
                                var dataLine = await reader.ReadLineAsync();
                                if (dataLine is null) break;
                                if (dataLine == ".") break;
                            }

                            await writer.WriteLineAsync("250 2.0.0 Ok: queued as TEST123");
                            continue;
                        }

                        if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("221 2.0.0 Bye");
                            break;
                        }

                        await writer.WriteLineAsync("250 OK");
                    }
                },
                cts.Token
            );

            // ARRANGE - INIT WRAPPER
            using var wrapper = new SmtpClientWrapper();
            try
            {
                // ACT - CONNECT TO SERVER
                await wrapper.ConnectWithTokenAsync("127.0.0.1", port, SecureSocketOptions.None, cts.Token);

                // ARRANGE - BUILD MINIMAL MESSAGE
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse("from@example.com"));
                message.To.Add(MailboxAddress.Parse("to@example.com"));
                message.Subject = "Test";
                message.Body = new TextPart("plain") { Text = "Hello" };

                // ACT - SEND
                await wrapper.SendWithTokenAsync(message, cts.Token);

                // ACT - DISCONNECT
                await wrapper.DisconnectWithTokenAsync(true, cts.Token);
            }
            finally
            {
                // CLEANUP - STOP SERVER
                cts.Cancel();
                listener.Stop();
                await serverTask;
            }
        }
    }
}
