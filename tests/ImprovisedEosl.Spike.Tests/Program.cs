using ImprovisedEosl.Core;
using ImprovisedEosl.ModalDialog;
using ImprovisedEosl.Spike.SyncModal;
using System.Windows.Input;

var tests = new (string Name, Action Body)[]
{
    ("parses sizes and positions", ParsesSizesAndPositions),
    ("parses booleans", ParsesBooleans),
    ("parses documented boolean aliases", ParsesDocumentedBooleanAliases),
    ("accepts explicit wrapper boolean extensions", AcceptsExplicitWrapperBooleanExtensions),
    ("parses equals separators", ParsesEqualsSeparators),
    ("accepts mixed separators and empty entries", AcceptsMixedSeparatorsAndEmptyEntries),
    ("requires px units for dialog size", RequiresPxUnitsForDialogSize),
    ("parses decimal dialog sizes from IE mode measurement", ParsesDecimalDialogSizesFromIeModeMeasurement),
    ("ignores negative dialog sizes from IE mode measurement", IgnoresNegativeDialogSizesFromIeModeMeasurement),
    ("uses last duplicate size from IE mode measurement", UsesLastDuplicateSizeFromIeModeMeasurement),
    ("clamps timeout", ClampsTimeout),
    ("records unsupported fields", RecordsUnsupportedFields),
    ("ignores malformed values", IgnoresMalformedValues),
    ("calculates reference-validated window options", CalculatesReferenceValidatedWindowOptions),
    ("calculates omitted resizable from IE mode measurement", CalculatesOmittedResizableFromIeModeMeasurement),
    ("records measured and explicit feature policy diagnostics", RecordsMeasuredAndExplicitFeaturePolicyDiagnostics),
    ("canonicalizes compatibility origins with explicit ports", CanonicalizesCompatibilityOriginsWithExplicitPorts),
    ("rejects unsupported compatibility origins", RejectsUnsupportedCompatibilityOrigins),
    ("persists user-approved compatibility origins", PersistsUserApprovedCompatibilityOrigins),
    ("logs discarded approval entries", LogsDiscardedApprovalEntries),
    ("fails closed for a corrupt approval store", FailsClosedForCorruptApprovalStore),
    ("loads configured compatibility profiles", LoadsConfiguredCompatibilityProfiles),
    ("discards invalid configured profiles", DiscardsInvalidConfiguredProfiles),
    ("fails closed for corrupt compatibility profiles", FailsClosedForCorruptCompatibilityProfiles),
    ("rejects oversized compatibility profile files", RejectsOversizedCompatibilityProfileFiles),
    ("keeps configured grants separate from user approvals", KeepsConfiguredGrantsSeparateFromUserApprovals),
    ("resolves startup compatibility profiles", ResolvesStartupCompatibilityProfiles),
    ("accepts separated startup profile arguments", AcceptsSeparatedStartupProfileArguments),
    ("rejects invalid startup profile selections", RejectsInvalidStartupProfileSelections),
    ("accepts bounded JSON payloads", AcceptsBoundedJsonPayloads),
    ("accepts 5000-character strings from Edge IE measurement", AcceptsMeasuredFiveThousandCharacterStrings),
    ("rejects malformed and oversized JSON payloads", RejectsMalformedAndOversizedJsonPayloads),
    ("accepts undefined only as a return sentinel", AcceptsUndefinedOnlyAsReturnSentinel),
    ("requires claimed host origin to match current document", RequiresClaimedHostOriginToMatchCurrentDocument),
    ("restricts test host methods to the local test origin", RestrictsTestHostMethodsToLocalTestOrigin),
    ("accepts bounded HTTP dialog URLs", AcceptsBoundedHttpDialogUrls),
    ("rejects unsafe dialog URL forms", RejectsUnsafeDialogUrlForms),
    ("accepts bounded dialog feature input", AcceptsBoundedDialogFeatureInput),
    ("rejects excessive dialog feature input", RejectsExcessiveDialogFeatureInput),
    ("rotates diagnostic file logs", RotatesDiagnosticFileLogs),
    ("requires consecutive renderer-unresponsive observations", RequiresConsecutiveRendererUnresponsiveObservations),
    ("resets renderer-unresponsive observations after recovery", ResetsRendererUnresponsiveObservationsAfterRecovery),
    ("accepts local HTML selections", AcceptsLocalHtmlSelections),
    ("rejects invalid local HTML selections", RejectsInvalidLocalHtmlSelections),
    ("persists main window placement", PersistsMainWindowPlacement),
    ("rejects invalid main window placement", RejectsInvalidMainWindowPlacement),
    ("normalizes minimized window startup state", NormalizesMinimizedWindowStartupState),
    ("captures bounded window.open scrollbars and status features", CapturesWindowOpenFeatures),
    ("keeps legacy compatibility grants and denials separate", KeepsCompatibilityDecisionsSeparate),
    ("reports structured compatibility presentation states", ReportsStructuredCompatibilityPresentationStates),
    ("maps compatibility states to accessible shell presentation", MapsCompatibilityStatesToAccessibleShellPresentation),
    ("keeps operational status separate from compatibility policy", KeepsOperationalStatusSeparateFromCompatibilityPolicy),
    ("resolves raw window.open features before WebView2 hints", ResolvesWindowOpenFeatureApplication),
    ("accepts a same-origin first-child handoff", AcceptsSameOriginFirstChildHandoff),
    ("rejects unsafe and additional handoff children", RejectsUnsafeAndAdditionalHandoffChildren),
    ("requires handoff origin to remain current at close", RequiresHandoffOriginToRemainCurrentAtClose),
    ("persists browser initial URL settings", PersistsBrowserInitialUrlSettings),
    ("falls back for invalid browser settings", FallsBackForInvalidBrowserSettings),
    ("rejects unsafe browser initial URLs", RejectsUnsafeBrowserInitialUrls),
    ("resolves startup navigation precedence", ResolvesStartupNavigationPrecedence),
    ("round-trips portable user settings", RoundTripsPortableUserSettings),
    ("rejects conflicting portable settings", RejectsConflictingPortableSettings),
    ("rejects configured fields in portable settings", RejectsConfiguredFieldsInPortableSettings),
    ("formats main window title from document title", FormatsMainWindowTitleFromDocumentTitle),
    ("recognizes browser find shortcut", RecognizesBrowserFindShortcut),
    ("converts native window colors to COLORREF", ConvertsNativeWindowColorsToColorRef),
};

foreach (var test in tests)
{
    test.Body();
    Console.WriteLine($"PASS {test.Name}");
}

