namespace ImprovisedEosl.Core;

public sealed record WindowOpenFeatureApplication(
    bool DisplayScrollbars,
    bool DisplayStatus,
    string ScrollbarsSource,
    string StatusSource);

public static class WindowOpenFeatureApplicationPolicy
{
    public static WindowOpenFeatureApplication Resolve(
        WindowOpenFeatureCapture? capture,
        bool exposedScrollbars,
        bool exposedStatus)
    {
        var captureIsUsable = capture?.IsValid == true;
        return new WindowOpenFeatureApplication(
            captureIsUsable && capture!.Scrollbars.HasValue
                ? capture.Scrollbars.Value
                : true,
            captureIsUsable && capture!.Status.HasValue
                ? capture.Status.Value
                : exposedStatus,
            captureIsUsable && capture!.Scrollbars.HasValue ? "raw" : "safe-default",
            captureIsUsable && capture!.Status.HasValue ? "raw" : "webview2");
    }
}
