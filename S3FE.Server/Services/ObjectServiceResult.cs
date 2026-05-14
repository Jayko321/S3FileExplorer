using System.IO;

namespace S3FE.Server.Services;

using S3FE.Shared.DTOs;

public class ObjectServiceResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; } = 200;
    public string? ErrorMessage { get; set; }
}

public sealed class CopyResult : ObjectServiceResult
{
    public string? DestinationKey { get; private set; }

    public static CopyResult Success(string key) =>
        new() { IsSuccess = true, DestinationKey = key };

    public static CopyResult Failure(int statusCode, string message) =>
        new() { StatusCode = statusCode, ErrorMessage = message };
}

public sealed class DeleteResult : ObjectServiceResult
{
    public static DeleteResult Success() =>
        new() { IsSuccess = true, StatusCode = 204 };

    public static DeleteResult Failure(int statusCode, string message) =>
        new() { StatusCode = statusCode, ErrorMessage = message };
}

public sealed class ListObjectsResult : ObjectServiceResult
{
    public ObjectListingDTO? Listing { get; private set; }

    public static ListObjectsResult Success(ObjectListingDTO listing) =>
        new() { IsSuccess = true, Listing = listing };

    public static ListObjectsResult Failure(int statusCode, string message) =>
        new() { StatusCode = statusCode, ErrorMessage = message };
}

public sealed class UploadObjectResult : ObjectServiceResult
{
    public UploadObjectResponseDTO? Response { get; private set; }

    public static UploadObjectResult Success(UploadObjectResponseDTO response) =>
        new() { IsSuccess = true, Response = response };

    public static UploadObjectResult Failure(int statusCode, string message) =>
        new() { StatusCode = statusCode, ErrorMessage = message };
}

public sealed class RenameResult : ObjectServiceResult
{
    public string? NewKey { get; private set; }
    public bool IsPartialFailure { get; private set; }

    public static RenameResult Success(string key) =>
        new() { IsSuccess = true, NewKey = key };

    public static RenameResult Failure(int statusCode, string message) =>
        new() { StatusCode = statusCode, ErrorMessage = message };

    public static RenameResult PartialFailure(string key, string message) =>
        new()
        {
            IsSuccess = true,
            IsPartialFailure = true,
            NewKey = key,
            StatusCode = 202,
            ErrorMessage = message
        };
}

public sealed class ObjectVersionsResult : ObjectServiceResult
{
    public List<S3ObjectDTO> Versions { get; private set; } = [];

    public static ObjectVersionsResult Success(List<S3ObjectDTO> versions) =>
        new() { IsSuccess = true, Versions = versions };

    public static ObjectVersionsResult Failure(int statusCode, string message) =>
        new() { StatusCode = statusCode, ErrorMessage = message };
}

public sealed class DownloadResult : ObjectServiceResult
{
    public Stream? ContentStream { get; init; }
    public string? ContentType { get; init; }

    public static DownloadResult Success(Stream stream, string contentType) =>
        new() { IsSuccess = true, ContentStream = stream, ContentType = contentType };

    public static DownloadResult Failure(int statusCode, string message) =>
        new() { StatusCode = statusCode, ErrorMessage = message };
}
