namespace ImprovisedEosl.Core;

public enum BrowserShellPolicyCommandLineMode
{
    RunBrowser,
    ExportShellPolicy,
    ApplyShellPolicy,
    ResetUserSettings
}

public enum BrowserShellPolicyCommandLineError
{
    MultipleOperations,
    MissingExportPath,
    MultipleExportPaths,
    MissingApplySourcePath,
    MultipleApplySourcePaths,
    MissingApplyTargetPath,
    InvalidApplyTargetPath
}

public sealed record BrowserShellPolicyCommandLineResult(
    BrowserShellPolicyCommandLineMode Mode,
    string? ExportPath,
    string? ApplySourcePath,
    string? ApplyTargetPath,
    BrowserShellPolicyCommandLineError? Error);

public static class BrowserShellPolicyCommandLine
{
    private const string ExportShellPolicyOption = "--export-shell-policy";
    private const string ApplyShellPolicyOption = "--apply-shell-policy";
    private const string ResetUserSettingsOption = "--reset-user-settings";

    public static BrowserShellPolicyCommandLineResult Resolve(
        IReadOnlyList<string> arguments,
        string defaultShellPolicyPath)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultShellPolicyPath);

        var exportPaths = ReadOptionValues(arguments, ExportShellPolicyOption);
        var applySourcePaths = ReadOptionValues(arguments, ApplyShellPolicyOption);
        var resetRequested = arguments.Any(argument =>
            argument.Equals(ResetUserSettingsOption, StringComparison.OrdinalIgnoreCase));
        var requestedOperationCount =
            (exportPaths.Count > 0 ? 1 : 0) +
            (applySourcePaths.Count > 0 ? 1 : 0) +
            (resetRequested ? 1 : 0);
        if (requestedOperationCount == 0)
        {
            return new(
                BrowserShellPolicyCommandLineMode.RunBrowser,
                null,
                null,
                null,
                null);
        }

        if (requestedOperationCount > 1)
        {
            return Failure(BrowserShellPolicyCommandLineError.MultipleOperations);
        }

        if (exportPaths.Count > 0)
        {
            if (exportPaths.Count > 1)
            {
                return Failure(BrowserShellPolicyCommandLineError.MultipleExportPaths);
            }

            var exportPath = NormalizePathValue(exportPaths[0]);
            return exportPath is null
                ? Failure(BrowserShellPolicyCommandLineError.MissingExportPath)
                : new(
                    BrowserShellPolicyCommandLineMode.ExportShellPolicy,
                    exportPath,
                    null,
                    null,
                    null);
        }

        if (applySourcePaths.Count > 0)
        {
            if (applySourcePaths.Count > 1)
            {
                return Failure(BrowserShellPolicyCommandLineError.MultipleApplySourcePaths);
            }

            var sourcePath = NormalizePathValue(applySourcePaths[0]);
            if (sourcePath is null)
            {
                return Failure(BrowserShellPolicyCommandLineError.MissingApplySourcePath);
            }

            var target = BrowserShellPolicySourceSelection.Resolve(arguments, defaultShellPolicyPath);
            if (target.Error is not null)
            {
                return Failure(BrowserShellPolicyCommandLineError.InvalidApplyTargetPath);
            }

            return !target.IsExplicit || NormalizePathValue(target.Path) is not { } targetPath
                ? Failure(BrowserShellPolicyCommandLineError.MissingApplyTargetPath)
                : new(
                    BrowserShellPolicyCommandLineMode.ApplyShellPolicy,
                    null,
                    sourcePath,
                    targetPath,
                    null);
        }

        return new(
            BrowserShellPolicyCommandLineMode.ResetUserSettings,
            null,
            null,
            null,
            null);
    }

    private static BrowserShellPolicyCommandLineResult Failure(BrowserShellPolicyCommandLineError error) =>
        new(BrowserShellPolicyCommandLineMode.RunBrowser, null, null, null, error);

    private static IReadOnlyList<string?> ReadOptionValues(
        IReadOnlyList<string> arguments,
        string optionName)
    {
        var values = new List<string?>();
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument.Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                var value = index + 1 < arguments.Count &&
                    !arguments[index + 1].StartsWith("--", StringComparison.Ordinal)
                    ? arguments[++index]
                    : null;
                values.Add(value);
                continue;
            }

            if (argument.StartsWith(optionName + "=", StringComparison.OrdinalIgnoreCase))
            {
                values.Add(argument[(optionName.Length + 1)..]);
            }
        }

        return values;
    }

    private static string? NormalizePathValue(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
