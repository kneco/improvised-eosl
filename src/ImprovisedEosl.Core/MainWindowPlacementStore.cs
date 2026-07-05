using System.Text.Json;

namespace ImprovisedEosl.Core;

public sealed record MainWindowPlacement(
    double Left,
    double Top,
    double Width,
    double Height,
    bool Maximized);

public sealed record MainWindowPlacementLoadResult(
    MainWindowPlacement? Placement,
    string? Diagnostic);

public enum MainWindowDisplayState
{
    Normal,
    Minimized,
    Maximized
}

public sealed class MainWindowPlacementStore
{
    public const int CurrentVersion = 1;
    public const int MaxFileBytes = 32 * 1024;
    public const double MinWidth = 320;
    public const double MinHeight = 240;
    public const double MaxDimension = 32_768;
    public const double MaxCoordinateMagnitude = 100_000;

    private readonly string _path;

    public MainWindowPlacementStore(string path)
    {
        _path = path;
    }

    public string Path => _path;

    public MainWindowPlacementLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return new(null, null);
        }

        try
        {
            var info = new FileInfo(_path);
            if (info.Length > MaxFileBytes)
            {
                return new(null, $"window placement file exceeds {MaxFileBytes} bytes");
            }

            var json = File.ReadAllText(_path);
            var document = JsonSerializer.Deserialize<PlacementDocument>(json);
            if (document is null || document.Version != CurrentVersion)
            {
                return new(null, "window placement file has an unsupported version");
            }

            var placement = new MainWindowPlacement(
                document.Left,
                document.Top,
                document.Width,
                document.Height,
                document.Maximized);
            return IsValid(placement)
                ? new(placement, null)
                : new(null, "window placement file contains invalid bounds");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new(null, $"window placement file could not be loaded: {ex.GetType().Name}");
        }
    }

    public void Save(MainWindowPlacement placement)
    {
        if (!IsValid(placement))
        {
            throw new ArgumentException("Window placement is outside the supported bounds.", nameof(placement));
        }

        var directory = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = _path + ".tmp";
        var document = new PlacementDocument
        {
            Version = CurrentVersion,
            Left = placement.Left,
            Top = placement.Top,
            Width = placement.Width,
            Height = placement.Height,
            Maximized = placement.Maximized
        };
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document));
        File.Move(temporaryPath, _path, overwrite: true);
    }

    public static bool IsValid(MainWindowPlacement placement) =>
        IsFiniteWithin(placement.Left, MaxCoordinateMagnitude) &&
        IsFiniteWithin(placement.Top, MaxCoordinateMagnitude) &&
        IsFiniteWithin(placement.Width, MaxDimension) &&
        IsFiniteWithin(placement.Height, MaxDimension) &&
        placement.Width >= MinWidth &&
        placement.Height >= MinHeight;

    public static bool ShouldReopenMaximized(
        MainWindowDisplayState current,
        MainWindowDisplayState lastNonMinimized) =>
        current == MainWindowDisplayState.Maximized ||
        (current == MainWindowDisplayState.Minimized &&
         lastNonMinimized == MainWindowDisplayState.Maximized);

    private static bool IsFiniteWithin(double value, double maximumMagnitude) =>
        double.IsFinite(value) && Math.Abs(value) <= maximumMagnitude;

    private sealed class PlacementDocument
    {
        public int Version { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Maximized { get; set; }
    }
}
