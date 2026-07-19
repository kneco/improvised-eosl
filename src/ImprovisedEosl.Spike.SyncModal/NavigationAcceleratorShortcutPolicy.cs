using System.Windows.Input;

namespace ImprovisedEosl.Spike.SyncModal;

public enum NavigationAcceleratorCommand
{
    HistoryBack,
    HistoryForward,
    Reload,
    FocusAddress,
    Fullscreen
}

public static class NavigationAcceleratorShortcutPolicy
{
    public static bool TryGetCommand(
        Key key,
        Key systemKey,
        ModifierKeys modifiers,
        out NavigationAcceleratorCommand command)
    {
        var effectiveKey = key == Key.System ? systemKey : key;

        if (effectiveKey == Key.Left && modifiers == ModifierKeys.Alt)
        {
            command = NavigationAcceleratorCommand.HistoryBack;
            return true;
        }

        if (effectiveKey == Key.Right && modifiers == ModifierKeys.Alt)
        {
            command = NavigationAcceleratorCommand.HistoryForward;
            return true;
        }

        if (effectiveKey == Key.BrowserBack && modifiers == ModifierKeys.None)
        {
            command = NavigationAcceleratorCommand.HistoryBack;
            return true;
        }

        if (effectiveKey == Key.BrowserForward && modifiers == ModifierKeys.None)
        {
            command = NavigationAcceleratorCommand.HistoryForward;
            return true;
        }

        if (effectiveKey == Key.R && modifiers == ModifierKeys.Control)
        {
            command = NavigationAcceleratorCommand.Reload;
            return true;
        }

        if (effectiveKey == Key.F5 && modifiers == ModifierKeys.None)
        {
            command = NavigationAcceleratorCommand.Reload;
            return true;
        }

        if (effectiveKey == Key.F6 && modifiers == ModifierKeys.None)
        {
            command = NavigationAcceleratorCommand.FocusAddress;
            return true;
        }

        if (effectiveKey == Key.F11 && modifiers == ModifierKeys.None)
        {
            command = NavigationAcceleratorCommand.Fullscreen;
            return true;
        }

        command = default;
        return false;
    }

    public static string FormatCommand(NavigationAcceleratorCommand command) => command switch
    {
        NavigationAcceleratorCommand.HistoryBack => "history-back",
        NavigationAcceleratorCommand.HistoryForward => "history-forward",
        NavigationAcceleratorCommand.Reload => "reload",
        NavigationAcceleratorCommand.FocusAddress => "focus-address",
        NavigationAcceleratorCommand.Fullscreen => "fullscreen",
        _ => "unknown"
    };
}
