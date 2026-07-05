using System.Runtime.InteropServices;
using ImprovisedEosl.Core;

namespace ImprovisedEosl.Spike.SyncModal;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public sealed class CompatibilityBroker
{
    private readonly Func<Uri?> _getCurrentDocument;
    private readonly CompatibilityOriginPolicy _compatibilityPolicy;
    private readonly Action<string, string> _legacyApiDetected;
    private readonly ModalDialogHost _dialogHost;
    private readonly Action<string> _log;
    private readonly Action<string, WindowOpenFeatureCapture> _windowOpenFeatureCaptured;
    private readonly Func<string, string, string, string, string>? _stageTopLevelHandoff;
    private readonly Action<string, string, string>? _releaseTopLevelHandoff;

    internal CompatibilityBroker(
        Func<Uri?> getCurrentDocument,
        CompatibilityOriginPolicy compatibilityPolicy,
        Action<string, string> legacyApiDetected,
        ModalDialogHost dialogHost,
        Action<string, WindowOpenFeatureCapture> windowOpenFeatureCaptured,
        Action<string> log,
        Func<string, string, string, string, string>? stageTopLevelHandoff = null,
        Action<string, string, string>? releaseTopLevelHandoff = null)
    {
        _getCurrentDocument = getCurrentDocument;
        _compatibilityPolicy = compatibilityPolicy;
        _legacyApiDetected = legacyApiDetected;
        _dialogHost = dialogHost;
        _windowOpenFeatureCaptured = windowOpenFeatureCaptured;
        _log = log;
        _stageTopLevelHandoff = stageTopLevelHandoff;
        _releaseTopLevelHandoff = releaseTopLevelHandoff;
    }

    public bool IsShowModalDialogAllowed(string origin)
    {
        return IsCurrentDocumentOrigin(origin) &&
            _compatibilityPolicy.IsAllowed(origin, CompatibilityApi.ShowModalDialog);
    }

    public bool IsWindowOpenFeaturesAllowed(string origin)
    {
        return IsCurrentDocumentOrigin(origin) &&
            _compatibilityPolicy.IsAllowed(origin, CompatibilityApi.WindowOpenFeatures);
    }

    public bool IsTopLevelCloseHandoffAllowed(string origin)
    {
        return IsCurrentDocumentOrigin(origin) &&
            _compatibilityPolicy.IsAllowed(origin, CompatibilityApi.TopLevelCloseHandoff);
    }

    public bool IsTopLevelCloseHandoffDenied(string origin)
    {
        return IsCurrentDocumentOrigin(origin) &&
            _compatibilityPolicy.IsDenied(origin, CompatibilityApi.TopLevelCloseHandoff);
    }

    public string StageTopLevelCloseHandoff(
        string origin,
        string url,
        string? name,
        string? features)
    {
        if (!IsCurrentDocumentOrigin(origin) || _stageTopLevelHandoff is null)
        {
            return string.Empty;
        }

        return _stageTopLevelHandoff(origin, url, name ?? string.Empty, features ?? string.Empty);
    }

    public void ReleaseTopLevelCloseHandoff(string origin, string token, string reason)
    {
        if (IsCurrentDocumentOrigin(origin))
        {
            _releaseTopLevelHandoff?.Invoke(origin, token, reason);
        }
    }

    public void DetectLegacyApi(string origin, string apiName)
    {
        if (!IsCurrentDocumentOrigin(origin))
        {
            var currentDocument = _getCurrentDocument();
            var actualOrigin = currentDocument is null
                ? "(none)"
                : CompatibilityOriginPolicy.GetOrigin(currentDocument);
            _log(
                $"blocked legacy API detection with mismatched origin: claimed={origin}; " +
                $"actual={actualOrigin}");
            return;
        }

        if (_compatibilityPolicy.IsDenied(origin, apiName))
        {
            _log($"legacy API call denied without prompting: origin={origin}; api={apiName}");
            return;
        }

        _legacyApiDetected(origin, apiName);
    }

    public string ShowDialog(string callerOrigin, string url, string? serializedArguments, string? features)
    {
        return _dialogHost.ShowDialog(callerOrigin, url, serializedArguments, features);
    }

    public void CaptureWindowOpenFeatures(string callerOrigin, string? name, string? features)
    {
        if (!IsCurrentDocumentOrigin(callerOrigin))
        {
            _log("blocked window.open feature capture with mismatched origin");
            return;
        }

        if (!_compatibilityPolicy.IsAllowed(callerOrigin, CompatibilityApi.WindowOpenFeatures))
        {
            return;
        }

        _windowOpenFeatureCaptured(name ?? string.Empty, WindowOpenFeatureCapturePolicy.Parse(features));
    }

    private bool IsCurrentDocumentOrigin(string? claimedOrigin)
    {
        return HostOriginGuard.IsClaimedOriginCurrent(_getCurrentDocument(), claimedOrigin);
    }
}