return 0;

static void CapturesWindowOpenFeatures()
{
    var parsed = WindowOpenFeatureCapturePolicy.Parse(
        "width=640,scrollbars=no,status=yes,scrollbars=on,status=0");
    Equal(true, parsed.IsValid);
    Equal(true, parsed.Scrollbars);
    Equal(false, parsed.Status);
    Equal(5, parsed.EntryCount);

    var omitted = WindowOpenFeatureCapturePolicy.Parse("width=640,height=480");
    Equal(null, omitted.Scrollbars);
    Equal(null, omitted.Status);

    var bare = WindowOpenFeatureCapturePolicy.Parse("scrollbars,status");
    Equal(true, bare.Scrollbars);
    Equal(true, bare.Status);

    var oversized = WindowOpenFeatureCapturePolicy.Parse(new string('x', WindowOpenFeatureCapturePolicy.MaxUtf8Bytes + 1));
    Equal(false, oversized.IsValid);
    Equal("too-large", oversized.ErrorCode);
}

static void FormatsMainWindowTitleFromDocumentTitle()
{
    Equal("Improvised EOSL", MainWindowTitlePolicy.Format(null));
    Equal("Improvised EOSL", MainWindowTitlePolicy.Format(""));
    Equal("Improvised EOSL", MainWindowTitlePolicy.Format("   "));
    Equal("Improvised EOSL", MainWindowTitlePolicy.Format("Improvised EOSL"));
    Equal("Legacy Order Entry - Improvised EOSL", MainWindowTitlePolicy.Format("Legacy Order Entry"));
    Equal("Legacy Order Entry - Improvised EOSL", MainWindowTitlePolicy.Format("  Legacy Order Entry  "));
}

static void RecognizesBrowserFindShortcut()
{
    Equal(true, BrowserFindShortcutPolicy.IsFindShortcut(Key.F, ModifierKeys.Control));
    Equal(false, BrowserFindShortcutPolicy.IsFindShortcut(Key.F, ModifierKeys.None));
    Equal(false, BrowserFindShortcutPolicy.IsFindShortcut(Key.F, ModifierKeys.Control | ModifierKeys.Shift));
    Equal(false, BrowserFindShortcutPolicy.IsFindShortcut(Key.G, ModifierKeys.Control));
}

static void ConvertsNativeWindowColorsToColorRef()
{
    Equal(0x001E140A, NativeWindowVisuals.ToColorRef(0x0A, 0x14, 0x1E));
    Equal(0x00EDECF5, NativeWindowVisuals.ToColorRef(245, 236, 237));
}

static void KeepsCompatibilityDecisionsSeparate()
{
    const string origin = "https://legacy.example";
    var modal = new UserApprovedCompatibility(origin, CompatibilityApi.ShowModalDialog);
    var windowOpen = new UserApprovedCompatibility(origin, CompatibilityApi.WindowOpenFeatures);
    var topLevelClose = new UserApprovedCompatibility(
        "https://legacy.example:443",
        CompatibilityApi.TopLevelCloseHandoff);
    var policy = new CompatibilityOriginPolicy([windowOpen], denials: [modal]);

    Equal(true, policy.IsAllowed(origin, CompatibilityApi.WindowOpenFeatures));
    Equal(false, policy.IsAllowed(origin, CompatibilityApi.ShowModalDialog));
    Equal(true, policy.IsDenied(origin, CompatibilityApi.ShowModalDialog));

    policy.Allow(origin, CompatibilityApi.ShowModalDialog);
    Equal(true, policy.IsAllowed(origin, CompatibilityApi.ShowModalDialog));
    Equal(false, policy.IsDenied(origin, CompatibilityApi.ShowModalDialog));
    Equal(
        "Compatibility: known legacy features enabled for this origin",
        policy.GetStatus(new Uri(origin)).Label);
    Equal(true, policy.Allow(origin, CompatibilityApi.TopLevelCloseHandoff));
    Equal(true, policy.IsAllowed(origin, CompatibilityApi.TopLevelCloseHandoff));
    Equal(true, policy.GetApprovals().Contains(topLevelClose));
    Equal(true, policy.Revoke(origin, CompatibilityApi.TopLevelCloseHandoff));
    Equal(false, policy.IsAllowed(origin, CompatibilityApi.TopLevelCloseHandoff));
    Equal(true, policy.Deny(origin, CompatibilityApi.TopLevelCloseHandoff));
    Equal(true, policy.IsDenied(origin, CompatibilityApi.TopLevelCloseHandoff));
    Equal(false, policy.GetApprovals().Contains(topLevelClose));
}

static void ReportsStructuredCompatibilityPresentationStates()
{
    const string origin = "https://status.example";
    var uri = new Uri(origin);
    var policy = new CompatibilityOriginPolicy();

    var untouched = policy.GetStatus(uri);
    Equal(CompatibilityStatusState.Undecided, untouched.State);
    Equal(0, untouched.EnabledApis.Count);
    Equal(0, untouched.DeniedApis.Count);
    Equal(0, untouched.DetectedApis.Count);
    Equal("Compatibility: off", untouched.Label);

    policy.Detect("https://status.example:443", CompatibilityApi.ShowModalDialog);
    var pending = policy.GetStatus(uri);
    Equal(CompatibilityStatusState.DetectionPending, pending.State);
    Equal(CompatibilityApi.ShowModalDialog, pending.DetectedApis.Single());

    policy.ClearPendingDetection();
    policy.Deny(origin, CompatibilityApi.ShowModalDialog);
    var denied = policy.GetStatus(uri);
    Equal(CompatibilityStatusState.Denied, denied.State);
    Equal(CompatibilityApi.ShowModalDialog, denied.DeniedApis.Single());
    Equal("Compatibility: off", denied.Label);

    policy.Allow(origin, CompatibilityApi.WindowOpenFeatures);
    var mixed = policy.GetStatus(uri);
    Equal(CompatibilityStatusState.Enabled, mixed.State);
    Equal(CompatibilityApi.WindowOpenFeatures, mixed.EnabledApis.Single());
    Equal(CompatibilityApi.ShowModalDialog, mixed.DeniedApis.Single());

    policy.Allow(origin, CompatibilityApi.ShowModalDialog);
    var multipleEnabled = policy.GetStatus(uri);
    Equal(CompatibilityStatusState.Enabled, multipleEnabled.State);
    Equal(2, multipleEnabled.EnabledApis.Count);
    Equal(0, multipleEnabled.DeniedApis.Count);
    Equal(
        "Compatibility: known legacy features enabled for this origin",
        multipleEnabled.Label);

    policy.ClearDecision(origin, CompatibilityApi.ShowModalDialog);
    policy.ClearDecision(origin, CompatibilityApi.WindowOpenFeatures);
    Equal(CompatibilityStatusState.Undecided, policy.GetStatus(uri).State);

    var blocked = policy.GetStatus(new Uri("file:///C:/legacy/index.html"));
    Equal(CompatibilityStatusState.Blocked, blocked.State);
    Equal("opaque", blocked.Origin);
}

