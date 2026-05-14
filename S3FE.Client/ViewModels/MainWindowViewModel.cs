using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using S3FE.Client.Models;
using S3FE.Client.Services;
using S3FE.Shared.DTOs;

namespace S3FE.Client.ViewModels;

public partial class MainWindowViewModel(IAuthApiClient authApiClient, IStorageModelService storageModelService, IFilePickerService filePickerService, IFileSaveService fileSaveService) : ViewModelBase
{
    private readonly IAuthApiClient _authApiClient = authApiClient;
    private readonly IStorageModelService _storageModelService = storageModelService;
    private readonly IFilePickerService _filePickerService = filePickerService;
    private readonly IFileSaveService _fileSaveService = fileSaveService;

    [ObservableProperty]
    public partial string Endpoint { get; set; } = "http://localhost:9000";

    [ObservableProperty]
    public partial string AccessKey { get; set; } = "minioadmin";

    [ObservableProperty]
    public partial string SecretKey { get; set; } = "minioadmin";

    [ObservableProperty]
    public partial bool IsConnecting { get; set; }

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial bool IsCreateBucketPopupOpen { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmDeleteOpen { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Enter your MinIO connection details.";

    [ObservableProperty]
    public partial string NewBucketName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CreateVersioned { get; set; }

    [ObservableProperty]
    public partial string ConfirmDeleteMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BucketItemViewModel? SelectedBucket { get; set; }

    [ObservableProperty]
    public partial BucketTabViewModel? SelectedBucketTab { get; set; }

    public ObservableCollection<BucketItemViewModel> Buckets { get; } = [];

    public ObservableCollection<BucketTabViewModel> BucketTabs { get; } = [];

    [ObservableProperty]
    public partial bool IsUploadVisible { get; set; }

    [RelayCommand]
    private async Task UploadFileAsync()
    {
        if (SelectedBucketTab is null)
            return;

        await SelectedBucketTab.UploadFileAsync();
    }

    public MainWindowViewModel()
        : this(new AuthApiClient(), new StorageModelService(new StorageApiClient()), new FilePickerService(), new FileSaveService())
    {
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        IsConnecting = true;
        IsConnected = false;
        StatusMessage = "Connecting to MinIO...";
        Buckets.Clear();
        BucketTabs.Clear();

        try
        {
            var response = await _authApiClient.ConnectAsync(new ConnectRequestDTO
            {
                Endpoint = Endpoint,
                AccessKey = AccessKey,
                SecretKey = SecretKey
            });

            _storageModelService.SetSessionToken(response.Token);
            await LoadBucketsAsync();

            IsConnected = true;
            StatusMessage = $"Connected to {response.Endpoint}. Loaded {Buckets.Count} bucket(s).";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private void OpenCreateBucketPopup()
    {
        NewBucketName = string.Empty;
        CreateVersioned = false;
        IsCreateBucketPopupOpen = true;
    }

    [RelayCommand]
    private void CancelCreateBucket()
    {
        IsCreateBucketPopupOpen = false;
        NewBucketName = string.Empty;
    }

    [RelayCommand]
    private async Task AddBucketAsync()
    {
        var bucketName = NewBucketName.Trim();

        if (string.IsNullOrWhiteSpace(bucketName))
        {
            StatusMessage = "Enter a bucket name first.";
            return;
        }

        try
        {
            var bucket = await _storageModelService.CreateBucketAsync(bucketName, CreateVersioned);
            var bucketItem = new BucketItemViewModel(bucket);

            Buckets.Add(bucketItem);
            SelectedBucket = bucketItem;
            IsCreateBucketPopupOpen = false;
            NewBucketName = string.Empty;
            StatusMessage = $"Bucket '{bucketName}' created.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create bucket: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RequestDeleteBucket()
    {
        if (SelectedBucket is null)
        {
            StatusMessage = "Select a bucket to delete.";
            return;
        }

        ConfirmDeleteMessage = $"Are you sure you want to delete '{SelectedBucket.Name}'?";
        IsConfirmDeleteOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsConfirmDeleteOpen = false;
        var bucketToDelete = SelectedBucket;

        if (bucketToDelete is null)
            return;

        var bucketName = bucketToDelete.Name;

        try
        {
            await _storageModelService.DeleteBucketAsync(bucketToDelete.Bucket);

            var tab = BucketTabs.FirstOrDefault(bucketTab => bucketTab.BucketName == bucketName);
            if (tab is not null)
                BucketTabs.Remove(tab);

            Buckets.Remove(bucketToDelete);
            SelectedBucket = Buckets.Count > 0 ? Buckets[0] : null;
            SelectedBucketTab = BucketTabs.Count > 0 ? BucketTabs[0] : null;
            StatusMessage = $"Bucket '{bucketName}' deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to delete bucket: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsConfirmDeleteOpen = false;
    }

    partial void OnSelectedBucketChanged(BucketItemViewModel? value)
    {
        foreach (var bucket in Buckets)
            bucket.IsSelected = bucket == value;

        if (value is null)
        {
            SelectedBucketTab = null;
            return;
        }

        var matchingTab = BucketTabs.FirstOrDefault(bucketTab => bucketTab.BucketName == value.Name);
        if (matchingTab is not null)
        {
            SelectedBucketTab = matchingTab;
            return;
        }

        _ = OpenBucketTabAsync(value.Name, value.Bucket);
    }

    partial void OnSelectedBucketTabChanged(BucketTabViewModel? value)
    {
        IsUploadVisible = value is not null;

        foreach (var tab in BucketTabs)
        {
            tab.IsSelected = tab == value;
            if (tab != value)
                tab.IsObjectInfoPanelOpen = false;
        }

        if (value is null)
            return;

        var matchingBucket = Buckets.FirstOrDefault(bucket => bucket.Name == value.BucketName);
        if (matchingBucket is not null && SelectedBucket != matchingBucket)
            SelectedBucket = matchingBucket;
    }

    private async Task LoadBucketsAsync()
    {
        var buckets = await _storageModelService.GetBucketsAsync();

        foreach (var bucket in buckets)
            Buckets.Add(new BucketItemViewModel(bucket));

        SelectedBucket = Buckets.Count > 0 ? Buckets[0] : null;
    }

    private void CloseTab(BucketTabViewModel tab)
    {
        var index = BucketTabs.IndexOf(tab);
        BucketTabs.Remove(tab);

        if (SelectedBucketTab == tab)
        {
            if (BucketTabs.Count == 0)
            {
                SelectedBucketTab = null;
                SelectedBucket = null;
                return;
            }

            var nextIndex = Math.Clamp(index, 0, BucketTabs.Count - 1);
            SelectedBucketTab = BucketTabs[nextIndex];
        }
    }

    private async Task OpenBucketTabAsync(string bucketName, Bucket bucket)
    {
        try
        {
            var existingTab = BucketTabs.FirstOrDefault(bucketTab => bucketTab.BucketName == bucketName);
            if (existingTab is not null)
            {
                SelectedBucketTab = existingTab;
                StatusMessage = $"Opened bucket '{bucketName}'.";
                return;
            }

            StatusMessage = $"Loading objects for bucket '{bucketName}'...";
            var tab = new BucketTabViewModel(bucketName, CloseTab, bucket, _storageModelService, _filePickerService, _fileSaveService);
            BucketTabs.Add(tab);
            SelectedBucketTab = tab;
            await tab.LoadObjectsAsync();
            tab.StartAutoReload();
            StatusMessage = $"Opened bucket '{bucketName}' with {tab.Files.Count} object(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open bucket '{bucketName}': {ex.Message}";
        }
    }
}
