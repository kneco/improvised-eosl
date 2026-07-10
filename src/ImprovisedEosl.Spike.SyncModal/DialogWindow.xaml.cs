using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ImprovisedEosl.Core;
using ImprovisedEosl.ModalDialog;
using Microsoft.Web.WebView2.Core;

namespace ImprovisedEosl.Spike.SyncModal;

public partial class DialogWindow : Window
{
    private const double MeasuredIeFrameWidth = 16;
    private const double MeasuredIeFrameHeight = 39;

    private readonly DialogRequest _request;
    private readonly RendererUnresponsiveTracker _rendererUnresponsiveTracker;
    private CompatibilityBroker? _nestedCompatibilityBroker;
    private bool _navigationBlocked;
    private bool _nativeCloseScheduled;
    private bool _rendererCrashScheduled;
    private bool _browserCrashScheduled;
    private bool _rendererHangScheduled;
    private bool _responsivenessProbePending;
    private bool _unresponsiveGracePending;
    private bool _rendererFailureClosing;
    private bool _closed;
    private bool _ownerWasEnabled;
    private int _ownerRestored;

    public DialogWindow(DialogRequest request)
    {
        InitializeComponent();
        _request = request;
        _rendererUnresponsiveTracker = new RendererUnresponsiveTracker(2);
        SerializedReturnValue = "undefined";
        ApplyWindowOptions(request.WindowOptions);
        PreviewKeyDown += DialogWindow_PreviewKeyDown;
        Loaded += DialogWindow_Loaded;
        Closed += DialogWindow_Closed;
    }

    public string SerializedReturnValue { get; set; }

