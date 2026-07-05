using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ImprovisedEosl.Spike.SyncModal;

public sealed class LocalTestServer : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _acceptLoop;
    private readonly TcpListener _listener;
    private readonly string _root;
    private readonly Action<string> _log;

    private LocalTestServer(TcpListener listener, string root, Action<string> log)
    {
        _listener = listener;
        _root = root;
        _log = log;
        BaseUri = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    public Uri BaseUri { get; }

    public static LocalTestServer Start(string root, Action<string> log, int? preferredPort = null)
    {
        var listener = StartListener(preferredPort, log);
        var server = new LocalTestServer(listener, root, log);
        log($"local HTTP test server listening on {server.BaseUri}");
        return server;
    }

    private static TcpListener StartListener(int? preferredPort, Action<string> log)
    {
        if (preferredPort is not null)
        {
            var preferredListener = new TcpListener(IPAddress.Loopback, preferredPort.Value);
            try
            {
                preferredListener.Start();
                return preferredListener;
            }
            catch (SocketException ex)
            {
                preferredListener.Stop();
                log($"preferred local HTTP port {preferredPort.Value} unavailable; falling back to dynamic port: {ex.Message}");
            }
        }

        var dynamicListener = new TcpListener(IPAddress.Loopback, 0);
        dynamicListener.Start();
        return dynamicListener;
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        try
        {
            _acceptLoop.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Best-effort shutdown for a spike utility.
        }
        _stop.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                _ = Task.Run(() => HandleClientAsync(client), _stop.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                _log("local HTTP accept failed: " + ex.Message);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        await using var stream = client.GetStream();
        using (client)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                var requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(requestLine))
                {
                    return;
                }

                while (!string.IsNullOrEmpty(await reader.ReadLineAsync()))
                {
                }

                var parts = requestLine.Split(' ');
                if (parts.Length < 2 || parts[0] != "GET")
                {
                    await WriteResponseAsync(stream, 405, "text/plain; charset=utf-8", "Method Not Allowed");
                    return;
                }

                var requestPath = Uri.UnescapeDataString(parts[1].Split('?', 2)[0]);
                if (requestPath == "/")
                {
                    requestPath = "/home.html";
                }

                requestPath = requestPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(Path.Combine(_root, requestPath));
                if (!fullPath.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(fullPath))
                {
                    _log($"local HTTP 404: {requestPath}");
                    await WriteResponseAsync(stream, 404, "text/plain; charset=utf-8", "Not Found");
                    return;
                }

                var bytes = await File.ReadAllBytesAsync(fullPath, _stop.Token);
                await WriteResponseAsync(stream, 200, GetContentType(fullPath), bytes);
            }
            catch (Exception ex)
            {
                _log("local HTTP request failed: " + ex.Message);
            }
        }
    }

    private static async Task WriteResponseAsync(Stream stream, int status, string contentType, string body)
    {
        await WriteResponseAsync(stream, status, contentType, Encoding.UTF8.GetBytes(body));
    }

    private static async Task WriteResponseAsync(Stream stream, int status, string contentType, byte[] body)
    {
        var reason = status switch
        {
            200 => "OK",
            404 => "Not Found",
            405 => "Method Not Allowed",
            _ => "Error"
        };
        var header =
            $"HTTP/1.1 {status} {reason}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Cache-Control: no-store\r\n" +
            "Connection: close\r\n" +
            "\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
        await stream.WriteAsync(body);
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            _ => "application/octet-stream"
        };
    }
}
