namespace S3FE.Server.Services;

using Amazon.S3;

public interface IS3SessionStore
{
    S3Session CreateSession(string endpoint, IAmazonS3 client);

    bool TryGetSession(string token, out S3Session session);

    void RemoveSession(string token);
}
