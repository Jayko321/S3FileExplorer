namespace S3FE.Client.Services;

using System;
using System.Collections.Generic;
using System.IO;
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

    public async Task<Bucket> CreateBucketAsync(string bucketName, bool versioned = false)
    {
        var dto = await _storageApiClient.CreateBucketAsync(bucketName, versioned);
        return dto.ToModel();
    }

    public Task DeleteBucketAsync(Bucket bucket)
    {
        //TODO: Passthrough errors from the API
        return _storageApiClient.DeleteBucketAsync(bucket.Name);
    }

    public Task DeleteObjectAsync(Bucket bucket, S3Object s3Object, string? versioning = null)
    {
        //TODO: Passthrough errors from the API
        return _storageApiClient.DeleteObjectAsync(bucket.Name, s3Object.Key, versioning);
    }

    public async Task<ListObjectsResult> ListObjectsAsync(Bucket bucket, string? prefix = null)
    {
        var listing = await _storageApiClient.ListObjectsAsync(bucket.Name, prefix);

        var files = listing.Files.Select(dto => dto.ToModel()).ToList();

        var folders = listing.Folders
            .Select(f => new FolderItem(
                GetFolderDisplayName(f, prefix ?? string.Empty),
                f))
            .ToList();

        return new ListObjectsResult(folders, files);
    }

    private static string GetFolderDisplayName(string folderPrefix, string currentPrefix)
    {
        var name = folderPrefix.StartsWith(currentPrefix)
            ? folderPrefix[currentPrefix.Length..]
            : folderPrefix;

        return name.TrimEnd('/');
    }

    public async Task<S3Object> UploadObjectAsync(Bucket bucket, string fileName, Stream fileStream, string contentType, string? prefix = null)
    {
        var dto = await _storageApiClient.UploadObjectAsync(bucket.Name, fileName, fileStream, contentType, prefix);
        return new S3Object(dto.Key, null, DateTime.UtcNow, dto.VersionId ?? string.Empty);
    }

    public async Task<S3Object> CopyObjectAsync(Bucket bucket, string sourceKey, string destinationKey)
    {
        var dto = await _storageApiClient.CopyObjectAsync(bucket.Name, sourceKey, destinationKey);
        return new S3Object(dto.Key, null, null, string.Empty);
    }

    public async Task<S3Object> RenameObjectAsync(Bucket bucket, string sourceKey, string destinationKey, string? versioning = null)
    {
        var dto = await _storageApiClient.RenameObjectAsync(bucket.Name, sourceKey, destinationKey, versioning);
        return new S3Object(dto.Key, null, null, string.Empty);
    }

    public async Task<(Stream ContentStream, string ContentType)> DownloadObjectAsync(Bucket bucket, S3Object s3Object)
    {
        return await _storageApiClient.DownloadObjectAsync(bucket.Name, s3Object.Key);
    }
}
