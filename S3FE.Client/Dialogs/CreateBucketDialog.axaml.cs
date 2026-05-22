using Avalonia.Controls;

namespace S3FE.Client.Dialogs;

public partial class CreateBucketDialog : Window
{
    public bool Confirmed { get; private set; }
    public string BucketName { get; private set; } = string.Empty;
    public bool Versioned { get; private set; }

    public CreateBucketDialog()
    {
        InitializeComponent();
    }

    private void OnCreateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BucketName = BucketNameTextBox.Text?.Trim() ?? string.Empty;
        Versioned = VersionedCheckBox.IsChecked == true;
        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
