using System.Text;
using System.Text.Json;

namespace ImprovisedEosl.ModalDialog;

public sealed record DialogFeatureInputValidation(
    bool IsValid,
    string Value,
    string? ErrorCode,
    int Utf8Bytes,
    int EntryCount);

public static class DialogFeatureInputPolicy
{
    public const int MaxUtf8Bytes = 16 * 1024;
    public const int MaxEntries = 128;

    public static DialogFeatureInputValidation Validate(string? features)
    {
        var value = features ?? string.Empty;
        var utf8Bytes = Encoding.UTF8.GetByteCount(value);
        if (utf8Bytes > MaxUtf8Bytes)
        {
            return new DialogFeatureInputValidation(
                false,
                string.Empty,
                "too-large",
                utf8Bytes,
                0);
        }

        var entryCount = value.Length == 0 ? 0 : value.Count(character => character == ';') + 1;
        if (entryCount > MaxEntries)
        {
            return new DialogFeatureInputValidation(
                false,
                string.Empty,
                "too-many-entries",
                utf8Bytes,
                entryCount);
        }

        return new DialogFeatureInputValidation(true, value, null, utf8Bytes, entryCount);
    }

    public static string CreateFailureJson(DialogFeatureInputValidation validation)
    {
        return JsonSerializer.Serialize(new
        {
            kind = "invalid-dialog-features",
            ok = false,
            reason = validation.ErrorCode,
            utf8Bytes = validation.Utf8Bytes,
            entryCount = validation.EntryCount,
            maxUtf8Bytes = MaxUtf8Bytes,
            maxEntries = MaxEntries
        });
    }
}
