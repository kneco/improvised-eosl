using System.Runtime.InteropServices;
using ImprovisedEosl.Core;
using ImprovisedEosl.ModalDialog;
using Microsoft.Web.WebView2.Core;

namespace ImprovisedEosl.Spike.SyncModal;

internal sealed class TestProbeRegistration
{
    private readonly CoreWebView2 _core;
    private readonly Uri _testOrigin;
    private readonly TestProbe _probe;
    private readonly Action<string> _log;
    private bool _registered;

    public TestProbeRegistration(CoreWebView2 core, Uri testOrigin, TestProbe probe, Action<string> log)
    {
        _core = core;
        _testOrigin = testOrigin;
        _probe = probe;
        _log = log;
    }

    public void Update(Uri? targetUri)
    {
        var shouldRegister = HostOriginGuard.IsSameOrigin(targetUri, _testOrigin);
        if (shouldRegister && !_registered)
        {
            _core.AddHostObjectToScript("testProbe", _probe);
            _registered = true;
            _log("registered test-only host object for local test origin");
            return;
        }

        if (!shouldRegister && _registered)
        {
            _core.RemoveHostObjectFromScript("testProbe");
            _registered = false;
            _log("removed test-only host object before non-test navigation");
        }
    }
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public sealed class TestProbe
{
    private readonly Func<bool> _isTrustedLocalTestDocument;
    private readonly Func<bool> _isAnyAutoRun;
    private readonly Func<bool> _isCurrentOriginConfigured;
    private readonly Func<string?> _getSelectedStartupProfileId;
    private readonly Func<int> _getBrowserRecoveryCount;
    private readonly Func<int> _getParentUnresponsiveRecoveryCount;
    private readonly Action<string> _finishAutoRun;
    private readonly Action<string> _failAutoRun;
    private readonly Action<string> _log;

    internal TestProbe(
        Func<bool> isTrustedLocalTestDocument,
        Func<bool> isAnyAutoRun,
        Func<bool> isCurrentOriginConfigured,
        Func<string?> getSelectedStartupProfileId,
        Func<int> getBrowserRecoveryCount,
        Func<int> getParentUnresponsiveRecoveryCount,
        Action<string> finishAutoRun,
        Action<string> failAutoRun,
        Action<string> log)
    {
        _isTrustedLocalTestDocument = isTrustedLocalTestDocument;
        _isAnyAutoRun = isAnyAutoRun;
        _isCurrentOriginConfigured = isCurrentOriginConfigured;
        _getSelectedStartupProfileId = getSelectedStartupProfileId;
        _getBrowserRecoveryCount = getBrowserRecoveryCount;
        _getParentUnresponsiveRecoveryCount = getParentUnresponsiveRecoveryCount;
        _finishAutoRun = finishAutoRun;
        _failAutoRun = failAutoRun;
        _log = log;
    }

    public string Ping(string value)
    {
        if (!_isTrustedLocalTestDocument())
        {
            _log("blocked test-only Ping for non-test document");
            return "{\"kind\":\"blocked\",\"ok\":false}";
        }

        _log($"Ping entered on host thread {Environment.CurrentManagedThreadId}; value={value}");
        Thread.Sleep(TimeSpan.FromSeconds(3));
        _log($"Ping returning on host thread {Environment.CurrentManagedThreadId}");
        return "{\"kind\":\"ping\",\"ok\":true}";
    }

    public int GetMaxJsonPayloadBytes()
    {
        return _isTrustedLocalTestDocument() ? JsonPayloadPolicy.MaxPayloadUtf8Bytes : 0;
    }

    public int GetMaxDialogFeatureBytes()
    {
        return _isTrustedLocalTestDocument() ? DialogFeatureInputPolicy.MaxUtf8Bytes : 0;
    }

    public bool IsConfiguredShowModalDialog()
    {
        return _isTrustedLocalTestDocument() && _isCurrentOriginConfigured();
    }

    public string GetSelectedStartupProfileId()
    {
        return _isTrustedLocalTestDocument()
            ? _getSelectedStartupProfileId() ?? string.Empty
            : string.Empty;
    }

    public int GetBrowserRecoveryCount()
    {
        return _isTrustedLocalTestDocument() ? _getBrowserRecoveryCount() : 0;
    }

    public int GetParentUnresponsiveRecoveryCount()
    {
        return _isTrustedLocalTestDocument() ? _getParentUnresponsiveRecoveryCount() : 0;
    }

    public void FinishAutoRun(string summary)
    {
        if (_isAnyAutoRun() && _isTrustedLocalTestDocument())
        {
            _finishAutoRun(summary);
            return;
        }

        _log("blocked test-only FinishAutoRun for non-test document or non-auto run");
    }

    public void FailAutoRun(string summary)
    {
        if (_isAnyAutoRun() && _isTrustedLocalTestDocument())
        {
            _failAutoRun(summary);
            return;
        }

        _log("blocked test-only FailAutoRun for non-test document or non-auto run");
    }

    public void LogEvent(string message)
    {
        if (_isTrustedLocalTestDocument())
        {
            _log($"page event on host thread {Environment.CurrentManagedThreadId}: {message}");
            return;
        }

        _log("blocked test-only LogEvent for non-test document");
    }
}
