using Microsoft.Extensions.DependencyInjection;

namespace CoverGo.Samples.Infrastructure.GatewayV1Client;

public static class GatewayV1ClientRegistration
{
    /// <summary>
    /// Registers the generated V1 gateway client against <paramref name="gatewayUrl"/>, with
    /// <typeparamref name="THandler"/> attaching authentication to every operation.
    /// </summary>
    public static IServiceCollection AddCoverGoGatewayV1<THandler>(
        this IServiceCollection services, Uri gatewayUrl)
        where THandler : DelegatingHandler
    {
        services
            .AddGatewayV1Client()
            .ConfigureHttpClient(
                client => client.BaseAddress = gatewayUrl,
                builder => builder.AddHttpMessageHandler<THandler>());

        return services;
    }
}
