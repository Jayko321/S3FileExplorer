namespace S3FE.Server.Services;

using Amazon.S3;
using System.Security.Claims;

public class CurrentS3ClientProvider(IHttpContextAccessor httpContextAccessor, IS3SessionStore sessionStore) : ICurrentS3ClientProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IS3SessionStore _sessionStore = sessionStore;

    public IAmazonS3 GetClient()
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(token) || !_sessionStore.TryGetSession(token, out var session))
            throw new UnauthorizedAccessException("No valid S3 session was found for the current request.");

        return session.Client;
    }
}
