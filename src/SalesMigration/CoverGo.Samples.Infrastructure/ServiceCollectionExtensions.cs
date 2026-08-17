using CoverGo.Samples.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoverGo.Samples.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates <see cref="CoverGoOptions"/>, and registers the bearer-token
    /// handler the gateway clients authenticate with.
    /// </summary>
    /// <remarks>
    /// Validation runs at startup so a missing setting stops the sample immediately and names
    /// the setting, rather than surfacing later as an authentication failure.
    /// </remarks>
    public static IServiceCollection AddCoverGoSamples(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<CoverGoOptions>()
            .Bind(configuration.GetSection(CoverGoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<CoverGoOptions>>().Value);

        // Named client for the token endpoint itself. It must not carry the token handler,
        // or acquiring a token would require a token.
        services.AddHttpClient(nameof(CoverGoTokenHandler));

        // Singleton so the token cache is shared by every request the run makes.
        services.AddSingleton<CoverGoTokenHandler>();

        return services;
    }
}
