namespace S3FE.Client.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using S3FE.Shared.DTOs;

public interface IStorageApiClient
{
    void SetSessionToken(string token);

    Task<IReadOnlyList<BucketDTO>> GetBucketsAsync();

    Task<BucketDTO> CreateBucketAsync(string bucketName, bool versioned = false);

    Task DeleteBucketAsync(string bucketName);

    Task DeleteObjectAsync(string bucketName, string key);

    Task<ObjectListingDTO> ListObjectsAsync(string bucketName, string? prefix = null);

    Task<UploadObjectResponseDTO> CopyObjectAsync(string bucketName, string sourceKey, string destinationKey);

    Task<UploadObjectResponseDTO> RenameObjectAsync(string bucketName, string sourceKey, string destinationKey);
}
