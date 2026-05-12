namespace S3FE.Server.Services;

using System.IO;

public interface IObjectStorageService
{
    Task<ListObjectsResult> ListObjectsAsync(string bucketName, string? prefix = null);

    Task<UploadObjectResult> UploadObjectAsync(string bucketName, string? prefix, Stream fileStream, string fileName, string contentType);

    Task<CopyResult> CopyObjectAsync(string bucketName, string sourceKey, string destinationKey,
        string versioningBehavior = "latest");

    Task<DeleteResult> DeleteObjectAsync(string bucketName, string key);

    Task<RenameResult> RenameObjectAsync(string bucketName, string sourceKey, string destinationKey,
        string versioningBehavior = "latest");
}
