namespace S3FE.Client.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S3FE.Shared.DTOs;

public partial class BucketTabViewModel(string bucketName, IEnumerable<S3ObjectDTO> files, Action<BucketTabViewModel> closeTab) : ViewModelBase
{
    private readonly Action<BucketTabViewModel> _closeTab = closeTab;

    public string BucketName { get; } = bucketName;

    public ObservableCollection<S3ObjectDTO> Files { get; } = new ObservableCollection<S3ObjectDTO>(files);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [RelayCommand]
    private void Close()
    {
        _closeTab(this);
    }
}
