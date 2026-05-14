namespace S3FE.Client.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S3FE.Client.Models;
using S3FE.Client.Services;

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
    public partial bool IsConfirmDeleteOpen { get; set; }

    [ObservableProperty]
    public partial string ConfirmDeleteMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsObjectInfoPanelOpen { get; set; }

    [ObservableProperty]
    public partial bool IsUploadPopupOpen { get; set; }

    [ObservableProperty]
    public partial string UploadFilePrefix { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool DeleteAllVersions { get; set; }

    [ObservableProperty]
    public partial bool IsRenamePopupOpen { get; set; }

    [ObservableProperty]
    public partial string RenameDestinationKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool RenameAllVersions { get; set; }

    public bool IsDeleteVersioningChoiceVisible => IsVersioned && IsConfirmDeleteOpen;

    public bool IsRenameVersioningChoiceVisible => IsVersioned && IsRenamePopupOpen;

    partial void OnIsConfirmDeleteOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(IsDeleteVersioningChoiceVisible));
        if (!value)
            DeleteAllVersions = false;
    }

    partial void OnIsRenamePopupOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(IsRenameVersioningChoiceVisible));
        if (!value)
            RenameAllVersions = false;
    }

    private CancellationTokenSource? _autoReloadCts;

    partial void OnCurrentPrefixChanged(string value)
    {
        CanGoUp = !string.IsNullOrEmpty(value);
        UpdateBreadcrumbs(value);
        UploadFilePrefix = value;
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

    private S3Object? _pendingObjectDelete;
    private Stream? _pendingUploadStream;
    private string _pendingUploadFileName = string.Empty;
    private string _pendingUploadContentType = string.Empty;
    private bool _isReloading;

    partial void OnSelectedObjectChanged(S3Object? value)
    {
        if (!_isReloading)
            IsObjectInfoPanelOpen = value is not null;
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
    private async Task NavigateToFolderAsync(FolderItem folder)
    {
        if (folder is null)
            return;

        CurrentPrefix = folder.FullPrefix;
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task NavigateToBreadcrumbAsync(BreadcrumbItem breadcrumb)
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

        _pendingUploadStream = pickResult.Stream;
        _pendingUploadFileName = pickResult.FileName;
        _pendingUploadContentType = pickResult.ContentType;
        IsUploadPopupOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmUploadAsync()
    {
        var stream = _pendingUploadStream;
        var fileName = _pendingUploadFileName;
        var contentType = _pendingUploadContentType;
        var prefix = UploadFilePrefix.Trim();

        _pendingUploadStream = null;
        _pendingUploadFileName = string.Empty;
        _pendingUploadContentType = string.Empty;

        if (stream is null)
            return;

        await using (stream.ConfigureAwait(false))
        {
            try
            {
                await _storageModelService.UploadObjectAsync(_bucket, fileName, stream, contentType, prefix);
                await LoadObjectsAsync();
                IsUploadPopupOpen = false;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    [RelayCommand]
    private void CancelUpload()
    {
        var stream = _pendingUploadStream;
        _pendingUploadStream = null;
        _pendingUploadFileName = string.Empty;
        _pendingUploadContentType = string.Empty;
        IsUploadPopupOpen = false;
        stream?.Dispose();
    }

    private S3Object? _pendingRenameObject;

    [RelayCommand]
    private void RenameObject(S3Object objectToRename)
    {
        if (objectToRename is null)
            return;

        _pendingRenameObject = objectToRename;
        RenameDestinationKey = objectToRename.Key;
        RenameAllVersions = false;
        IsRenamePopupOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmRenameAsync()
    {
        var objectToRename = _pendingRenameObject;
        var destinationKey = RenameDestinationKey.Trim();
        var renameAllVersions = RenameAllVersions;
        _pendingRenameObject = null;
        IsRenamePopupOpen = false;

        if (objectToRename is null || string.IsNullOrWhiteSpace(destinationKey))
            return;

        var versioning = IsVersioned && renameAllVersions ? "all" : "latest";
        await _storageModelService.RenameObjectAsync(_bucket, objectToRename.Key, destinationKey, versioning);
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private void CancelRename()
    {
        _pendingRenameObject = null;
        IsRenamePopupOpen = false;
    }

    [RelayCommand]
    private async Task DownloadObjectAsync(S3Object objectToDownload)
    {
        if (objectToDownload is null)
            return;

        var (stream, contentType) = await _storageModelService.DownloadObjectAsync(_bucket, objectToDownload);

        await _fileSaveService.SaveFileAsync(objectToDownload.Key, stream);
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

                if (!IsLoadingObjects && !IsUploadPopupOpen && !IsConfirmDeleteOpen && !IsRenamePopupOpen)
                    await LoadObjectsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    [RelayCommand]
    private void DeleteObject(S3Object objectToDelete)
    {
        if (objectToDelete is null)
            return;

        _pendingObjectDelete = objectToDelete;
        DeleteAllVersions = false;
        ConfirmDeleteMessage = $"Are you sure you want to delete '{objectToDelete.Key}'?";
        IsConfirmDeleteOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        var objectToDelete = _pendingObjectDelete;
        var deleteAllVersions = DeleteAllVersions;
        _pendingObjectDelete = null;
        IsConfirmDeleteOpen = false;

        if (objectToDelete is null)
            return;

        var versioning = IsVersioned && deleteAllVersions ? "all" : "latest";
        await _storageModelService.DeleteObjectAsync(_bucket, objectToDelete, versioning);
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private void CancelDelete()
    {
        _pendingObjectDelete = null;
        IsConfirmDeleteOpen = false;
    }
}
