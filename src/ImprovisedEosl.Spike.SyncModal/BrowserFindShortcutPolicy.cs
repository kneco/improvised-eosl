using System.Windows.Input;

namespace ImprovisedEosl.Spike.SyncModal;

public static class BrowserFindShortcutPolicy
{
    public static bool IsFindShortcut(Key key, ModifierKeys modifiers) =>
        key == Key.F && modifiers == ModifierKeys.Control;
}
