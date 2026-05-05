namespace S3FE.Server.Helpers;

using Amazon.S3;
using Microsoft.AspNetCore.Mvc;

public static class S3ErrorResponses
{
    public static IActionResult FromException(ControllerBase controller, AmazonS3Exception exception)
    {
        return controller.StatusCode((int)exception.StatusCode, exception.Message);
    }

    public static IActionResult BucketAlreadyExists(ControllerBase controller, string bucketName)
    {
        return controller.Conflict($"Bucket '{bucketName}' already exists.");
    }

    public static IActionResult BucketDoesNotExist(ControllerBase controller, string bucketName)
    {
        return controller.NotFound($"Bucket '{bucketName}' does not exist.");
    }

    public static IActionResult BucketIsNotEmpty(ControllerBase controller, string bucketName)
    {
        return controller.Conflict($"Bucket '{bucketName}' is not empty.");
    }

    public static IActionResult ObjectDoesNotExist(ControllerBase controller, string bucketName, string key)
    {
        return controller.NotFound($"Object '{key}' does not exist in bucket '{bucketName}'.");
    }
}
