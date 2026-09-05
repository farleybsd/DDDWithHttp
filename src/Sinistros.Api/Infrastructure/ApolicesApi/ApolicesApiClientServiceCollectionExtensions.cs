using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Infrastructure.ApolicesApi;

public static class ApolicesApiClientServiceCollectionExtensions
{
    /// <summary>
    /// Registra o cliente HTTP tipado usado para consultar a API de Apólices, já com o pipeline
    /// de resiliência (retry, circuit breaker e timeout) configurado a partir da seção
    /// <see cref="ApolicesApiOptions.SectionName"/> de <paramref name="configuration"/>.
    /// </summary>
    public static IServiceCollection AddApolicesApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApolicesApiOptions>(configuration.GetSection(ApolicesApiOptions.SectionName));

        services
            .AddHttpClient<IApolicesApiClient, ApolicesApiClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ApolicesApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddResilienceHandler("apolices-pipeline", (pipelineBuilder, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<ApolicesApiOptions>>().Value;

                // Ordem: Retry (mais externo) -> Circuit Breaker -> Timeout por tentativa (mais interno).
                // Só GET é feito aqui (consulta), então repetir a chamada é seguro.
                pipelineBuilder
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = options.Retry.MaxRetryAttempts,
                        Delay = options.Retry.BaseDelay
                    })
                    .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = options.CircuitBreaker.FailureRatio,
                        SamplingDuration = options.CircuitBreaker.SamplingDuration,
                        MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                        BreakDuration = options.CircuitBreaker.BreakDuration
                    })
                    .AddTimeout(options.Timeout);
            });

        return services;
    }
}
