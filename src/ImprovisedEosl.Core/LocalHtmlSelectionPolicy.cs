namespace ImprovisedEosl.Core;

public enum LocalHtmlSelectionError
{
    None,
    EmptyPath,
    PathNotAbsolute,
    FileNotFound,
    UnsupportedExtension
}

public sealed record LocalHtmlSelection(
    string FullPath,
    string RootPath,
    string RelativeUrlPath);

public sealed record LocalHtmlSelectionResult(
    LocalHtmlSelection? Selection,
    LocalHtmlSelectionError Error)
{
    public bool IsValid => Selection is not null && Error == LocalHtmlSelectionError.None;
}

public static class LocalHtmlSelectionPolicy
{
    public static LocalHtmlSelectionResult Validate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Failure(LocalHtmlSelectionError.EmptyPath);
        }

        if (!Path.IsPathFullyQualified(path))
        {
            return Failure(LocalHtmlSelectionError.PathNotAbsolute);
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            return Failure(LocalHtmlSelectionError.FileNotFound);
        }

        var extension = Path.GetExtension(fullPath);
        if (!extension.Equals(".html", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
        {
            return Failure(LocalHtmlSelectionError.UnsupportedExtension);
        }

        var rootPath = Path.GetDirectoryName(fullPath)!;
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        var relativeUrlPath = string.Join(
            '/',
            relativePath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

        return new LocalHtmlSelectionResult(
            new LocalHtmlSelection(fullPath, rootPath, relativeUrlPath),
            LocalHtmlSelectionError.None);
    }

    private static LocalHtmlSelectionResult Failure(LocalHtmlSelectionError error)
    {
        return new LocalHtmlSelectionResult(null, error);
    }
}
