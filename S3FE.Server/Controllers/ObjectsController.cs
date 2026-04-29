namespace S3FE.Server.Controllers;

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/buckets/{bucketName}/objects")]
public class ObjectsController(IAmazonS3 s3Client) : ControllerBase
{
    private readonly IAmazonS3 _s3Client = s3Client;

    [HttpGet]
    public async Task<IActionResult> ListObjectsAsync(
        [FromRoute] string bucketName,
        [FromQuery] string? prefix = null)
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix,
                Delimiter = "/"
            };

            var response = await _s3Client.ListObjectsV2Async(request);

            var result = new
            {
                Folders = response.CommonPrefixes,
                Files = response.S3Objects.Select(o => new
                {
                    o.Key,
                    o.Size,
                    o.LastModified,
                    o.ETag
                })
            };

            return Ok(result);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return NotFound($"Bucket '{bucketName}' does not exist.");
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
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

            await _s3Client.PutObjectAsync(request);

            return Ok(new { key });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return NotFound($"Bucket '{bucketName}' does not exist.");
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
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
            await _s3Client.GetObjectMetadataAsync(bucketName, key);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound($"Object '{key}' does not exist in bucket '{bucketName}'.");
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return NotFound($"Bucket '{bucketName}' does not exist.");
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }

        try
        {
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            });
            return NoContent();
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
    }
}
