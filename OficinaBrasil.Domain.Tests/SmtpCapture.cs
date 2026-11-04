using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OficinaBrasil.Domain.Tests;

// Local SMTP sink used only by integration tests. It never relays a message.
internal sealed class SmtpCapture : IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource stop = new();
    private readonly List<string> messages = new();
    private readonly Task server;
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public string[] Messages { get { lock (messages) return messages.ToArray(); } }

    public SmtpCapture() { listener.Start(); server = Task.Run(Serve); }

    private async Task Serve()
    {
        try
        {
            while (!stop.IsCancellationRequested)
            {
                using var client = await listener.AcceptTcpClientAsync(stop.Token);
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII);
                using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };
                await writer.WriteLineAsync("220 localhost integration");
                while (await reader.ReadLineAsync(stop.Token) is { } line)
                {
                    if (line.StartsWith("EHLO") || line.StartsWith("HELO")) await writer.WriteLineAsync("250 localhost");
                    else if (line == "DATA")
                    {
                        await writer.WriteLineAsync("354 send data");
                        var body = new StringBuilder();
                        while (await reader.ReadLineAsync(stop.Token) is { } data && data != ".") body.AppendLine(data);
                        lock (messages) messages.Add(body.ToString());
                        await writer.WriteLineAsync("250 queued");
                    }
                    else if (line == "QUIT") { await writer.WriteLineAsync("221 bye"); break; }
                    else await writer.WriteLineAsync("250 ok");
                }
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (SocketException) when (stop.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        stop.Cancel(); listener.Stop(); await server; stop.Dispose();
    }
}
