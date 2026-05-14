using CommunityToolkit.Mvvm.ComponentModel;
using S3FE.Client.Models;

namespace S3FE.Client.ViewModels;

public partial class BucketItemViewModel(Bucket bucket) : ViewModelBase
{
    private readonly Bucket _bucket = bucket;

    public string Name => _bucket.Name;

    internal Bucket Bucket => _bucket;

    public bool IsVersioned => _bucket.IsVersioned;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}