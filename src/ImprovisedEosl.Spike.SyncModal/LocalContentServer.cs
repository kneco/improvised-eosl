using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ImprovisedEosl.Spike.SyncModal;

public sealed class LocalContentServer : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _acceptLoop;
    private readonly TcpListener _listener;
    private readonly Action<string> _log;

    private LocalContentServer(TcpListener listener, string rootPath, Action<string> log)
    {
        _listener = listener;
        RootPath = Path.GetFullPath(rootPath);
        _log = log;
        BaseUri = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    public Uri BaseUri { get; }

    public string RootPath { get; }

    public static LocalContentServer Start(
        string rootPath,
        Action<string> log,
        int preferredPort = 18081)
    {
        var listener = StartListener(preferredPort, log);
        var server = new LocalContentServer(listener, rootPath, log);
        log($"local content server listening: origin={server.BaseUri.GetLeftPart(UriPartial.Authority)}");
        return server;
    }

    public Uri CreateUri(string relativeUrlPath)
    {
        return new Uri(BaseUri, relativeUrlPath);
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
            // Best-effort shutdown for local content serving.
        }
        _stop.Dispose();
    }

    private static TcpListener StartListener(int preferredPort, Action<string> log)
    {
        var preferredListener = new TcpListener(IPAddress.Loopback, preferredPort);
        try
        {
            preferredListener.Start();
            return preferredListener;
        }
        catch (SocketException ex)
        {
            preferredListener.Stop();
            log($"preferred local content port {preferredPort} unavailable; falling back to dynamic port: {ex.Message}");
        }

        var dynamicListener = new TcpListener(IPAddress.Loopback, 0);
        dynamicListener.Start();
        return dynamicListener;
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
                _log("local content accept failed: " + ex.Message);
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
                if (!TryResolveRequestPath(requestPath, out var fullPath))
                {
                    _log("local content request rejected");
                    await WriteResponseAsync(stream, 404, "text/plain; charset=utf-8", "Not Found");
                    return;
                }

                var bytes = await File.ReadAllBytesAsync(fullPath, _stop.Token);
                await WriteResponseAsync(stream, 200, GetContentType(fullPath), bytes);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log("local content request failed: " + ex.Message);
            }
        }
    }

    private bool TryResolveRequestPath(string requestPath, out string fullPath)
    {
        var relativeRequestPath = requestPath
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
        fullPath = Path.GetFullPath(Path.Combine(RootPath, relativeRequestPath));
        var relativePath = Path.GetRelativePath(RootPath, fullPath);
        if (Path.IsPathRooted(relativePath) ||
            relativePath.Equals("..", StringComparison.Ordinal) ||
            relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            !File.Exists(fullPath) ||
            ContainsReparsePoint(relativePath))
        {
            fullPath = string.Empty;
            return false;
        }

        return true;
    }

    private bool ContainsReparsePoint(string relativePath)
    {
        var currentPath = RootPath;
        foreach (var part in relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, part);
            if ((File.GetAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }
        }

        return false;
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
            "X-Content-Type-Options: nosniff\r\n" +
            "Connection: close\r\n" +
            "\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
        await stream.WriteAsync(body);
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" or ".htm" => "text/html",
            ".js" => "text/javascript",
            ".css" => "text/css",
            ".json" => "application/json",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            _ => "application/octet-stream"
        };
    }
}
