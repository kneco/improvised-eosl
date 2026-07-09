using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Interop;
using ImprovisedEosl.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ImprovisedEosl.Spike.SyncModal;

public partial class MainWindow : Window
{
    private readonly bool _autoRun;
    private readonly bool _sessionAutoRun;
    private readonly bool _failureAutoRun;
    private readonly bool _featureAutoRun;
    private readonly bool _payloadAutoRun;
    private readonly bool _originGuardAutoRun;
    private readonly bool _navigationAutoRun;
    private readonly bool _profileAutoRun;
    private readonly bool _startupProfileAutoRun;
    private readonly bool _nativeCloseAutoRun;
    private readonly bool _nativeXUiRun;
    private readonly bool _nestedAutoRun;
    private readonly bool _processFailureAutoRun;
    private readonly bool _browserProcessFailureAutoRun;
    private readonly bool _unresponsiveAutoRun;
    private readonly bool _parentUnresponsiveAutoRun;
    private readonly bool _windowOpenObservation;
    private readonly bool _windowOpenObservationAuto;
    private readonly bool _browserSettingsAutoRun;
    private readonly bool _topLevelCloseAutoRun;
    private readonly bool _topLevelCloseManualRun;
    private readonly bool _topLevelCloseNormalPopupAutoRun;
    private readonly bool _revokedPermissionAutoRun;
    private readonly bool _showDiagnosticsAtStartup;
    private readonly CompatibilityOriginPolicy _compatibilityPolicy;
    private readonly UserApprovedOriginStore _approvalStore;
    private readonly HashSet<UserApprovedCompatibility> _userApprovedCompatibility;
    private readonly HashSet<UserApprovedCompatibility> _userDeniedCompatibility;
    private readonly StartupProfileSelectionResult _startupProfileSelection;
    private BrowserSettings _browserSettings;
    private readonly BrowserSettingsStore? _browserSettingsStore;
    private readonly RollingFileLog _fileLog;
    private readonly MainWindowPlacementStore? _windowPlacementStore;
    private readonly string _userDataFolder;
    private readonly RendererUnresponsiveTracker _parentUnresponsiveTracker = new(2);
    private readonly string? _browserSettingsAutoFolder;
    private readonly Uri? _browserSettingsAutoExpectedUri;
    private LocalTestServer? _testServer;
    private LocalContentServer? _localContentServer;
    private string? _localContentOrigin;
    private TestProbeRegistration? _testProbeRegistration;
    private CoreWebView2Environment? _parentEnvironment;
    private TaskCompletionSource<CoreWebView2BrowserProcessExitedEventArgs>? _browserProcessExited;
    private bool _diagnosticsVisible;
    private bool _recoveringParentWebView;
    private bool _parentResponsivenessProbePending;
    private bool _parentUnresponsiveGracePending;
    private bool _parentHangScheduled;
    private bool _parentUnresponsiveReloading;
    private bool _closing;
    private int _browserRecoveryCount;
    private int _parentUnresponsiveRecoveryCount;
    private bool _windowOpenObservationTriggered;
    private bool _browserSettingsAutoCompleted;
    private bool _topLevelCloseRequestHandling;
    private bool _revokedPermissionAutoTriggered;
    private PendingTopLevelHandoff? _pendingTopLevelHandoff;
    private string? _pendingTopLevelHandoffToken;
    private int _windowOpenObservationNextCase;
    private const int WindowOpenObservationCaseCount = 21;
    private readonly object _windowOpenCaptureLock = new();
    private readonly List<(string Name, WindowOpenFeatureCapture Capture)> _windowOpenCaptures = [];
    private readonly HashSet<NewWindowObservationWindow> _modelessWindows = [];
    private WindowState _lastNonMinimizedWindowState = WindowState.Normal;
    private string _compatibilityStatusDetail = string.Empty;

    private sealed record PendingTopLevelHandoff(
        Uri TargetUri,
        string ParentOrigin,
        string WindowName,
        WindowOpenFeatureApplication Features,
        bool DisplayToolbar,
        double? Width,
        double? Height);

