namespace S3FE.Server.Controllers;

using Amazon.S3;
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
            var buckets = (response.Buckets ?? [])
                .Select(bucket => new BucketDTO
                {
                    Name = bucket.BucketName
                })
                .ToList();

            return Ok(buckets);
        }
        catch (AmazonS3Exception ex)
        {
            return S3ErrorResponses.FromException(this, ex);
        }
    }

    [HttpPut("{bucketName}")]
    public async Task<IActionResult> CreateBucketAsync([FromRoute] string bucketName)
    {
        try
        {
            var s3Client = _s3ClientProvider.GetClient();
            await s3Client.PutBucketAsync(bucketName);
            return Ok();
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
