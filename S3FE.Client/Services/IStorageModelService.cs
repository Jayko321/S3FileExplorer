namespace S3FE.Client.Services;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using S3FE.Client.Models;

public interface IStorageModelService
{
    void SetSessionToken(string token);

    Task<IReadOnlyList<Bucket>> GetBucketsAsync();

    Task<Bucket> CreateBucketAsync(string bucketName, bool versioned = false);

    Task DeleteBucketAsync(Bucket bucket);

    Task DeleteObjectAsync(Bucket bucket, S3Object s3Object, string? versioning = null);

    Task<ListObjectsResult> ListObjectsAsync(Bucket bucket, string? prefix = null);

    Task<S3Object> UploadObjectAsync(Bucket bucket, string fileName, Stream fileStream, string contentType, string? prefix = null);

    Task<S3Object> CopyObjectAsync(Bucket bucket, string sourceKey, string destinationKey);

    Task<S3Object> RenameObjectAsync(Bucket bucket, string sourceKey, string destinationKey, string? versioning = null);

    Task<(Stream ContentStream, string ContentType)> DownloadObjectAsync(Bucket bucket, S3Object s3Object);
}