namespace S3FE.Server.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using S3FE.Server.Services;
using S3FE.Shared.DTOs;

[Authorize]
[ApiController]
[Route("api/buckets/{bucketName}/objects")]
public class ObjectsController(IObjectStorageService objectStorageService) : ControllerBase
{
    private readonly IObjectStorageService _objectStorageService = objectStorageService;

    [HttpGet]
    public async Task<IActionResult> ListObjectsAsync(
        [FromRoute] string bucketName,
        [FromQuery] string? prefix = null)
    {
        var result = await _objectStorageService.ListObjectsAsync(bucketName, prefix);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.ErrorMessage);

        return Ok(result.Listing);
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

        var result = await _objectStorageService.UploadObjectAsync(
            bucketName, prefix, file.OpenReadStream(), file.FileName, file.ContentType);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.ErrorMessage);

        return Ok(result.Response);
    }

    // POST /api/buckets/{bucketName}/objects/copy/{**sourceKey}?destinationKey=...&versioning=latest|all
    [HttpPost("copy/{**sourceKey}")]
    public async Task<IActionResult> CopyObjectAsync(
        [FromRoute] string bucketName,
        [FromRoute] string sourceKey,
        [FromQuery] string destinationKey,
        [FromQuery] string? versioning = null)
    {
        if (string.IsNullOrWhiteSpace(destinationKey))
            return BadRequest("Destination key is required.");

        if (versioning is not null and not "latest" and not "all")
            return BadRequest("Versioning must be 'latest' or 'all'.");

        var result = await _objectStorageService.CopyObjectAsync(bucketName, sourceKey, destinationKey, versioning ?? "latest");

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.ErrorMessage);

        return Ok(new UploadObjectResponseDTO { Key = result.DestinationKey! });
    }

    // POST /api/buckets/{bucketName}/objects/rename/{**sourceKey}?destinationKey=...&versioning=latest|all
    [HttpPost("rename/{**sourceKey}")]
    public async Task<IActionResult> RenameObjectAsync(
        [FromRoute] string bucketName,
        [FromRoute] string sourceKey,
        [FromQuery] string destinationKey,
        [FromQuery] string? versioning = null)
    {
        if (string.IsNullOrWhiteSpace(destinationKey))
            return BadRequest("Destination key is required.");

        if (versioning is not null and not "latest" and not "all")
            return BadRequest("Versioning must be 'latest' or 'all'.");

        var result = await _objectStorageService.RenameObjectAsync(bucketName, sourceKey, destinationKey, versioning ?? "latest");

        if (result.IsPartialFailure)
            return StatusCode(result.StatusCode, result.ErrorMessage);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.ErrorMessage);

        return Ok(new UploadObjectResponseDTO { Key = result.NewKey! });
    }

    // DELETE /api/buckets/{bucketName}/objects/folder/file.txt
    [HttpDelete("{**key}")]
    public async Task<IActionResult> DeleteObjectAsync(
        [FromRoute] string bucketName,
        [FromRoute] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest("Object key is required.");

        var result = await _objectStorageService.DeleteObjectAsync(bucketName, key);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.ErrorMessage);

        return NoContent();
    }
}
