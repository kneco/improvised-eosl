using ImprovisedEosl.Core;
using ImprovisedEosl.ModalDialog;

namespace ImprovisedEosl.Spike.SyncModal;

internal sealed class ModalDialogHost
{
    private const int MaxDialogDepth = 4;

    private readonly Func<Uri?> _getCurrentDocument;
    private readonly CompatibilityOriginPolicy _compatibilityPolicy;
    private readonly string _userDataFolder;
    private readonly Action<string> _log;
    private readonly TimeSpan? _nativeCloseDelay;
    private readonly int _dialogDepth;
    private readonly bool _crashRendererAfterNavigation;
    private readonly bool _crashBrowserAfterNavigation;
    private readonly bool _hangRendererAfterNavigation;
    private readonly nint _ownerWindowHandle;

    public ModalDialogHost(
        Func<Uri?> getCurrentDocument,
        CompatibilityOriginPolicy compatibilityPolicy,
        string userDataFolder,
        Action<string> log,
        nint ownerWindowHandle,
        TimeSpan? nativeCloseDelay = null,
        int dialogDepth = 0,
        bool crashRendererAfterNavigation = false,
        bool crashBrowserAfterNavigation = false,
        bool hangRendererAfterNavigation = false)
    {
        _getCurrentDocument = getCurrentDocument;
        _compatibilityPolicy = compatibilityPolicy;
        _userDataFolder = userDataFolder;
        _log = log;
        _ownerWindowHandle = ownerWindowHandle;
        _nativeCloseDelay = nativeCloseDelay;
        _dialogDepth = dialogDepth;
        _crashRendererAfterNavigation = crashRendererAfterNavigation;
        _crashBrowserAfterNavigation = crashBrowserAfterNavigation;
        _hangRendererAfterNavigation = hangRendererAfterNavigation;
    }

    public string ShowDialog(string callerOrigin, string url, string? serializedArguments, string? features)
    {
        var currentDocument = _getCurrentDocument();
        var actualOrigin = currentDocument is null
            ? null
            : CompatibilityOriginPolicy.GetOrigin(currentDocument);
        if (!HostOriginGuard.IsClaimedOriginCurrent(currentDocument, callerOrigin))
        {
            _log(
                $"blocked ShowDialog for mismatched origin: claimed={callerOrigin}; " +
                $"actual={actualOrigin ?? "(none)"}");
            return "undefined";
        }

        if (actualOrigin is null ||
            !_compatibilityPolicy.IsAllowed(actualOrigin, CompatibilityApi.ShowModalDialog))
        {
            _log($"blocked ShowDialog for non-allowed origin: {actualOrigin}");
            return "undefined";
        }

        if (_dialogDepth >= MaxDialogDepth)
        {
            _log(
                $"blocked nested ShowDialog at depth limit: depth={_dialogDepth}; " +
                $"maxDepth={MaxDialogDepth}; origin={actualOrigin}");
            return
                $"{{\"kind\":\"nested-dialog-depth-exceeded\",\"ok\":false,\"maxDepth\":{MaxDialogDepth}}}";
        }

        _log(
            $"ShowDialog entered on host thread {Environment.CurrentManagedThreadId}; depth={_dialogDepth + 1}; " +
            $"origin={actualOrigin}; " +
            $"url={DialogNavigationPolicy.FormatForLog(url)}; " +
            $"args={JsonPayloadPolicy.Summarize(serializedArguments)}; " +
            $"features={JsonPayloadPolicy.Summarize(features)}");

        var featureInputValidation = DialogFeatureInputPolicy.Validate(features);
        if (!featureInputValidation.IsValid)
        {
            _log(
                $"dialog features rejected: reason={featureInputValidation.ErrorCode}; " +
                $"utf8Bytes={featureInputValidation.Utf8Bytes}; entries={featureInputValidation.EntryCount}");
            return DialogFeatureInputPolicy.CreateFailureJson(featureInputValidation);
        }

        var navigationValidation = DialogNavigationPolicy.Validate(url);
        if (!navigationValidation.IsValid)
        {
            _log($"dialog URL rejected: reason={navigationValidation.ErrorCode}");
            return DialogNavigationPolicy.CreateFailureJson(navigationValidation);
        }

        var argumentValidation = JsonPayloadPolicy.ValidateArguments(serializedArguments);
        if (!argumentValidation.IsValid)
        {
            _log(
                $"dialog arguments rejected: reason={argumentValidation.ErrorCode}; " +
                $"utf8Bytes={argumentValidation.Utf8Bytes}");
            return JsonPayloadPolicy.CreateFailureJson("invalid-arguments", argumentValidation);
        }

        var dialogFeatures = DialogFeatureParser.Parse(featureInputValidation.Value);
        var windowOptions = DialogFeatureApplicationPolicy.Calculate(dialogFeatures);
        var request = new DialogRequest(
            navigationValidation.Uri!,
            argumentValidation.Json!,
            featureInputValidation.Value,
            windowOptions,
            _userDataFolder,
            _compatibilityPolicy,
            _dialogDepth + 1,
            _ownerWindowHandle,
            _log,
            _nativeCloseDelay,
            _crashRendererAfterNavigation,
            _crashBrowserAfterNavigation,
            _hangRendererAfterNavigation);

        _log(
            "calculated dialog window options; " +
            $"width={FormatOption(windowOptions.Width)}; " +
            $"height={FormatOption(windowOptions.Height)}; " +
            $"left={FormatOption(windowOptions.Left)}; " +
            $"top={FormatOption(windowOptions.Top)}; " +
            $"center={windowOptions.Center}; " +
            $"resizeMode={windowOptions.ResizeMode}; " +
            $"policyStatus={windowOptions.PolicyStatus}; " +
            "appliedToWpf=true");
        foreach (var diagnostic in windowOptions.Diagnostics)
        {
            _log(
                "dialog feature diagnostic: " +
                $"feature={diagnostic.Feature}; " +
                $"kind={diagnostic.Kind}; " +
                $"raw={diagnostic.RawValue ?? "(none)"}; " +
                $"applied={diagnostic.AppliedValue ?? "(none)"}; " +
                $"reason={diagnostic.Reason}");
            if (diagnostic.Kind == DialogFeatureDiagnosticKind.Unsupported)
            {
                _log(
                    "unsupported dialog feature ignored: " +
                    $"{diagnostic.Feature}={diagnostic.RawValue ?? "(none)"}; " +
                    $"reason={diagnostic.Reason}");
            }
        }

        var result = DialogStaRunner.Run(request, dialogFeatures.Timeout);
        _log(
            $"ShowDialog returning on host thread {Environment.CurrentManagedThreadId}; " +
            $"result={JsonPayloadPolicy.Summarize(result)}");

        return result;
    }

    private static string FormatOption(double? value)
    {
        return value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(none)";
    }
}
