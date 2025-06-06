namespace AzureDevOps.MCP.Services.Infrastructure;

public class ResilientExecutor
{
	readonly ICircuitBreaker _circuitBreaker;
	readonly IRetryPolicy _retryPolicy;
	readonly ILogger<ResilientExecutor> _logger;

	public ResilientExecutor (
		ICircuitBreaker circuitBreaker,
		IRetryPolicy retryPolicy,
		ILogger<ResilientExecutor> logger)
	{
		_circuitBreaker = circuitBreaker ?? throw new ArgumentNullException (nameof (circuitBreaker));
		_retryPolicy = retryPolicy ?? throw new ArgumentNullException (nameof (retryPolicy));
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
	}

	public async Task<T> ExecuteAsync<T> (
		string operationName,
		Func<CancellationToken, Task<T>> operation,
		CancellationToken cancellationToken = default)
	{
		return await _circuitBreaker.ExecuteAsync (async ct => {
			return await _retryPolicy.ExecuteAsync (async retryToken => {
				using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource (ct, retryToken);
				return await operation (combinedCts.Token);
			}, ct);
		}, cancellationToken);
	}

	public async Task ExecuteAsync (
		string operationName,
		Func<CancellationToken, Task> operation,
		CancellationToken cancellationToken = default)
	{
		await ExecuteAsync (operationName, async ct => {
			await operation (ct);
			return true;
		}, cancellationToken);
	}
}