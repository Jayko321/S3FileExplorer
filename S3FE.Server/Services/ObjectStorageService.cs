namespace S3FE.Server.Services;

using Amazon.S3;
using Amazon.S3.Model;
using S3FE.Shared.DTOs;
using System.IO;
using System.Net;

public sealed class ObjectStorageService(
    ICurrentS3ClientProvider s3ClientProvider) : IObjectStorageService
{
    private readonly ICurrentS3ClientProvider _s3ClientProvider = s3ClientProvider;

    public async Task<ListObjectsResult> ListObjectsAsync(string bucketName, string? prefix = null)
    {
        var s3Client = _s3ClientProvider.GetClient();

        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix,
                Delimiter = "/"
            };

            var response = await s3Client.ListObjectsV2Async(request);

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

            var existingKeys = new HashSet<string>(listing.Files.Select(f => f.Key));

            var versionsPerKey = new Dictionary<string, List<S3ObjectVersion>>(StringComparer.Ordinal);
            var listVersionsReq = new ListVersionsRequest
            {
                BucketName = bucketName,
                Prefix = prefix
            };

            ListVersionsResponse listVersionsRes;
            do
            {
                listVersionsRes = await s3Client.ListVersionsAsync(listVersionsReq);

                foreach (var version in listVersionsRes.Versions)
                {
                    var relativeKey = string.IsNullOrEmpty(prefix)
                        ? version.Key
                        : version.Key[prefix!.Length..];

                    if (relativeKey.Contains('/'))
                        continue;

                    if (!versionsPerKey.TryGetValue(version.Key, out var list))
                    {
                        list = [];
                        versionsPerKey[version.Key] = list;
                    }
                    list.Add(version);
                }

                listVersionsReq.KeyMarker = listVersionsRes.NextKeyMarker;
                listVersionsReq.VersionIdMarker = listVersionsRes.NextVersionIdMarker;
            } while (listVersionsRes.IsTruncated == true);

            foreach (var file in listing.Files)
            {
                if (versionsPerKey.TryGetValue(file.Key, out var versions))
                {
                    file.VersionIds = [.. versions.Select(v => v.VersionId)];
                    file.VersionId = file.VersionIds.FirstOrDefault("");
                }
            }

            foreach (var (key, versions) in versionsPerKey)
            {
                if (existingKeys.Contains(key))
                    continue;

                var latest = versions.OrderByDescending(v => v.LastModified).First();

                if (latest.IsDeleteMarker != true)
                    continue;

                var latestRealVersion = versions
                    .Where(v => v.IsDeleteMarker != true)
                    .OrderByDescending(v => v.LastModified)
                    .FirstOrDefault();

                listing.Files.Add(new S3ObjectDTO
                {
                    Key = key,
                    Size = latestRealVersion?.Size ?? 0,
                    LastModified = latest.LastModified,
                    ETag = latestRealVersion?.ETag ?? string.Empty,
                    VersionId = latest.VersionId,
                    IsDeleteMarker = true,
                    IsLatest = true,
                    VersionIds = [.. versions.Select(v => v.VersionId)]
                });
                existingKeys.Add(key);
            }

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
        var s3Client = _s3ClientProvider.GetClient();

        try
        {
            var sdkVersions = await FetchObjectVersionsAsync(s3Client, bucketName, key);

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
        var s3Client = _s3ClientProvider.GetClient();

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

            await s3Client.PutObjectAsync(request);

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
        var s3Client = _s3ClientProvider.GetClient();
        var sourceCheck = await CheckSourceExistsAsync(s3Client, bucketName, sourceKey);

        if (!sourceCheck.IsSuccess)
            return CopyResult.Failure(sourceCheck.StatusCode, sourceCheck.ErrorMessage!);

        var destCheck = await CheckDestinationAbsentAsync(s3Client, bucketName, destinationKey);

        if (!destCheck.IsSuccess)
            return CopyResult.Failure(destCheck.StatusCode, destCheck.ErrorMessage!);

        if (versioningBehavior == "all")
            return await CopyAllVersionsAsync(s3Client, bucketName, sourceKey, destinationKey);

        return await CopyLatestVersionAsync(s3Client, bucketName, sourceKey, destinationKey);
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
        var s3Client = _s3ClientProvider.GetClient();

        if (versioning == "all")
            return await DeleteAllVersionsAsync(bucketName, key);

        var sourceCheck = await CheckSourceExistsAsync(s3Client, bucketName, key);

        if (!sourceCheck.IsSuccess)
            return DeleteResult.Failure(sourceCheck.StatusCode, sourceCheck.ErrorMessage!);

        try
        {
            await s3Client.DeleteObjectAsync(new DeleteObjectRequest
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
        var s3Client = _s3ClientProvider.GetClient();

        try
        {
            using var response = await s3Client.GetObjectAsync(bucketName, key);
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
        var s3Client = _s3ClientProvider.GetClient();

        try
        {
            var versions = await ListAllVersionsIncludingMarkersAsync(s3Client, bucketName, sourceKey);

            if (versions.Count == 0)
                return DeleteResult.Success();

            var deleteRequest = new DeleteObjectsRequest
            {
                BucketName = bucketName,
                Objects = [.. versions.Select(v => new KeyVersion { Key = v.Key, VersionId = v.VersionId })]
            };

            await s3Client.DeleteObjectsAsync(deleteRequest);

            return DeleteResult.Success();
        }
        catch (AmazonS3Exception ex)
        {
            return DeleteResult.Failure((int)ex.StatusCode, ex.Message);
        }
    }

    private static async Task<List<S3ObjectVersion>> FetchObjectVersionsAsync(IAmazonS3 s3Client, string bucketName, string key)
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
                .Where(v => v.Key == key && v.IsDeleteMarker != true));
            request.KeyMarker = response.NextKeyMarker;
            request.VersionIdMarker = response.NextVersionIdMarker;
        } while (response.IsTruncated == true);

        return versions;
    }

    private static async Task<List<S3ObjectVersion>> ListAllVersionsIncludingMarkersAsync(IAmazonS3 s3Client, string bucketName, string key)
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
            versions.AddRange(response.Versions.Where(v => v.Key == key));
            request.KeyMarker = response.NextKeyMarker;
            request.VersionIdMarker = response.NextVersionIdMarker;
        } while (response.IsTruncated == true);

        return versions;
    }

    private static async Task<ObjectServiceResult> CheckSourceExistsAsync(IAmazonS3 s3Client, string bucketName, string key)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(bucketName, key);
            return new ObjectServiceResult { IsSuccess = true };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new ObjectServiceResult
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

    private static async Task<ObjectServiceResult> CheckDestinationAbsentAsync(IAmazonS3 s3Client, string bucketName, string key)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(bucketName, key);
            return new ObjectServiceResult
            {
                StatusCode = 409,
                ErrorMessage = $"Object '{key}' already exists in bucket '{bucketName}'."
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new ObjectServiceResult { IsSuccess = true };
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
