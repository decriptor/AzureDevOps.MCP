using AzureDevOps.MCP.Common;
using System.Collections.Concurrent;

namespace AzureDevOps.MCP.Services.Infrastructure;

public class CircuitBreaker : ICircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly object _lock = new();
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private DateTime _openedTime;

    public CircuitBreaker(
        int failureThreshold = 5,
        TimeSpan? openDuration = null,
        ILogger<CircuitBreaker>? logger = null)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromMinutes(1);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CircuitBreaker>.Instance;
    }

    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open && DateTime.UtcNow - _openedTime >= _openDuration)
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _logger.LogInformation("Circuit breaker moved to half-open state");
                }
                return _state;
            }
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (State == CircuitBreakerState.Open)
        {
            throw new CircuitBreakerException(CircuitBreakerState.Open, "Circuit breaker is open");
        }

        try
        {
            var result = await operation(cancellationToken);
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _lastFailureTime = default;
            _openedTime = default;
            _logger.LogInformation("Circuit breaker reset to closed state");
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _logger.LogInformation("Circuit breaker moved to closed state after successful operation");
            }
        }
    }

    private void OnFailure(Exception exception)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Open;
                _openedTime = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker opened due to failure in half-open state: {Exception}", exception.Message);
            }
            else if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _openedTime = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker opened due to {FailureCount} failures. Last failure: {Exception}", 
                    _failureCount, exception.Message);
            }
        }
    }
}

public class RetryPolicy : IRetryPolicy
{
    private readonly RetryOptions _options;
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(RetryOptions? options = null, ILogger<RetryPolicy>? logger = null)
    {
        _options = options ?? new RetryOptions();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RetryPolicy>.Instance;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        Exception lastException = null!;

        for (int attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt < _options.MaxAttempts && _options.ShouldRetry(ex))
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);
                
                _logger.LogWarning("Operation failed on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms. Error: {Error}",
                    attempt, _options.MaxAttempts, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(_options.BaseDelay.TotalMilliseconds * Math.Pow(_options.BackoffMultiplier, attempt - 1));
        return delay > _options.MaxDelay ? _options.MaxDelay : delay;
    }
}

public class ResilientExecutor
{
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<ResilientExecutor> _logger;

    public ResilientExecutor(
        ICircuitBreaker circuitBreaker,
        IRetryPolicy retryPolicy,
        ILogger<ResilientExecutor> logger)
    {
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async ct =>
        {
            return await _retryPolicy.ExecuteAsync(async retryToken =>
            {
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, retryToken);
                return await operation(combinedCts.Token);
            }, ct);
        }, cancellationToken);
    }

    public async Task ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(operationName, async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }
}