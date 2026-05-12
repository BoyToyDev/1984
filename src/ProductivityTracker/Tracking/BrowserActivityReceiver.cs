using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ProductivityTracker.Data;

namespace ProductivityTracker.Tracking;

internal sealed class BrowserActivityReceiver : IDisposable
{
    private readonly TrackerDatabase _database;
    private readonly AppSettings _settings;
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellation;
    private DateTimeOffset? _lastPluginSeenAt;

    public BrowserActivityReceiver(TrackerDatabase database, AppSettings settings)
    {
        _database = database;
        _settings = settings;
    }

    public DateTimeOffset? LastPluginSeenAt => _lastPluginSeenAt;

    public void Start()
    {
        _cancellation = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, _settings.BrowserReceiverPort);
        _listener.Start();
        _ = Task.Run(() => ListenAsync(_cancellation.Token));
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                continue;
            }

            _ = Task.Run(() => HandleAsync(client), cancellationToken);
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using var disposableClient = client;
        await using var stream = client.GetStream();

        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var requestLine = await reader.ReadLineAsync();
            if (requestLine is null)
            {
                return;
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                var separator = line.IndexOf(':');
                if (separator > 0)
                {
                    headers[line[..separator].Trim()] = line[(separator + 1)..].Trim();
                }
            }

            var parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            var method = parts.Length > 0 ? parts[0] : string.Empty;
            var path = parts.Length > 1 ? parts[1] : string.Empty;

            if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(stream, 204, string.Empty);
                return;
            }

            if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
                || (path != "/browser-activity" && path != "/browser-heartbeat"))
            {
                await WriteResponseAsync(stream, 404, string.Empty);
                return;
            }

            var length = headers.TryGetValue("Content-Length", out var contentLengthText)
                && int.TryParse(contentLengthText, out var contentLength)
                    ? contentLength
                    : 0;

            var body = new char[length];
            var read = 0;
            while (read < length)
            {
                var chunk = await reader.ReadAsync(body, read, length - read);
                if (chunk == 0)
                {
                    break;
                }

                read += chunk;
            }

            await using var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body, 0, read));
            if (path == "/browser-heartbeat")
            {
                _lastPluginSeenAt = DateTimeOffset.Now;
                await WriteResponseAsync(stream, 204, string.Empty);
                return;
            }

            var payload = await JsonSerializer.DeserializeAsync<BrowserActivityPayload>(
                bodyStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                CancellationToken.None);

            if (payload is null || string.IsNullOrWhiteSpace(payload.Url))
            {
                await WriteResponseAsync(stream, 400, string.Empty);
                return;
            }

            var endedAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.EndedAtUnixMs).ToLocalTime();
            var startedAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.StartedAtUnixMs).ToLocalTime();
            if (endedAt <= startedAt)
            {
                endedAt = startedAt.AddSeconds(1);
            }

            _database.InsertBrowserActivity(new BrowserActivityRecord(
                startedAt,
                endedAt,
                Environment.UserName,
                "plugin",
                payload.Browser ?? "unknown",
                payload.Url,
                GetDomain(payload.Url),
                payload.Title ?? string.Empty));
            _lastPluginSeenAt = DateTimeOffset.Now;

            await WriteResponseAsync(stream, 204, string.Empty);
        }
        catch
        {
            try
            {
                await WriteResponseAsync(stream, 500, string.Empty);
            }
            catch
            {
            }
        }
    }

    private static async Task WriteResponseAsync(NetworkStream stream, int statusCode, string body)
    {
        var reason = statusCode switch
        {
            204 => "No Content",
            400 => "Bad Request",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "OK"
        };
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var header = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "Access-Control-Allow-Headers: content-type\r\n" +
            "Access-Control-Allow-Methods: POST, OPTIONS\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n\r\n");

        await stream.WriteAsync(header);
        if (bodyBytes.Length > 0)
        {
            await stream.WriteAsync(bodyBytes);
        }
    }

    private static string GetDomain(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host
            : string.Empty;
    }

    public void Dispose()
    {
        _cancellation?.Cancel();
        _listener?.Stop();
        _cancellation?.Dispose();
    }

    private sealed class BrowserActivityPayload
    {
        public string? Browser { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Title { get; set; }
        public long StartedAtUnixMs { get; set; }
        public long EndedAtUnixMs { get; set; }
    }
}
