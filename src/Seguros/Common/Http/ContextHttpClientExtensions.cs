using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Seguros.Common.Settings;

namespace Seguros.Common.Http;

public static class ContextHttpClientExtensions
{
    /// <summary>
    /// Registra o cliente HTTP tipado que um contexto usa para falar com outro contexto. O
    /// endereço vem de <see cref="ServicesSettings"/> e o pipeline de resiliência de
    /// <see cref="HttpResilienceSettings"/>.
    /// </summary>
    /// <remarks>
    /// Ordem do pipeline (mais externo → mais interno): Limite de concorrência → Retry → Circuit
    /// Breaker → Timeout por tentativa. Use apenas para chamadas idempotentes (consultas), já que o
    /// retry as repete.
    /// </remarks>
    public static IHttpClientBuilder AddContextHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        string nomeDoPipeline,
        Func<ServicesSettings, string> enderecoDoContexto)
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<ServicesSettings>>().Value;
            client.BaseAddress = ServicesSettings.ComBarraFinal(enderecoDoContexto(settings));
        });

        builder.AddResilienceHandler(nomeDoPipeline, (pipeline, context) =>
        {
            var opcoes = context.ServiceProvider.GetRequiredService<IOptions<HttpResilienceSettings>>().Value;

            // Mais externo: o limite vale para a chamada inteira (incluindo retentativas), e uma
            // chamada recusada por ele não é retentada.
            pipeline.AddConcurrencyLimiter(
                permitLimit: opcoes.ConcurrencyLimiter.PermitLimit,
                queueLimit: opcoes.ConcurrencyLimiter.QueueLimit);

            // O Polly exige ao menos 1 retentativa; com 0 a estratégia simplesmente não entra.
            if (opcoes.Retry.MaxRetryAttempts > 0)
            {
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = opcoes.Retry.MaxRetryAttempts,
                    Delay = opcoes.Retry.BaseDelay
                });
            }

            pipeline
                .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = opcoes.CircuitBreaker.FailureRatio,
                    SamplingDuration = opcoes.CircuitBreaker.SamplingDuration,
                    MinimumThroughput = opcoes.CircuitBreaker.MinimumThroughput,
                    BreakDuration = opcoes.CircuitBreaker.BreakDuration
                })
                .AddTimeout(opcoes.Timeout);
        });

        return builder;
    }
}
