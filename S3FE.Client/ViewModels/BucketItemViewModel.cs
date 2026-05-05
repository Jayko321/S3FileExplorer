namespace S3FE.Client.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class BucketItemViewModel(string name) : ViewModelBase
{
    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
