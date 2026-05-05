namespace S3FE.Server.Services;

using Amazon.S3;

public class S3Session
{
    public string Token { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;

    public IAmazonS3 Client { get; init; } = default!;

    public DateTime ExpiresAtUtc { get; init; }
}
