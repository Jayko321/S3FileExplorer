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
[Route("api/buckets")]
public class BucketsController(ICurrentS3ClientProvider s3ClientProvider) : ControllerBase
{
    private readonly ICurrentS3ClientProvider _s3ClientProvider = s3ClientProvider;

    [HttpGet]
    public async Task<IActionResult> GetBucketsAsync()
    {
        try
        {
            var s3Client = _s3ClientProvider.GetClient();
            var response = await s3Client.ListBucketsAsync();
            var buckets = new List<BucketDTO>();

            foreach (var bucket in response.Buckets ?? [])
            {
                var isVersioned = false;

                try
                {
                    var versioningResponse = await s3Client.GetBucketVersioningAsync(bucket.BucketName);
                    isVersioned = versioningResponse is not null
                        && versioningResponse.VersioningConfig is not null
                        && versioningResponse.VersioningConfig.Status == VersionStatus.Enabled;
                }
                catch
                {
                    // Best-effort: treat versioning as unknown/false
                }

                buckets.Add(new BucketDTO
                {
                    Name = bucket.BucketName,
                    IsVersioned = isVersioned
                });
            }

            return Ok(buckets);
        }
        catch (AmazonS3Exception ex)
        {
            return S3ErrorResponses.FromException(this, ex);
        }
    }

    [HttpPut("{bucketName}")]
    public async Task<IActionResult> CreateBucketAsync([FromRoute] string bucketName, [FromQuery] bool? versioned = null)
    {
        try
        {
            var s3Client = _s3ClientProvider.GetClient();
            await s3Client.PutBucketAsync(bucketName);

            if (versioned is true)
            {

                await s3Client.PutBucketVersioningAsync(new PutBucketVersioningRequest
                {
                    BucketName = bucketName,
                    VersioningConfig = new S3BucketVersioningConfig
                    {
                        Status = VersionStatus.Enabled
                    }
                });
            }
            return Ok(new BucketDTO
            {
                Name = bucketName,
                IsVersioned = versioned is true
            });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou" || ex.ErrorCode == "BucketAlreadyExists")
        {
            return S3ErrorResponses.BucketAlreadyExists(this, bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            return S3ErrorResponses.FromException(this, ex);
        }
    }

    [HttpDelete("{bucketName}")]
    public async Task<IActionResult> DeleteBucketAsync([FromRoute] string bucketName)
    {
        try
        {
            var s3Client = _s3ClientProvider.GetClient();
            await s3Client.DeleteBucketAsync(bucketName);
            return Ok();
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return S3ErrorResponses.BucketDoesNotExist(this, bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketNotEmpty")
        {
            return S3ErrorResponses.BucketIsNotEmpty(this, bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            return S3ErrorResponses.FromException(this, ex);
        }
    }
}
