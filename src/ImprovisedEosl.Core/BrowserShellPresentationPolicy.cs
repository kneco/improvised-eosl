namespace ImprovisedEosl.Core;

public sealed record BrowserShellPresentation(
    bool PrimaryToolbarVisible,
    bool AddressEntryVisible,
    bool HistoryCommandVisible,
    bool ReloadCommandVisible,
    bool GoCommandVisible,
    bool SettingsCommandVisible,
    bool DiagnosticsCommandVisible,
    bool CompatibilityStatusVisible);

public sealed record BrowserShellPresentationResult(
    BrowserShellPresentation Presentation,
    IReadOnlyList<string> Diagnostics);

public static class BrowserShellPresentationPolicy
{
    public static BrowserShellPresentationResult Resolve(BrowserShellPolicy policy)
    {
        var diagnostics = new List<string>();
        if (policy.ToolbarPrimaryToolbarHidden)
        {
            diagnostics.Add(
                "toolbar-primary-toolbar-hidden hides the complete primary toolbar; " +
                "individual toolbar visibility keys are ignored");
            return new BrowserShellPresentationResult(
                new BrowserShellPresentation(
                    PrimaryToolbarVisible: false,
                    AddressEntryVisible: false,
                    HistoryCommandVisible: false,
                    ReloadCommandVisible: false,
                    GoCommandVisible: false,
                    SettingsCommandVisible: false,
                    DiagnosticsCommandVisible: false,
                    CompatibilityStatusVisible: false),
                diagnostics);
        }

        if (policy.ToolbarAddressEntryHidden && !policy.ToolbarGoCommandHidden)
        {
            diagnostics.Add(
                "toolbar-address-entry-hidden treats toolbar-go-command-hidden as true");
        }

        var addressEntryVisible = !policy.ToolbarAddressEntryHidden;
        return new BrowserShellPresentationResult(
            new BrowserShellPresentation(
                PrimaryToolbarVisible: true,
                AddressEntryVisible: addressEntryVisible,
                HistoryCommandVisible: !policy.ToolbarHistoryCommandHidden,
                ReloadCommandVisible: !policy.ToolbarReloadCommandHidden,
                GoCommandVisible: addressEntryVisible && !policy.ToolbarGoCommandHidden,
                SettingsCommandVisible: !policy.ToolbarSettingsCommandHidden,
                DiagnosticsCommandVisible: !policy.ToolbarDiagnosticsCommandHidden,
                CompatibilityStatusVisible: true),
            diagnostics);
    }
}
