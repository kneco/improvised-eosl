namespace ImprovisedEosl.Core;

public sealed record BrowserShellPresentation(
    bool PrimaryToolbarVisible,
    bool AddressEntryVisible,
    bool HistoryCommandVisible,
    bool ReloadCommandVisible,
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
                    SettingsCommandVisible: false,
                    DiagnosticsCommandVisible: false,
                    CompatibilityStatusVisible: false),
                diagnostics);
        }

        if (policy.ToolbarGoCommandHidden)
        {
            diagnostics.Add(
                "toolbar-go-command-hidden is accepted for schema compatibility; " +
                "standard navigation uses the address entry Enter key");
        }

        return new BrowserShellPresentationResult(
            new BrowserShellPresentation(
                PrimaryToolbarVisible: true,
                AddressEntryVisible: !policy.ToolbarAddressEntryHidden,
                HistoryCommandVisible: !policy.ToolbarHistoryCommandHidden,
                ReloadCommandVisible: !policy.ToolbarReloadCommandHidden,
                SettingsCommandVisible: !policy.ToolbarSettingsCommandHidden,
                DiagnosticsCommandVisible: !policy.ToolbarDiagnosticsCommandHidden,
                CompatibilityStatusVisible: true),
            diagnostics);
    }
}
