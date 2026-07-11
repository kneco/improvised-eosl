using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImprovisedEosl.Core;

public sealed record BrowserShellPolicy(
    bool ToolbarPrimaryToolbarHidden,
    bool ToolbarAddressEntryHidden,
    bool ToolbarHistoryCommandHidden,
    bool ToolbarReloadCommandHidden,
    bool ToolbarGoCommandHidden,
    bool ToolbarSettingsCommandHidden,
    bool ToolbarDiagnosticsCommandHidden,
    bool KeyboardHistoryCommandDisabled,
    bool KeyboardReloadCommandDisabled)
{
    public static BrowserShellPolicy Standard { get; } = new(
        ToolbarPrimaryToolbarHidden: false,
        ToolbarAddressEntryHidden: false,
        ToolbarHistoryCommandHidden: false,
        ToolbarReloadCommandHidden: false,
        ToolbarGoCommandHidden: false,
        ToolbarSettingsCommandHidden: false,
        ToolbarDiagnosticsCommandHidden: false,
        KeyboardHistoryCommandDisabled: false,
        KeyboardReloadCommandDisabled: false);
}

public sealed record BrowserShellPolicyLoadResult(
    BrowserShellPolicy Policy,
    IReadOnlyList<string> Diagnostics);

public sealed class BrowserShellPolicyStore
{
    public const int CurrentVersion = 1;
    public const int MaxFileBytes = 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 32
    };

    private readonly string _path;

    public BrowserShellPolicyStore(string path)
    {
        _path = path;
    }

    public string Path => _path;

    public BrowserShellPolicyLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return new(BrowserShellPolicy.Standard, Array.Empty<string>());
        }

        try
        {
            var fileRead = ReadBoundedFile();
            if (fileRead.Diagnostic is not null)
            {
                return Failure(fileRead.Diagnostic);
            }

            var document = JsonSerializer.Deserialize<PolicyDocument>(fileRead.Json!, JsonOptions);
            if (document is null || document.Version != CurrentVersion)
            {
                return Failure($"Unsupported browser shell policy version in {_path}.");
            }

            if (document.Extra is { Count: > 0 })
            {
                return Failure($"Unknown root properties in browser shell policy file {_path}.");
            }

            if (document.BrowserShell is null)
            {
                return Failure($"Missing browserShell section in browser shell policy file {_path}.");
            }

            if (document.BrowserShell.Extra is { Count: > 0 })
            {
                return Failure($"Unknown browserShell properties in browser shell policy file {_path}.");
            }

            return new(ToPolicy(document.BrowserShell), Array.Empty<string>());
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or JsonException or DecoderFallbackException)
        {
            return Failure($"Could not load browser shell policy from {_path}: {ex.GetType().Name}");
        }
    }

    public static string CreateStandardPolicyJson()
    {
        var document = new PolicyDocument
        {
            Version = CurrentVersion,
            BrowserShell = new BrowserShellSection()
        };

        return JsonSerializer.Serialize(
            document,
            new JsonSerializerOptions(JsonOptions)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
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
            return (null, $"Browser shell policy file exceeds {MaxFileBytes} bytes: {_path}");
        }

        var offset = total >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? 3
            : 0;
        return (new UTF8Encoding(false, true).GetString(bytes, offset, total - offset), null);
    }

    private static BrowserShellPolicy ToPolicy(BrowserShellSection section) => new(
        section.ToolbarPrimaryToolbarHidden,
        section.ToolbarAddressEntryHidden,
        section.ToolbarHistoryCommandHidden,
        section.ToolbarReloadCommandHidden,
        section.ToolbarGoCommandHidden,
        section.ToolbarSettingsCommandHidden,
        section.ToolbarDiagnosticsCommandHidden,
        section.KeyboardHistoryCommandDisabled,
        section.KeyboardReloadCommandDisabled);

    private static BrowserShellPolicyLoadResult Failure(string diagnostic) =>
        new(BrowserShellPolicy.Standard, new[] { diagnostic });

    private sealed class PolicyDocument
    {
        public int Version { get; init; }

        public BrowserShellSection? BrowserShell { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    private sealed class BrowserShellSection
    {
        [JsonPropertyName("toolbar-primary-toolbar-hidden")]
        public bool ToolbarPrimaryToolbarHidden { get; init; }

        [JsonPropertyName("toolbar-address-entry-hidden")]
        public bool ToolbarAddressEntryHidden { get; init; }

        [JsonPropertyName("toolbar-history-command-hidden")]
        public bool ToolbarHistoryCommandHidden { get; init; }

        [JsonPropertyName("toolbar-reload-command-hidden")]
        public bool ToolbarReloadCommandHidden { get; init; }

        [JsonPropertyName("toolbar-go-command-hidden")]
        public bool ToolbarGoCommandHidden { get; init; }

        [JsonPropertyName("toolbar-settings-command-hidden")]
        public bool ToolbarSettingsCommandHidden { get; init; }

        [JsonPropertyName("toolbar-diagnostics-command-hidden")]
        public bool ToolbarDiagnosticsCommandHidden { get; init; }

        [JsonPropertyName("keyboard-history-command-disabled")]
        public bool KeyboardHistoryCommandDisabled { get; init; }

        [JsonPropertyName("keyboard-reload-command-disabled")]
        public bool KeyboardReloadCommandDisabled { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }
}
