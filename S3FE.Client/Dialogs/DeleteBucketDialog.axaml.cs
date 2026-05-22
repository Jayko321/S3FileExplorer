using Avalonia.Controls;

namespace S3FE.Client.Dialogs;

public partial class DeleteBucketDialog : Window
{
    public bool Confirmed { get; private set; }

    public DeleteBucketDialog(string bucketName)
    {
        InitializeComponent();
        MessageText.Text = $"Are you sure you want to delete '{bucketName}'?";
    }

    private void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
