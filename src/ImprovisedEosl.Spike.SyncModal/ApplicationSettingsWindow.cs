using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ImprovisedEosl.Core;
using Microsoft.Win32;

namespace ImprovisedEosl.Spike.SyncModal;

public sealed class ApplicationSettingsWindow : Window
{
    private readonly TextBox _initialUrl;
    private readonly ObservableCollection<CompatibilityDecisionEntry> _decisions;
    private readonly ListView _decisionList;
    private readonly Button _revokeButton;
    private readonly Border _dropTarget;
    private readonly PortableUserSettingsStore _portableStore = new();

    public ApplicationSettingsWindow(
        Uri? initialUrl,
        IEnumerable<UserApprovedCompatibility> approvals,
        IEnumerable<UserApprovedCompatibility> denials)
    {
        _decisions = new ObservableCollection<CompatibilityDecisionEntry>(
            ToEntries(approvals, denials));

        Title = UiText.Get(UiText.ApplicationSettingsTitle);
        Width = 780;
        Height = 600;
        MinWidth = 620;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var intro = new TextBlock
        {
            Text = UiText.Get(UiText.ApplicationSettingsIntro),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        root.Children.Add(intro);

        var initialUrlLabel = new TextBlock
        {
            Text = UiText.Get(UiText.InitialUrlLabel),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(initialUrlLabel, 1);
        root.Children.Add(initialUrlLabel);

        var editor = new Grid();
        editor.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editor.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _initialUrl = new TextBox
        {
            Text = initialUrl?.OriginalString ?? string.Empty,
            Padding = new Thickness(6, 4, 6, 4)
        };
        editor.Children.Add(_initialUrl);
        var useHome = new Button
        {
            Content = UiText.Get(UiText.UseHomeButton),
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(10, 4, 10, 4)
        };
        useHome.Click += (_, _) => _initialUrl.Clear();
        Grid.SetColumn(useHome, 1);
        editor.Children.Add(useHome);
        Grid.SetRow(editor, 2);
        root.Children.Add(editor);

        var note = new TextBlock
        {
            Text = UiText.Get(UiText.InitialUrlNextLaunchNote),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 12)
        };
        Grid.SetRow(note, 3);
        root.Children.Add(note);

        var decisionsHeader = new Grid();
        decisionsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        decisionsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var decisionsLabel = new TextBlock
        {
            Text = UiText.Get(UiText.CompatibilityDecisionsLabel),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        decisionsHeader.Children.Add(decisionsLabel);
        _revokeButton = new Button
        {
            Content = UiText.Get(UiText.RevokeButton),
            Padding = new Thickness(10, 4, 10, 4)
        };
        _revokeButton.Click += Revoke_Click;
        Grid.SetColumn(_revokeButton, 1);
        decisionsHeader.Children.Add(_revokeButton);
        Grid.SetRow(decisionsHeader, 4);
        root.Children.Add(decisionsHeader);

        _decisionList = new ListView
        {
            ItemsSource = _decisions,
            Margin = new Thickness(0, 6, 0, 12)
        };
        _decisionList.SelectionChanged += (_, _) => UpdateDecisionState();
        var columns = new GridView();
        columns.Columns.Add(new GridViewColumn
        {
            Header = UiText.Get(UiText.OriginColumn),
            DisplayMemberBinding = new Binding(nameof(CompatibilityDecisionEntry.Origin)),
            Width = 420
        });
        columns.Columns.Add(new GridViewColumn
        {
            Header = UiText.Get(UiText.DecisionColumn),
            DisplayMemberBinding = new Binding(nameof(CompatibilityDecisionEntry.Decision)),
            Width = 90
        });
        columns.Columns.Add(new GridViewColumn
        {
            Header = UiText.Get(UiText.ApiColumn),
            DisplayMemberBinding = new Binding(nameof(CompatibilityDecisionEntry.ApiName)),
            Width = 190
        });
        _decisionList.View = columns;
        Grid.SetRow(_decisionList, 5);
        root.Children.Add(_decisionList);

        _dropTarget = new Border
        {
            AllowDrop = true,
            BorderBrush = new SolidColorBrush(Color.FromRgb(151, 105, 79)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Color.FromRgb(250, 244, 239)),
            Padding = new Thickness(14, 10, 14, 10),
            Child = new TextBlock
            {
                Text = UiText.Get(UiText.SettingsDropTarget),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            }
        };
        _dropTarget.PreviewDragOver += DropTarget_PreviewDragOver;
        _dropTarget.DragLeave += (_, _) => ResetDropTarget();
        _dropTarget.PreviewDrop += DropTarget_PreviewDrop;
        Grid.SetRow(_dropTarget, 6);
        root.Children.Add(_dropTarget);

        var actions = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var transferActions = new StackPanel { Orientation = Orientation.Horizontal };
        var import = CreateButton(UiText.ImportButton, Import_Click);
        import.Margin = new Thickness(0, 0, 8, 0);
        var export = CreateButton(UiText.ExportButton, Export_Click);
        transferActions.Children.Add(import);
        transferActions.Children.Add(export);
        actions.Children.Add(transferActions);

        var closeActions = new StackPanel { Orientation = Orientation.Horizontal };
        var cancel = CreateButton(UiText.CancelButton, null);
        cancel.IsCancel = true;
        cancel.MinWidth = 90;
        cancel.Margin = new Thickness(0, 0, 8, 0);
        var save = CreateButton(UiText.SaveButton, Save_Click);
        save.IsDefault = true;
        save.MinWidth = 90;
        closeActions.Children.Add(cancel);
        closeActions.Children.Add(save);
        Grid.SetColumn(closeActions, 2);
        actions.Children.Add(closeActions);
        Grid.SetRow(actions, 7);
        root.Children.Add(actions);

        Content = root;
        Loaded += (_, _) =>
        {
            _initialUrl.Focus();
            _initialUrl.SelectAll();
        };
        UpdateDecisionState();
    }

    public Uri? InitialUrl { get; private set; }

    public IReadOnlyList<UserApprovedCompatibility> Approvals => _decisions
        .Where(item => item.IsAllowed)
        .Select(item => new UserApprovedCompatibility(item.Origin, item.ApiName))
        .ToArray();

    public IReadOnlyList<UserApprovedCompatibility> Denials => _decisions
        .Where(item => !item.IsAllowed)
        .Select(item => new UserApprovedCompatibility(item.Origin, item.ApiName))
        .ToArray();

    private static Button CreateButton(string textKey, RoutedEventHandler? click)
    {
        var button = new Button
        {
            Content = UiText.Get(textKey),
            Padding = new Thickness(10, 4, 10, 4)
        };
        if (click is not null) button.Click += click;
        return button;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!BrowserSettingsStore.TryParseInitialUrl(_initialUrl.Text.Trim(), out var initialUrl))
        {
            ShowInvalidInitialUrl();
            return;
        }
        InitialUrl = initialUrl;
        DialogResult = true;
    }

    private void Revoke_Click(object sender, RoutedEventArgs e)
    {
        if (_decisionList.SelectedItem is CompatibilityDecisionEntry decision)
        {
            _decisions.Remove(decision);
        }
        UpdateDecisionState();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog
        {
            Title = UiText.Get(UiText.ImportDialogTitle),
            Filter = UiText.Get(UiText.SettingsJsonFilter),
            Multiselect = false,
            CheckFileExists = true
        };
        if (picker.ShowDialog(this) == true) ImportFromPath(picker.FileName);
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetStagedSettings(out var settings)) return;
        var picker = new SaveFileDialog
        {
            Title = UiText.Get(UiText.ExportDialogTitle),
            Filter = UiText.Get(UiText.SettingsJsonFilter),
            AddExtension = true,
            DefaultExt = ".json",
            FileName = "improvised-eosl-settings.json",
            OverwritePrompt = true
        };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            _portableStore.Save(picker.FileName, settings!);
            MessageBox.Show(this, UiText.Get(UiText.ExportSucceededBody),
                UiText.Get(UiText.ExportSucceededTitle), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            ShowTransferError(UiText.ExportFailedTitle, UiText.Format(UiText.ExportFailedBody, ex.Message));
        }
    }

