namespace S3FE.Client.Services;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using S3FE.Shared.DTOs;

public interface IStorageApiClient
{
    void SetSessionToken(string token);

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task<IReadOnlyList<BucketDTO>> GetBucketsAsync();

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task<BucketDTO> CreateBucketAsync(string bucketName, bool versioned = false);

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task DeleteBucketAsync(string bucketName);

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task DeleteObjectAsync(string bucketName, string key, string? versioning = null);

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task<ObjectListingDTO> ListObjectsAsync(string bucketName, string? prefix = null);

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task<UploadObjectResponseDTO> UploadObjectAsync(string bucketName, string fileName, Stream fileStream, string contentType, string? prefix = null);

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task<UploadObjectResponseDTO> CopyObjectAsync(string bucketName, string sourceKey, string destinationKey);

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task<UploadObjectResponseDTO> RenameObjectAsync(string bucketName, string sourceKey, string destinationKey, string? versioning = null);

    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    Task<(Stream ContentStream, string ContentType)> DownloadObjectAsync(string bucketName, string key);
}
