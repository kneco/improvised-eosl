namespace ImprovisedEosl.Core;

public static class CompatibilityApi
{
    public const string ShowModalDialog = "window.showModalDialog";
    public const string WindowOpenFeatures = "window.open features";
    public const string TopLevelCloseHandoff = "window.close handoff";

    public static IReadOnlyList<string> Known { get; } =
        [ShowModalDialog, WindowOpenFeatures, TopLevelCloseHandoff];

    public static bool IsKnown(string apiName) => Known.Contains(apiName, StringComparer.Ordinal);
}
