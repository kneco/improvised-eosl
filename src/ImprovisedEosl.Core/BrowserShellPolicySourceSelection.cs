namespace ImprovisedEosl.Core;

public enum BrowserShellPolicySourceSelectionError
{
    MissingPath,
    MultipleSelections
}

public sealed record BrowserShellPolicySourceSelectionResult(
    string? Path,
    bool IsExplicit,
    BrowserShellPolicySourceSelectionError? Error);

public static class BrowserShellPolicySourceSelection
{
    private const string ShellPolicyOption = "--shell-policy";

    public static BrowserShellPolicySourceSelectionResult Resolve(
        IReadOnlyList<string> arguments,
        string defaultPath)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultPath);

        var requestedPaths = new List<string?>();
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument.Equals(ShellPolicyOption, StringComparison.OrdinalIgnoreCase))
            {
                var value = index + 1 < arguments.Count &&
                    !arguments[index + 1].StartsWith("--", StringComparison.Ordinal)
                    ? arguments[++index]
                    : null;
                requestedPaths.Add(value);
                continue;
            }

            if (argument.StartsWith(ShellPolicyOption + "=", StringComparison.OrdinalIgnoreCase))
            {
                requestedPaths.Add(argument[(ShellPolicyOption.Length + 1)..]);
            }
        }

        if (requestedPaths.Count == 0)
        {
            return new BrowserShellPolicySourceSelectionResult(defaultPath, false, null);
        }

        if (requestedPaths.Count > 1)
        {
            return new BrowserShellPolicySourceSelectionResult(null, true, BrowserShellPolicySourceSelectionError.MultipleSelections);
        }

        var requestedPath = requestedPaths[0]?.Trim();
        return string.IsNullOrEmpty(requestedPath)
            ? new BrowserShellPolicySourceSelectionResult(null, true, BrowserShellPolicySourceSelectionError.MissingPath)
            : new BrowserShellPolicySourceSelectionResult(requestedPath, true, null);
    }
}
