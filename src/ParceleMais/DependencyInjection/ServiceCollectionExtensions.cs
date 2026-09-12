using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Polly;
using ParceleMais.Authentication;
using ParceleMais.Configuration;
using ParceleMais.Http;
using ParceleMais.Idempotency;
using ParceleMais.Orders;
using ParceleMais.Resilience;
using ParceleMais.Simulations;

namespace ParceleMais.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddParceleMais(this IServiceCollection services, Action<ParceleMaisOptions> configureOptions)
    {
        services.AddOptions<ParceleMaisOptions>().Configure(configureOptions).ValidateDataAnnotations();
        services.AddSingleton<IValidateOptions<ParceleMaisOptions>, ParceleMaisOptionsValidator>();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ParceleMaisOptions>>().Value);
        services.AddSingleton(sp => ResiliencePipelineFactory.Create(sp.GetRequiredService<ParceleMaisOptions>().Resilience));

        services.AddSingleton<IAccessTokenProvider, AccessTokenProvider>();
        services.AddTransient<AuthenticationDelegatingHandler>();
        services.AddTransient<IdempotencyKeyDelegatingHandler>();
        services.AddTransient<ResilienceDelegatingHandler>();

        services.AddHttpClient<ITokenApiClient, TokenApiClient>(HttpClientNames.Auth, (sp, client) =>
        {
            client.BaseAddress = sp.GetRequiredService<ParceleMaisOptions>().ResolveBaseUrl();
        });

        var apiClientBuilder = services.AddHttpClient(HttpClientNames.Api, (sp, client) =>
            {
                client.BaseAddress = sp.GetRequiredService<ParceleMaisOptions>().ResolveBaseUrl();
            })
            .AddHttpMessageHandler<IdempotencyKeyDelegatingHandler>()
            .AddHttpMessageHandler<ResilienceDelegatingHandler>()
            .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

        services.AddHttpClient<IOrdersClient, OrdersClient>(HttpClientNames.Api);
        services.AddHttpClient<ISimulationsClient, SimulationsClient>(HttpClientNames.Api);
        services.AddSingleton<IParceleMaisClient, ParceleMaisClient>();

        return apiClientBuilder;
    }
}
