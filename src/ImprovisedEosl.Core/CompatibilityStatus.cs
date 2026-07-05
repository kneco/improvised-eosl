namespace ImprovisedEosl.Core;

public sealed record CompatibilityStatus(string Origin, string Label)
{
    public string DisplayText => $"{Label} ({Origin})";
}
