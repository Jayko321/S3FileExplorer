namespace S3FE.Server.Authentication;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using S3FE.Server.Services;

public class S3SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IS3SessionStore sessionStore)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "S3Session";

    private readonly IS3SessionStore _sessionStore = sessionStore;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        const string bearerPrefix = "Bearer ";
        if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Invalid authorization header."));

        var token = authorizationHeader[bearerPrefix.Length..].Trim();

        if (!_sessionStore.TryGetSession(token, out var session))
            return Task.FromResult(AuthenticateResult.Fail("Invalid or expired session token."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, token),
            new Claim("s3_endpoint", session.Endpoint)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
