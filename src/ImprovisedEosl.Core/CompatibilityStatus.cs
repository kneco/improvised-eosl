namespace ImprovisedEosl.Core;

public enum CompatibilityStatusState
{
    Undecided,
    DetectionPending,
    Enabled,
    Denied,
    Blocked
}

public sealed record CompatibilityStatus(
    string Origin,
    string Label,
    CompatibilityStatusState State,
    IReadOnlyList<string> EnabledApis,
    IReadOnlyList<string> DeniedApis,
    IReadOnlyList<string> DetectedApis)
{
    public string DisplayText => $"{Label} ({Origin})";
}
