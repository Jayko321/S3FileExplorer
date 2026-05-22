namespace S3FE.Client.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dialogs;
using Models;
using Services;

public partial class BucketTabViewModel(string bucketName, Action<BucketTabViewModel> closeTab, Bucket bucket, IStorageModelService storageModelService, IFilePickerService filePickerService, IFileSaveService fileSaveService) : ViewModelBase
{
    private readonly Action<BucketTabViewModel> _closeTab = closeTab;
    private readonly Bucket _bucket = bucket;
    private readonly IStorageModelService _storageModelService = storageModelService;
    private readonly IFilePickerService _filePickerService = filePickerService;
    private readonly IFileSaveService _fileSaveService = fileSaveService;

    public string BucketName { get; } = bucketName;

    public bool IsVersioned => _bucket.IsVersioned;

    public ObservableCollection<S3Object> Files { get; } = [];

    public ObservableCollection<FolderItem> Folders { get; } = [];

    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; } = [new BreadcrumbItem("root", string.Empty)];

    [ObservableProperty]
    public partial string CurrentPrefix { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoadingObjects { get; set; }

    [ObservableProperty]
    public partial bool CanGoUp { get; set; }

    [ObservableProperty]
    public partial S3Object? SelectedObject { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsObjectInfoPanelOpen { get; set; }

    private CancellationTokenSource? _autoReloadCts;
    private bool _isReloading;

    partial void OnCurrentPrefixChanged(string value)
    {
        CanGoUp = !string.IsNullOrEmpty(value);
        UpdateBreadcrumbs(value);
    }

    partial void OnSelectedObjectChanged(S3Object? value)
    {
        if (!_isReloading)
            IsObjectInfoPanelOpen = value is not null;
    }

    [RelayCommand]
    private void CloseObjectInfoPanel()
    {
        IsObjectInfoPanelOpen = false;
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        await LoadObjectsAsync();
    }

    public async Task LoadObjectsAsync()
    {
        IsLoadingObjects = true;

        var previousKey = SelectedObject?.Key;
        var previousETag = SelectedObject?.ETag;
        var wasPanelOpen = IsObjectInfoPanelOpen;

        try
        {
            var result = await _storageModelService.ListObjectsAsync(_bucket, CurrentPrefix);

            Folders.Clear();
            foreach (var folder in result.Folders)
                Folders.Add(folder);

            _isReloading = true;
            Files.Clear();
            foreach (var file in result.Files.Reverse())
                Files.Add(file);

            if (wasPanelOpen && previousKey is not null)
            {
                var match = Files.FirstOrDefault(f => f.Key == previousKey && f.ETag == previousETag);
                SelectedObject = match;
                IsObjectInfoPanelOpen = match is not null;
            }
            else
            {
                SelectedObject = null;
            }
            _isReloading = false;
        }
        finally
        {
            IsLoadingObjects = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToFolderAsync(FolderItem? folder)
    {
        if (folder is null)
            return;

        CurrentPrefix = folder.FullPrefix;
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task NavigateToBreadcrumbAsync(BreadcrumbItem? breadcrumb)
    {
        if (breadcrumb is null)
            return;

        CurrentPrefix = breadcrumb.Prefix;
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task NavigateUpAsync()
    {
        if (string.IsNullOrEmpty(CurrentPrefix))
            return;

        var trimmed = CurrentPrefix.TrimEnd('/');
        var lastSlash = trimmed.LastIndexOf('/');

        CurrentPrefix = lastSlash >= 0
            ? trimmed[..(lastSlash + 1)]
            : string.Empty;

        await LoadObjectsAsync();
    }

    private void UpdateBreadcrumbs(string prefix)
    {
        Breadcrumbs.Clear();
        Breadcrumbs.Add(new BreadcrumbItem("root", string.Empty));

        if (string.IsNullOrEmpty(prefix))
            return;

        var segments = prefix.TrimEnd('/').Split('/');
        var accumulated = string.Empty;

        foreach (var segment in segments)
        {
            accumulated += segment + "/";
            Breadcrumbs.Add(new BreadcrumbItem(segment, accumulated));
        }
    }

    public async Task UploadFileAsync()
    {
        var pickResult = await _filePickerService.PickFileAsync();

        if (pickResult is null)
            return;

        var dialog = new UploadObjectDialog(pickResult.FileName, CurrentPrefix);
        await dialog.ShowDialog(MainWindow!);

        if (dialog.Confirmed)
        {
            await using var stream = pickResult.Stream;
            await _storageModelService.UploadObjectAsync(_bucket, pickResult.FileName, stream, pickResult.ContentType, dialog.Prefix);
            await LoadObjectsAsync();
        }
        else
        {
            pickResult.Stream.Dispose();
        }
    }

    [RelayCommand]
    private async Task DownloadObjectAsync(S3Object? objectToDownload)
    {
        if (objectToDownload is null)
            return;

        var (stream, _) = await _storageModelService.DownloadObjectAsync(_bucket, objectToDownload);

        await _fileSaveService.SaveFileAsync(objectToDownload.Key, stream);
    }

    [RelayCommand]
    private async Task DeleteObjectAsync(S3Object? objectToDelete)
    {
        if (objectToDelete is null)
            return;

        var dialog = new DeleteObjectDialog(objectToDelete.Key, IsVersioned);
        await dialog.ShowDialog(MainWindow!);

        if (!dialog.Confirmed)
            return;

        var versioning = IsVersioned && dialog.DeleteAllVersions ? "all" : "latest";
        await _storageModelService.DeleteObjectAsync(_bucket, objectToDelete, versioning);
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task RenameObjectAsync(S3Object? objectToRename)
    {
        if (objectToRename is null)
            return;

        var dialog = new RenameObjectDialog(objectToRename.Key, IsVersioned);
        await dialog.ShowDialog(MainWindow!);

        if (!dialog.Confirmed)
            return;

        var versioning = IsVersioned && dialog.RenameAllVersions ? "all" : "latest";
        await _storageModelService.RenameObjectAsync(_bucket, objectToRename.Key, dialog.DestinationKey, versioning);
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private void Close()
    {
        StopAutoReload();
        _closeTab(this);
    }

    public void StartAutoReload()
    {
        StopAutoReload();
        _autoReloadCts = new CancellationTokenSource();
        _ = RunAutoReloadAsync(_autoReloadCts.Token);
    }

    public void StopAutoReload()
    {
        _autoReloadCts?.Cancel();
        _autoReloadCts?.Dispose();
        _autoReloadCts = null;
    }

    private async Task RunAutoReloadAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, ct);

                if (!IsLoadingObjects)
                    await LoadObjectsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