    public MainWindow()
    {
        InitializeComponent();
        Title = MainWindowTitlePolicy.Format(null);
        SetCompatibilityStatusPresentation(
            CompatibilityStatusPresentationPolicy.CreateOperational(CompatibilityOperationalStatus.Initializing));
        var args = Environment.GetCommandLineArgs();
        _autoRun = args.Any(arg => arg.Equals("--auto", StringComparison.OrdinalIgnoreCase));
        _sessionAutoRun = args.Any(arg => arg.Equals("--session-auto", StringComparison.OrdinalIgnoreCase));
        _failureAutoRun = args.Any(arg => arg.Equals("--failure-auto", StringComparison.OrdinalIgnoreCase));
        _featureAutoRun = args.Any(arg => arg.Equals("--feature-auto", StringComparison.OrdinalIgnoreCase));
        _payloadAutoRun = args.Any(arg => arg.Equals("--payload-auto", StringComparison.OrdinalIgnoreCase));
        _originGuardAutoRun = args.Any(arg => arg.Equals("--origin-guard-auto", StringComparison.OrdinalIgnoreCase));
        _navigationAutoRun = args.Any(arg => arg.Equals("--navigation-auto", StringComparison.OrdinalIgnoreCase));
        _profileAutoRun = args.Any(arg => arg.Equals("--profile-auto", StringComparison.OrdinalIgnoreCase));
        _startupProfileAutoRun = args.Any(arg => arg.Equals("--startup-profile-auto", StringComparison.OrdinalIgnoreCase));
        _nativeCloseAutoRun = args.Any(arg => arg.Equals("--native-close-auto", StringComparison.OrdinalIgnoreCase));
        _nativeXUiRun = args.Any(arg => arg.Equals("--native-x-ui", StringComparison.OrdinalIgnoreCase));
        _nestedAutoRun = args.Any(arg => arg.Equals("--nested-auto", StringComparison.OrdinalIgnoreCase));
        _processFailureAutoRun = args.Any(arg => arg.Equals("--process-failure-auto", StringComparison.OrdinalIgnoreCase));
        _browserProcessFailureAutoRun = args.Any(arg => arg.Equals("--browser-process-failure-auto", StringComparison.OrdinalIgnoreCase));
        _unresponsiveAutoRun = args.Any(arg => arg.Equals("--unresponsive-auto", StringComparison.OrdinalIgnoreCase));
        _parentUnresponsiveAutoRun = args.Any(arg => arg.Equals("--parent-unresponsive-auto", StringComparison.OrdinalIgnoreCase));
        _windowOpenObservation = args.Any(arg => arg.Equals("--window-open-observation", StringComparison.OrdinalIgnoreCase));
        _windowOpenObservationAuto = args.Any(arg => arg.Equals("--window-open-observation-auto", StringComparison.OrdinalIgnoreCase));
        _browserSettingsAutoRun = args.Any(arg => arg.Equals("--browser-settings-auto", StringComparison.OrdinalIgnoreCase));
        _topLevelCloseAutoRun = args.Any(arg => arg.Equals("--top-level-close-auto", StringComparison.OrdinalIgnoreCase));
        _topLevelCloseManualRun = args.Any(arg => arg.Equals("--top-level-close-manual", StringComparison.OrdinalIgnoreCase));
        _topLevelCloseNormalPopupAutoRun = args.Any(arg => arg.Equals("--top-level-close-popup-auto", StringComparison.OrdinalIgnoreCase));
        _revokedPermissionAutoRun = args.Any(arg => arg.Equals("--revoked-permission-auto", StringComparison.OrdinalIgnoreCase));
        if (_revokedPermissionAutoRun)
        {
            Environment.ExitCode = 1;
        }
        if (_topLevelCloseNormalPopupAutoRun)
        {
            Environment.ExitCode = 1;
        }
        if (_topLevelCloseAutoRun)
        {
            Environment.ExitCode = 1;
        }
        _showDiagnosticsAtStartup = args.Any(arg => arg.Equals("--show-diagnostics", StringComparison.OrdinalIgnoreCase));
        var applicationSettingsText = UiText.Get(UiText.ApplicationSettingsButton);
        ApplicationSettingsText.Text = applicationSettingsText;
        ApplicationSettingsButton.ToolTip = applicationSettingsText;
        AutomationProperties.SetName(ApplicationSettingsButton, applicationSettingsText);
        SetDiagnosticsVisibility(_showDiagnosticsAtStartup);
        _fileLog = new RollingFileLog(
            Path.Combine(AppContext.BaseDirectory, "artifacts", "sync-modal-poc.log"));
        AppendLog($"diagnostic panel initialized: visible={_diagnosticsVisible}; fileLogging=true");
        var applicationDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImprovisedEosl",
            "SyncModalSpike");
        if (_browserSettingsAutoRun)
        {
            _browserSettingsAutoFolder = Path.Combine(
                Path.GetTempPath(),
                "ImprovisedEosl",
                "SyncModalSpike",
                $"BrowserSettingsAuto-{Environment.ProcessId}");
            _browserSettingsAutoExpectedUri = new Uri(
                "http://127.0.0.1:18080/parent.html?browserSettingsAuto=1");
            _browserSettingsStore = new BrowserSettingsStore(
                Path.Combine(_browserSettingsAutoFolder, "browser-settings.json"));
            _browserSettingsStore.Save(new BrowserSettings(_browserSettingsAutoExpectedUri));
            var browserSettingsLoad = _browserSettingsStore.Load();
            _browserSettings = browserSettingsLoad.Settings;
            if (browserSettingsLoad.Diagnostic is not null)
            {
                throw new InvalidOperationException(
                    "Browser settings automatic fixture could not be loaded: " + browserSettingsLoad.Diagnostic);
            }
        }
        else if (IsAnyAutoRun())
        {
            _browserSettings = new BrowserSettings(null);
            _browserSettingsStore = null;
        }
        else
        {
            _browserSettingsStore = new BrowserSettingsStore(
                Path.Combine(applicationDataFolder, "browser-settings.json"));
            var browserSettingsLoad = _browserSettingsStore.Load();
            _browserSettings = browserSettingsLoad.Settings;
            if (browserSettingsLoad.Diagnostic is not null)
            {
                AppendLog("browser settings load warning: " + browserSettingsLoad.Diagnostic);
            }
            else
            {
                AppendLog(
                    "loaded browser settings: " +
                    $"initialUrl={FormatOptionalUrlForLog(_browserSettings.InitialUrl)}; " +
                    $"path={_browserSettingsStore.Path}");
            }
        }
        if (!IsAnyAutoRun())
        {
            _windowPlacementStore = new MainWindowPlacementStore(
                Path.Combine(applicationDataFolder, "main-window-placement.json"));
            RestoreMainWindowPlacement();
        }
        _userDataFolder = IsAnyAutoRun()
            ? Path.Combine(
                Path.GetTempPath(),
                "ImprovisedEosl",
                "SyncModalSpike",
                $"WebView2UserData-{Environment.ProcessId}")
            : Path.Combine(applicationDataFolder, "WebView2UserData");
        _approvalStore = new UserApprovedOriginStore(
            Path.Combine(applicationDataFolder, "user-approved-compatibility.json"));
        var approvalLoad = _approvalStore.Load();
        var profileFileName = _profileAutoRun || _startupProfileAutoRun
            ? "compatibility-profiles.auto.json"
            : "compatibility-profiles.json";
        var profileStore = new CompatibilityProfileStore(
            Path.Combine(AppContext.BaseDirectory, "config", profileFileName));
        var profileLoad = profileStore.Load();
        _startupProfileSelection = StartupProfileSelection.Resolve(args, profileLoad.Profiles);
        _userApprovedCompatibility = new HashSet<UserApprovedCompatibility>(approvalLoad.Approvals);
        _userDeniedCompatibility = new HashSet<UserApprovedCompatibility>(approvalLoad.Denials);
        _compatibilityPolicy = new CompatibilityOriginPolicy(
            approvalLoad.Approvals,
            profileLoad.Compatibility,
            approvalLoad.Denials);
        if (approvalLoad.Diagnostic is not null)
        {
            AppendLog("approval store load warning: " + approvalLoad.Diagnostic);
        }
        else
        {
            AppendLog(
                $"loaded user compatibility decisions: approvals={approvalLoad.Approvals.Count}; " +
                $"denials={approvalLoad.Denials.Count}; path={_approvalStore.Path}");
        }
        foreach (var diagnostic in profileLoad.Diagnostics)
        {
            AppendLog("compatibility profile load warning: " + diagnostic);
        }
        AppendLog(
            $"loaded configured compatibility profiles: profiles={profileLoad.Profiles.Count}; " +
            $"grants={profileLoad.Compatibility.Count}; path={profileStore.Path}");
        if (_startupProfileSelection.Profile is not null)
        {
            AppendLog(
                $"selected startup compatibility profile: id={_startupProfileSelection.Profile.Id}; " +
                $"startUrl={DialogNavigationPolicy.FormatForLog(_startupProfileSelection.Profile.StartUrl.ToString())}");
        }
        Loaded += MainWindow_Loaded;
        StateChanged += MainWindow_StateChanged;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
    }

    private void RestoreMainWindowPlacement()
    {
        var load = _windowPlacementStore!.Load();
        if (load.Diagnostic is not null)
        {
            AppendLog("main window placement load warning: " + load.Diagnostic);
        }

        if (load.Placement is not { } placement)
        {
            return;
        }

        var virtualDesktop = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        var savedBounds = new Rect(placement.Left, placement.Top, placement.Width, placement.Height);
        var visibleBounds = Rect.Intersect(virtualDesktop, savedBounds);
        if (visibleBounds.Width < 64 || visibleBounds.Height < 64)
        {
            AppendLog("main window placement ignored: saved bounds are outside the current virtual desktop");
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = placement.Left;
        Top = placement.Top;
        Width = placement.Width;
        Height = placement.Height;
        if (placement.Maximized)
        {
            _lastNonMinimizedWindowState = WindowState.Maximized;
            WindowState = WindowState.Maximized;
        }

        AppendLog(
            $"main window placement restored: left={placement.Left:F0}; top={placement.Top:F0}; " +
            $"width={placement.Width:F0}; height={placement.Height:F0}; maximized={placement.Maximized}");
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            _lastNonMinimizedWindowState = WindowState;
        }

        ParentWebView.Visibility = WindowState == WindowState.Minimized
            ? Visibility.Hidden
            : Visibility.Visible;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _closing = true;

        if (_windowPlacementStore is not null)
        {
            SaveMainWindowPlacement();
        }

        if (WindowState == WindowState.Minimized)
        {
            AppendLog("main window minimized-close preparation started");
            Visibility = Visibility.Hidden;
            WindowState = WindowState.Normal;
            UpdateLayout();
            AppendLog("main window minimized-close preparation finished");
        }
    }

    private void SaveMainWindowPlacement()
    {
        var bounds = RestoreBounds;
        var placement = new MainWindowPlacement(
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            MainWindowPlacementStore.ShouldReopenMaximized(
                ToDisplayState(WindowState),
                ToDisplayState(_lastNonMinimizedWindowState)));
        if (!MainWindowPlacementStore.IsValid(placement))
        {
            AppendLog("main window placement not saved: WPF restore bounds are invalid");
            return;
        }

        try
        {
            _windowPlacementStore!.Save(placement);
            AppendLog(
                $"main window placement saved: left={placement.Left:F0}; top={placement.Top:F0}; " +
                $"width={placement.Width:F0}; height={placement.Height:F0}; maximized={placement.Maximized}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppendLog("main window placement save warning: " + ex.GetType().Name);
        }
    }

    private static MainWindowDisplayState ToDisplayState(WindowState state) => state switch
    {
        WindowState.Minimized => MainWindowDisplayState.Minimized,
        WindowState.Maximized => MainWindowDisplayState.Maximized,
        _ => MainWindowDisplayState.Normal
    };

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            AppendLog($"main window STA thread: {Environment.CurrentManagedThreadId}");
            var startupProfileError = GetStartupProfileErrorMessage();
            if (startupProfileError is not null)
            {
                AppendLog(
                    $"startup profile selection failed: error={_startupProfileSelection.Error}; " +
                    $"requestedId={_startupProfileSelection.RequestedId ?? "(none)"}; autoRun={IsAnyAutoRun()}");
                if (IsAnyAutoRun())
                {
                    FailAutoRun(startupProfileError);
                    return;
                }

                MessageBox.Show(
                    this,
                    startupProfileError,
                    UiText.Get(UiText.StartupProfileErrorTitle),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
                return;
            }

            _testServer = LocalTestServer.Start(
                Path.Combine(AppContext.BaseDirectory, "pages"),
                AppendLog,
                preferredPort: 18080);
            if ((_profileAutoRun || _startupProfileAutoRun || _browserSettingsAutoRun) &&
                _testServer.BaseUri.Port != 18080)
            {
                throw new InvalidOperationException(
                    "Configured-profile automatic validation requires local test port 18080.");
            }

            Directory.CreateDirectory(_userDataFolder);
            await InitializeParentWebViewAsync(ParentWebView);

            var parentUri = new UriBuilder(new Uri(_testServer.BaseUri, "parent.html"));
            if (RequiresRuntimeTestAllowance())
            {
                _compatibilityPolicy.Allow(
                    CompatibilityOriginPolicy.GetOrigin(parentUri.Uri),
                    CompatibilityApi.ShowModalDialog);
            }
            if (_windowOpenObservation || _windowOpenObservationAuto)
            {
                _compatibilityPolicy.Allow(
                    CompatibilityOriginPolicy.GetOrigin(parentUri.Uri),
                    CompatibilityApi.WindowOpenFeatures);
            }
            if (_topLevelCloseAutoRun)
            {
                _compatibilityPolicy.Allow(
                    CompatibilityOriginPolicy.GetOrigin(parentUri.Uri),
                    CompatibilityApi.WindowOpenFeatures);
                _compatibilityPolicy.Allow(
                    CompatibilityOriginPolicy.GetOrigin(parentUri.Uri),
                    CompatibilityApi.ShowModalDialog);
                _compatibilityPolicy.Allow(
                    CompatibilityOriginPolicy.GetOrigin(parentUri.Uri),
                    CompatibilityApi.TopLevelCloseHandoff);
            }
            if (_topLevelCloseNormalPopupAutoRun)
            {
                _compatibilityPolicy.Allow(
                    CompatibilityOriginPolicy.GetOrigin(parentUri.Uri),
                    CompatibilityApi.WindowOpenFeatures);
            }
            if (_autoRun)
            {
                parentUri.Query = "auto=1";
            }
            else if (_sessionAutoRun)
            {
                parentUri.Query = "sessionAuto=1";
            }
            else if (_failureAutoRun)
            {
                parentUri.Query = "failureAuto=1";
            }
            else if (_featureAutoRun)
            {
                parentUri.Query = "featureAuto=1";
            }
            else if (_payloadAutoRun)
            {
                parentUri.Query = "payloadAuto=1";
            }
            else if (_originGuardAutoRun)
            {
                parentUri.Query = "originGuardAuto=1";
            }
            else if (_navigationAutoRun)
            {
                parentUri.Query = "navigationAuto=1";
            }
            else if (_profileAutoRun)
            {
                parentUri.Query = "profileAuto=1";
            }
            else if (_nativeCloseAutoRun)
            {
                parentUri.Query = "nativeCloseAuto=1";
            }
            else if (_nativeXUiRun)
            {
                parentUri.Query = "nativeCloseAuto=1";
            }
            else if (_nestedAutoRun)
            {
                parentUri.Query = "nestedAuto=1";
            }
            else if (_processFailureAutoRun)
            {
                parentUri.Query = "processFailureAuto=1";
            }
            else if (_browserProcessFailureAutoRun)
            {
                parentUri.Query = "browserProcessFailureAuto=1";
            }
            else if (_unresponsiveAutoRun)
            {
                parentUri.Query = "unresponsiveAuto=1";
            }
            else if (_parentUnresponsiveAutoRun)
            {
                parentUri.Query = "parentUnresponsiveAuto=1";
            }
            else if (_revokedPermissionAutoRun)
            {
                parentUri.Query = "revokedPermissionAuto=1";
            }

            var automaticStartupUri = _topLevelCloseNormalPopupAutoRun
                ? new Uri(_testServer.BaseUri, "top-level-close-normal-popup.html")
                : _topLevelCloseAutoRun || _topLevelCloseManualRun
                ? new Uri(_testServer.BaseUri, "top-level-close-dummy.html")
                : _windowOpenObservation || _windowOpenObservationAuto
                ? new Uri(_testServer.BaseUri, "window-open-reference-ie.html")
                : IsAnyAutoRun() && !_startupProfileAutoRun && !_browserSettingsAutoRun
                    ? parentUri.Uri
                    : null;
            var startupDecision = StartupNavigationPolicy.Resolve(
                automaticStartupUri,
                _startupProfileSelection.Profile,
                _browserSettings,
                new Uri(_testServer.BaseUri, "home.html"));
            var startupUri = startupDecision.Uri;
            if (_startupProfileAutoRun)
            {
                var startupUriBuilder = new UriBuilder(startupUri)
                {
                    Query = "startupProfileAuto=1"
                };
                startupUri = startupUriBuilder.Uri;
            }
            var startupSource = _windowOpenObservation || _windowOpenObservationAuto
                ? "window-open-observation"
                : startupDecision.Source switch
                {
                    StartupNavigationSource.AutomaticValidation => "automatic-test",
                    StartupNavigationSource.SelectedProfile => "profile",
                    StartupNavigationSource.UserSettings => "user-settings",
                    _ => "home"
                };
            AppendLog(
                $"startup navigation selected: source={startupSource}; " +
                $"url={DialogNavigationPolicy.FormatForLog(startupUri.ToString())}");
            ParentWebView.Source = startupUri;
            AddressBox.Text = startupUri.ToString();
            UpdateCompatibilityStatus(startupUri);
            AppendLog($"parent WebView2 ready; shared UDF: {_userDataFolder}");
        }
        catch (Exception ex)
        {
            AppendLog("startup failed: " + ex);
            if (IsAnyAutoRun())
            {
                Environment.ExitCode = 1;
                Close();
            }
        }
    }

    private async Task InitializeParentWebViewAsync(WebView2 webView)
    {
        if (_testServer is null)
        {
            throw new InvalidOperationException("The local test server must be started before WebView2 initialization.");
        }

        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: _userDataFolder);
        AppendLog(
            $"parent WebView2 environment created: runtime={environment.BrowserVersionString}; " +
            $"os={Environment.OSVersion.VersionString}; udf={environment.UserDataFolder}");
        var browserProcessExited = new TaskCompletionSource<CoreWebView2BrowserProcessExitedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        environment.BrowserProcessExited += (_, args) =>
        {
            AppendLog(
                $"parent WebView2 environment browser process exited: kind={args.BrowserProcessExitKind}; " +
                $"pid={args.BrowserProcessId}");
            browserProcessExited.TrySetResult(args);
        };

        _parentEnvironment = environment;
        _browserProcessExited = browserProcessExited;
        await webView.EnsureCoreWebView2Async(environment)
            .WaitAsync(TimeSpan.FromSeconds(30));
        webView.NavigationStarting += ParentWebView_NavigationStarting;
        webView.NavigationCompleted += ParentWebView_NavigationCompleted;
        webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
        webView.CoreWebView2.DocumentTitleChanged += ParentWebView_DocumentTitleChanged;
        webView.CoreWebView2.ProcessFailed += ParentWebView_ProcessFailed;
        webView.CoreWebView2.NewWindowRequested += ParentWebView_NewWindowRequested;
        webView.CoreWebView2.WindowCloseRequested += ParentWebView_WindowCloseRequested;

        var modalDialogHost = new ModalDialogHost(
            () => ParentWebView.Source,
            _compatibilityPolicy,
            _userDataFolder,
            AppendLog,
            new WindowInteropHelper(this).Handle,
            _nativeCloseAutoRun ? TimeSpan.FromSeconds(1) : null,
            crashRendererAfterNavigation: _processFailureAutoRun,
            crashBrowserAfterNavigation: _browserProcessFailureAutoRun && _browserRecoveryCount == 0,
            hangRendererAfterNavigation: _unresponsiveAutoRun);
        var compatibilityBroker = new CompatibilityBroker(
            () => ParentWebView.Source,
            _compatibilityPolicy,
            OnLegacyApiDetected,
            modalDialogHost,
            CaptureWindowOpenFeatures,
            AppendLog,
            StageTopLevelCloseHandoff,
            ReleaseTopLevelCloseHandoff);
        await LegacyCompatibilityBridge.InstallAsync(webView.CoreWebView2, compatibilityBroker);
        _testProbeRegistration = new TestProbeRegistration(
            webView.CoreWebView2,
            _testServer.BaseUri,
            new TestProbe(
                IsTrustedLocalTestDocument,
                IsAnyAutoRun,
                IsCurrentOriginConfigured,
                GetSelectedStartupProfileId,
                () => _browserRecoveryCount,
                () => _parentUnresponsiveRecoveryCount,
                FinishAutoRun,
                FailAutoRun,
                AppendLog),
            AppendLog);
    }

    private async Task RecoverParentWebViewAsync(string trigger)
    {
        if (_closing || _recoveringParentWebView)
        {
            return;
        }

        _recoveringParentWebView = true;
        var recoveryUri = ParentWebView.Source;
        AppendLog(
            $"parent WebView2 browser recovery started: trigger={trigger}; " +
            $"source={DialogNavigationPolicy.FormatForLog(recoveryUri?.ToString() ?? "(none)")}");
        SetCompatibilityStatusPresentation(
            CompatibilityStatusPresentationPolicy.CreateOperational(CompatibilityOperationalStatus.Recovering));
        BackButton.IsEnabled = false;
        ForwardButton.IsEnabled = false;

        try
        {
            if (_browserProcessExited is not null)
            {
                await _browserProcessExited.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }

            var oldWebView = ParentWebView;
            oldWebView.NavigationStarting -= ParentWebView_NavigationStarting;
            oldWebView.NavigationCompleted -= ParentWebView_NavigationCompleted;
            if (oldWebView.CoreWebView2 is not null)
            {
                oldWebView.CoreWebView2.SourceChanged -= CoreWebView2_SourceChanged;
                oldWebView.CoreWebView2.DocumentTitleChanged -= ParentWebView_DocumentTitleChanged;
                oldWebView.CoreWebView2.ProcessFailed -= ParentWebView_ProcessFailed;
                oldWebView.CoreWebView2.NewWindowRequested -= ParentWebView_NewWindowRequested;
                oldWebView.CoreWebView2.WindowCloseRequested -= ParentWebView_WindowCloseRequested;
            }
            ParentWebViewHost.Children.Remove(oldWebView);
            oldWebView.Dispose();

            var replacement = new WebView2 { Margin = new Thickness(8) };
            ParentWebView = replacement;
            ParentWebViewHost.Children.Add(replacement);
            _browserRecoveryCount++;
            await InitializeParentWebViewAsync(replacement);

            if ((_browserProcessFailureAutoRun || _parentUnresponsiveAutoRun) && recoveryUri is not null)
            {
                var builder = new UriBuilder(recoveryUri);
                var separator = string.IsNullOrEmpty(builder.Query) ? string.Empty : builder.Query.TrimStart('?') + "&";
                builder.Query = separator + "recovered=1";
                recoveryUri = builder.Uri;
            }

            if (recoveryUri is not null)
            {
                replacement.Source = recoveryUri;
                AddressBox.Text = recoveryUri.ToString();
                UpdateCompatibilityStatus(recoveryUri);
            }

            AppendLog(
                $"parent WebView2 browser recovery completed: count={_browserRecoveryCount}; " +
                $"source={DialogNavigationPolicy.FormatForLog(recoveryUri?.ToString() ?? "(none)")}");
        }
        catch (Exception ex)
        {
            AppendLog("parent WebView2 browser recovery failed: " + ex);
            if (IsAnyAutoRun())
            {
                FailAutoRun("parent WebView2 browser recovery failed: " + ex.Message);
            }
            else
            {
                SetCompatibilityStatusPresentation(
                    CompatibilityStatusPresentationPolicy.CreateOperational(
                        CompatibilityOperationalStatus.RecoveryFailed));
            }
        }
        finally
        {
            _recoveringParentWebView = false;
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _closing = true;
        AppendLog("main window closed cleanup started");
        ParentWebView.Dispose();
        AppendLog("parent WebView2 disposed during main window close");
        _localContentServer?.Dispose();
        _localContentServer = null;
        _testServer?.Dispose();
        _testServer = null;
        if (_browserSettingsAutoFolder is not null)
        {
            try
            {
                Directory.Delete(_browserSettingsAutoFolder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                AppendLog("browser settings automatic fixture cleanup warning: " + ex.GetType().Name);
            }
        }
        AppendLog("main window closed cleanup finished");
    }

    private async void ParentWebView_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        AppendLog(
            $"parent WebView2 process failed: kind={e.ProcessFailedKind}; reason={e.Reason}; " +
            $"exitCode={e.ExitCode}; description={e.ProcessDescription}");

        if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
        {
            await RecoverParentWebViewAsync("ProcessFailed");
            return;
        }

        if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
        {
            var shouldReload = _parentUnresponsiveTracker.Observe();
            AppendLog(
                "parent renderer unresponsive observation: " +
                $"count={_parentUnresponsiveTracker.Count}; " +
                $"threshold={_parentUnresponsiveTracker.Threshold}; reload={shouldReload}");
            if (shouldReload)
            {
                RestartBrowserAfterParentUnresponsive("notification-threshold");
            }
            else
            {
                ProbeParentRendererResponsiveness();
                StartParentRendererUnresponsiveGracePeriod();
            }
        }
    }

    private async void ProbeParentRendererResponsiveness()
    {
        if (_parentResponsivenessProbePending)
        {
            return;
        }

        _parentResponsivenessProbePending = true;
        try
        {
            await ParentWebView.ExecuteScriptAsync("void 0");
            if (!_closing)
            {
                _parentUnresponsiveTracker.MarkResponsive();
                AppendLog("parent renderer responsiveness probe completed; observation count reset");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"parent renderer responsiveness probe ended: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _parentResponsivenessProbePending = false;
        }
    }

    private async void StartParentRendererUnresponsiveGracePeriod()
    {
        if (_parentUnresponsiveGracePending)
        {
            return;
        }

        _parentUnresponsiveGracePending = true;
        await Task.Delay(TimeSpan.FromSeconds(5));
        _parentUnresponsiveGracePending = false;
        if (!_closing && _parentUnresponsiveTracker.Count > 0)
        {
            RestartBrowserAfterParentUnresponsive("responsiveness-probe-timeout");
        }
    }

    private void RestartBrowserAfterParentUnresponsive(string action)
    {
        if (_closing || _parentUnresponsiveReloading)
        {
            return;
        }

        _parentUnresponsiveReloading = true;
        _parentUnresponsiveTracker.MarkResponsive();
        _parentUnresponsiveRecoveryCount++;

        try
        {
            var browserProcessId = checked((int)ParentWebView.CoreWebView2.BrowserProcessId);
            AppendLog(
                $"terminating browser process after parent renderer remained unresponsive: " +
                $"action={action}; pid={browserProcessId}; count={_parentUnresponsiveRecoveryCount}");
            System.Diagnostics.Process.GetProcessById(browserProcessId).Kill();
        }
        catch (Exception ex)
        {
            _parentUnresponsiveReloading = false;
            AppendLog($"parent renderer browser restart failed: {ex.GetType().Name}: {ex.Message}");
            if (IsAnyAutoRun())
            {
                FailAutoRun("parent renderer browser restart failed: " + ex.Message);
            }
        }
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        ParentWebView.Reload();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (ParentWebView.CoreWebView2?.CanGoBack == true)
        {
            ParentWebView.CoreWebView2.GoBack();
        }
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (ParentWebView.CoreWebView2?.CanGoForward == true)
        {
            ParentWebView.CoreWebView2.GoForward();
        }
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        NavigateFromAddressBar();
    }

    private void Diagnostics_Click(object sender, RoutedEventArgs e)
    {
        SetDiagnosticsVisibility(!_diagnosticsVisible);
        AppendLog($"diagnostic panel visibility changed: visible={_diagnosticsVisible}");
    }

    private void SetDiagnosticsVisibility(bool visible)
    {
        _diagnosticsVisible = visible;
        DiagnosticsRow.Height = visible ? new GridLength(180) : new GridLength(0);
        LogBox.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        var diagnosticsText = UiText.Get(visible ? UiText.DiagnosticsHide : UiText.DiagnosticsShow);
        DiagnosticsText.Text = diagnosticsText;
        DiagnosticsButton.ToolTip = diagnosticsText;
        AutomationProperties.SetName(DiagnosticsButton, diagnosticsText);
    }

    private void ApplicationSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_browserSettingsStore is null)
        {
            return;
        }

        var dialog = new ApplicationSettingsWindow(
            _browserSettings.InitialUrl,
            _userApprovedCompatibility,
            _userDeniedCompatibility) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        var updated = new BrowserSettings(dialog.InitialUrl);
        var updatedApprovals = dialog.Approvals.ToHashSet();
        var updatedDenials = dialog.Denials.ToHashSet();
        var previousSettings = _browserSettings;
        var previousApprovals = _userApprovedCompatibility.ToHashSet();
        var previousDenials = _userDeniedCompatibility.ToHashSet();
        try
        {
            _browserSettingsStore.Save(updated);
            _approvalStore.Save(updatedApprovals, updatedDenials);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var rollbackErrors = new List<string>();
            try
            {
                _browserSettingsStore.Save(previousSettings);
            }
            catch (Exception rollback) when (rollback is IOException or UnauthorizedAccessException)
            {
                rollbackErrors.Add("browser=" + rollback.GetType().Name);
            }
            try
            {
                _approvalStore.Save(previousApprovals, previousDenials);
            }
            catch (Exception rollback) when (rollback is IOException or UnauthorizedAccessException)
            {
                rollbackErrors.Add("compatibility=" + rollback.GetType().Name);
            }
            AppendLog(
                $"application settings save failed; runtime changes not applied: error={ex.Message}; " +
                $"rollback={string.Join(',', rollbackErrors.DefaultIfEmpty("ok"))}");
            MessageBox.Show(
                this,
                UiText.Format(UiText.ApplicationSettingsSaveErrorBody, ex.Message),
                UiText.Get(UiText.ApplicationSettingsSaveErrorTitle),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        _browserSettings = updated;
        ApplyUserCompatibilityDecisions(updatedApprovals, updatedDenials);
        AppendLog(
            "application settings saved for next normal launch: " +
            $"initialUrl={FormatOptionalUrlForLog(updated.InitialUrl)}; " +
            $"approvals={updatedApprovals.Count}; denials={updatedDenials.Count}; " +
            $"path={_browserSettingsStore.Path}");
    }

    private void ApplyUserCompatibilityDecisions(
        HashSet<UserApprovedCompatibility> approvals,
        HashSet<UserApprovedCompatibility> denials)
    {
        var affected = _userApprovedCompatibility
            .Concat(_userDeniedCompatibility)
            .Concat(approvals)
            .Concat(denials)
            .Distinct()
            .ToArray();
        foreach (var decision in affected)
        {
            _compatibilityPolicy.ClearDecision(decision.Origin, decision.ApiName);
        }
        foreach (var approval in approvals)
        {
            _compatibilityPolicy.Allow(approval.Origin, approval.ApiName);
        }
        foreach (var denial in denials)
        {
            _compatibilityPolicy.Deny(denial.Origin, denial.ApiName);
        }

        _userApprovedCompatibility.Clear();
        _userApprovedCompatibility.UnionWith(approvals);
        _userDeniedCompatibility.Clear();
        _userDeniedCompatibility.UnionWith(denials);
        if (ParentWebView.Source is not null)
        {
            UpdateCompatibilityStatus(ParentWebView.Source);
        }
    }

    private void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateFromAddressBar();
            e.Handled = true;
        }
    }

    private void NavigateFromAddressBar()
    {
        var text = AddressBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (TryGetLocalPath(text, out var localPath))
        {
            OpenLocalHtml(localPath, "address-bar");
            return;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            if (text.Contains('.') || text.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                text = "https://" + text;
            }
            else
            {
                text = "https://www.bing.com/search?q=" + Uri.EscapeDataString(text);
            }
        }

        ParentWebView.Source = new Uri(text);
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasSingleLocalHtmlDrop(e.Data)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_PreviewDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length != 1)
        {
            ShowLocalOpenError(UiText.Get(UiText.LocalOpenSingleFile));
            return;
        }

        OpenLocalHtml(paths[0], "drag-drop");
    }

    private static bool HasSingleLocalHtmlDrop(IDataObject data)
    {
        if (data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length != 1)
        {
            return false;
        }

        var extension = Path.GetExtension(paths[0]);
        return extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".htm", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetLocalPath(string text, out string path)
    {
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            path = uri.LocalPath;
            return true;
        }

        if (Path.IsPathFullyQualified(text))
        {
            path = text;
            return true;
        }

        path = string.Empty;
        return false;
    }

    private void OpenLocalHtml(string path, string source)
    {
        LocalHtmlSelectionResult result;
        try
        {
            result = LocalHtmlSelectionPolicy.Validate(path);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            AppendLog($"local HTML selection failed: source={source}; error={ex.GetType().Name}");
            ShowLocalOpenError(UiText.Get(UiText.LocalOpenFailed));
            return;
        }

        if (!result.IsValid)
        {
            AppendLog($"local HTML selection rejected: source={source}; error={result.Error}");
            var message = result.Error == LocalHtmlSelectionError.UnsupportedExtension
                ? UiText.Get(UiText.LocalOpenHtmlOnly)
                : UiText.Get(UiText.LocalOpenFailed);
            ShowLocalOpenError(message);
            return;
        }

        var selection = result.Selection!;
        var rootChanged = _localContentServer is null ||
            !string.Equals(
                _localContentServer.RootPath,
                selection.RootPath,
                StringComparison.OrdinalIgnoreCase);
        if (rootChanged)
        {
            RevokeLocalContentApproval();
            _localContentServer?.Dispose();
            try
            {
                _localContentServer = LocalContentServer.Start(selection.RootPath, AppendLog);
            }
            catch (Exception ex) when (ex is IOException or SocketException or UnauthorizedAccessException)
            {
                AppendLog($"local content server start failed: error={ex.GetType().Name}: {ex.Message}");
                ShowLocalOpenError(UiText.Get(UiText.LocalOpenFailed));
                return;
            }

            _localContentOrigin = CompatibilityOriginPolicy.GetOrigin(_localContentServer.BaseUri);
            RevokeLocalContentApproval();
        }

        var targetUri = _localContentServer!.CreateUri(selection.RelativeUrlPath);
        AppendLog(
            $"opening local HTML in parent WebView2: source={source}; " +
            $"fileName={Path.GetFileName(selection.FullPath)}; origin={_localContentOrigin}");
        ParentWebView.Source = targetUri;
        AddressBox.Text = targetUri.ToString();
    }

    private void RevokeLocalContentApproval()
    {
        if (_localContentOrigin is null)
        {
            return;
        }

        foreach (var apiName in CompatibilityApi.Known)
        {
            _compatibilityPolicy.ClearDecision(_localContentOrigin, apiName);
        }
        var removed = _userApprovedCompatibility.RemoveWhere(approval =>
            string.Equals(approval.Origin, _localContentOrigin, StringComparison.OrdinalIgnoreCase));
        removed += _userDeniedCompatibility.RemoveWhere(denial =>
            string.Equals(denial.Origin, _localContentOrigin, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            return;
        }

        try
        {
            _approvalStore.Save(_userApprovedCompatibility, _userDeniedCompatibility);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppendLog($"stale local compatibility approval removal failed: error={ex.Message}");
        }
    }

    private void ShowLocalOpenError(string message)
    {
        MessageBox.Show(
            this,
            message,
            UiText.Get(UiText.LocalOpenErrorTitle),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void ParentWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        Title = MainWindowTitlePolicy.Format(null);
        AddressBox.Text = e.Uri;
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
        {
            UpdateTestProbeRegistration(uri);
            UpdateCompatibilityStatus(uri);
        }
        else
        {
            UpdateTestProbeRegistration(null);
        }
    }

    private void UpdateTestProbeRegistration(Uri? targetUri)
    {
        _testProbeRegistration?.Update(targetUri);
    }

    private async void ParentWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _parentUnresponsiveReloading = false;
            ScheduleParentRendererHangForAutomaticValidation();
        }
        UpdateNavigationButtons();
        if (ParentWebView.Source is not null)
        {
            UpdateCompatibilityStatus(ParentWebView.Source);
        }
        if (e.IsSuccess && _windowOpenObservationAuto && !_windowOpenObservationTriggered &&
            ParentWebView.Source?.AbsolutePath.Equals("/window-open-reference-ie.html", StringComparison.Ordinal) == true)
        {
            _windowOpenObservationTriggered = true;
            await RequestNextWindowOpenObservationCaseAsync();
        }
        if (e.IsSuccess && _browserSettingsAutoRun && !_browserSettingsAutoCompleted)
        {
            _browserSettingsAutoCompleted = true;
            if (ParentWebView.Source == _browserSettingsAutoExpectedUri)
            {
                FinishAutoRun("browser settings initial URL selected from isolated persisted state");
            }
            else
            {
                FailAutoRun(
                    "browser settings initial URL mismatch: " +
                    DialogNavigationPolicy.FormatForLog(ParentWebView.Source?.ToString() ?? "(none)"));
            }
        }
        if (e.IsSuccess && _revokedPermissionAutoRun && !_revokedPermissionAutoTriggered)
        {
            _revokedPermissionAutoTriggered = true;
            var origin = CompatibilityOriginPolicy.GetOrigin(ParentWebView.Source!);
            if (!_compatibilityPolicy.Revoke(origin, CompatibilityApi.ShowModalDialog))
            {
                FailAutoRun("revoked-permission fixture could not revoke its runtime grant");
                return;
            }
            AppendLog($"revoked runtime permission on loaded document: origin={origin}");
            await ParentWebView.CoreWebView2.ExecuteScriptAsync(
                "window.showModalDialog('dialog.html', null, '')");
        }
    }

    private async void ScheduleParentRendererHangForAutomaticValidation()
    {
        if (_parentHangScheduled || !_parentUnresponsiveAutoRun ||
            ParentWebView.Source?.Query.Contains("recovered=1", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        _parentHangScheduled = true;
        AppendLog("parent renderer hang scheduled for automatic validation");
        await Task.Delay(250);
        if (_closing)
        {
            return;
        }

        _ = SendNativeInputForParentRendererHangValidation();
        _ = RunParentRendererHangWatchdog();
        try
        {
            await ParentWebView.ExecuteScriptAsync("while (true) {}");
        }
        catch (Exception ex)
        {
            AppendLog($"parent renderer hang command ended: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task RunParentRendererHangWatchdog()
    {
        await Task.Delay(TimeSpan.FromSeconds(45));
        if (_closing || _parentUnresponsiveRecoveryCount > 0 || _recoveringParentWebView)
        {
            return;
        }

        _parentUnresponsiveTracker.Observe();
        AppendLog(
            "parent renderer automatic-validation watchdog expired before ProcessFailed notification");
        RestartBrowserAfterParentUnresponsive("automatic-validation-watchdog");
    }

    private async Task SendNativeInputForParentRendererHangValidation()
    {
        await Task.Delay(1_000);
        if (!_closing)
        {
            NativeTestInput.ClickWindowCenter(new WindowInteropHelper(this).Handle, AppendLog);
        }
    }

    private void CoreWebView2_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        var source = ParentWebView.Source;
        if (source is not null)
        {
            AddressBox.Text = source.ToString();
            UpdateCompatibilityStatus(source);
        }
        UpdateNavigationButtons();
    }

    private void ParentWebView_DocumentTitleChanged(object? sender, object e)
    {
        var documentTitle = ParentWebView.CoreWebView2?.DocumentTitle;
        Title = MainWindowTitlePolicy.Format(documentTitle);
    }

    private async void ParentWebView_WindowCloseRequested(object? sender, object e)
    {
        if (_topLevelCloseRequestHandling)
        {
            return;
        }

        _topLevelCloseRequestHandling = true;
        var pending = _pendingTopLevelHandoff;
        if (pending is null)
        {
            HandleTopLevelCloseHandoffFailure(
                "top-level close requested without an eligible pending first child");
            return;
        }
        if (!TopLevelHandoffSelectionPolicy.CanApply(ParentWebView.Source, pending.ParentOrigin))
        {
            HandleTopLevelCloseHandoffFailure(
                "top-level close origin changed after the first child was captured");
            return;
        }
        if (!_compatibilityPolicy.IsAllowed(
                pending.ParentOrigin,
                CompatibilityApi.TopLevelCloseHandoff))
        {
            HandleTopLevelCloseHandoffFailure(
                "top-level close handoff is not allowed for the current origin");
            return;
        }
        CloseModelessWindows("top-level handoff execution");

        try
        {
            var navigation = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            void OnNavigationCompleted(object? _, CoreWebView2NavigationCompletedEventArgs args) =>
                navigation.TrySetResult(args.IsSuccess);

            ParentWebView.NavigationCompleted += OnNavigationCompleted;
            try
            {
                var serializedName = System.Text.Json.JsonSerializer.Serialize(pending.WindowName);
                await ParentWebView.CoreWebView2.ExecuteScriptAsync($"window.name = {serializedName}")
                    .WaitAsync(TimeSpan.FromSeconds(10));
                ApplyPendingTopLevelHandoffShell(pending);
                AppendLog(
                    "top-level close request converted to retained-parent navigation: " +
                    $"url={DialogNavigationPolicy.FormatForLog(pending.TargetUri.ToString())}; " +
                    $"name={FormatWindowName(pending.WindowName)}");
                ParentWebView.Source = pending.TargetUri;
                var navigationSucceeded = await navigation.Task.WaitAsync(TimeSpan.FromSeconds(30));
                if (!navigationSucceeded)
                {
                    throw new InvalidOperationException("business handoff navigation failed");
                }
            }
            finally
            {
                ParentWebView.NavigationCompleted -= OnNavigationCompleted;
            }

            if (!_topLevelCloseAutoRun)
            {
                AppendLog("top-level close handoff completed in retained parent window");
                _pendingTopLevelHandoff = null;
                _topLevelCloseRequestHandling = false;
                return;
            }

            await Task.Delay(250);
            var ticksResult = await ParentWebView.CoreWebView2
                .ExecuteScriptAsync("window.getTopLevelCloseTicks()")
                .WaitAsync(TimeSpan.FromSeconds(10));
            if (!int.TryParse(ticksResult, out var ticks) || ticks < 2)
            {
                throw new InvalidOperationException(
                    $"business DOM did not progress after handoff: {ticksResult}");
            }

            var modalResult = await InvokeTopLevelCloseParentModalAsync(616);

            Environment.ExitCode = 0;
            AppendLog(
                "top-level close auto-run passed: retained parent completed pending first-child " +
                $"handoff; ticks={ticks}; modalResult={modalResult}; closing final window");
            Close();
        }
        catch (Exception ex)
        {
            HandleTopLevelCloseHandoffFailure("top-level close handoff failed: " + ex.Message);
        }
    }

    private void HandleTopLevelCloseHandoffFailure(string message)
    {
        if (_topLevelCloseAutoRun)
        {
            FailAutoRun(message);
            return;
        }

        AppendLog(message);
        _topLevelCloseRequestHandling = false;
    }

    private void ApplyPendingTopLevelHandoffShell(PendingTopLevelHandoff pending)
    {
        BrowserCommandRow.Height = pending.DisplayToolbar
            ? GridLength.Auto
            : new GridLength(0);
        if (pending.Width is > 0)
        {
            Width = pending.Width.Value;
        }
        if (pending.Height is > 0)
        {
            Height = pending.Height.Value;
        }
        AppendLog(
            $"top-level close handoff shell applied: browserCommands={pending.DisplayToolbar}; " +
            $"status={pending.Features.DisplayStatus}; scrollbars={pending.Features.DisplayScrollbars}; " +
            $"width={pending.Width?.ToString() ?? "unchanged"}; " +
            $"height={pending.Height?.ToString() ?? "unchanged"}");
    }

    private async Task<int> InvokeTopLevelCloseParentModalAsync(int expectedId)
    {
        var modalResultJson = await ParentWebView.CoreWebView2
            .ExecuteScriptAsync($"window.runTopLevelCloseModal({expectedId})")
            .WaitAsync(TimeSpan.FromSeconds(10));
        using var modalResult = System.Text.Json.JsonDocument.Parse(modalResultJson);
        if (!modalResult.RootElement.TryGetProperty("invoked", out var invoked) ||
            invoked.ValueKind != System.Text.Json.JsonValueKind.True ||
            !modalResult.RootElement.TryGetProperty("value", out var value) ||
            value.ValueKind != System.Text.Json.JsonValueKind.Object ||
            !value.TryGetProperty("accepted", out var accepted) ||
            accepted.ValueKind != System.Text.Json.JsonValueKind.True ||
            !value.TryGetProperty("selectedId", out var selectedId) ||
            selectedId.GetInt32() != expectedId)
        {
            throw new InvalidOperationException(
                $"handoff modal result for {expectedId} was invalid: {modalResultJson}");
        }

        return selectedId.GetInt32();
    }

    private async void ParentWebView_NewWindowRequested(
        object? sender,
        CoreWebView2NewWindowRequestedEventArgs e)
    {
        if (sender is not CoreWebView2 core ||
            !Uri.TryCreate(core.Source, UriKind.Absolute, out var parentUri))
        {
            return;
        }

        var parentOrigin = CompatibilityOriginPolicy.GetOrigin(parentUri);
        var isObservationHarness = _testServer is not null &&
            HostOriginGuard.IsSameOrigin(parentUri, _testServer.BaseUri) &&
            parentUri.AbsolutePath.Equals("/window-open-reference-ie.html", StringComparison.Ordinal);
        if (!isObservationHarness &&
            !_compatibilityPolicy.IsAllowed(parentOrigin, CompatibilityApi.WindowOpenFeatures))
        {
            return;
        }

        var deferral = e.GetDeferral();
        e.Handled = true;
        NewWindowObservationWindow? observationWindow = null;
        var targetAssigned = false;
        try
        {
            var features = e.WindowFeatures;
            var rawCapture = TakeWindowOpenFeatureCapture(e.Name);
            var application = WindowOpenFeatureApplicationPolicy.Resolve(
                rawCapture,
                features.ShouldDisplayScrollBars,
                features.ShouldDisplayStatus);
            AppendLog(
                "window.open observation requested: " +
                $"uri={(e.Uri.Equals("about:blank", StringComparison.OrdinalIgnoreCase) ? "about:blank (staging)" : DialogNavigationPolicy.FormatForLog(e.Uri))}; " +
                $"name={FormatWindowName(e.Name)}; userInitiated={e.IsUserInitiated}; " +
                $"hasPosition={features.HasPosition}; left={features.Left}; top={features.Top}; " +
                $"hasSize={features.HasSize}; width={features.Width}; height={features.Height}; " +
                $"menuBar={features.ShouldDisplayMenuBar}; status={features.ShouldDisplayStatus}; " +
                $"toolBar={features.ShouldDisplayToolbar}; scrollBars={features.ShouldDisplayScrollBars}");
            AppendLog(
                "window.open raw feature capture: " +
                $"matched={rawCapture is not null}; valid={rawCapture?.IsValid}; " +
                $"scrollbars={FormatCapturedBoolean(rawCapture?.Scrollbars)}; " +
                $"status={FormatCapturedBoolean(rawCapture?.Status)}; " +
                $"error={rawCapture?.ErrorCode ?? "(none)"}; bytes={rawCapture?.Utf8Bytes}; " +
                $"entries={rawCapture?.EntryCount}");
            AppendLog(
                "window.open feature application: " +
                $"scrollbars={application.DisplayScrollbars}; scrollbarsSource={application.ScrollbarsSource}; " +
                $"status={application.DisplayStatus}; statusSource={application.StatusSource}");

            var stagedPending = _pendingTopLevelHandoff;
            var isStagedHandoffWindow =
                e.Uri.Equals("about:blank", StringComparison.OrdinalIgnoreCase) &&
                stagedPending is not null &&
                string.Equals(
                    stagedPending.ParentOrigin,
                    parentOrigin,
                    StringComparison.OrdinalIgnoreCase);
            if (isStagedHandoffWindow)
            {
                _pendingTopLevelHandoff = stagedPending! with
                {
                    Features = application,
                    DisplayToolbar = features.ShouldDisplayToolbar,
                    Width = features.HasSize ? features.Width : null,
                    Height = features.HasSize ? features.Height : null
                };
                AppendLog(
                    "top-level close staging window assigned without business navigation: " +
                    $"name={FormatWindowName(e.Name)}");
            }

            if (!isStagedHandoffWindow &&
                (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var childUri) ||
                 !HostOriginGuard.IsSameOrigin(childUri, parentUri)))
            {
                AppendLog("window.open blocked: target is not a same-origin HTTP(S) URL");
                return;
            }

            if (_parentEnvironment is null)
            {
                AppendLog("window.open observation blocked: parent environment is unavailable");
                return;
            }

            observationWindow = new NewWindowObservationWindow(
                application.DisplayScrollbars,
                application.DisplayStatus,
                AppendLog)
            {
                Owner = this
            };
            _modelessWindows.Add(observationWindow);
            observationWindow.Closed += (_, _) => _modelessWindows.Remove(observationWindow);
            observationWindow.Show();
            await observationWindow.InitializeAsync(_parentEnvironment);
            e.NewWindow = observationWindow.CoreWebView2;
            targetAssigned = true;
            AppendLog("window.open target assigned with bounded feature application");
        }
        catch (Exception ex)
        {
            observationWindow?.Close();
            AppendLog($"window.open observation failed: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            deferral.Complete();
        }

        if (targetAssigned && _windowOpenObservationAuto && observationWindow is not null)
        {
            try
            {
                var navigationSucceeded = await observationWindow.WaitForInitialNavigationAsync();
                AppendLog($"window.open observation child navigation completed: success={navigationSucceeded}");
            }
            catch (TimeoutException)
            {
                AppendLog("window.open observation child navigation timed out");
            }
            finally
            {
                observationWindow.Close();
            }

            await Task.Delay(200);
            await RequestNextWindowOpenObservationCaseAsync();
        }

        if (targetAssigned && _topLevelCloseNormalPopupAutoRun && observationWindow is not null)
        {
            try
            {
                var navigationSucceeded = await observationWindow.WaitForNonBlankNavigationAsync();
                if (!navigationSucceeded)
                {
                    throw new InvalidOperationException("normal popup navigation failed after staging release");
                }
                await Task.Delay(250);
                var ticksResult = await observationWindow.CoreWebView2
                    .ExecuteScriptAsync("window.getTopLevelCloseTicks()")
                    .WaitAsync(TimeSpan.FromSeconds(10));
                if (!int.TryParse(ticksResult, out var ticks) || ticks < 2)
                {
                    throw new InvalidOperationException(
                        $"normal popup DOM did not progress after staging release: {ticksResult}");
                }

                Environment.ExitCode = 0;
                AppendLog(
                    "top-level close normal popup auto-run passed: " +
                    $"staging released; business DOM ticks={ticks}");
                observationWindow.Close();
                FinishAutoRun("normal popup navigated after top-level handoff staging release");
            }
            catch (Exception ex)
            {
                observationWindow.Close();
                FailAutoRun("normal popup staging regression failed: " + ex.Message);
            }
        }
    }

    private async Task RequestNextWindowOpenObservationCaseAsync()
    {
        if (_windowOpenObservationNextCase >= WindowOpenObservationCaseCount)
        {
            AppendLog($"window.open observation auto-run completed: cases={WindowOpenObservationCaseCount}");
            return;
        }

        var index = _windowOpenObservationNextCase++;
        AppendLog($"window.open observation auto-run requesting case: index={index}");
        await ParentWebView.ExecuteScriptAsync($"runCase({index})");
    }

    private static string FormatWindowName(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "(empty)";
        }

        var bounded = value.Length > 128 ? value[..128] : value;
        return string.Concat(bounded.Select(character => char.IsControl(character) ? '?' : character));
    }

    private string StageTopLevelCloseHandoff(
        string origin,
        string url,
        string name,
        string features)
    {
        if (_windowOpenObservation || _windowOpenObservationAuto)
        {
            return string.Empty;
        }
        var selection = TopLevelHandoffSelectionPolicy.Select(
            ParentWebView.Source,
            url,
            _pendingTopLevelHandoff is not null);
        if (!selection.IsAccepted || selection.TargetUri is null ||
            selection.ParentOrigin is null ||
            !string.Equals(origin, selection.ParentOrigin, StringComparison.OrdinalIgnoreCase))
        {
            AppendLog(
                "top-level close staging rejected: " +
                $"reason={selection.Reason}; url={DialogNavigationPolicy.FormatForLog(url)}");
            return string.Empty;
        }

        var token = Guid.NewGuid().ToString("N");
        _pendingTopLevelHandoffToken = token;
        _pendingTopLevelHandoff = new PendingTopLevelHandoff(
            selection.TargetUri,
            selection.ParentOrigin,
            name,
            new WindowOpenFeatureApplication(true, true, "safe-default", "safe-default"),
            DisplayToolbar: true,
            Width: null,
            Height: null);
        AppendLog(
            "top-level close staged first child before navigation: " +
            $"url={DialogNavigationPolicy.FormatForLog(url)}; name={FormatWindowName(name)}; " +
            $"featureBytes={System.Text.Encoding.UTF8.GetByteCount(features)}");
        return token;
    }

    private void ReleaseTopLevelCloseHandoff(string origin, string token, string reason)
    {
        if (_pendingTopLevelHandoffToken != token ||
            _pendingTopLevelHandoff is not { } pending ||
            !string.Equals(origin, pending.ParentOrigin, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _pendingTopLevelHandoff = null;
        _pendingTopLevelHandoffToken = null;
        AppendLog($"top-level close staging released: reason={reason}");
    }

    private void CaptureWindowOpenFeatures(string name, WindowOpenFeatureCapture capture)
    {
        lock (_windowOpenCaptureLock)
        {
            _windowOpenCaptures.RemoveAll(item =>
                item.Name.Equals(name, StringComparison.Ordinal));
            _windowOpenCaptures.Add((name, capture));
            if (_windowOpenCaptures.Count > 64)
            {
                _windowOpenCaptures.RemoveAt(0);
            }
        }
    }

    private WindowOpenFeatureCapture? TakeWindowOpenFeatureCapture(string? name)
    {
        lock (_windowOpenCaptureLock)
        {
            var index = _windowOpenCaptures.FindIndex(item =>
                item.Name.Equals(name ?? string.Empty, StringComparison.Ordinal));
            if (index < 0)
            {
                return null;
            }

            var capture = _windowOpenCaptures[index].Capture;
            _windowOpenCaptures.RemoveAt(index);
            return capture;
        }
    }

    private static string FormatCapturedBoolean(bool? value) => value switch
    {
        true => "true",
        false => "false",
        null => "omitted"
    };

    private static string FormatOptionalUrlForLog(Uri? uri) => uri is null
        ? "(home)"
        : DialogNavigationPolicy.FormatForLog(uri.ToString());

    private void UpdateNavigationButtons()
    {
        BackButton.IsEnabled = ParentWebView.CoreWebView2?.CanGoBack == true;
        ForwardButton.IsEnabled = ParentWebView.CoreWebView2?.CanGoForward == true;
    }

    private void UpdateCompatibilityStatus(Uri uri)
    {
        SetCompatibilityStatusPresentation(
            CompatibilityStatusPresentationPolicy.Create(_compatibilityPolicy.GetStatus(uri)));
    }

    private void SetCompatibilityStatusPresentation(CompatibilityStatusPresentation presentation)
    {
        CompatibilityStatusText.Text = presentation.ShortLabel;
        CompatibilityStatusIconPath.Data = FindResource(GetCompatibilityStatusGeometryKey(presentation.Icon)) as System.Windows.Media.Geometry;
        CompatibilityStatusButton.ToolTip = presentation.DetailText;
        AutomationProperties.SetName(CompatibilityStatusButton, presentation.AccessibleText);
        _compatibilityStatusDetail = presentation.DetailText;
    }

    private static string GetCompatibilityStatusGeometryKey(CompatibilityStatusIcon icon) => icon switch
    {
        CompatibilityStatusIcon.Undecided => "CompatibilityUndecidedIconGeometry",
        CompatibilityStatusIcon.DetectionPending => "CompatibilityDetectedIconGeometry",
        CompatibilityStatusIcon.Enabled => "CompatibilityEnabledIconGeometry",
        CompatibilityStatusIcon.Denied => "CompatibilityDeniedIconGeometry",
        CompatibilityStatusIcon.Blocked => "CompatibilityBlockedIconGeometry",
        CompatibilityStatusIcon.Operational => "CompatibilityOperationalIconGeometry",
        CompatibilityStatusIcon.Error => "CompatibilityErrorIconGeometry",
        _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, "Unknown compatibility icon")
    };

    private void CompatibilityStatus_Click(object sender, RoutedEventArgs e)
    {
        var detailWindow = new CompatibilityStatusDetailWindow(_compatibilityStatusDetail)
        {
            Owner = this
        };
        detailWindow.ShowDialog();
    }

    private bool IsTrustedLocalTestDocument()
    {
        return _testServer is not null &&
            HostOriginGuard.IsSameOrigin(ParentWebView.Source, _testServer.BaseUri);
    }

    private bool IsAnyAutoRun()
    {
        return _autoRun || _sessionAutoRun || _failureAutoRun || _featureAutoRun ||
            _payloadAutoRun || _originGuardAutoRun || _navigationAutoRun || _profileAutoRun ||
            _startupProfileAutoRun || _nativeCloseAutoRun || _nativeXUiRun || _nestedAutoRun ||
            _processFailureAutoRun || _browserProcessFailureAutoRun || _unresponsiveAutoRun ||
            _parentUnresponsiveAutoRun || _windowOpenObservation || _windowOpenObservationAuto ||
            _browserSettingsAutoRun || _topLevelCloseAutoRun || _topLevelCloseNormalPopupAutoRun ||
            _revokedPermissionAutoRun;
    }

    private string? GetStartupProfileErrorMessage()
    {
        if (_startupProfileSelection.Profile is not null && IsAnyAutoRun() && !_startupProfileAutoRun)
        {
            return UiText.Get(UiText.StartupProfileAutoConflict);
        }

        return _startupProfileSelection.Error switch
        {
            StartupProfileSelectionError.MissingId => UiText.Get(UiText.StartupProfileMissingId),
            StartupProfileSelectionError.MultipleSelections => UiText.Get(UiText.StartupProfileMultiple),
            StartupProfileSelectionError.UnknownProfile => UiText.Format(
                UiText.StartupProfileUnknown,
                _startupProfileSelection.RequestedId ?? string.Empty),
            _ => null
        };
    }

    private bool RequiresRuntimeTestAllowance()
    {
        return _autoRun || _sessionAutoRun || _failureAutoRun || _featureAutoRun ||
            _payloadAutoRun || _originGuardAutoRun || _navigationAutoRun || _nativeCloseAutoRun ||
            _nativeXUiRun || _nestedAutoRun || _processFailureAutoRun || _browserProcessFailureAutoRun ||
            _unresponsiveAutoRun || _parentUnresponsiveAutoRun || _revokedPermissionAutoRun;
    }

    private bool IsCurrentOriginConfigured()
    {
        var source = ParentWebView.Source;
        return source is not null &&
            _compatibilityPolicy.IsConfigured(
                CompatibilityOriginPolicy.GetOrigin(source),
                CompatibilityApi.ShowModalDialog);
    }

    private string? GetSelectedStartupProfileId()
    {
        return _startupProfileSelection.Profile?.Id;
    }

    private void OnLegacyApiDetected(string origin, string apiName)
    {
        var normalizedOrigin = CompatibilityOriginPolicy.NormalizeOrigin(origin);
        if (normalizedOrigin is null)
        {
            AppendLog($"blocked legacy API detection for unsupported origin: origin={origin}; api={apiName}");
            return;
        }

        origin = normalizedOrigin;
        if (_revokedPermissionAutoRun)
        {
            if (_revokedPermissionAutoTriggered &&
                apiName == CompatibilityApi.ShowModalDialog &&
                !_compatibilityPolicy.IsAllowed(origin, apiName))
            {
                Environment.ExitCode = 0;
                FinishAutoRun(
                    "loaded document returned to low-privilege discovery after runtime revocation");
            }
            else
            {
                FailAutoRun(
                    $"unexpected revoked-permission detection: origin={origin}; api={apiName}");
            }
            return;
        }
        if (_compatibilityPolicy.IsDenied(origin, apiName))
        {
            AppendLog($"legacy API remains denied without prompting: origin={origin}; api={apiName}");
            return;
        }
        if (_compatibilityPolicy.PendingDetection is not null)
        {
            AppendLog($"legacy API detection ignored while consent is pending: origin={origin}; api={apiName}");
            return;
        }
        var detection = _compatibilityPolicy.Detect(origin, apiName);
        AppendLog($"legacy API detected: origin={origin}; api={apiName}");

        Dispatcher.BeginInvoke(() =>
        {
            if (_compatibilityPolicy.PendingDetection != detection)
            {
                return;
            }

            if (ParentWebView.Source is not null)
            {
                UpdateCompatibilityStatus(ParentWebView.Source);
            }

            if (apiName == CompatibilityApi.TopLevelCloseHandoff)
            {
                CloseModelessWindows("top-level handoff consent");
            }

            var currentlyAllowedApis = CompatibilityApi.Known
                .Where(knownApi => _compatibilityPolicy.IsAllowed(origin, knownApi))
                .ToHashSet(StringComparer.Ordinal);
            var dialog = new LegacyApiConsentWindow(origin, apiName, currentlyAllowedApis)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && dialog.Choice is not null)
            {
                var allowedApis = dialog.Choice switch
                {
                    LegacyApiConsentChoice.AllowAll => CompatibilityApi.Known.ToHashSet(StringComparer.Ordinal),
                    LegacyApiConsentChoice.AllowSelected => dialog.SelectedApis,
                    _ => new HashSet<string>(StringComparer.Ordinal)
                };
                foreach (var knownApi in CompatibilityApi.Known)
                {
                    if (allowedApis.Contains(knownApi))
                    {
                        _compatibilityPolicy.Allow(origin, knownApi);
                    }
                    else
                    {
                        _compatibilityPolicy.Deny(origin, knownApi);
                    }
                }
                var isLocalContentOrigin = string.Equals(
                    origin,
                    _localContentOrigin,
                    StringComparison.OrdinalIgnoreCase);
                if (isLocalContentOrigin)
                {
                    AppendLog($"local content compatibility decisions are session-only: origin={origin}");
                }
                else
                {
                    _userApprovedCompatibility.RemoveWhere(item =>
                        string.Equals(item.Origin, origin, StringComparison.OrdinalIgnoreCase));
                    _userDeniedCompatibility.RemoveWhere(item =>
                        string.Equals(item.Origin, origin, StringComparison.OrdinalIgnoreCase));
                    foreach (var knownApi in CompatibilityApi.Known)
                    {
                        var decision = new UserApprovedCompatibility(origin, knownApi);
                        (allowedApis.Contains(knownApi)
                            ? _userApprovedCompatibility
                            : _userDeniedCompatibility).Add(decision);
                    }
                    try
                    {
                        _approvalStore.Save(_userApprovedCompatibility, _userDeniedCompatibility);
                        AppendLog($"persisted user compatibility decisions: origin={origin}; path={_approvalStore.Path}");
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        AppendLog($"approval persistence failed; approval is session-only: origin={origin}; api={apiName}; error={ex.Message}");
                    }
                }
                AppendLog($"user completed legacy compatibility decision: origin={origin}; choice={dialog.Choice}");
                _compatibilityPolicy.ClearPendingDetection();
                if (allowedApis.Contains(apiName))
                {
                    ParentWebView.Reload();
                }
            }
            else
            {
                AppendLog($"user denied legacy API: origin={origin}; api={apiName}");
                _compatibilityPolicy.ClearPendingDetection();
                if (ParentWebView.Source is not null)
                {
                    UpdateCompatibilityStatus(ParentWebView.Source);
                }
            }
        });
    }

    private void CloseModelessWindows(string reason)
    {
        var windows = _modelessWindows.ToArray();
        foreach (var window in windows)
        {
            window.Close();
        }
        AppendLog(
            $"closed modeless windows: reason={reason}; count={windows.Length}");
    }

    private static string GetPagePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "pages", fileName));
    }

    public static Uri GetPageUri(string fileName)
    {
        return new Uri(GetPagePath(fileName));
    }

    public void AppendLog(string message)
    {
        var line = $"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}";
        _fileLog.AppendLine(line);

        Dispatcher.BeginInvoke(() =>
        {
            LogBox.AppendText(line + Environment.NewLine);
            LogBox.ScrollToEnd();
        });
    }

    public void FinishAutoRun(string summary)
    {
        AppendLog("auto-run finished: " + summary);
        Dispatcher.BeginInvoke(Close);
    }

    public void FailAutoRun(string summary)
    {
        Environment.ExitCode = 1;
        AppendLog("auto-run failed: " + summary);
        Dispatcher.BeginInvoke(Close);
    }
}
