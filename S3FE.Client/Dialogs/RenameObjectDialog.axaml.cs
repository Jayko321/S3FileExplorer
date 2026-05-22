using Avalonia.Controls;

namespace S3FE.Client.Dialogs;

public partial class RenameObjectDialog : Window
{
    public bool Confirmed { get; private set; }
    public string DestinationKey { get; private set; } = string.Empty;
    public bool RenameAllVersions { get; private set; }

    public RenameObjectDialog(string currentKey, bool isVersioned)
    {
        InitializeComponent();
        DestinationKeyTextBox.Text = currentKey;
        VersioningPanel.IsVisible = isVersioned;
    }

    private void OnRenameClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DestinationKey = DestinationKeyTextBox.Text?.Trim() ?? string.Empty;
        RenameAllVersions = AllVersionsRadio.IsChecked == true;
        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
