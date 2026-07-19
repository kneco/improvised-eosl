using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace ImprovisedEosl.Spike.SyncModal;

public enum ShellHubAction
{
    ApplicationSettings,
    CompatibilityStatus,
    ToggleDiagnostics
}

public sealed class ShellHubActionRequestedEventArgs : EventArgs
{
    public ShellHubActionRequestedEventArgs(ShellHubAction action)
    {
        Action = action;
    }

    public ShellHubAction Action { get; }
}

public sealed class ShellHubWindow : Window
{
    public ShellHubWindow(
        bool applicationSettingsEnabled,
        bool diagnosticsVisible,
        string compatibilityStatusDetail)
    {
        Title = UiText.Get(UiText.ShellHubTitle);
        Width = 520;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 520;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeWindowVisuals.UseBrownFrame(this);

        var title = new TextBlock
        {
            Text = UiText.Get(UiText.ShellHubTitle),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var intro = new TextBlock
        {
            Text = UiText.Get(UiText.ShellHubIntro),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var settings = CreateActionButton(
            UiText.Get(UiText.ShellHubApplicationSettings),
            UiText.Get(UiText.ShellHubApplicationSettingsDescription),
            ShellHubAction.ApplicationSettings);
        settings.IsEnabled = applicationSettingsEnabled;

        var compatibility = CreateActionButton(
            UiText.Get(UiText.ShellHubCompatibilityStatus),
            compatibilityStatusDetail,
            ShellHubAction.CompatibilityStatus);

        var diagnostics = CreateActionButton(
            UiText.Get(diagnosticsVisible ? UiText.DiagnosticsHide : UiText.DiagnosticsShow),
            UiText.Get(UiText.ShellHubDiagnosticsDescription),
            ShellHubAction.ToggleDiagnostics);

        var close = new Button
        {
            Content = UiText.Get(UiText.CloseButton),
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
        content.Children.Add(title);
        content.Children.Add(intro);
        content.Children.Add(settings);
        content.Children.Add(compatibility);
        content.Children.Add(diagnostics);
        content.Children.Add(close);

        Content = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        Loaded += (_, _) =>
        {
            if (settings.IsEnabled)
            {
                settings.Focus();
            }
            else
            {
                close.Focus();
            }
        };
    }

    public event EventHandler<ShellHubActionRequestedEventArgs>? ActionRequested;

    private Button CreateActionButton(string title, string description, ShellHubAction action)
    {
        var titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold
        };
        var descriptionBlock = new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var stack = new StackPanel();
        stack.Children.Add(titleBlock);
        stack.Children.Add(descriptionBlock);

        var button = new Button
        {
            Content = stack,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };
        AutomationProperties.SetName(button, title);
        button.Click += (_, _) => RequestAction(action);
        return button;
    }

    private void RequestAction(ShellHubAction action)
    {
        Close();
        ActionRequested?.Invoke(this, new ShellHubActionRequestedEventArgs(action));
    }
}
