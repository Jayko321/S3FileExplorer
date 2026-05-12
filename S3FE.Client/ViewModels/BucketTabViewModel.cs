namespace S3FE.Client.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S3FE.Client.Models;
using S3FE.Client.Services;

public partial class BucketTabViewModel(string bucketName, IEnumerable<S3Object> files, Action<BucketTabViewModel> closeTab, Bucket bucket, IStorageModelService storageModelService) : ViewModelBase
{
    private readonly Action<BucketTabViewModel> _closeTab = closeTab;
    private readonly Bucket _bucket = bucket;
    private readonly IStorageModelService _storageModelService = storageModelService;

    public string BucketName { get; } = bucketName;

    public ObservableCollection<S3Object> Files { get; } = new ObservableCollection<S3Object>(files);

    [ObservableProperty]
    public partial S3Object? SelectedObject { get; set; }
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [RelayCommand]
    private void Close()
    {
        _closeTab(this);
    }

    [RelayCommand]
    private async Task DeleteSelectedObjectAsync()
    {
        if (SelectedObject is null)
            return;

        //TODO: Show confirmation dialog before deleting
        //TODO: Handle errors from a server do not update UI before removing is completed
        await _storageModelService.DeleteObjectAsync(_bucket, SelectedObject);
        Files.Remove(SelectedObject);
        SelectedObject = null;
    }
}
