using System.Text;

namespace ImprovisedEosl.Core;

public sealed record WindowOpenFeatureCapture(
    bool IsValid,
    bool? Scrollbars,
    bool? Status,
    string? ErrorCode,
    int Utf8Bytes,
    int EntryCount);

public static class WindowOpenFeatureCapturePolicy
{
    public const int MaxUtf8Bytes = 4 * 1024;
    public const int MaxEntries = 64;

    public static WindowOpenFeatureCapture Parse(string? features)
    {
        var value = features ?? string.Empty;
        var utf8Bytes = Encoding.UTF8.GetByteCount(value);
        if (utf8Bytes > MaxUtf8Bytes)
        {
            return Invalid("too-large", utf8Bytes, 0);
        }

        var entries = value.Length == 0 ? [] : value.Split(',');
        if (entries.Length > MaxEntries)
        {
            return Invalid("too-many-entries", utf8Bytes, entries.Length);
        }

        bool? scrollbars = null;
        bool? status = null;
        foreach (var entry in entries)
        {
            var parts = entry.Split('=', 2);
            var name = parts[0].Trim();
            if (!name.Equals("scrollbars", StringComparison.OrdinalIgnoreCase) &&
                !name.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryParseBoolean(parts.Length == 1 ? string.Empty : parts[1], out var parsed))
            {
                continue;
            }

            if (name.Equals("scrollbars", StringComparison.OrdinalIgnoreCase))
            {
                scrollbars = parsed;
            }
            else
            {
                status = parsed;
            }
        }

        return new WindowOpenFeatureCapture(true, scrollbars, status, null, utf8Bytes, entries.Length);
    }

    private static bool TryParseBoolean(string raw, out bool value)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "":
            case "yes":
            case "true":
            case "1":
            case "on":
                value = true;
                return true;
            case "no":
            case "false":
            case "0":
            case "off":
                value = false;
                return true;
            default:
                value = false;
                return false;
        }
    }

    private static WindowOpenFeatureCapture Invalid(string errorCode, int utf8Bytes, int entryCount) =>
        new(false, null, null, errorCode, utf8Bytes, entryCount);
}
