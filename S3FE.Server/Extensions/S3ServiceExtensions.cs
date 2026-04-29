namespace S3FE.Server.Extensions;

using Amazon.S3;

public static class S3ServiceExtensions
{
    public static IServiceCollection AddS3Client(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            configuration["Minio:AccessKey"],
            configuration["Minio:SecretKey"],
            new AmazonS3Config
            {
                ServiceURL = configuration["Minio:Endpoint"],
                ForcePathStyle = true
            }));

        return services;
    }
}