static void MapsCompatibilityStatesToAccessibleShellPresentation()
{
    const string origin = "https://status.example:443";
    var cases = new[]
    {
        (CompatibilityStatusState.Undecided, "互換: 未決定", CompatibilityStatusIcon.Undecided),
        (CompatibilityStatusState.DetectionPending, "互換: 検出済み", CompatibilityStatusIcon.DetectionPending),
        (CompatibilityStatusState.Enabled, "互換: 有効", CompatibilityStatusIcon.Enabled),
        (CompatibilityStatusState.Denied, "互換: 拒否", CompatibilityStatusIcon.Denied),
        (CompatibilityStatusState.Blocked, "互換: ブロック", CompatibilityStatusIcon.Blocked)
    };

    foreach (var (state, expectedLabel, expectedIcon) in cases)
    {
        var status = new CompatibilityStatus(
            origin,
            "diagnostic label must not be parsed",
            state,
            state == CompatibilityStatusState.Enabled ? [CompatibilityApi.ShowModalDialog] : [],
            state == CompatibilityStatusState.Denied ? [CompatibilityApi.WindowOpenFeatures] : [],
            state == CompatibilityStatusState.DetectionPending ? [CompatibilityApi.TopLevelCloseHandoff] : []);

        var presentation = CompatibilityStatusPresentationPolicy.Create(status);

        Equal(expectedLabel, presentation.ShortLabel);
        Equal(expectedIcon, presentation.Icon);
        Equal(presentation.AccessibleText, presentation.DetailText);
        Equal(true, presentation.AccessibleText.Contains(origin, StringComparison.Ordinal));
        Equal(true, presentation.AccessibleText.Contains("有効なAPI:", StringComparison.Ordinal));
        Equal(true, presentation.AccessibleText.Contains("拒否したAPI:", StringComparison.Ordinal));
        Equal(true, presentation.AccessibleText.Contains("検出したAPI:", StringComparison.Ordinal));
        Equal(false, presentation.AccessibleText.Contains(status.Label, StringComparison.Ordinal));
    }

    var mixed = CompatibilityStatusPresentationPolicy.Create(new CompatibilityStatus(
        origin,
        "ignored",
        CompatibilityStatusState.Enabled,
        [CompatibilityApi.ShowModalDialog, CompatibilityApi.WindowOpenFeatures],
        [CompatibilityApi.TopLevelCloseHandoff],
        []));
    Equal(true, mixed.AccessibleText.Contains(CompatibilityApi.ShowModalDialog, StringComparison.Ordinal));
    Equal(true, mixed.AccessibleText.Contains(CompatibilityApi.WindowOpenFeatures, StringComparison.Ordinal));
    Equal(true, mixed.AccessibleText.Contains(CompatibilityApi.TopLevelCloseHandoff, StringComparison.Ordinal));
}

static void KeepsOperationalStatusSeparateFromCompatibilityPolicy()
{
    var initializing = CompatibilityStatusPresentationPolicy.CreateOperational(
        CompatibilityOperationalStatus.Initializing);
    Equal("互換: 確認中", initializing.ShortLabel);
    Equal(CompatibilityStatusIcon.Operational, initializing.Icon);
    Equal(true, initializing.AccessibleText.Contains("初期化中", StringComparison.Ordinal));

    var recovering = CompatibilityStatusPresentationPolicy.CreateOperational(
        CompatibilityOperationalStatus.Recovering);
    Equal("互換: 確認中", recovering.ShortLabel);
    Equal(CompatibilityStatusIcon.Operational, recovering.Icon);
    Equal(true, recovering.AccessibleText.Contains("復旧中", StringComparison.Ordinal));

    var failed = CompatibilityStatusPresentationPolicy.CreateOperational(
        CompatibilityOperationalStatus.RecoveryFailed);
    Equal("互換: エラー", failed.ShortLabel);
    Equal(CompatibilityStatusIcon.Error, failed.Icon);
    Equal(true, failed.AccessibleText.Contains("許可・拒否を表す状態ではありません", StringComparison.Ordinal));
}

static void ResolvesWindowOpenFeatureApplication()
{
    var raw = WindowOpenFeatureCapturePolicy.Parse("scrollbars=yes,status=no");
    var resolved = WindowOpenFeatureApplicationPolicy.Resolve(raw, exposedScrollbars: false, exposedStatus: true);
    Equal(true, resolved.DisplayScrollbars);
    Equal(false, resolved.DisplayStatus);
    Equal("raw", resolved.ScrollbarsSource);
    Equal("raw", resolved.StatusSource);

    var omitted = WindowOpenFeatureApplicationPolicy.Resolve(
        WindowOpenFeatureCapturePolicy.Parse("width=640"),
        exposedScrollbars: true,
        exposedStatus: false);
    Equal(true, omitted.DisplayScrollbars);
    Equal(false, omitted.DisplayStatus);
    Equal("safe-default", omitted.ScrollbarsSource);

    var omittedDespiteFalseHint = WindowOpenFeatureApplicationPolicy.Resolve(
        WindowOpenFeatureCapturePolicy.Parse("status=no"),
        exposedScrollbars: false,
        exposedStatus: true);
    Equal(true, omittedDespiteFalseHint.DisplayScrollbars);
    Equal("safe-default", omittedDespiteFalseHint.ScrollbarsSource);
}

