using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace S3FE.Client.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public static Window? MainWindow { get; set; }
}
