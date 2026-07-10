using System.Windows.Input;

namespace ImprovisedEosl.Spike.SyncModal;

public static class BrowserHelpShortcutPolicy
{
    public const string SuppressionDetectionScript =
        """
        (() => {
          const sources = [];
          const append = value => {
            if (typeof value === "string") {
              sources.push(value);
            } else if (typeof value === "function") {
              sources.push(Function.prototype.toString.call(value));
            }
          };
          append(document.documentElement && document.documentElement.getAttribute("onhelp"));
          append(document.body && document.body.getAttribute("onhelp"));
          append(window.onhelp);
          append(document.onhelp);
          append(document.body && document.body.onhelp);
          return sources.some(source => /\breturn\s+false\b/i.test(source));
        })();
        """;

    public static bool IsHelpShortcut(Key key) => key == Key.F1;

    public static bool IsSuppressionRequested(string? executeScriptResult) =>
        string.Equals(executeScriptResult?.Trim(), "true", StringComparison.Ordinal);
}
