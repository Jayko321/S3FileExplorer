namespace S3FE.Client.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using S3FE.Shared.DTOs;

public interface IStorageApiClient
{
    void SetSessionToken(string token);

    Task<IReadOnlyList<BucketDTO>> GetBucketsAsync();

    Task CreateBucketAsync(string bucketName);

    Task DeleteBucketAsync(string bucketName);

    Task<ObjectListingDTO> ListObjectsAsync(string bucketName, string? prefix = null);
}
