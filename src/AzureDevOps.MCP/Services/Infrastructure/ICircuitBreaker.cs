namespace AzureDevOps.MCP.Services.Infrastructure;

public interface ICircuitBreaker
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    CircuitBreakerState State { get; }
    void Reset();
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerException : Exception
{
    public CircuitBreakerState State { get; }

    public CircuitBreakerException(CircuitBreakerState state, string message) : base(message)
    {
        State = state;
    }

    public CircuitBreakerException(CircuitBreakerState state, string message, Exception innerException) : base(message, innerException)
    {
        State = state;
    }
}

public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}

public class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public double BackoffMultiplier { get; set; } = 2.0;
    public Func<Exception, bool> ShouldRetry { get; set; } = DefaultShouldRetry;

    private static bool DefaultShouldRetry(Exception exception)
    {
        return exception switch
        {
            TaskCanceledException => false,
            OperationCanceledException => false,
            ArgumentException => false,
            InvalidOperationException => false,
            HttpRequestException => true,
            TimeoutException => true,
            _ => true
        };
    }
}