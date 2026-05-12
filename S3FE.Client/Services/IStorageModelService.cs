namespace S3FE.Client.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using S3FE.Client.Models;

public interface IStorageModelService
{
    void SetSessionToken(string token);

    Task<IReadOnlyList<Bucket>> GetBucketsAsync();

    Task<Bucket> CreateBucketAsync(string bucketName);

    Task DeleteBucketAsync(Bucket bucket);

    Task DeleteObjectAsync(Bucket bucket, S3Object s3Object);

    Task<IReadOnlyList<S3Object>> ListObjectsAsync(Bucket bucket, string? prefix = null);
}