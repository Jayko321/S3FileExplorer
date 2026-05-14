using Avalonia.Controls;
using Avalonia.Input;
using S3FE.Client.Models;
using S3FE.Client.ViewModels;

namespace S3FE.Client.Controls;

public partial class ObjectTabContent : UserControl
{
    public ObjectTabContent() => InitializeComponent();

    private void OnFolderDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is FolderItem folder)
        {
            if (DataContext is BucketTabViewModel vm)
            {
                vm.NavigateToFolderCommand.Execute(folder);
            }
        }
    }
}
