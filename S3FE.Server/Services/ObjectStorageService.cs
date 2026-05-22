namespace S3FE.Server.Services;

using Amazon.S3;
using Amazon.S3.Model;
using S3FE.Shared.DTOs;
using System.IO;
using System.Net;

public sealed class ObjectStorageService(
    ICurrentS3ClientProvider s3ClientProvider) : IObjectStorageService
{
    private IAmazonS3 S3Client => s3ClientProvider.GetClient();

    public async Task<ListObjectsResult> ListObjectsAsync(string bucketName, string? prefix = null)
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix,
                Delimiter = "/"
            };

            var response = await S3Client.ListObjectsV2Async(request);

            var s3Objects = response.S3Objects ?? [];

            var listing = new ObjectListingDTO
            {
                Folders = response.CommonPrefixes ?? [],
                Files = [.. s3Objects.Select(s3Object => new S3ObjectDTO
                    {
                        Key = s3Object.Key,
                        Size = s3Object.Size,
                        LastModified = s3Object.LastModified,
                        ETag = s3Object.ETag
                    })]
            };
            
            return ListObjectsResult.Success(listing);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return ListObjectsResult.Failure(404, $"Bucket '{bucketName}' does not exist.");
        }
        catch (AmazonS3Exception ex)
        {
            return ListObjectsResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    public async Task<ObjectVersionsResult> ListVersionsAsync(string bucketName, string key)
    {
        try
        {
            var sdkVersions = await FetchObjectVersionsAsync(S3Client, bucketName, key);

            var versions = sdkVersions.Select(v => new S3ObjectDTO
            {
                Key = v.Key,
                VersionId = v.VersionId,
                Size = v.Size,
                LastModified = v.LastModified,
                ETag = v.ETag,
                IsLatest = v.IsLatest ?? false,
                IsDeleteMarker = v.IsDeleteMarker ?? false,
                VersionIds = [v.VersionId]
            }).ToList();

            return ObjectVersionsResult.Success(versions);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return ObjectVersionsResult.Failure(404, $"Bucket '{bucketName}' does not exist.");
        }
        catch (AmazonS3Exception ex)
        {
            return ObjectVersionsResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    public async Task<UploadObjectResult> UploadObjectAsync(string bucketName, string? prefix, Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var s3Key = string.IsNullOrEmpty(prefix)
                ? fileName
                : $"{prefix.TrimEnd('/')}/{fileName}";

            if (fileStream.CanSeek)
                fileStream.Seek(0, SeekOrigin.Begin);

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = s3Key,
                InputStream = fileStream,
                ContentType = contentType,
                UseChunkEncoding = false
            };

            await S3Client.PutObjectAsync(request);

            return UploadObjectResult.Success(new UploadObjectResponseDTO { Key = s3Key });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return UploadObjectResult.Failure(404, $"Bucket '{bucketName}' does not exist.");
        }
        catch (AmazonS3Exception ex)
        {
            return UploadObjectResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    public async Task<CopyResult> CopyObjectAsync(string bucketName, string sourceKey, string destinationKey,
        string versioningBehavior = "latest")
    {
        var sourceCheck = await CheckObjectAsync(S3Client, bucketName, sourceKey, expectAbsent: false);

        if (!sourceCheck.IsSuccess)
            return CopyResult.Failure(sourceCheck.StatusCode, sourceCheck.ErrorMessage!);

        var destCheck = await CheckObjectAsync(S3Client, bucketName, destinationKey, expectAbsent: true);

        if (!destCheck.IsSuccess)
            return CopyResult.Failure(destCheck.StatusCode, destCheck.ErrorMessage!);

        if (versioningBehavior == "all")
            return await CopyAllVersionsAsync(S3Client, bucketName, sourceKey, destinationKey);

        return await CopyLatestVersionAsync(S3Client, bucketName, sourceKey, destinationKey);
    }

    private static async Task<CopyResult> CopyLatestVersionAsync(IAmazonS3 s3Client, string bucketName, string sourceKey, string destinationKey)
    {
        try
        {
            var request = new CopyObjectRequest
            {
                SourceBucket = bucketName,
                SourceKey = sourceKey,
                DestinationBucket = bucketName,
                DestinationKey = destinationKey,
                MetadataDirective = S3MetadataDirective.COPY
            };

            await s3Client.CopyObjectAsync(request);
            return CopyResult.Success(destinationKey);
        }
        catch (AmazonS3Exception ex)
        {
            return CopyResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    private static async Task<CopyResult> CopyAllVersionsAsync(IAmazonS3 s3Client, string bucketName, string sourceKey, string destinationKey)
    {
        try
        {
            var versions = await FetchObjectVersionsAsync(s3Client, bucketName, sourceKey);

            if (versions.Count == 0)
                return CopyResult.Failure(404, $"Object '{sourceKey}' does not exist in bucket '{bucketName}'.");

            foreach (var version in versions.OrderBy(v => v.LastModified))
            {
                var request = new CopyObjectRequest
                {
                    SourceBucket = bucketName,
                    SourceKey = sourceKey,
                    SourceVersionId = version.VersionId,
                    DestinationBucket = bucketName,
                    DestinationKey = destinationKey,
                    MetadataDirective = S3MetadataDirective.COPY
                };

                await s3Client.CopyObjectAsync(request);
            }

            return CopyResult.Success(destinationKey);
        }
        catch (AmazonS3Exception ex)
        {
            return CopyResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    public async Task<DeleteResult> DeleteObjectAsync(string bucketName, string key, string versioning = "latest")
    {
        if (versioning == "all")
            return await DeleteAllVersionsAsync(bucketName, key);

        var sourceCheck = await CheckObjectAsync(S3Client, bucketName, key, expectAbsent: false);

        if (!sourceCheck.IsSuccess)
            return DeleteResult.Failure(sourceCheck.StatusCode, sourceCheck.ErrorMessage!);

        try
        {
            await S3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            });

            return DeleteResult.Success();
        }
        catch (AmazonS3Exception ex)
        {
            return DeleteResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    public async Task<DownloadResult> DownloadObjectAsync(string bucketName, string key)
    {
        try
        {
            using var response = await S3Client.GetObjectAsync(bucketName, key);
            var contentType = response.Headers.ContentType ?? "application/octet-stream";
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            ms.Position = 0;
            return DownloadResult.Success(ms, contentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchBucket")
        {
            return DownloadResult.Failure(404, $"Object '{key}' does not exist in bucket '{bucketName}'.");
        }
        catch (AmazonS3Exception ex)
        {
            return DownloadResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    public async Task<RenameResult> RenameObjectAsync(string bucketName, string sourceKey, string destinationKey,
        string versioningBehavior = "latest")
    {
        var copyResult = await CopyObjectAsync(bucketName, sourceKey, destinationKey, versioningBehavior);

        if (!copyResult.IsSuccess)
            return RenameResult.Failure(copyResult.StatusCode, copyResult.ErrorMessage!);

        var deleteResult = versioningBehavior == "all"
            ? await DeleteAllVersionsAsync(bucketName, sourceKey)
            : await DeleteObjectAsync(bucketName, sourceKey);

        if (!deleteResult.IsSuccess)
        {
            return RenameResult.PartialFailure(destinationKey,
                $"Object was copied to '{destinationKey}' but the original '{sourceKey}' could not be deleted: {deleteResult.ErrorMessage}");
        }

        return RenameResult.Success(destinationKey);
    }

    private async Task<DeleteResult> DeleteAllVersionsAsync(string bucketName, string sourceKey)
    {
        try
        {
            var versions = await FetchObjectVersionsAsync(S3Client, bucketName, sourceKey, includeDeleteMarkers: true);

            if (versions.Count == 0)
                return DeleteResult.Success();

            var deleteRequest = new DeleteObjectsRequest
            {
                BucketName = bucketName,
                Objects = [.. versions.Select(v => new KeyVersion { Key = v.Key, VersionId = v.VersionId })]
            };

            await S3Client.DeleteObjectsAsync(deleteRequest);

            return DeleteResult.Success();
        }
        catch (AmazonS3Exception ex)
        {
            return DeleteResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    private static async Task<List<S3ObjectVersion>> FetchObjectVersionsAsync(IAmazonS3 s3Client, string bucketName, string key, bool includeDeleteMarkers = false)
    {
        var versions = new List<S3ObjectVersion>();
        var request = new ListVersionsRequest
        {
            BucketName = bucketName,
            Prefix = key
        };

        ListVersionsResponse response;
        do
        {
            response = await s3Client.ListVersionsAsync(request);
            versions.AddRange(response.Versions
                .Where(v => v.Key == key && (includeDeleteMarkers || v.IsDeleteMarker != true)));
            request.KeyMarker = response.NextKeyMarker;
            request.VersionIdMarker = response.NextVersionIdMarker;
        } while (response.IsTruncated == true);

        return versions;
    }

    private static async Task<ObjectServiceResult> CheckObjectAsync(IAmazonS3 s3Client, string bucketName, string key, bool expectAbsent)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(bucketName, key);

            return expectAbsent
                ? new ObjectServiceResult
                {
                    StatusCode = 409,
                    ErrorMessage = $"Object '{key}' already exists in bucket '{bucketName}'."
                }
                : new ObjectServiceResult { IsSuccess = true };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return expectAbsent
                ? new ObjectServiceResult { IsSuccess = true }
                : new ObjectServiceResult
                {
                    StatusCode = 404,
                    ErrorMessage = $"Object '{key}' does not exist in bucket '{bucketName}'."
                };
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return new ObjectServiceResult
            {
                StatusCode = 404,
                ErrorMessage = $"Bucket '{bucketName}' does not exist."
            };
        }
        catch (AmazonS3Exception ex)
        {
            return new ObjectServiceResult
            {
                StatusCode = (int)ex.StatusCode,
                ErrorMessage = ex.Message
            };
        }
    }
}