static void AcceptsLocalHtmlSelections()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    var path = Path.Combine(directory, "small parent #1.HTML");
    File.WriteAllText(path, "<html></html>");

    try
    {
        var result = LocalHtmlSelectionPolicy.Validate(path);

        Equal(true, result.IsValid);
        Equal(Path.GetFullPath(path), result.Selection?.FullPath);
        Equal(Path.GetFullPath(directory), result.Selection?.RootPath);
        Equal("small%20parent%20%231.HTML", result.Selection?.RelativeUrlPath);
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void RejectsInvalidLocalHtmlSelections()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    var textPath = Path.Combine(directory, "notes.txt");
    File.WriteAllText(textPath, "not html");

    try
    {
        Equal(LocalHtmlSelectionError.EmptyPath, LocalHtmlSelectionPolicy.Validate(" ").Error);
        Equal(LocalHtmlSelectionError.PathNotAbsolute, LocalHtmlSelectionPolicy.Validate("page.html").Error);
        Equal(LocalHtmlSelectionError.FileNotFound, LocalHtmlSelectionPolicy.Validate(Path.Combine(directory, "missing.html")).Error);
        Equal(LocalHtmlSelectionError.UnsupportedExtension, LocalHtmlSelectionPolicy.Validate(textPath).Error);
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void RequiresConsecutiveRendererUnresponsiveObservations()
{
    var tracker = new RendererUnresponsiveTracker(2);

    Equal(false, tracker.Observe());
    Equal(1, tracker.Count);
    Equal(true, tracker.Observe());
    Equal(2, tracker.Count);
}

static void ResetsRendererUnresponsiveObservationsAfterRecovery()
{
    var tracker = new RendererUnresponsiveTracker(2);

    tracker.Observe();
    tracker.MarkResponsive();

    Equal(0, tracker.Count);
    Equal(false, tracker.Observe());
}

static void ParsesSizesAndPositions()
{
    var parsed = DialogFeatureParser.Parse(
        "dialogWidth:500px;dialogHeight:300px;dialogLeft:10;dialogTop:20px");

    Equal(500, parsed.Width);
    Equal(300, parsed.Height);
    Equal(10, parsed.Left);
    Equal(20, parsed.Top);
}

static void ParsesBooleans()
{
    var parsed = DialogFeatureParser.Parse(
        "center:yes;resizable:no;status:1;scroll:off");

    Equal(true, parsed.Center);
    Equal(false, parsed.Resizable);
    Equal(true, parsed.Status);
    Equal(false, parsed.Scroll);
}

static void ParsesDocumentedBooleanAliases()
{
    foreach (var value in new[] { "yes", "on", "1" })
    {
        Equal(true, DialogFeatureParser.Parse($"center:{value}").Center);
    }

    foreach (var value in new[] { "no", "off", "0" })
    {
        Equal(false, DialogFeatureParser.Parse($"center:{value}").Center);
    }
}

static void AcceptsExplicitWrapperBooleanExtensions()
{
    Equal(true, DialogFeatureParser.Parse("center:true").Center);
    Equal(false, DialogFeatureParser.Parse("center:false").Center);
    Equal(true, DialogFeatureParser.Parse("center:").Center);
}

static void ParsesEqualsSeparators()
{
    var parsed = DialogFeatureParser.Parse("dialogWidth=640px;center=true");

    Equal(640, parsed.Width);
    Equal(true, parsed.Center);
}

static void AcceptsMixedSeparatorsAndEmptyEntries()
{
    var parsed = DialogFeatureParser.Parse(
        ";;dialogWidth:640px;dialogHeight=480px;;center:on;");

    Equal(640, parsed.Width);
    Equal(480, parsed.Height);
    Equal(true, parsed.Center);
}

static void RequiresPxUnitsForDialogSize()
{
    var parsed = DialogFeatureParser.Parse(
        "dialogWidth:500;dialogHeight:300;dialogLeft:10;dialogTop:20");

    Equal(null, parsed.Width);
    Equal(null, parsed.Height);
    Equal(10, parsed.Left);
    Equal(20, parsed.Top);
}

static void ParsesDecimalDialogSizesFromIeModeMeasurement()
{
    var parsed = DialogFeatureParser.Parse("dialogWidth:500.8px;dialogHeight:300.2px");

    Equal(500, parsed.Width);
    Equal(300, parsed.Height);
}

static void IgnoresNegativeDialogSizesFromIeModeMeasurement()
{
    var parsed = DialogFeatureParser.Parse("dialogWidth:-500px;dialogHeight:-300px;dialogLeft:-10px;dialogTop:-20px");

    Equal(null, parsed.Width);
    Equal(null, parsed.Height);
    Equal(-10, parsed.Left);
    Equal(-20, parsed.Top);
}

static void UsesLastDuplicateSizeFromIeModeMeasurement()
{
    var parsed = DialogFeatureParser.Parse("dialogWidth:400px;dialogWidth:700px;dialogHeight:250px;dialogHeight:450px");

    Equal(700, parsed.Width);
    Equal(450, parsed.Height);
}

static void ClampsTimeout()
{
    Equal(TimeSpan.FromMilliseconds(1_000), DialogFeatureParser.Parse("timeoutMs:10").Timeout);
    Equal(TimeSpan.FromMilliseconds(90_000), DialogFeatureParser.Parse("timeoutMs:999999").Timeout);
    Equal(TimeSpan.FromMilliseconds(3_000), DialogFeatureParser.Parse("timeoutMs:3000").Timeout);
}

static void RecordsUnsupportedFields()
{
    var parsed = DialogFeatureParser.Parse("help:yes;foo:bar");

    Equal("yes", parsed.Unsupported["help"]);
    Equal("bar", parsed.Unsupported["foo"]);
}

static void IgnoresMalformedValues()
{
    var parsed = DialogFeatureParser.Parse("dialogWidth:wide;center:maybe;timeoutMs:nope");

    Equal(null, parsed.Width);
    Equal(null, parsed.Center);
    Equal(TimeSpan.FromMilliseconds(90_000), parsed.Timeout);
}

static void CalculatesReferenceValidatedWindowOptions()
{
    var parsed = DialogFeatureParser.Parse(
        "dialogWidth:500px;dialogHeight:300px;dialogLeft:10px;dialogTop:20px;center:no;resizable:no");

    var options = DialogFeatureApplicationPolicy.Calculate(parsed);

    Equal(500d, options.Width);
    Equal(300d, options.Height);
    Equal(10d, options.Left);
    Equal(20d, options.Top);
    Equal(false, options.Center);
    Equal(DialogResizeMode.NoResize, options.ResizeMode);
    Equal(DialogFeaturePolicyStatus.ReferenceValidated, options.PolicyStatus);
}

static void CalculatesOmittedResizableFromIeModeMeasurement()
{
    var parsed = DialogFeatureParser.Parse("dialogWidth:500px;dialogHeight:300px");

    var options = DialogFeatureApplicationPolicy.Calculate(parsed);

    Equal(DialogResizeMode.NoResize, options.ResizeMode);
    ContainsDiagnostic(options, "resizable", DialogFeatureDiagnosticKind.Applied);
}

static void RecordsMeasuredAndExplicitFeaturePolicyDiagnostics()
{
    var parsed = DialogFeatureParser.Parse(
        "dialogWidth:10px;dialogHeight:9999px;status:yes;scroll:no;foo:bar");

    var options = DialogFeatureApplicationPolicy.Calculate(parsed);

    Equal(250d, options.Width);
    Equal(2000d, options.Height);
    Equal(DialogFeaturePolicyStatus.ReferenceValidated, options.PolicyStatus);
    ContainsDiagnostic(options, "dialogWidth", DialogFeatureDiagnosticKind.Clamped);
    ContainsDiagnostic(options, "dialogHeight", DialogFeatureDiagnosticKind.Clamped);
    ContainsDiagnostic(options, "center", DialogFeatureDiagnosticKind.Applied);
    ContainsDiagnostic(options, "resizable", DialogFeatureDiagnosticKind.Applied);
    ContainsDiagnostic(options, "status", DialogFeatureDiagnosticKind.Unsupported);
    ContainsDiagnostic(options, "scroll", DialogFeatureDiagnosticKind.Unsupported);
    ContainsDiagnostic(options, "foo", DialogFeatureDiagnosticKind.Unsupported);
}

static void CanonicalizesCompatibilityOriginsWithExplicitPorts()
{
    Equal("https://example.com:443", CompatibilityOriginPolicy.NormalizeOrigin("HTTPS://Example.COM/path"));
    Equal("http://127.0.0.1:18080", CompatibilityOriginPolicy.NormalizeOrigin("http://127.0.0.1:18080/page"));

    var policy = new CompatibilityOriginPolicy();
    Equal(true, policy.Allow("https://example.com", CompatibilityApi.ShowModalDialog));
    Equal(true, policy.IsAllowed("https://EXAMPLE.com:443", CompatibilityApi.ShowModalDialog));
}

static void RejectsUnsupportedCompatibilityOrigins()
{
    var policy = new CompatibilityOriginPolicy();

    Equal(false, policy.Allow("file:///C:/legacy/index.html", CompatibilityApi.ShowModalDialog));
    Equal(false, policy.Allow("not an origin", CompatibilityApi.ShowModalDialog));
    Equal(false, policy.IsAllowed("file:///C:/legacy/index.html", CompatibilityApi.ShowModalDialog));
    Equal("opaque", CompatibilityOriginPolicy.GetOrigin(new Uri("file:///C:/legacy/index.html")));
}

static void PersistsUserApprovedCompatibilityOrigins()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "approvals.json");
    try
    {
        var store = new UserApprovedOriginStore(path);
        store.Save(new[]
        {
            new UserApprovedCompatibility("https://Example.com", CompatibilityApi.ShowModalDialog),
            new UserApprovedCompatibility("https://example.com:443/path", CompatibilityApi.ShowModalDialog),
            new UserApprovedCompatibility("file:///C:/legacy.html", CompatibilityApi.ShowModalDialog)
        }, [new UserApprovedCompatibility("https://Example.com", CompatibilityApi.WindowOpenFeatures)]);

        var loaded = store.Load();
        Equal(null, loaded.Diagnostic);
        Equal(1, loaded.Approvals.Count);
        Equal(1, loaded.Denials.Count);
        Equal("https://example.com:443", loaded.Approvals[0].Origin);
        Equal(CompatibilityApi.WindowOpenFeatures, loaded.Denials[0].ApiName);

        var policy = new CompatibilityOriginPolicy(loaded.Approvals);
        Equal(true, policy.IsAllowed("https://example.com", CompatibilityApi.ShowModalDialog));
        Equal(true, policy.Revoke("https://example.com:443", CompatibilityApi.ShowModalDialog));
        Equal(0, policy.GetApprovals().Count);
        store.Save(policy.GetApprovals());
        Equal(0, store.Load().Approvals.Count);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void FailsClosedForCorruptApprovalStore()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "approvals.json");
    try
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "{not-json");

        var loaded = new UserApprovedOriginStore(path).Load();
        Equal(0, loaded.Approvals.Count);
        Equal(true, loaded.Diagnostic is not null);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void LogsDiscardedApprovalEntries()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "approvals.json");
    try
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            path,
            """
            {
              "Version": 1,
              "Approvals": [
                { "Origin": "https://valid.example", "ApiName": "window.showModalDialog" },
                { "Origin": "file:///C:/legacy.html", "ApiName": "window.showModalDialog" },
                { "Origin": "https://other.example", "ApiName": "window.futureApi" }
              ]
            }
            """);

        var loaded = new UserApprovedOriginStore(path).Load();
        Equal(1, loaded.Approvals.Count);
        Equal("https://valid.example:443", loaded.Approvals[0].Origin);
        Equal(true, loaded.Diagnostic is not null);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void LoadsConfiguredCompatibilityProfiles()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "compatibility-profiles.json");
    try
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            path,
            """
            {
              "version": 1,
              "profiles": [
                {
                  "id": "legacy-orders",
                  "displayName": "Legacy Orders",
                  "startUrl": "https://orders.example/app/",
                  "allowedOrigins": [
                    "https://ORDERS.example",
                    "https://orders.example:443/"
                  ],
                  "compatibility": { "showModalDialog": true }
                },
                {
                  "id": "modern-site",
                  "startUrl": "https://modern.example/",
                  "allowedOrigins": ["https://modern.example"],
                  "compatibility": { "showModalDialog": false }
                }
              ]
            }
            """);

        var loaded = new CompatibilityProfileStore(path).Load();
        Equal(2, loaded.Profiles.Count);
        Equal(1, loaded.Compatibility.Count);
        Equal(0, loaded.Diagnostics.Count);
        Equal("https://orders.example:443", loaded.Compatibility[0].Origin);
        Equal("modern-site", loaded.Profiles[1].DisplayName);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void DiscardsInvalidConfiguredProfiles()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "compatibility-profiles.json");
    try
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            path,
            """
            {
              "version": 1,
              "profiles": [
                {
                  "id": "valid",
                  "startUrl": "https://valid.example/app/",
                  "allowedOrigins": ["https://valid.example"],
                  "compatibility": { "showModalDialog": true }
                },
                {
                  "id": "path-is-not-origin",
                  "startUrl": "https://invalid.example/",
                  "allowedOrigins": ["https://invalid.example/app"],
                  "compatibility": { "showModalDialog": true }
                },
                {
                  "id": "valid",
                  "startUrl": "https://duplicate.example/",
                  "allowedOrigins": ["https://duplicate.example"],
                  "compatibility": { "showModalDialog": true }
                },
                {
                  "id": "misspelled-api",
                  "startUrl": "https://typo.example/",
                  "allowedOrigins": ["https://typo.example"],
                  "compatibility": { "showModalDilog": true }
                },
                null
              ]
            }
            """);

        var loaded = new CompatibilityProfileStore(path).Load();
        Equal(1, loaded.Profiles.Count);
        Equal(1, loaded.Compatibility.Count);
        Equal(4, loaded.Diagnostics.Count);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void FailsClosedForCorruptCompatibilityProfiles()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "compatibility-profiles.json");
    try
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "{not-json");

        var loaded = new CompatibilityProfileStore(path).Load();
        Equal(0, loaded.Profiles.Count);
        Equal(0, loaded.Compatibility.Count);
        Equal(1, loaded.Diagnostics.Count);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void KeepsConfiguredGrantsSeparateFromUserApprovals()
{
    var configured = new ConfiguredCompatibility(
        "legacy-orders",
        "https://configured.example",
        CompatibilityApi.ShowModalDialog);
    var userApproval = new UserApprovedCompatibility(
        "https://user.example",
        CompatibilityApi.ShowModalDialog);
    var policy = new CompatibilityOriginPolicy(new[] { userApproval }, new[] { configured });

    Equal(true, policy.IsConfigured("https://configured.example", CompatibilityApi.ShowModalDialog));
    Equal(true, policy.IsAllowed("https://configured.example", CompatibilityApi.ShowModalDialog));
    Equal(false, policy.Revoke("https://configured.example", CompatibilityApi.ShowModalDialog));
    Equal(true, policy.IsAllowed("https://configured.example", CompatibilityApi.ShowModalDialog));
    Equal(1, policy.GetApprovals().Count);

    Equal(true, policy.Revoke("https://user.example", CompatibilityApi.ShowModalDialog));
    Equal(false, policy.IsAllowed("https://user.example", CompatibilityApi.ShowModalDialog));
    Equal(true, policy.IsAllowed("https://configured.example", CompatibilityApi.ShowModalDialog));
    Equal(0, policy.GetApprovals().Count);
}