    private void DialogWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!BrowserHelpShortcutPolicy.IsHelpShortcut(e.Key))
        {
            return;
        }

        e.Handled = true;
        _request.Log("suppressed F1 help shortcut in dialog window");
    }

    internal void AttachOwnerWindow()
    {
        _ownerWasEnabled = NativeWindowModality.AttachAndDisableOwner(
            this,
            _request.OwnerWindowHandle,
            _request.Log);
    }

    internal void RestoreOwnerWindow()
    {
        if (Interlocked.Exchange(ref _ownerRestored, 1) != 0)
        {
            return;
        }

        NativeWindowModality.RestoreOwner(
            _request.OwnerWindowHandle,
            _ownerWasEnabled,
            _request.Log);
    }

    private void ApplyWindowOptions(DialogWindowOptions options)
    {
        MinWidth = 100;
        MinHeight = 100;

        if (options.Width is not null)
        {
            Width = options.Width.Value + MeasuredIeFrameWidth;
        }

        if (options.Height is not null)
        {
            Height = options.Height.Value + MeasuredIeFrameHeight;
        }

        ClampSizeToPrimaryWorkArea();

        ResizeMode = options.ResizeMode switch
        {
            DialogResizeMode.CanResize => ResizeMode.CanResize,
            _ => ResizeMode.NoResize
        };

        var hasExplicitPosition = options.Left is not null || options.Top is not null;
        if (hasExplicitPosition)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            ApplyExplicitPosition(options);
            return;
        }

        if (options.Center)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = SystemParameters.WorkArea.Left;
        Top = SystemParameters.WorkArea.Top;
        _request.Log("dialog window options applied: center=false without explicit position; approximated to primary work-area top-left");
    }

    private void ApplyExplicitPosition(DialogWindowOptions options)
    {
        var workArea = SystemParameters.WorkArea;
        var width = Width;
        var height = Height;

        var requestedLeft = options.Left ?? workArea.Left;
        var requestedTop = options.Top ?? workArea.Top;
        var maxLeft = workArea.Right - width;
        var maxTop = workArea.Bottom - height;
        var clampedLeft = Math.Clamp(requestedLeft, workArea.Left, Math.Max(workArea.Left, maxLeft));
        var clampedTop = Math.Clamp(requestedTop, workArea.Top, Math.Max(workArea.Top, maxTop));

        Left = clampedLeft;
        Top = clampedTop;

        _request.Log(
            "dialog window options applied: " +
            $"requestedLeft={FormatDouble(requestedLeft)}; " +
            $"requestedTop={FormatDouble(requestedTop)}; " +
            $"left={FormatDouble(Left)}; " +
            $"top={FormatDouble(Top)}; " +
            $"width={FormatDouble(Width)}; " +
            $"height={FormatDouble(Height)}; " +
            $"workArea={FormatDouble(workArea.Left)},{FormatDouble(workArea.Top)},{FormatDouble(workArea.Width)}x{FormatDouble(workArea.Height)}");
    }

    private void ClampSizeToPrimaryWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Width = Math.Clamp(Width, MinWidth, Math.Max(MinWidth, workArea.Width));
        Height = Math.Clamp(Height, MinHeight, Math.Max(MinHeight, workArea.Height));
    }

    private static string FormatDouble(double value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private async void DialogWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LogNativeBounds("load");
        try
        {
            _request.Log($"child window loaded on thread {Environment.CurrentManagedThreadId}");
            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: _request.UserDataFolder);
            _request.Log($"child WebView2 environment created with UDF {environment.UserDataFolder}");

            await ChildWebView.EnsureCoreWebView2Async(environment);
            ChildWebView.CoreWebView2.ProcessFailed += ChildWebView_ProcessFailed;
            ChildWebView.CoreWebView2.NavigationStarting += ChildWebView_NavigationStarting;
            ChildWebView.CoreWebView2.WindowCloseRequested += (_, _) => Close();
            ChildWebView.CoreWebView2.NavigationCompleted += ChildWebView_NavigationCompleted;
            ChildWebView.CoreWebView2.DocumentTitleChanged += ChildWebView_DocumentTitleChanged;
            ChildWebView.CoreWebView2.WebMessageReceived += ChildWebView_WebMessageReceived;
            var nestedDialogHost = new ModalDialogHost(
                () => ChildWebView.Source,
                _request.CompatibilityPolicy,
                _request.UserDataFolder,
                _request.Log,
                new WindowInteropHelper(this).Handle,
                dialogDepth: _request.Depth);
            _nestedCompatibilityBroker = new CompatibilityBroker(
                () => ChildWebView.Source,
                _request.CompatibilityPolicy,
                OnNestedLegacyApiDetected,
                nestedDialogHost,
                (_, _) => { },
                _request.Log);
            await LegacyCompatibilityBridge.InstallAsync(
                ChildWebView.CoreWebView2,
                _nestedCompatibilityBroker);
            await ChildWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                $$"""
                Object.defineProperty(window, "dialogArguments", {
                  configurable: true,
                  value: JSON.parse({{System.Text.Json.JsonSerializer.Serialize(_request.SerializedArguments)}})
                });
                window.returnValue = undefined;
                let closePosted = false;
                function postClose(value) {
                  if (closePosted) {
                    return;
                  }
                  closePosted = true;
                  let serializedValue;
                  try {
                    serializedValue = JSON.stringify(value);
                  } catch (error) {
                    chrome.webview.postMessage({
                      kind: "close",
                      serializationError: String(error && error.message ? error.message : error)
                    });
                    return;
                  }
                  chrome.webview.postMessage({
                    kind: "close",
                    value: serializedValue
                  });
                }
                Object.defineProperty(window, "close", {
                  configurable: true,
                  writable: true,
                  value: function close() {
                    postClose(window.returnValue);
                  }
                });
                """);
            ChildWebView.Source = _request.Url;
            _request.Log($"child WebView2 navigated to {_request.Url}");
        }
        catch (Exception ex)
        {
            _request.Log("child WebView2 initialization failed: " + ex);
            SerializedReturnValue = "{\"kind\":\"child-init-failure\",\"ok\":false}";
            Close();
        }
    }

    private void DialogWindow_Closed(object? sender, EventArgs e)
    {
        _closed = true;
        RestoreOwnerWindow();
        LogNativeBounds("close");
        ChildWebView.Dispose();
        _request.Log("child WebView2 disposed");
    }

    private void OnNestedLegacyApiDetected(string origin, string apiName)
    {
        _request.Log(
            $"nested legacy API detected without an active origin grant: origin={origin}; " +
            $"api={apiName}; approval requires top-level navigation and reload");
    }

    private void LogNativeBounds(string stage)
    {
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        _request.Log(
            "child WPF bounds: " +
            $"stage={stage}; " +
            $"left={FormatDouble(Left)}; " +
            $"top={FormatDouble(Top)}; " +
            $"width={FormatDouble(ActualWidth)}; " +
            $"height={FormatDouble(ActualHeight)}; " +
            $"dpiScale={FormatDouble(dpi.DpiScaleX)}x{FormatDouble(dpi.DpiScaleY)}");
    }

    private void ChildWebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var json = e.WebMessageAsJson;
        _request.Log(
            $"child message on thread {Environment.CurrentManagedThreadId}: " +
            JsonPayloadPolicy.Summarize(json));

        if (System.Text.Encoding.UTF8.GetByteCount(json) > JsonPayloadPolicy.MaxTransportUtf8Bytes)
        {
            var validation = new JsonPayloadValidation(
                false,
                null,
                "transport-too-large",
                System.Text.Encoding.UTF8.GetByteCount(json));
            SerializedReturnValue = JsonPayloadPolicy.CreateFailureJson("return-value-rejected", validation);
            _request.Log("child message rejected: transport payload is too large");
            Close();
            return;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                _request.Log("child message ignored: transport root is not an object");
                return;
            }

            if (!root.TryGetProperty("kind", out var kind) ||
                kind.ValueKind != System.Text.Json.JsonValueKind.String ||
                kind.GetString() != "close")
            {
                return;
            }

            if (root.TryGetProperty("serializationError", out _))
            {
                var serializationFailure = new JsonPayloadValidation(false, null, "not-json-compatible", 0);
                SerializedReturnValue = JsonPayloadPolicy.CreateFailureJson(
                    "return-value-rejected",
                    serializationFailure);
                _request.Log("child return value rejected: JavaScript JSON serialization failed");
                Close();
                return;
            }

            var candidate = "undefined";
            if (root.TryGetProperty("value", out var value))
            {
                if (value.ValueKind != System.Text.Json.JsonValueKind.String)
                {
                    var invalidShape = new JsonPayloadValidation(false, null, "invalid-transport-shape", 0);
                    SerializedReturnValue = JsonPayloadPolicy.CreateFailureJson(
                        "return-value-rejected",
                        invalidShape);
                    _request.Log("child return value rejected: close message value is not a string");
                    Close();
                    return;
                }

                candidate = value.GetString() ?? "undefined";
            }
            var validation = JsonPayloadPolicy.ValidateReturnValue(candidate);
            SerializedReturnValue = validation.IsValid
                ? validation.Json!
                : JsonPayloadPolicy.CreateFailureJson("return-value-rejected", validation);
            if (!validation.IsValid)
            {
                _request.Log(
                    $"child return value rejected: reason={validation.ErrorCode}; utf8Bytes={validation.Utf8Bytes}");
            }
            Close();
        }
        catch (System.Text.Json.JsonException ex)
        {
            _request.Log($"child message rejected: invalid transport JSON; error={ex.Message}");
        }
    }

    private void ChildWebView_DocumentTitleChanged(object? sender, object e)
    {
        var documentTitle = ChildWebView.CoreWebView2?.DocumentTitle?.Trim();
        if (string.IsNullOrWhiteSpace(documentTitle))
        {
            Title = "Dialog";
            return;
        }

        Title = documentTitle.Length <= 256 ? documentTitle : documentTitle[..256];
    }

    private void ChildWebView_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        _request.Log(
            $"child WebView2 process failed: kind={e.ProcessFailedKind}; reason={e.Reason}; " +
            $"exitCode={e.ExitCode}; description={e.ProcessDescription}");

        if (_closed)
        {
            return;
        }

        if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
        {
            var shouldClose = _rendererUnresponsiveTracker.Observe();
            _request.Log(
                "child renderer unresponsive observation: " +
                $"count={_rendererUnresponsiveTracker.Count}; " +
                $"threshold={_rendererUnresponsiveTracker.Threshold}; close={shouldClose}");
            if (shouldClose)
            {
                CompleteRendererUnresponsiveFailure("notification-threshold");
            }
            else
            {
                ProbeRendererResponsiveness();
                StartRendererUnresponsiveGracePeriod();
            }
            return;
        }

        if (e.ProcessFailedKind is not (
                CoreWebView2ProcessFailedKind.BrowserProcessExited or
                CoreWebView2ProcessFailedKind.RenderProcessExited))
        {
            return;
        }

        SerializedReturnValue = System.Text.Json.JsonSerializer.Serialize(new
        {
            kind = "child-process-failure",
            ok = false,
            processFailedKind = e.ProcessFailedKind.ToString(),
            reason = e.Reason.ToString(),
            exitCode = e.ExitCode
        });
        Dispatcher.BeginInvoke(Close);
    }

    private void ChildWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_navigationBlocked)
        {
            return;
        }

        if (e.IsSuccess && e.HttpStatusCode < 400)
        {
            _request.Log($"child navigation completed: status={e.HttpStatusCode}");
            ScheduleNativeCloseForAutomaticValidation();
            ScheduleRendererCrashForAutomaticValidation();
            ScheduleBrowserCrashForAutomaticValidation();
            ScheduleRendererHangForAutomaticValidation();
            return;
        }

        SerializedReturnValue =
            $"{{\"kind\":\"navigation-failure\",\"ok\":false,\"webErrorStatus\":\"{e.WebErrorStatus}\",\"httpStatusCode\":{e.HttpStatusCode}}}";
        _request.Log(
            $"child navigation failed: webErrorStatus={e.WebErrorStatus}; httpStatusCode={e.HttpStatusCode}");
        Close();
    }

    private async void ScheduleNativeCloseForAutomaticValidation()
    {
        if (_nativeCloseScheduled || _request.NativeCloseDelay is not { } delay)
        {
            return;
        }

        _nativeCloseScheduled = true;
        _request.Log($"native child close scheduled for automatic validation after {delay.TotalMilliseconds:n0}ms");
        await Task.Delay(delay);
        if (!_closed)
        {
            _request.Log("closing child through native WPF Close for automatic validation");
            Close();
        }
    }

    private async void ScheduleRendererCrashForAutomaticValidation()
    {
        if (_rendererCrashScheduled || !_request.CrashRendererAfterNavigation)
        {
            return;
        }

        _rendererCrashScheduled = true;
        _request.Log("child renderer crash scheduled for automatic validation");
        await Task.Delay(250);
        if (_closed)
        {
            return;
        }

        try
        {
            await ChildWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.crash", "{}");
        }
        catch (Exception ex)
        {
            _request.Log($"child renderer crash command ended: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async void ScheduleBrowserCrashForAutomaticValidation()
    {
        if (_browserCrashScheduled || !_request.CrashBrowserAfterNavigation)
        {
            return;
        }

        _browserCrashScheduled = true;
        _request.Log("shared browser process crash scheduled for automatic validation");
        await Task.Delay(500);
        if (_closed)
        {
            return;
        }

        try
        {
            var browserProcessId = checked((int)ChildWebView.CoreWebView2.BrowserProcessId);
            _request.Log($"terminating shared browser process for automatic validation: pid={browserProcessId}");
            System.Diagnostics.Process.GetProcessById(browserProcessId).Kill();
        }
        catch (Exception ex)
        {
            _request.Log($"shared browser process crash command failed: {ex.GetType().Name}: {ex.Message}");
            SerializedReturnValue = "{\"kind\":\"browser-crash-injection-failure\",\"ok\":false}";
            Close();
        }
    }

    private async void ScheduleRendererHangForAutomaticValidation()
    {
        if (_rendererHangScheduled || !_request.HangRendererAfterNavigation)
        {
            return;
        }

        _rendererHangScheduled = true;
        _request.Log("child renderer hang scheduled for automatic validation");
        await Task.Delay(250);
        if (_closed)
        {
            return;
        }

        try
        {
            _ = SendNativeInputForRendererHangValidation();
            await ChildWebView.ExecuteScriptAsync("while (true) {}");
        }
        catch (Exception ex)
        {
            _request.Log($"child renderer hang command ended: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task SendNativeInputForRendererHangValidation()
    {
        await Task.Delay(1_000);
        if (_closed)
        {
            return;
        }

        try
        {
            NativeTestInput.ClickWindowCenter(new WindowInteropHelper(this).Handle, _request.Log);
        }
        catch (Exception ex)
        {
            _request.Log($"native unresponsive-renderer test input failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async void ProbeRendererResponsiveness()
    {
        if (_responsivenessProbePending)
        {
            return;
        }

        _responsivenessProbePending = true;
        try
        {
            await ChildWebView.ExecuteScriptAsync("void 0");
            if (!_closed)
            {
                _rendererUnresponsiveTracker.MarkResponsive();
                _request.Log("child renderer responsiveness probe completed; observation count reset");
            }
        }
        catch (Exception ex)
        {
            _request.Log($"child renderer responsiveness probe ended: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _responsivenessProbePending = false;
        }
    }

    private async void StartRendererUnresponsiveGracePeriod()
    {
        if (_unresponsiveGracePending)
        {
            return;
        }

        _unresponsiveGracePending = true;
        await Task.Delay(TimeSpan.FromSeconds(5));
        _unresponsiveGracePending = false;
        if (!_closed && _rendererUnresponsiveTracker.Count > 0)
        {
            CompleteRendererUnresponsiveFailure("responsiveness-probe-timeout");
        }
    }

    private void CompleteRendererUnresponsiveFailure(string action)
    {
        if (_closed || _rendererFailureClosing)
        {
            return;
        }

        _rendererFailureClosing = true;
        SerializedReturnValue = System.Text.Json.JsonSerializer.Serialize(new
        {
            kind = "child-process-failure",
            ok = false,
            processFailedKind = CoreWebView2ProcessFailedKind.RenderProcessUnresponsive.ToString(),
            reason = CoreWebView2ProcessFailedReason.Unresponsive.ToString(),
            notificationCount = _rendererUnresponsiveTracker.Count,
            action
        });
        Dispatcher.BeginInvoke(Close);
    }

    private void ChildWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var validation = DialogNavigationPolicy.Validate(e.Uri);
        if (validation.IsValid)
        {
            return;
        }

        e.Cancel = true;
        _navigationBlocked = true;
        SerializedReturnValue = DialogNavigationPolicy.CreateFailureJson(
            validation,
            "blocked-dialog-navigation");
        _request.Log(
            $"child navigation blocked: url={DialogNavigationPolicy.FormatForLog(e.Uri)}; reason={validation.ErrorCode}");
        Dispatcher.BeginInvoke(Close);
    }
}
