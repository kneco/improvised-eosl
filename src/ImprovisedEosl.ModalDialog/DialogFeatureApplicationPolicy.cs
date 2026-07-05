namespace ImprovisedEosl.ModalDialog;

public static class DialogFeatureApplicationPolicy
{
    private const int MinWidth = 250;
    private const int MinHeight = 100;
    private const int MaxWidth = 3000;
    private const int MaxHeight = 2000;

    public static DialogWindowOptions Calculate(DialogFeatures features)
    {
        var diagnostics = new List<DialogFeatureDiagnostic>();

        var width = ApplyDimension("dialogWidth", features.Width, MinWidth, MaxWidth, diagnostics);
        var height = ApplyDimension("dialogHeight", features.Height, MinHeight, MaxHeight, diagnostics);
        var left = ApplyPosition("dialogLeft", features.Left, diagnostics);
        var top = ApplyPosition("dialogTop", features.Top, diagnostics);
        var center = features.Center ?? true;
        var resizeMode = features.Resizable is true
            ? DialogResizeMode.CanResize
            : DialogResizeMode.NoResize;

        if (features.Center is null)
        {
            diagnostics.Add(new DialogFeatureDiagnostic(
                "center",
                null,
                DialogFeatureDiagnosticKind.Applied,
                "Omitted center defaults to center:yes based on Edge IE mode reference measurement.",
                center.ToString()));
        }
        else
        {
            diagnostics.Add(new DialogFeatureDiagnostic(
                "center",
                features.Center.Value.ToString(),
                DialogFeatureDiagnosticKind.Applied,
                "Parsed center value is applied unless explicit dialogLeft/dialogTop are present; Edge IE mode measurement shows explicit position wins.",
                center.ToString()));
        }

        if (features.Resizable is null)
        {
            diagnostics.Add(new DialogFeatureDiagnostic(
                "resizable",
                null,
                DialogFeatureDiagnosticKind.Applied,
                "Omitted resizable defaults to no resize based on Edge IE mode reference measurement.",
                resizeMode.ToString()));
        }
        else
        {
            diagnostics.Add(new DialogFeatureDiagnostic(
                "resizable",
                features.Resizable.Value.ToString(),
                DialogFeatureDiagnosticKind.Applied,
                "Corrected Edge IE mode measurement showed resizable:no disables resizing.",
                resizeMode.ToString()));
        }

        AddUnsupportedIfPresent("status", features.Status, diagnostics);
        AddUnsupportedIfPresent("scroll", features.Scroll, diagnostics);

        foreach (var unsupported in features.Unsupported)
        {
            diagnostics.Add(new DialogFeatureDiagnostic(
                unsupported.Key,
                unsupported.Value,
                DialogFeatureDiagnosticKind.Unsupported,
                "Unknown dialog feature is ignored.",
                null));
        }

        return new DialogWindowOptions(
            width,
            height,
            left,
            top,
            center,
            resizeMode,
            DialogFeaturePolicyStatus.ReferenceValidated,
            diagnostics);
    }

    private static double? ApplyDimension(
        string name,
        int? value,
        int min,
        int max,
        List<DialogFeatureDiagnostic> diagnostics)
    {
        if (value is null)
        {
            return null;
        }

        var clamped = Math.Clamp(value.Value, min, max);
        if (clamped != value.Value)
        {
            diagnostics.Add(new DialogFeatureDiagnostic(
                name,
                value.Value.ToString(),
                DialogFeatureDiagnosticKind.Clamped,
                "Dimension is clamped to the MVP safety bounds; Edge IE mode also kept zero and oversized dialogs on-screen.",
                clamped.ToString()));
            return clamped;
        }

        diagnostics.Add(new DialogFeatureDiagnostic(
            name,
            value.Value.ToString(),
            DialogFeatureDiagnosticKind.Applied,
            "Dimension is calculated for WPF application; child window adds measured IE frame compensation.",
            value.Value.ToString()));
        return value.Value;
    }

    private static double? ApplyPosition(
        string name,
        int? value,
        List<DialogFeatureDiagnostic> diagnostics)
    {
        if (value is null)
        {
            return null;
        }

        diagnostics.Add(new DialogFeatureDiagnostic(
            name,
            value.Value.ToString(),
            DialogFeatureDiagnosticKind.Applied,
            "Position is applied to the WPF child and clamped to the visible work area.",
            value.Value.ToString()));
        return value.Value;
    }

    private static void AddUnsupportedIfPresent(
        string name,
        bool? value,
        List<DialogFeatureDiagnostic> diagnostics)
    {
        if (value is null)
        {
            return;
        }

        diagnostics.Add(new DialogFeatureDiagnostic(
            name,
            value.Value.ToString(),
            DialogFeatureDiagnosticKind.Unsupported,
            "Parsed feature is not faithfully implemented in WebView2/WPF yet.",
            null));
    }
}
