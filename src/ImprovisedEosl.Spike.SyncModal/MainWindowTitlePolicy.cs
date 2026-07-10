namespace ImprovisedEosl.Spike.SyncModal;

public static class MainWindowTitlePolicy
{
    public const string ApplicationTitle = "Improvised EOSL";

    public static string Format(string? documentTitle)
    {
        var normalizedTitle = documentTitle?.Trim();
        return string.IsNullOrEmpty(normalizedTitle) ||
            string.Equals(normalizedTitle, ApplicationTitle, StringComparison.Ordinal)
            ? ApplicationTitle
            : $"{normalizedTitle} - {ApplicationTitle}";
    }
}
