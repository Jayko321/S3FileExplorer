namespace S3FE.Server.Controllers;

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using S3FE.Server.Helpers;
using S3FE.Server.Services;
using S3FE.Shared.DTOs;

[Authorize]
[ApiController]
[Route("api/buckets/{bucketName}/objects")]
public class ObjectsController(ICurrentS3ClientProvider s3ClientProvider) : ControllerBase
{
    private readonly ICurrentS3ClientProvider _s3ClientProvider = s3ClientProvider;

    [HttpGet]
    public async Task<IActionResult> ListObjectsAsync(
        [FromRoute] string bucketName,
        [FromQuery] string? prefix = null)
    {
        try
        {
            var s3Client = _s3ClientProvider.GetClient();
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix,
                Delimiter = "/"
            };

            var response = await s3Client.ListObjectsV2Async(request);

            var result = new ObjectListingDTO
            {
                Folders = response.CommonPrefixes ?? [],
                Files = (response.S3Objects ?? [])
                    .Select(s3Object => new S3ObjectDTO
                    {
                        Key = s3Object.Key,
                        Size = s3Object.Size,
                        LastModified = s3Object.LastModified,
                        ETag = s3Object.ETag
                    })
                    .ToList()
            };

            return Ok(result);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return S3ErrorResponses.BucketDoesNotExist(this, bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            return S3ErrorResponses.FromException(this, ex);
        }
    }

    // POST /api/buckets/{bucketName}/objects?prefix=folder/
    [HttpPost]
    [RequestSizeLimit(5L * 1024 * 1024 * 1024)] // 5 GB
    public async Task<IActionResult> UploadObjectAsync(
        [FromRoute] string bucketName,
        [FromQuery] string? prefix,
        IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("File is empty.");

        try
        {
            var s3Client = _s3ClientProvider.GetClient();
            var key = string.IsNullOrEmpty(prefix)
                ? file.FileName
                : $"{prefix.TrimEnd('/')}/{file.FileName}";

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType,
                UseChunkEncoding = false
            };

            await s3Client.PutObjectAsync(request);

            return Ok(new UploadObjectResponseDTO
            {
                Key = key
            });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return S3ErrorResponses.BucketDoesNotExist(this, bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            return S3ErrorResponses.FromException(this, ex);
        }
    }

    // DELETE /api/buckets/{bucketName}/objects/folder/file.txt
    [HttpDelete("{**key}")]
    public async Task<IActionResult> DeleteObjectAsync(
        [FromRoute] string bucketName,
        [FromRoute] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest("Object key is required.");

        try
        {
            var s3Client = _s3ClientProvider.GetClient();
            await s3Client.GetObjectMetadataAsync(bucketName, key);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return S3ErrorResponses.ObjectDoesNotExist(this, bucketName, key);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return S3ErrorResponses.BucketDoesNotExist(this, bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            return S3ErrorResponses.FromException(this, ex);
        }

        try
        {
            var s3Client = _s3ClientProvider.GetClient();
            await s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            });
            return NoContent();
        }
        catch (AmazonS3Exception ex)
        {
            return S3ErrorResponses.FromException(this, ex);
        }
    }
}
