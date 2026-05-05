namespace S3FE.Server.Services;

using Amazon.S3;

public interface ICurrentS3ClientProvider
{
    IAmazonS3 GetClient();
}
