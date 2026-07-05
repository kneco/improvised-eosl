namespace ImprovisedEosl.ModalDialog;

public sealed record DialogFeatures(
    int? Width,
    int? Height,
    int? Left,
    int? Top,
    bool? Center,
    bool? Resizable,
    bool? Status,
    bool? Scroll,
    TimeSpan Timeout,
    IReadOnlyDictionary<string, string> Unsupported);
