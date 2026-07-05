using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImprovisedEosl.Core;

public sealed record BrowserSettings(Uri? InitialUrl);

public sealed record BrowserSettingsLoadResult(
    BrowserSettings Settings,
    string? Diagnostic);

public sealed class BrowserSettingsStore
{
    public const int CurrentVersion = 1;
    public const int MaxFileBytes = 1024 * 1024;
    public const int MaxUrlCharacters = 8192;

    private readonly string _path;

    public BrowserSettingsStore(string path)
    {
        _path = path;
    }

    public string Path => _path;

    public BrowserSettingsLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return new(new BrowserSettings(null), null);
        }

        try
        {
            var json = ReadBoundedFile();
            if (json is null)
            {
                return Failure($"browser settings file exceeds {MaxFileBytes} bytes");
            }

            var document = JsonSerializer.Deserialize<BrowserSettingsDocument>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    MaxDepth = 32
                });
            if (document is null || document.Version != CurrentVersion)
            {
                return Failure("browser settings file has an unsupported version");
            }
            if (document.Extra is { Count: > 0 })
            {
                return Failure("browser settings file contains unknown properties");
            }
            if (!TryParseInitialUrl(document.InitialUrl, out var initialUrl))
            {
                return Failure("browser settings file contains an invalid initial URL");
            }

            return new(new BrowserSettings(initialUrl), null);
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or JsonException or DecoderFallbackException)
        {
            return Failure($"browser settings file could not be loaded: {ex.GetType().Name}");
        }
    }

    public void Save(BrowserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.InitialUrl is not null && !IsValidInitialUrl(settings.InitialUrl))
        {
            throw new ArgumentException("Initial URL is outside the supported boundary.", nameof(settings));
        }

        var directory = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new BrowserSettingsDocument
        {
            Version = CurrentVersion,
            InitialUrl = settings.InitialUrl?.OriginalString
        };
        var temporaryPath = _path + ".tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(document),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static bool TryParseInitialUrl(string? value, out Uri? initialUrl)
    {
        initialUrl = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }
        if (value.Length > MaxUrlCharacters ||
            !Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            !IsValidInitialUrl(parsed))
        {
            return false;
        }

        initialUrl = parsed;
        return true;
    }

    public static bool IsValidInitialUrl(Uri uri) =>
        uri.IsAbsoluteUri &&
        (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) &&
        uri.UserInfo.Length == 0 &&
        uri.OriginalString.Length <= MaxUrlCharacters;

    private string? ReadBoundedFile()
    {
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bytes = new byte[MaxFileBytes + 1];
        var total = 0;
        while (total < bytes.Length)
        {
            var read = stream.Read(bytes, total, bytes.Length - total);
            if (read == 0)
            {
                break;
            }
            total += read;
        }
        if (total > MaxFileBytes)
        {
            return null;
        }

        var offset = total >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? 3
            : 0;
        return new UTF8Encoding(false, true).GetString(bytes, offset, total - offset);
    }

    private static BrowserSettingsLoadResult Failure(string diagnostic) =>
        new(new BrowserSettings(null), diagnostic);

    private sealed class BrowserSettingsDocument
    {
        public int Version { get; init; }
        public string? InitialUrl { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }
}
