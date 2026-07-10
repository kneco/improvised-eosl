using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace ImprovisedEosl.Spike.SyncModal;

public partial class NewWindowObservationWindow : Window
{
    private readonly Action<string> _log;
    private readonly TaskCompletionSource<bool> _initialNavigation = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _nonBlankNavigation = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly bool _displayScrollbars;
    private bool _suppressF1HelpForCurrentDocument;

    public NewWindowObservationWindow(
        bool displayScrollbars,
        bool displayStatus,
        Action<string> log)
    {
        _log = log;
        _displayScrollbars = displayScrollbars;
        InitializeComponent();
        StatusArea.Visibility = displayStatus ? Visibility.Visible : Visibility.Collapsed;
        PreviewKeyDown += NewWindowObservationWindow_PreviewKeyDown;
        Loaded += (_, _) => LogNativeBounds("loaded");
        Closed += (_, _) =>
        {
            LogNativeBounds("closed");
            ObservationWebView.Dispose();
        };
    }

    public CoreWebView2 CoreWebView2 => ObservationWebView.CoreWebView2;

    private void NewWindowObservationWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!BrowserHelpShortcutPolicy.IsHelpShortcut(e.Key))
        {
            return;
        }

        if (!_suppressF1HelpForCurrentDocument)
        {
            return;
        }

        e.Handled = true;
        _log("suppressed F1 help shortcut requested by current document in modeless browser window");
    }

    public async Task InitializeAsync(CoreWebView2Environment environment)
    {
        await ObservationWebView.EnsureCoreWebView2Async(environment)
            .WaitAsync(TimeSpan.FromSeconds(30));
        ObservationWebView.CoreWebView2.WindowCloseRequested += (_, _) => Close();
        ObservationWebView.CoreWebView2.SourceChanged += (_, _) =>
        {
            _suppressF1HelpForCurrentDocument = false;
            UpdateStatusText();
        };
        ObservationWebView.CoreWebView2.NavigationCompleted += async (_, args) =>
        {
            _initialNavigation.TrySetResult(args.IsSuccess);
            if (!ObservationWebView.CoreWebView2.Source.Equals(
                    "about:blank",
                    StringComparison.OrdinalIgnoreCase))
            {
                _nonBlankNavigation.TrySetResult(args.IsSuccess);
            }
            if (args.IsSuccess)
            {
                await UpdateF1HelpSuppressionForCurrentDocumentAsync();
            }
        };
        if (!_displayScrollbars)
        {
            await ObservationWebView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Emulation.setScrollbarsHidden",
                "{\"hidden\":true}");
            await ObservationWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                """
                (() => {
                  const apply = () => {
                    let style = document.getElementById("__improvisedEoslNoScrollbars");
                    if (!style) {
                      style = document.createElement("style");
                      style.id = "__improvisedEoslNoScrollbars";
                      style.textContent =
                        "html,body{overflow:hidden!important;}" +
                        "::-webkit-scrollbar{display:none!important;width:0!important;height:0!important;}";
                      (document.head || document.documentElement).appendChild(style);
                    }
                    document.documentElement.style.setProperty("overflow", "hidden", "important");
                    if (document.body) {
                      document.body.style.setProperty("overflow", "hidden", "important");
                    }
                  };
                  apply();
                  document.addEventListener("DOMContentLoaded", apply, { once: true });
                  document.addEventListener("wheel", event => event.preventDefault(),
                    { capture: true, passive: false });
                  document.addEventListener("touchmove", event => event.preventDefault(),
                    { capture: true, passive: false });
                })();
                """);
        }
        _log($"window.open child WebView2 initialized: displayScrollbars={_displayScrollbars}; " +
            $"displayStatus={StatusArea.Visibility == Visibility.Visible}");
    }

    public Task<bool> WaitForInitialNavigationAsync() =>
        _initialNavigation.Task.WaitAsync(TimeSpan.FromSeconds(30));

    public Task<bool> WaitForNonBlankNavigationAsync() =>
        _nonBlankNavigation.Task.WaitAsync(TimeSpan.FromSeconds(30));

    private void LogNativeBounds(string stage)
    {
        _log(
            "window.open observation native bounds: " +
            $"stage={stage}; left={Format(Left)}; top={Format(Top)}; " +
            $"width={Format(ActualWidth)}; height={Format(ActualHeight)}; state={WindowState}");
    }

    private static string Format(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private void UpdateStatusText()
    {
        var source = ObservationWebView.CoreWebView2?.Source;
        StatusText.Text = Uri.TryCreate(source, UriKind.Absolute, out var uri)
            ? $"{uri.Scheme}://{uri.Authority}"
            : "Status";
    }

    private async Task UpdateF1HelpSuppressionForCurrentDocumentAsync()
    {
        if (ObservationWebView.CoreWebView2 is null)
        {
            _suppressF1HelpForCurrentDocument = false;
            return;
        }

        try
        {
            var result = await ObservationWebView.CoreWebView2.ExecuteScriptAsync(
                BrowserHelpShortcutPolicy.SuppressionDetectionScript);
            _suppressF1HelpForCurrentDocument = BrowserHelpShortcutPolicy.IsSuppressionRequested(result);
            _log(
                $"F1 help suppression detection completed in modeless browser window: " +
                $"enabled={_suppressF1HelpForCurrentDocument}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException)
        {
            _suppressF1HelpForCurrentDocument = false;
            _log($"F1 help suppression detection failed in modeless browser window: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
