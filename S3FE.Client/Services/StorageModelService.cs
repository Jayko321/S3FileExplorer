namespace S3FE.Client.Services;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using S3FE.Client.Extensions;
using S3FE.Client.Models;
using S3FE.Shared.DTOs;

public sealed class StorageModelService(IStorageApiClient storageApiClient) : IStorageModelService
{
    private readonly IStorageApiClient _storageApiClient = storageApiClient;

    public void SetSessionToken(string token) => _storageApiClient.SetSessionToken(token);

    public async Task<IReadOnlyList<Bucket>> GetBucketsAsync()
    {
        var buckets = await _storageApiClient.GetBucketsAsync();
        return [.. buckets.Select(dto => dto.ToModel())];
    }

    public async Task<Bucket> CreateBucketAsync(string bucketName)
    {
        await _storageApiClient.CreateBucketAsync(bucketName);
        return new Bucket(bucketName);
    }

    public Task DeleteBucketAsync(Bucket bucket)
    {
        return _storageApiClient.DeleteBucketAsync(bucket.Name);
    }

    public Task DeleteObjectAsync(Bucket bucket, S3Object s3Object)
    {
        return _storageApiClient.DeleteObjectAsync(bucket.Name, s3Object.Key);
    }

    public async Task<IReadOnlyList<S3Object>> ListObjectsAsync(Bucket bucket, string? prefix = null)
    {
        var listing = await _storageApiClient.ListObjectsAsync(bucket.Name, prefix);
        return [.. listing.Files.Select(dto => dto.ToModel())];
    }
}