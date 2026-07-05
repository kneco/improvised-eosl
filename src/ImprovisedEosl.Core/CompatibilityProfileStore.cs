using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImprovisedEosl.Core;

public sealed record CompatibilityProfile(
    string Id,
    string DisplayName,
    Uri StartUrl,
    IReadOnlyList<string> AllowedOrigins,
    bool ShowModalDialog);

public sealed record ConfiguredCompatibility(string ProfileId, string Origin, string ApiName);

public sealed record CompatibilityProfileLoadResult(
    IReadOnlyList<CompatibilityProfile> Profiles,
    IReadOnlyList<ConfiguredCompatibility> Compatibility,
    IReadOnlyList<string> Diagnostics);

public sealed class CompatibilityProfileStore
{
    public const int MaxFileBytes = 1024 * 1024;
    public const int MaxProfiles = 128;
    public const int MaxOriginsPerProfile = 128;

    private const int CurrentVersion = 1;
    private readonly string _path;

    public CompatibilityProfileStore(string path)
    {
        _path = path;
    }

    public string Path => _path;

    public CompatibilityProfileLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return Empty();
        }

        try
        {
            var fileRead = ReadBoundedFile();
            if (fileRead.Diagnostic is not null)
            {
                return Failure(fileRead.Diagnostic);
            }

            var document = JsonSerializer.Deserialize<ProfileDocument>(
                fileRead.Json!,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    MaxDepth = 32
                });
            if (document is null || document.Version != CurrentVersion)
            {
                return Failure($"Unsupported compatibility profile version in {_path}.");
            }

            if (document.Extra is { Count: > 0 })
            {
                return Failure($"Unknown root properties in compatibility profile file {_path}.");
            }

            var sourceProfiles = document.Profiles ?? Array.Empty<ProfileEntry?>();
            if (sourceProfiles.Count > MaxProfiles)
            {
                return Failure($"Compatibility profile count exceeds {MaxProfiles} in {_path}.");
            }

            var profiles = new List<CompatibilityProfile>();
            var compatibility = new List<ConfiguredCompatibility>();
            var diagnostics = new List<string>();
            var profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < sourceProfiles.Count; index++)
            {
                var entry = sourceProfiles[index];
                if (!TryNormalize(entry, profileIds, out var profile, out var diagnostic))
                {
                    diagnostics.Add($"Discarded compatibility profile at index {index}: {diagnostic}");
                    continue;
                }

                profiles.Add(profile!);
                if (profile!.ShowModalDialog)
                {
                    compatibility.AddRange(profile.AllowedOrigins.Select(origin =>
                        new ConfiguredCompatibility(
                            profile.Id,
                            origin,
                            CompatibilityApi.ShowModalDialog)));
                }
            }

            return new CompatibilityProfileLoadResult(profiles, compatibility, diagnostics);
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or JsonException or DecoderFallbackException)
        {
            return Failure($"Could not load compatibility profiles from {_path}: {ex.Message}");
        }
    }

    private (string? Json, string? Diagnostic) ReadBoundedFile()
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
            return (null, $"Compatibility profile file exceeds {MaxFileBytes} bytes: {_path}");
        }

        var offset = total >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? 3
            : 0;
        return (new UTF8Encoding(false, true).GetString(bytes, offset, total - offset), null);
    }

    private static bool TryNormalize(
        ProfileEntry? entry,
        ISet<string> profileIds,
        out CompatibilityProfile? profile,
        out string diagnostic)
    {
        profile = null;
        if (entry is null)
        {
            diagnostic = "entry is null.";
            return false;
        }

        if (entry.Extra is { Count: > 0 } || entry.Compatibility?.Extra is { Count: > 0 })
        {
            diagnostic = "entry contains unknown properties.";
            return false;
        }

        var id = entry.Id?.Trim();
        if (string.IsNullOrEmpty(id) || id.Length > 128)
        {
            diagnostic = "id is missing or too long.";
            return false;
        }

        if (profileIds.Contains(id))
        {
            diagnostic = $"duplicate id '{id}'.";
            return false;
        }

        var displayName = string.IsNullOrWhiteSpace(entry.DisplayName)
            ? id
            : entry.DisplayName.Trim();
        if (displayName.Length > 256)
        {
            diagnostic = $"displayName is too long for '{id}'.";
            return false;
        }

        if (!TryParseHttpUrl(entry.StartUrl, out var startUrl))
        {
            diagnostic = $"startUrl is not a safe absolute HTTP(S) URL for '{id}'.";
            return false;
        }

        var sourceOrigins = entry.AllowedOrigins ?? Array.Empty<string>();
        if (sourceOrigins.Count == 0 || sourceOrigins.Count > MaxOriginsPerProfile)
        {
            diagnostic = $"allowedOrigins count is outside 1..{MaxOriginsPerProfile} for '{id}'.";
            return false;
        }

        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceOrigin in sourceOrigins)
        {
            if (!TryNormalizeOriginOnly(sourceOrigin, out var origin))
            {
                diagnostic = $"allowedOrigins contains a non-origin value for '{id}'.";
                return false;
            }

            origins.Add(origin!);
        }

        profile = new CompatibilityProfile(
            id,
            displayName,
            startUrl!,
            origins.OrderBy(origin => origin, StringComparer.OrdinalIgnoreCase).ToArray(),
            entry.Compatibility?.ShowModalDialog == true);
        profileIds.Add(id);
        diagnostic = string.Empty;
        return true;
    }

    private static bool TryParseHttpUrl(string? value, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            parsed.UserInfo.Length > 0 ||
            CompatibilityOriginPolicy.NormalizeOrigin(parsed.ToString()) is null)
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool TryNormalizeOriginOnly(string? value, out string? origin)
    {
        origin = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.UserInfo.Length > 0 ||
            uri.Query.Length > 0 ||
            uri.Fragment.Length > 0 ||
            (uri.AbsolutePath.Length > 0 && uri.AbsolutePath != "/"))
        {
            return false;
        }

        origin = CompatibilityOriginPolicy.NormalizeOrigin(uri.ToString());
        return origin is not null;
    }

    private static CompatibilityProfileLoadResult Empty()
    {
        return new CompatibilityProfileLoadResult(
            Array.Empty<CompatibilityProfile>(),
            Array.Empty<ConfiguredCompatibility>(),
            Array.Empty<string>());
    }

    private static CompatibilityProfileLoadResult Failure(string diagnostic)
    {
        return new CompatibilityProfileLoadResult(
            Array.Empty<CompatibilityProfile>(),
            Array.Empty<ConfiguredCompatibility>(),
            new[] { diagnostic });
    }

    private sealed class ProfileDocument
    {
        public int Version { get; init; }

        public IReadOnlyList<ProfileEntry?>? Profiles { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    private sealed class ProfileEntry
    {
        public string? Id { get; init; }

        public string? DisplayName { get; init; }

        public string? StartUrl { get; init; }

        public IReadOnlyList<string>? AllowedOrigins { get; init; }

        public CompatibilityEntry? Compatibility { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    private sealed class CompatibilityEntry
    {
        public bool ShowModalDialog { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }
}
