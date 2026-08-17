using Microsoft.Extensions.DependencyInjection;

namespace CoverGo.Samples.Infrastructure.GatewayV2Client;

public static class GatewayV2ClientRegistration
{
    /// <summary>
    /// Registers the generated V2 supergraph client against <paramref name="gatewayUrl"/>.
    /// </summary>
    public static IServiceCollection AddCoverGoGatewayV2(this IServiceCollection services, Uri gatewayUrl)
    {
        services
            .AddGatewayV2Client()
            .ConfigureHttpClient(client => client.BaseAddress = gatewayUrl);

        return services;
    }
}