    private void DropTarget_PreviewDragOver(object sender, DragEventArgs e)
    {
        var accepted = TryGetSingleJsonPath(e.Data, out _);
        e.Effects = accepted ? DragDropEffects.Copy : DragDropEffects.None;
        _dropTarget.Background = new SolidColorBrush(accepted
            ? Color.FromRgb(229, 244, 225)
            : Color.FromRgb(252, 232, 232));
        e.Handled = true;
    }

    private void DropTarget_PreviewDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        ResetDropTarget();
        if (!TryGetSingleJsonPath(e.Data, out var path))
        {
            ShowTransferError(UiText.ImportFailedTitle, UiText.Get(UiText.ImportSingleJsonBody));
            return;
        }
        ImportFromPath(path!);
    }

    private void ResetDropTarget() =>
        _dropTarget.Background = new SolidColorBrush(Color.FromRgb(250, 244, 239));

    private void ImportFromPath(string path)
    {
        var loaded = _portableStore.Load(path);
        if (loaded.Settings is null)
        {
            ShowTransferError(UiText.ImportFailedTitle,
                UiText.Format(UiText.ImportFailedBody, loaded.Diagnostic ?? "unknown error"));
            return;
        }
        var imported = loaded.Settings;
        var confirmation = UiText.Format(
            UiText.ImportConfirmationBody,
            imported.Browser.InitialUrl?.OriginalString ?? UiText.Get(UiText.HomeValue),
            imported.Approvals.Count,
            imported.Denials.Count);
        if (MessageBox.Show(this, confirmation, UiText.Get(UiText.ImportConfirmationTitle),
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
        {
            return;
        }

        _initialUrl.Text = imported.Browser.InitialUrl?.OriginalString ?? string.Empty;
        _decisions.Clear();
        foreach (var decision in ToEntries(imported.Approvals, imported.Denials))
        {
            _decisions.Add(decision);
        }
        UpdateDecisionState();
    }

    private bool TryGetStagedSettings(out PortableUserSettings? settings)
    {
        settings = null;
        if (!BrowserSettingsStore.TryParseInitialUrl(_initialUrl.Text.Trim(), out var initialUrl))
        {
            ShowInvalidInitialUrl();
            return false;
        }
        settings = new PortableUserSettings(new BrowserSettings(initialUrl), Approvals, Denials);
        return true;
    }

    private void ShowInvalidInitialUrl()
    {
        MessageBox.Show(this, UiText.Get(UiText.InitialUrlInvalidBody),
            UiText.Get(UiText.InitialUrlInvalidTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
        _initialUrl.Focus();
        _initialUrl.SelectAll();
    }

    private void UpdateDecisionState()
    {
        _revokeButton.IsEnabled = _decisionList.SelectedItem is not null;
    }

    private void ShowTransferError(string titleKey, string body) =>
        MessageBox.Show(this, body, UiText.Get(titleKey), MessageBoxButton.OK, MessageBoxImage.Error);

    private static bool TryGetSingleJsonPath(IDataObject data, out string? path)
    {
        path = null;
        if (data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length != 1 ||
            !Path.GetExtension(paths[0]).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        path = paths[0];
        return true;
    }

    private static IEnumerable<CompatibilityDecisionEntry> ToEntries(
        IEnumerable<UserApprovedCompatibility> approvals,
        IEnumerable<UserApprovedCompatibility> denials) => approvals
            .Select(item => new CompatibilityDecisionEntry(item.Origin, item.ApiName, true))
            .Concat(denials.Select(item => new CompatibilityDecisionEntry(item.Origin, item.ApiName, false)))
            .OrderBy(item => item.Origin, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ApiName, StringComparer.Ordinal);
}

public sealed record CompatibilityDecisionEntry(string Origin, string ApiName, bool IsAllowed)
{
    public string Decision => UiText.Get(IsAllowed ? UiText.DecisionAllowed : UiText.DecisionDenied);
}
