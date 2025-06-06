namespace AzureDevOps.MCP.Services.Infrastructure;

public class CircuitBreaker : ICircuitBreaker
{
	readonly int _failureThreshold;
	readonly TimeSpan _openDuration;
	readonly ILogger<CircuitBreaker> _logger;
	readonly object _lock = new ();

	CircuitBreakerState _state = CircuitBreakerState.Closed;
	int _failureCount;
	DateTime _lastFailureTime;
	DateTime _openedTime;

	public CircuitBreaker (
		int failureThreshold = 5,
		TimeSpan? openDuration = null,
		ILogger<CircuitBreaker>? logger = null)
	{
		_failureThreshold = failureThreshold;
		_openDuration = openDuration ?? TimeSpan.FromMinutes (1);
		_logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CircuitBreaker>.Instance;
	}

	public CircuitBreakerState State {
		get {
			lock (_lock) {
				if (_state == CircuitBreakerState.Open && DateTime.UtcNow - _openedTime >= _openDuration) {
					_state = CircuitBreakerState.HalfOpen;
					_logger.LogInformation ("Circuit breaker moved to half-open state");
				}
				return _state;
			}
		}
	}

	public async Task<T> ExecuteAsync<T> (Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
	{
		if (State == CircuitBreakerState.Open) {
			throw new CircuitBreakerException (CircuitBreakerState.Open, "Circuit breaker is open");
		}

		try {
			var result = await operation (cancellationToken);
			OnSuccess ();
			return result;
		} catch (Exception ex) {
			OnFailure (ex);
			throw;
		}
	}

	public async Task ExecuteAsync (Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
	{
		await ExecuteAsync (async ct => {
			await operation (ct);
			return true;
		}, cancellationToken);
	}

	public void Reset ()
	{
		lock (_lock) {
			_state = CircuitBreakerState.Closed;
			_failureCount = 0;
			_lastFailureTime = default;
			_openedTime = default;
			_logger.LogInformation ("Circuit breaker reset to closed state");
		}
	}

	void OnSuccess ()
	{
		lock (_lock) {
			if (_state == CircuitBreakerState.HalfOpen) {
				_state = CircuitBreakerState.Closed;
				_failureCount = 0;
				_logger.LogInformation ("Circuit breaker moved to closed state after successful operation");
			}
		}
	}

	void OnFailure (Exception exception)
	{
		lock (_lock) {
			_failureCount++;
			_lastFailureTime = DateTime.UtcNow;

			if (_state == CircuitBreakerState.HalfOpen) {
				_state = CircuitBreakerState.Open;
				_openedTime = DateTime.UtcNow;
				_logger.LogWarning ("Circuit breaker opened due to failure in half-open state: {Exception}", exception.Message);
			} else if (_failureCount >= _failureThreshold) {
				_state = CircuitBreakerState.Open;
				_openedTime = DateTime.UtcNow;
				_logger.LogWarning ("Circuit breaker opened due to {FailureCount} failures. Last failure: {Exception}",
					_failureCount, exception.Message);
			}
		}
	}
}