namespace S3FE.Server.Controllers;

using Amazon.S3;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/buckets")]
public class BucketsController(IAmazonS3 s3Client) : ControllerBase
{
    private readonly IAmazonS3 _s3Client = s3Client;

    [HttpGet]
    public async Task<IActionResult> GetBucketsAsync()
    {
        try
        {
            var response = await _s3Client.ListBucketsAsync();
            return Ok(response.Buckets.Select(b => b.BucketName));
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
    }

    [HttpPut("{bucketName}")]
    public async Task<IActionResult> CreateBucketAsync([FromRoute] string bucketName)
    {
        try
        {
            await _s3Client.PutBucketAsync(bucketName);
            return Ok();
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou" || ex.ErrorCode == "BucketAlreadyExists")
        {
            return Conflict($"Bucket '{bucketName}' already exists.");
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
    }

    [HttpDelete("{bucketName}")]
    public async Task<IActionResult> DeleteBucketAsync([FromRoute] string bucketName)
    {
        try
        {
            await _s3Client.DeleteBucketAsync(bucketName);
            return Ok();
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return NotFound($"Bucket '{bucketName}' does not exist.");
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketNotEmpty")
        {
            return Conflict($"Bucket '{bucketName}' is not empty.");
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
    }
}
