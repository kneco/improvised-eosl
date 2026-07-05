using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImprovisedEosl.Core;

public sealed record PortableUserSettings(
    BrowserSettings Browser,
    IReadOnlyList<UserApprovedCompatibility> Approvals,
    IReadOnlyList<UserApprovedCompatibility> Denials);

public sealed record PortableUserSettingsLoadResult(
    PortableUserSettings? Settings,
    string? Diagnostic);

public sealed class PortableUserSettingsStore
{
    public const int CurrentVersion = 1;
    public const int MaxFileBytes = 1024 * 1024;
    public const int MaxDecisions = 512;

    public PortableUserSettingsLoadResult Load(string path)
    {
        try
        {
            var json = ReadBoundedFile(path);
            if (json is null)
            {
                return Failure($"portable settings file exceeds {MaxFileBytes} bytes");
            }

            var document = JsonSerializer.Deserialize<PortableDocument>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    MaxDepth = 32
                });
            if (document is null || document.Version != CurrentVersion)
            {
                return Failure("portable settings file has an unsupported version");
            }
            if (document.Extra is { Count: > 0 })
            {
                return Failure("portable settings file contains unknown properties");
            }
            if (!BrowserSettingsStore.TryParseInitialUrl(document.InitialUrl, out var initialUrl))
            {
                return Failure("portable settings file contains an invalid initial URL");
            }

            var approvalResult = Normalize(document.Approvals, "approvals");
            if (approvalResult.Diagnostic is not null)
            {
                return Failure(approvalResult.Diagnostic);
            }
            var denialResult = Normalize(document.Denials, "denials");
            if (denialResult.Diagnostic is not null)
            {
                return Failure(denialResult.Diagnostic);
            }

            var approvals = approvalResult.Decisions!;
            var denials = denialResult.Decisions!;
            if (approvals.Intersect(denials).Any())
            {
                return Failure("portable settings file contains conflicting allow and deny decisions");
            }

            return new(
                new PortableUserSettings(new BrowserSettings(initialUrl), approvals, denials),
                null);
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or JsonException or DecoderFallbackException)
        {
            return Failure($"portable settings file could not be loaded: {ex.GetType().Name}");
        }
    }

    public void Save(string path, PortableUserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Browser.InitialUrl is not null &&
            !BrowserSettingsStore.IsValidInitialUrl(settings.Browser.InitialUrl))
        {
            throw new ArgumentException("Initial URL is outside the supported boundary.", nameof(settings));
        }

        var approvals = ValidateForSave(settings.Approvals, nameof(settings.Approvals));
        var denials = ValidateForSave(settings.Denials, nameof(settings.Denials));
        if (approvals.Intersect(denials).Any())
        {
            throw new ArgumentException("A compatibility decision cannot be both allowed and denied.", nameof(settings));
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        var document = new PortableDocument
        {
            Version = CurrentVersion,
            InitialUrl = settings.Browser.InitialUrl?.OriginalString,
            Approvals = approvals.Select(ToEntry).ToArray(),
            Denials = denials.Select(ToEntry).ToArray()
        };
        var temporaryPath = path + ".tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(document, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                new UTF8Encoding(false));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static (IReadOnlyList<UserApprovedCompatibility>? Decisions, string? Diagnostic) Normalize(
        IReadOnlyList<DecisionEntry?>? entries,
        string propertyName)
    {
        var source = entries ?? Array.Empty<DecisionEntry?>();
        if (source.Count > MaxDecisions)
        {
            return (null, $"portable settings {propertyName} exceed {MaxDecisions} entries");
        }

        var decisions = new HashSet<UserApprovedCompatibility>();
        foreach (var entry in source)
        {
            if (entry is null || entry.Extra is { Count: > 0 })
            {
                return (null, $"portable settings {propertyName} contain an invalid entry");
            }
            var origin = CompatibilityOriginPolicy.NormalizeOrigin(entry.Origin ?? string.Empty);
            if (origin is null || entry.ApiName is null || !CompatibilityApi.IsKnown(entry.ApiName))
            {
                return (null, $"portable settings {propertyName} contain an invalid origin or API");
            }
            if (!decisions.Add(new UserApprovedCompatibility(origin, entry.ApiName!)))
            {
                return (null, $"portable settings {propertyName} contain a duplicate entry");
            }
        }

        return (Order(decisions), null);
    }

    private static IReadOnlyList<UserApprovedCompatibility> ValidateForSave(
        IReadOnlyList<UserApprovedCompatibility> source,
        string parameterName)
    {
        if (source.Count > MaxDecisions)
        {
            throw new ArgumentException($"Decision count exceeds {MaxDecisions}.", parameterName);
        }
        var normalized = new HashSet<UserApprovedCompatibility>();
        foreach (var decision in source)
        {
            var origin = CompatibilityOriginPolicy.NormalizeOrigin(decision.Origin);
            if (origin is null || !CompatibilityApi.IsKnown(decision.ApiName) ||
                !normalized.Add(new UserApprovedCompatibility(origin, decision.ApiName)))
            {
                throw new ArgumentException("Compatibility decisions contain invalid or duplicate entries.", parameterName);
            }
        }
        return Order(normalized);
    }

    private static IReadOnlyList<UserApprovedCompatibility> Order(
        IEnumerable<UserApprovedCompatibility> decisions) => decisions
            .OrderBy(item => item.Origin, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ApiName, StringComparer.Ordinal)
            .ToArray();

    private static DecisionEntry ToEntry(UserApprovedCompatibility decision) => new()
    {
        Origin = decision.Origin,
        ApiName = decision.ApiName
    };

    private static string? ReadBoundedFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bytes = new byte[MaxFileBytes + 1];
        var total = 0;
        while (total < bytes.Length)
        {
            var read = stream.Read(bytes, total, bytes.Length - total);
            if (read == 0) break;
            total += read;
        }
        if (total > MaxFileBytes) return null;
        var offset = total >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        return new UTF8Encoding(false, true).GetString(bytes, offset, total - offset);
    }

    private static PortableUserSettingsLoadResult Failure(string diagnostic) => new(null, diagnostic);

    private sealed class PortableDocument
    {
        public int Version { get; init; }
        public string? InitialUrl { get; init; }
        public IReadOnlyList<DecisionEntry?>? Approvals { get; init; }
        public IReadOnlyList<DecisionEntry?>? Denials { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    private sealed class DecisionEntry
    {
        public string? Origin { get; init; }
        public string? ApiName { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }
}
