using Avalonia.Controls;

namespace S3FE.Client.Dialogs;

public partial class DeleteObjectDialog : Window
{
    public bool Confirmed { get; private set; }
    public bool DeleteAllVersions { get; private set; }

    public DeleteObjectDialog(string key, bool isVersioned)
    {
        InitializeComponent();
        MessageText.Text = $"Are you sure you want to delete '{key}'?";
        VersioningPanel.IsVisible = isVersioned;
    }

    private void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DeleteAllVersions = AllVersionsRadio.IsChecked == true;
        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
