namespace ImprovisedEosl.Spike.SyncModal;

public static class MainWindowTitlePolicy
{
    public const string ApplicationTitle = "Improvised EOSL";

    public static string Format(string? documentTitle)
    {
        var normalizedTitle = documentTitle?.Trim();
        return string.IsNullOrEmpty(normalizedTitle)
            ? ApplicationTitle
            : $"{normalizedTitle} - {ApplicationTitle}";
    }
}