static void RejectsOversizedCompatibilityProfileFiles()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "compatibility-profiles.json");
    try
    {
        Directory.CreateDirectory(directory);
        using (var stream = File.Create(path))
        {
            stream.SetLength(CompatibilityProfileStore.MaxFileBytes + 1L);
        }

        var loaded = new CompatibilityProfileStore(path).Load();
        Equal(0, loaded.Profiles.Count);
        Equal(0, loaded.Compatibility.Count);
        Equal(1, loaded.Diagnostics.Count);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void ResolvesStartupCompatibilityProfiles()
{
    var profiles = CreateStartupProfiles();
    var selected = StartupProfileSelection.Resolve(
        new[] { "app.exe", "--profile=LEGACY-ORDERS" },
        profiles);

    Equal(null, selected.Error);
    Equal(true, selected.IsSpecified);
    Equal("legacy-orders", selected.Profile?.Id);
    Equal(new Uri("https://orders.example/app/"), selected.Profile?.StartUrl);

    var unspecified = StartupProfileSelection.Resolve(new[] { "app.exe" }, profiles);
    Equal(false, unspecified.IsSpecified);
    Equal(null, unspecified.Profile);
}

static void AcceptsSeparatedStartupProfileArguments()
{
    var selected = StartupProfileSelection.Resolve(
        new[] { "app.exe", "--profile", "legacy-orders" },
        CreateStartupProfiles());

    Equal(null, selected.Error);
    Equal("legacy-orders", selected.Profile?.Id);
}

static void RejectsInvalidStartupProfileSelections()
{
    var profiles = CreateStartupProfiles();

    Equal(
        StartupProfileSelectionError.MissingId,
        StartupProfileSelection.Resolve(new[] { "app.exe", "--profile" }, profiles).Error);
    Equal(
        StartupProfileSelectionError.MultipleSelections,
        StartupProfileSelection.Resolve(
            new[] { "app.exe", "--profile=legacy-orders", "--profile", "other" },
            profiles).Error);

    var unknown = StartupProfileSelection.Resolve(
        new[] { "app.exe", "--profile=missing" },
        profiles);
    Equal(StartupProfileSelectionError.UnknownProfile, unknown.Error);
    Equal("missing", unknown.RequestedId);
}

static IReadOnlyList<CompatibilityProfile> CreateStartupProfiles()
{
    return new[]
    {
        new CompatibilityProfile(
            "legacy-orders",
            "Legacy Orders",
            new Uri("https://orders.example/app/"),
            new[] { "https://orders.example:443" },
            true)
    };
}

static void AcceptsBoundedJsonPayloads()
{
    Equal(true, JsonPayloadPolicy.ValidateArguments("{\"id\":123,\"items\":[true,null,\"x\"]}").IsValid);
    Equal(true, JsonPayloadPolicy.ValidateArguments("42").IsValid);
    Equal(true, JsonPayloadPolicy.ValidateReturnValue("\"done\"").IsValid);
}

static void AcceptsMeasuredFiveThousandCharacterStrings()
{
    var serializedString = "\"" + new string('x', 5_000) + "\"";

    var validation = JsonPayloadPolicy.ValidateArguments(serializedString);

    Equal(true, validation.IsValid);
    Equal(serializedString, validation.Json);
}

static void RejectsMalformedAndOversizedJsonPayloads()
{
    Equal("invalid-json", JsonPayloadPolicy.ValidateArguments("{bad-json").ErrorCode);

    var oversized = "\"" + new string('x', JsonPayloadPolicy.MaxPayloadUtf8Bytes) + "\"";
    var validation = JsonPayloadPolicy.ValidateArguments(oversized);
    Equal(false, validation.IsValid);
    Equal("too-large", validation.ErrorCode);
    Equal(true, JsonPayloadPolicy.Summarize(oversized).Contains("[truncated;", StringComparison.Ordinal));
}

static void AcceptsUndefinedOnlyAsReturnSentinel()
{
    Equal(false, JsonPayloadPolicy.ValidateArguments("undefined").IsValid);
    Equal(true, JsonPayloadPolicy.ValidateReturnValue("undefined").IsValid);
    Equal("undefined", JsonPayloadPolicy.ValidateReturnValue(null).Json);
}

static void RequiresClaimedHostOriginToMatchCurrentDocument()
{
    var current = new Uri("https://legacy.example/app/page");

    Equal(true, HostOriginGuard.IsClaimedOriginCurrent(current, "https://LEGACY.example:443"));
    Equal(false, HostOriginGuard.IsClaimedOriginCurrent(current, "https://attacker.example"));
    Equal(false, HostOriginGuard.IsClaimedOriginCurrent(current, "file:///C:/legacy.html"));
    Equal(false, HostOriginGuard.IsClaimedOriginCurrent(null, "https://legacy.example"));
}

static void RestrictsTestHostMethodsToLocalTestOrigin()
{
    var testBase = new Uri("http://127.0.0.1:18080/");

    Equal(true, HostOriginGuard.IsSameOrigin(new Uri("http://127.0.0.1:18080/parent.html"), testBase));
    Equal(false, HostOriginGuard.IsSameOrigin(new Uri("http://127.0.0.1:18081/parent.html"), testBase));
    Equal(false, HostOriginGuard.IsSameOrigin(new Uri("https://example.com/"), testBase));
    Equal(false, HostOriginGuard.IsSameOrigin(new Uri("file:///C:/test.html"), testBase));
}

static void AcceptsBoundedHttpDialogUrls()
{
    Equal(true, DialogNavigationPolicy.Validate("https://legacy.example/dialog?id=123#section").IsValid);
    Equal(true, DialogNavigationPolicy.Validate("http://127.0.0.1:18080/dialog.html").IsValid);
    Equal("https://legacy.example/dialog", DialogNavigationPolicy.FormatForLog("https://legacy.example/dialog?secret=value#fragment"));
}

static void RejectsUnsafeDialogUrlForms()
{
    Equal("unsupported-scheme", DialogNavigationPolicy.Validate("file:///C:/Windows/win.ini").ErrorCode);
    Equal("unsupported-scheme", DialogNavigationPolicy.Validate("javascript:alert(1)").ErrorCode);
    Equal("userinfo-not-allowed", DialogNavigationPolicy.Validate("https://user:password@example.com/dialog").ErrorCode);
    Equal("too-long", DialogNavigationPolicy.Validate("https://example.com/" + new string('x', DialogNavigationPolicy.MaxUrlCharacters)).ErrorCode);
    Equal("missing", DialogNavigationPolicy.Validate(null).ErrorCode);
}

static void AcceptsBoundedDialogFeatureInput()
{
    var validation = DialogFeatureInputPolicy.Validate(
        "dialogWidth:500px;dialogHeight:300px;center:yes");

    Equal(true, validation.IsValid);
    Equal(3, validation.EntryCount);
    Equal(true, DialogFeatureInputPolicy.Validate(null).IsValid);
}

static void RejectsExcessiveDialogFeatureInput()
{
    var oversized = DialogFeatureInputPolicy.Validate(
        new string('x', DialogFeatureInputPolicy.MaxUtf8Bytes + 1));
    Equal("too-large", oversized.ErrorCode);

    var tooManyEntries = DialogFeatureInputPolicy.Validate(
        string.Join(';', Enumerable.Repeat("center:yes", DialogFeatureInputPolicy.MaxEntries + 1)));
    Equal("too-many-entries", tooManyEntries.ErrorCode);
    Equal(DialogFeatureInputPolicy.MaxEntries + 1, tooManyEntries.EntryCount);
}

static void RotatesDiagnosticFileLogs()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "diagnostic.log");
    try
    {
        var log = new RollingFileLog(path, maxBytes: 32);
        log.AppendLine("first-entry-that-fills-the-log");
        log.AppendLine("second-entry");

        Equal(true, File.Exists(log.Path));
        Equal(true, File.Exists(log.BackupPath));
        Equal(true, File.ReadAllText(log.BackupPath).Contains("first-entry", StringComparison.Ordinal));
        Equal(true, File.ReadAllText(log.Path).Contains("second-entry", StringComparison.Ordinal));
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void PersistsMainWindowPlacement()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "main-window-placement.json");
    try
    {
        var store = new MainWindowPlacementStore(path);
        var expected = new MainWindowPlacement(-1200, 80, 1100, 760, true);
        store.Save(expected);

        var loaded = store.Load();
        Equal(expected, loaded.Placement);
        Equal<string?>(null, loaded.Diagnostic);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void RejectsInvalidMainWindowPlacement()
{
    Equal(false, MainWindowPlacementStore.IsValid(new(0, 0, 100, 100, false)));
    Equal(false, MainWindowPlacementStore.IsValid(new(double.NaN, 0, 1100, 760, false)));
    Equal(false, MainWindowPlacementStore.IsValid(new(0, 0, 50_000, 760, false)));

    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "main-window-placement.json");
    Directory.CreateDirectory(directory);
    File.WriteAllText(path, "{not-json");
    try
    {
        var loaded = new MainWindowPlacementStore(path).Load();
        Equal<MainWindowPlacement?>(null, loaded.Placement);
        Equal(true, loaded.Diagnostic is not null);
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void NormalizesMinimizedWindowStartupState()
{
    Equal(false, MainWindowPlacementStore.ShouldReopenMaximized(
        MainWindowDisplayState.Minimized,
        MainWindowDisplayState.Normal));
    Equal(true, MainWindowPlacementStore.ShouldReopenMaximized(
        MainWindowDisplayState.Minimized,
        MainWindowDisplayState.Maximized));
    Equal(true, MainWindowPlacementStore.ShouldReopenMaximized(
        MainWindowDisplayState.Maximized,
        MainWindowDisplayState.Normal));
}

static void AcceptsSameOriginFirstChildHandoff()
{
    var selected = TopLevelHandoffSelectionPolicy.Select(
        new Uri("https://legacy.example/launcher"),
        "https://legacy.example:443/business?id=1",
        hasPendingChild: false);

    Equal(true, selected.IsAccepted);
    Equal("https://legacy.example/business?id=1", selected.TargetUri?.ToString());
    Equal("https://legacy.example:443", selected.ParentOrigin);
    Equal("accepted", selected.Reason);
}

static void RejectsUnsafeAndAdditionalHandoffChildren()
{
    var parent = new Uri("https://legacy.example/launcher");
    var crossOrigin = TopLevelHandoffSelectionPolicy.Select(
        parent,
        "https://other.example/business",
        hasPendingChild: false);
    Equal(false, crossOrigin.IsAccepted);
    Equal("cross-origin", crossOrigin.Reason);

    var unsafeTarget = TopLevelHandoffSelectionPolicy.Select(
        parent,
        "file:///C:/business.html",
        hasPendingChild: false);
    Equal(false, unsafeTarget.IsAccepted);
    Equal("invalid-target:unsupported-scheme", unsafeTarget.Reason);

    var additional = TopLevelHandoffSelectionPolicy.Select(
        parent,
        "https://legacy.example/second",
        hasPendingChild: true);
    Equal(false, additional.IsAccepted);
    Equal("additional-child", additional.Reason);
}

static void RequiresHandoffOriginToRemainCurrentAtClose()
{
    Equal(
        true,
        TopLevelHandoffSelectionPolicy.CanApply(
            new Uri("https://legacy.example/launcher?state=ready"),
            "https://legacy.example"));
    Equal(
        false,
        TopLevelHandoffSelectionPolicy.CanApply(
            new Uri("https://other.example/launcher"),
            "https://legacy.example"));
}

static void PersistsBrowserInitialUrlSettings()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "browser-settings.json");
    try
    {
        var store = new BrowserSettingsStore(path);
        var expected = new BrowserSettings(new Uri("https://legacy.example/start?mode=1#top"));
        store.Save(expected);

        var loaded = store.Load();
        Equal(expected, loaded.Settings);
        Equal<string?>(null, loaded.Diagnostic);

        store.Save(new BrowserSettings(null));
        Equal<Uri?>(null, store.Load().Settings.InitialUrl);
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

static void FallsBackForInvalidBrowserSettings()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "browser-settings.json");
    Directory.CreateDirectory(directory);
    try
    {
        File.WriteAllText(path, "{\"version\":1,\"initialUrl\":\"file:///C:/legacy.html\"}");
        var unsafeUrl = new BrowserSettingsStore(path).Load();
        Equal<Uri?>(null, unsafeUrl.Settings.InitialUrl);
        Equal(true, unsafeUrl.Diagnostic is not null);

        File.WriteAllText(path, "{\"version\":1,\"initialUrl\":null,\"unexpected\":true}");
        var unknownProperty = new BrowserSettingsStore(path).Load();
        Equal<Uri?>(null, unknownProperty.Settings.InitialUrl);
        Equal(true, unknownProperty.Diagnostic is not null);
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void RejectsUnsafeBrowserInitialUrls()
{
    Equal(true, BrowserSettingsStore.TryParseInitialUrl(null, out var empty));
    Equal<Uri?>(null, empty);
    Equal(true, BrowserSettingsStore.TryParseInitialUrl("https://example.com/app", out _));
    Equal(false, BrowserSettingsStore.TryParseInitialUrl("file:///C:/legacy.html", out _));
    Equal(false, BrowserSettingsStore.TryParseInitialUrl("https://user:secret@example.com/", out _));
    Equal(false, BrowserSettingsStore.TryParseInitialUrl(
        "https://example.com/" + new string('x', BrowserSettingsStore.MaxUrlCharacters), out _));
}

static void ResolvesStartupNavigationPrecedence()
{
    var home = new Uri("http://127.0.0.1:18080/home.html");
    var automatic = new Uri("http://127.0.0.1:18080/parent.html?auto=1");
    var user = new BrowserSettings(new Uri("https://user.example/start"));
    var profile = new CompatibilityProfile(
        "orders",
        "Orders",
        new Uri("https://profile.example/start"),
        ["https://profile.example:443"],
        true);

    var automaticDecision = StartupNavigationPolicy.Resolve(automatic, profile, user, home);
    Equal(automatic, automaticDecision.Uri);
    Equal(StartupNavigationSource.AutomaticValidation, automaticDecision.Source);

    var profileDecision = StartupNavigationPolicy.Resolve(null, profile, user, home);
    Equal(profile.StartUrl, profileDecision.Uri);
    Equal(StartupNavigationSource.SelectedProfile, profileDecision.Source);

    var userDecision = StartupNavigationPolicy.Resolve(null, null, user, home);
    Equal(user.InitialUrl, userDecision.Uri);
    Equal(StartupNavigationSource.UserSettings, userDecision.Source);

    var homeDecision = StartupNavigationPolicy.Resolve(null, null, new BrowserSettings(null), home);
    Equal(home, homeDecision.Uri);
    Equal(StartupNavigationSource.BuiltInHome, homeDecision.Source);
}

static void RoundTripsPortableUserSettings()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "portable-settings.json");
    try
    {
        var expected = new PortableUserSettings(
            new BrowserSettings(new Uri("https://legacy.example/start?mode=1")),
            [new UserApprovedCompatibility("https://legacy.example:443", CompatibilityApi.ShowModalDialog)],
            [new UserApprovedCompatibility("https://denied.example:443", CompatibilityApi.WindowOpenFeatures)]);
        var store = new PortableUserSettingsStore();
        store.Save(path, expected);

        var loaded = store.Load(path);
        Equal<string?>(null, loaded.Diagnostic);
        Equal(expected.Browser, loaded.Settings!.Browser);
        Equal(true, expected.Approvals.SequenceEqual(loaded.Settings.Approvals));
        Equal(true, expected.Denials.SequenceEqual(loaded.Settings.Denials));
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}

static void RejectsConflictingPortableSettings()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "portable-settings.json");
    Directory.CreateDirectory(directory);
    try
    {
        File.WriteAllText(path, """
            {"version":1,"initialUrl":null,
             "approvals":[{"origin":"https://legacy.example","apiName":"window.showModalDialog"}],
             "denials":[{"origin":"https://legacy.example:443","apiName":"window.showModalDialog"}]}
            """);
        var loaded = new PortableUserSettingsStore().Load(path);
        Equal<PortableUserSettings?>(null, loaded.Settings);
        Equal(true, loaded.Diagnostic?.Contains("conflicting", StringComparison.Ordinal) == true);
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void RejectsConfiguredFieldsInPortableSettings()
{
    var directory = Path.Combine(Path.GetTempPath(), "ImprovisedEoslTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "portable-settings.json");
    Directory.CreateDirectory(directory);
    try
    {
        File.WriteAllText(path, "{\"version\":1,\"initialUrl\":null,\"approvals\":[],\"denials\":[],\"profiles\":[]}");
        var loaded = new PortableUserSettingsStore().Load(path);
        Equal<PortableUserSettings?>(null, loaded.Settings);
        Equal(true, loaded.Diagnostic?.Contains("unknown properties", StringComparison.Ordinal) == true);
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}");
    }
}

static void ContainsDiagnostic(
    DialogWindowOptions options,
    string feature,
    DialogFeatureDiagnosticKind kind)
{
    if (!options.Diagnostics.Any(d =>
        d.Feature.Equals(feature, StringComparison.OrdinalIgnoreCase) &&
        d.Kind == kind))
    {
        throw new InvalidOperationException($"Expected diagnostic {feature}/{kind}");
    }
}
