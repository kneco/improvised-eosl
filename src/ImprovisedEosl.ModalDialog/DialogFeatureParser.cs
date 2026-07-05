using System.Globalization;

namespace ImprovisedEosl.ModalDialog;

public static class DialogFeatureParser
{
    private const int DefaultTimeoutMs = 90_000;
    private const int MinTimeoutMs = 1_000;
    private const int MaxTimeoutMs = 90_000;

    public static DialogFeatures Parse(string? features)
    {
        int? width = null;
        int? height = null;
        int? left = null;
        int? top = null;
        bool? center = null;
        bool? resizable = null;
        bool? status = null;
        bool? scroll = null;
        var timeout = TimeSpan.FromMilliseconds(DefaultTimeoutMs);
        var unsupported = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in SplitEntries(features))
        {
            var (name, value) = SplitNameValue(entry);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            switch (NormalizeName(name))
            {
                case "dialogwidth":
                    width = ParseRequiredPixelUnit(value);
                    break;
                case "dialogheight":
                    height = ParseRequiredPixelUnit(value);
                    break;
                case "dialogleft":
                    left = ParsePixels(value);
                    break;
                case "dialogtop":
                    top = ParsePixels(value);
                    break;
                case "center":
                    center = ParseBoolean(value);
                    break;
                case "resizable":
                    resizable = ParseBoolean(value);
                    break;
                case "status":
                    status = ParseBoolean(value);
                    break;
                case "scroll":
                    scroll = ParseBoolean(value);
                    break;
                case "timeoutms":
                    timeout = ParseTimeout(value);
                    break;
                default:
                    unsupported[name.Trim()] = value.Trim();
                    break;
            }
        }

        return new DialogFeatures(
            width,
            height,
            left,
            top,
            center,
            resizable,
            status,
            scroll,
            timeout,
            unsupported);
    }

    private static IEnumerable<string> SplitEntries(string? features)
    {
        if (string.IsNullOrWhiteSpace(features))
        {
            yield break;
        }

        foreach (var entry in features.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(entry))
            {
                yield return entry;
            }
        }
    }

    private static (string Name, string Value) SplitNameValue(string entry)
    {
        var colon = entry.IndexOf(':');
        var equals = entry.IndexOf('=');
        var separator = colon >= 0 && equals >= 0
            ? Math.Min(colon, equals)
            : Math.Max(colon, equals);

        if (separator < 0)
        {
            return (entry, string.Empty);
        }

        return (entry[..separator], entry[(separator + 1)..]);
    }

    private static string NormalizeName(string name)
    {
        return name.Trim().Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private static int? ParsePixels(string value)
    {
        var normalized = value.Trim();
        if (normalized.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^2].Trim();
        }

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var pixelValue))
        {
            return (int)decimal.Truncate(pixelValue);
        }

        return null;
    }

    private static int? ParseRequiredPixelUnit(string value)
    {
        var normalized = value.Trim();
        if (!normalized.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var pixels = ParsePixels(normalized);
        return pixels >= 0 ? pixels : null;
    }

    private static bool? ParseBoolean(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "" => true,
            "1" => true,
            "yes" => true,
            "true" => true,
            "on" => true,
            "0" => false,
            "no" => false,
            "false" => false,
            "off" => false,
            _ => null
        };
    }

    private static TimeSpan ParseTimeout(string value)
    {
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeoutMs))
        {
            return TimeSpan.FromMilliseconds(DefaultTimeoutMs);
        }

        return TimeSpan.FromMilliseconds(Math.Clamp(timeoutMs, MinTimeoutMs, MaxTimeoutMs));
    }
}
