using Avalonia.Controls;

namespace S3FE.Client.Dialogs;

public partial class UploadObjectDialog : Window
{
    public bool Confirmed { get; private set; }
    public string Prefix { get; private set; } = string.Empty;

    public UploadObjectDialog(string fileName, string currentPrefix)
    {
        InitializeComponent();
        FileNameText.Text = $"File: {fileName}";
        PrefixTextBox.Text = currentPrefix;
    }

    private void OnUploadClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Prefix = PrefixTextBox.Text?.Trim() ?? string.Empty;
        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
