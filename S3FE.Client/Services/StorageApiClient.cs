namespace S3FE.Client.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using S3FE.Shared.DTOs;

public class StorageApiClient : IStorageApiClient
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:12000")
    };

    public void SetSessionToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<IReadOnlyList<BucketDTO>> GetBucketsAsync()
    {
        var response = await _httpClient.GetAsync("/api/buckets");

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, "Failed to load buckets.");

        var buckets = await response.Content.ReadFromJsonAsync<List<BucketDTO>>();
        return buckets ?? [];
    }

    public async Task<BucketDTO> CreateBucketAsync(string bucketName, bool versioned = false)
    {
        var url = $"/api/buckets/{Uri.EscapeDataString(bucketName)}";
        if (versioned)
            url += "?versioned=true";

        var response = await _httpClient.PutAsync(url, content: null);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to create bucket '{bucketName}'.");

        var dto = await response.Content.ReadFromJsonAsync<BucketDTO>();
        return dto ?? new BucketDTO { Name = bucketName, IsVersioned = versioned };
    }

    public async Task DeleteBucketAsync(string bucketName)
    {
        var response = await _httpClient.DeleteAsync($"/api/buckets/{Uri.EscapeDataString(bucketName)}");

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to delete bucket '{bucketName}'.");
    }

    public async Task DeleteObjectAsync(string bucketName, string key, string? versioning = null)
    {
        var url = $"/api/buckets/{Uri.EscapeDataString(bucketName)}/objects/{Uri.EscapeDataString(key)}";

        if (versioning is not null)
            url += $"?versioning={versioning}";

        var response = await _httpClient.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to delete object '{key}'.");
    }

    public async Task<ObjectListingDTO> ListObjectsAsync(string bucketName, string? prefix = null)
    {
        var url = $"/api/buckets/{Uri.EscapeDataString(bucketName)}/objects";

        if (!string.IsNullOrWhiteSpace(prefix))
            url += $"?prefix={Uri.EscapeDataString(prefix)}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to load objects for bucket '{bucketName}'.");

        var listing = await response.Content.ReadFromJsonAsync<ObjectListingDTO>();
        return listing ?? new ObjectListingDTO();
    }

    public async Task<UploadObjectResponseDTO> CopyObjectAsync(string bucketName, string sourceKey, string destinationKey)
    {
        var url = $"/api/buckets/{Uri.EscapeDataString(bucketName)}/objects/copy?sourceKey={Uri.EscapeDataString(sourceKey)}&destinationKey={Uri.EscapeDataString(destinationKey)}";
        var response = await _httpClient.PostAsync(url, content: null);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to copy object '{sourceKey}' to '{destinationKey}'.");

        var result = await response.Content.ReadFromJsonAsync<UploadObjectResponseDTO>();
        return result ?? new UploadObjectResponseDTO { Key = destinationKey };
    }

    public async Task<UploadObjectResponseDTO> UploadObjectAsync(string bucketName, string fileName, Stream fileStream, string contentType, string? prefix = null)
    {
        var url = $"/api/buckets/{Uri.EscapeDataString(bucketName)}/objects";

        if (!string.IsNullOrWhiteSpace(prefix))
            url += $"?prefix={Uri.EscapeDataString(prefix.TrimStart('/'))}";

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to upload object '{fileName}'.");

        var result = await response.Content.ReadFromJsonAsync<UploadObjectResponseDTO>();
        return result ?? new UploadObjectResponseDTO { Key = fileName };
    }

    public async Task<UploadObjectResponseDTO> RenameObjectAsync(string bucketName, string sourceKey, string destinationKey, string? versioning = null)
    {
        var url = $"/api/buckets/{Uri.EscapeDataString(bucketName)}/objects/rename/{Uri.EscapeDataString(sourceKey)}?destinationKey={Uri.EscapeDataString(destinationKey)}";

        if (versioning is not null)
            url += $"&versioning={versioning}";

        var response = await _httpClient.PostAsync(url, content: null);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to rename object '{sourceKey}' to '{destinationKey}'.");

        var result = await response.Content.ReadFromJsonAsync<UploadObjectResponseDTO>();
        return result ?? new UploadObjectResponseDTO { Key = destinationKey };
    }

    public async Task<(Stream ContentStream, string ContentType)> DownloadObjectAsync(string bucketName, string key)
    {
        var url = $"/api/buckets/{Uri.EscapeDataString(bucketName)}/objects/{Uri.EscapeDataString(key)}";

        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to download object '{key}'.");

        var stream = await response.Content.ReadAsStreamAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return (stream, contentType);
    }

    private static async Task<InvalidOperationException> CreateExceptionAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var error = await response.Content.ReadAsStringAsync();
        return new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? fallbackMessage : error);
    }
}
