namespace ImprovisedEosl.ModalDialog;

public sealed record DialogWindowOptions(
    double? Width,
    double? Height,
    double? Left,
    double? Top,
    bool Center,
    DialogResizeMode ResizeMode,
    DialogFeaturePolicyStatus PolicyStatus,
    IReadOnlyList<DialogFeatureDiagnostic> Diagnostics);

public enum DialogResizeMode
{
    CanResize,
    NoResize
}

public enum DialogFeaturePolicyStatus
{
    ReferenceValidated
}

public sealed record DialogFeatureDiagnostic(
    string Feature,
    string? RawValue,
    DialogFeatureDiagnosticKind Kind,
    string Reason,
    string? AppliedValue);

public enum DialogFeatureDiagnosticKind
{
    Applied,
    Ignored,
    Clamped,
    Approximated,
    Unsupported,
    Invalid
}
