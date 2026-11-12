namespace Seguros.Common.Settings;

/// <summary>
/// Parâmetros do pipeline de resiliência aplicado às chamadas HTTP entre contextos.
/// </summary>
public sealed class HttpResilienceSettings
{
    public const string SectionName = "HttpResilience";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
    public ConcurrencyLimiterSettings ConcurrencyLimiter { get; set; } = new();
    public RetrySettings Retry { get; set; } = new();
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Limita quantas chamadas a um contexto podem estar em andamento ao mesmo tempo. Como os
    /// contextos rodam no mesmo processo, uma rajada de chamadas internas disputaria threads e
    /// conexões com as requisições dos clientes; acima do limite (e com a fila cheia) a chamada é
    /// recusada na hora e vira 503, em vez de derrubar a aplicação.
    /// </summary>
    public sealed class ConcurrencyLimiterSettings
    {
        /// <summary>Chamadas simultâneas permitidas.</summary>
        public int PermitLimit { get; set; } = 50;

        /// <summary>Chamadas que podem aguardar na fila quando o limite é atingido. 0 = recusa na hora.</summary>
        public int QueueLimit { get; set; } = 10;
    }

    public sealed class RetrySettings
    {
        /// <summary>
        /// Retentativas após a primeira chamada. Nas chamadas internas (mesmo processo) a falha
        /// mais provável é a própria aplicação estar sobrecarregada, e retentar multiplicaria a
        /// carga — por isso o padrão é 1. Use 0 para desligar o retry.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 1;
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    }

    public sealed class CircuitBreakerSettings
    {
        public double FailureRatio { get; set; } = 0.5;
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);
        public int MinimumThroughput { get; set; } = 4;
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(15);
    }
}
