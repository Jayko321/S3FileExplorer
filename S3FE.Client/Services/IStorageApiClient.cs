namespace S3FE.Client.Services;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using S3FE.Shared.DTOs;

public interface IStorageApiClient
{
    void SetSessionToken(string token);

    Task<IReadOnlyList<BucketDTO>> GetBucketsAsync();

    Task<BucketDTO> CreateBucketAsync(string bucketName, bool versioned = false);

    Task DeleteBucketAsync(string bucketName);

    Task DeleteObjectAsync(string bucketName, string key, string? versioning = null);

    Task<ObjectListingDTO> ListObjectsAsync(string bucketName, string? prefix = null);

    Task<UploadObjectResponseDTO> UploadObjectAsync(string bucketName, string fileName, Stream fileStream, string contentType, string? prefix = null);

    Task<UploadObjectResponseDTO> CopyObjectAsync(string bucketName, string sourceKey, string destinationKey);

    Task<UploadObjectResponseDTO> RenameObjectAsync(string bucketName, string sourceKey, string destinationKey, string? versioning = null);

    Task<(Stream ContentStream, string ContentType)> DownloadObjectAsync(string bucketName, string key);
}
