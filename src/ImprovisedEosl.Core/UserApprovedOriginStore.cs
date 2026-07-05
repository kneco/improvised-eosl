using System.IO;
using System.Text.Json;

namespace ImprovisedEosl.Core;

public sealed record UserApprovedCompatibility(string Origin, string ApiName);

public sealed record UserApprovedOriginLoadResult(
    IReadOnlyList<UserApprovedCompatibility> Approvals,
    IReadOnlyList<UserApprovedCompatibility> Denials,
    string? Diagnostic);

public sealed class UserApprovedOriginStore
{
    private const int CurrentVersion = 2;
    private readonly string _path;

    public UserApprovedOriginStore(string path)
    {
        _path = path;
    }

    public string Path => _path;

    public UserApprovedOriginLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return new UserApprovedOriginLoadResult([], [], null);
        }

        try
        {
            var document = JsonSerializer.Deserialize<StoreDocument>(File.ReadAllText(_path));
            if (document is null || document.Version is not (1 or CurrentVersion))
            {
                return new UserApprovedOriginLoadResult(
                    [], [],
                    $"Unsupported approval-store version in {_path}.");
            }

            var storedApprovals = document.Approvals ?? Array.Empty<UserApprovedCompatibility>();
            var approvals = Normalize(storedApprovals);
            var storedDenials = document.Denials ?? Array.Empty<UserApprovedCompatibility>();
            var denials = Normalize(storedDenials).Except(approvals).ToArray();
            var diagnostic = approvals.Count == storedApprovals.Count && denials.Length == storedDenials.Count
                ? null
                : $"Discarded invalid or unsupported approval entries from {_path}.";
            return new UserApprovedOriginLoadResult(approvals, denials, diagnostic);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new UserApprovedOriginLoadResult(
                [], [],
                $"Could not load user-approved origins from {_path}: {ex.Message}");
        }
    }

    public void Save(
        IEnumerable<UserApprovedCompatibility> approvals,
        IEnumerable<UserApprovedCompatibility>? denials = null)
    {
        var normalized = Normalize(approvals);
        var directory = System.IO.Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("Approval-store path must have a directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(
                new StoreDocument
                {
                    Version = CurrentVersion,
                    Approvals = normalized,
                    Denials = Normalize(denials ?? []).Except(normalized).ToArray()
                },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(temporaryPath, json);
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

    private static IReadOnlyList<UserApprovedCompatibility> Normalize(
        IEnumerable<UserApprovedCompatibility> approvals)
    {
        return approvals
            .Where(approval => CompatibilityApi.IsKnown(approval.ApiName))
            .Select(approval => new UserApprovedCompatibility(
                CompatibilityOriginPolicy.NormalizeOrigin(approval.Origin) ?? string.Empty,
                approval.ApiName))
            .Where(approval => approval.Origin.Length > 0)
            .Distinct()
            .OrderBy(approval => approval.Origin, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed class StoreDocument
    {
        public int Version { get; init; }

        public IReadOnlyList<UserApprovedCompatibility>? Approvals { get; init; }

        public IReadOnlyList<UserApprovedCompatibility>? Denials { get; init; }
    }
}
