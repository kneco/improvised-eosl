using System.IO;
using System.Windows;
using ImprovisedEosl.Core;

namespace ImprovisedEosl.Spike.SyncModal;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var defaultShellPolicyPath = Path.Combine(
            AppContext.BaseDirectory,
            "config",
            "browser-shell-policy.json");
        var commandLine = BrowserShellPolicyCommandLine.Resolve(e.Args, defaultShellPolicyPath);
        if (commandLine.Error is not null)
        {
            Console.Error.WriteLine($"browser shell policy command-line error: {commandLine.Error}");
            Shutdown(1);
            return;
        }

        try
        {
            if (TryHandleCommandLineOperation(e.Args, defaultShellPolicyPath, commandLine))
            {
                Shutdown(0);
                return;
            }
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or InvalidOperationException or
            ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"browser shell policy command failed: {ex.Message}");
            Shutdown(1);
            return;
        }

        new MainWindow().Show();
    }

    private static bool TryHandleCommandLineOperation(
        IReadOnlyList<string> arguments,
        string defaultShellPolicyPath,
        BrowserShellPolicyCommandLineResult commandLine)
    {
        switch (commandLine.Mode)
        {
            case BrowserShellPolicyCommandLineMode.ExportShellPolicy:
                ExportShellPolicy(arguments, defaultShellPolicyPath, commandLine.ExportPath!);
                return true;
            case BrowserShellPolicyCommandLineMode.ApplyShellPolicy:
                ApplyShellPolicy(commandLine.ApplySourcePath!, commandLine.ApplyTargetPath!);
                return true;
            case BrowserShellPolicyCommandLineMode.ResetUserSettings:
                ResetUserSettings();
                return true;
            case BrowserShellPolicyCommandLineMode.RunBrowser:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(commandLine), commandLine.Mode, "Unknown command-line mode");
        }
    }

    private static void ExportShellPolicy(
        IReadOnlyList<string> arguments,
        string defaultShellPolicyPath,
        string exportPath)
    {
        var source = BrowserShellPolicySourceSelection.Resolve(arguments, defaultShellPolicyPath);
        if (source.Error is not null)
        {
            throw new InvalidOperationException(
                $"Shell policy source selection failed: {source.Error}");
        }

        var sourcePath = Path.GetFullPath(source.Path!);
        var load = new BrowserShellPolicyStore(sourcePath).Load();
        BrowserShellPolicyStore.SavePolicy(Path.GetFullPath(exportPath), load.Policy);
    }

    private static void ApplyShellPolicy(string sourcePath, string targetPath)
    {
        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new InvalidOperationException("Shell policy source file does not exist.");
        }

        var load = new BrowserShellPolicyStore(fullSourcePath).Load();
        if (load.Diagnostics.Count > 0)
        {
            throw new InvalidOperationException("Shell policy source file is invalid.");
        }

        BrowserShellPolicyStore.SavePolicy(Path.GetFullPath(targetPath), load.Policy);
    }

    private static void ResetUserSettings()
    {
        var applicationDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImprovisedEosl",
            "SyncModalSpike");
        new BrowserSettingsStore(Path.Combine(applicationDataFolder, "browser-settings.json"))
            .Save(new BrowserSettings(null));
        new UserApprovedOriginStore(Path.Combine(applicationDataFolder, "user-approved-compatibility.json"))
            .Save([]);
    }
}
