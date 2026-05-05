namespace S3FE.Client.Services;

using System;
using System.Collections.Generic;
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

    public async Task CreateBucketAsync(string bucketName)
    {
        var response = await _httpClient.PutAsync($"/api/buckets/{Uri.EscapeDataString(bucketName)}", content: null);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to create bucket '{bucketName}'.");
    }

    public async Task DeleteBucketAsync(string bucketName)
    {
        var response = await _httpClient.DeleteAsync($"/api/buckets/{Uri.EscapeDataString(bucketName)}");

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, $"Failed to delete bucket '{bucketName}'.");
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

    private static async Task<InvalidOperationException> CreateExceptionAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var error = await response.Content.ReadAsStringAsync();
        return new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? fallbackMessage : error);
    }
}
