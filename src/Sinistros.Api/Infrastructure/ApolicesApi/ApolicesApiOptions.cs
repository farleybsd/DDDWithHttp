namespace Sinistros.Api.Infrastructure.ApolicesApi;

public sealed class ApolicesApiOptions
{
    public const string SectionName = "ApolicesApi";

    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    public sealed class RetryOptions
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    }

    public sealed class CircuitBreakerOptions
    {
        public double FailureRatio { get; set; } = 0.5;
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);
        public int MinimumThroughput { get; set; } = 4;
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(15);
    }
}
