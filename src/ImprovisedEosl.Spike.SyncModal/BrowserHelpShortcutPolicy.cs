using System.Windows.Input;

namespace ImprovisedEosl.Spike.SyncModal;

public static class BrowserHelpShortcutPolicy
{
    public static bool IsHelpShortcut(Key key) => key == Key.F1;
}
