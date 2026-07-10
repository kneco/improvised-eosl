using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace ImprovisedEosl.Spike.SyncModal;

public sealed class CompatibilityStatusDetailWindow : Window
{
    public CompatibilityStatusDetailWindow(string detailText)
    {
        Title = UiText.Get(UiText.CompatibilityStatusDetailTitle);
        Width = 520;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 420;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeWindowVisuals.UseBrownFrame(this);

        var detail = new TextBlock
        {
            Text = detailText,
            TextWrapping = TextWrapping.Wrap,
            Focusable = false
        };
        AutomationProperties.SetName(detail, detailText);

        var close = new Button
        {
            Content = UiText.Get(UiText.CloseButton),
            IsDefault = true,
            IsCancel = true,
            MinWidth = 88,
            Padding = new Thickness(12, 5, 12, 5),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        close.Click += (_, _) => Close();

        var content = new StackPanel
        {
            Margin = new Thickness(20)
        };
        content.Children.Add(detail);
        content.Children.Add(close);

        Content = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        Loaded += (_, _) => close.Focus();
    }
}
