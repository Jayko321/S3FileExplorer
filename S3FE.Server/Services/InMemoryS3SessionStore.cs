namespace S3FE.Server.Services;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Amazon.S3;

public class InMemoryS3SessionStore : IS3SessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, S3Session> _sessions = new();

    public S3Session CreateSession(string endpoint, IAmazonS3 client)
    {
        var token = CreateToken();
        var session = new S3Session
        {
            Token = token,
            Endpoint = endpoint,
            Client = client,
            ExpiresAtUtc = DateTime.UtcNow.Add(SessionLifetime)
        };

        _sessions[token] = session;
        return session;
    }

    public bool TryGetSession(string token, out S3Session session)
    {
        if (_sessions.TryGetValue(token, out session!) && session.ExpiresAtUtc > DateTime.UtcNow)
            return true;

        if (!string.IsNullOrWhiteSpace(token))
            RemoveSession(token);

        session = default!;
        return false;
    }

    public void RemoveSession(string token)
    {
        if (_sessions.TryRemove(token, out var session) && session.Client is IDisposable disposable)
            disposable.Dispose();
    }

    private static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
