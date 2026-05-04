namespace S3FE.Server.Extensions;

using Microsoft.AspNetCore.Authentication;
using S3FE.Server.Authentication;
using S3FE.Server.Services;

public static class S3ServiceExtensions
{
    public static IServiceCollection AddS3SessionAuthentication(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IS3SessionStore, InMemoryS3SessionStore>();
        services.AddScoped<ICurrentS3ClientProvider, CurrentS3ClientProvider>();

        services
            .AddAuthentication(S3SessionAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, S3SessionAuthenticationHandler>(
                S3SessionAuthenticationHandler.SchemeName,
                options => { });

        services.AddAuthorization();

        return services;
    }
}
