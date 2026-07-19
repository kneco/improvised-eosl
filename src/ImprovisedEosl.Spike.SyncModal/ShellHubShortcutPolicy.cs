using System.Windows.Input;

namespace ImprovisedEosl.Spike.SyncModal;

public static class ShellHubShortcutPolicy
{
    public static bool IsShellHubShortcut(Key key, Key systemKey, ModifierKeys modifiers)
    {
        var effectiveKey = key == Key.System ? systemKey : key;
        return effectiveKey == Key.F1 && modifiers == ModifierKeys.None;
    }
}
