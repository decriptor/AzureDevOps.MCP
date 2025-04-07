namespace AzureDevOps.MCP.Services.Infrastructure;

public class RetryPolicy : IRetryPolicy
{
	readonly RetryOptions _options;
	readonly ILogger<RetryPolicy> _logger;

	public RetryPolicy (RetryOptions? options = null, ILogger<RetryPolicy>? logger = null)
	{
		_options = options ?? new RetryOptions ();
		_logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RetryPolicy>.Instance;
	}

	public async Task<T> ExecuteAsync<T> (Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
	{
		Exception lastException = null!;

		for (int attempt = 1; attempt <= _options.MaxAttempts; attempt++) {
			try {
				return await operation (cancellationToken);
			} catch (Exception ex) when (attempt < _options.MaxAttempts && _options.ShouldRetry (ex)) {
				lastException = ex;
				var delay = CalculateDelay (attempt);

				_logger.LogWarning ("Operation failed on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms. Error: {Error}",
					attempt, _options.MaxAttempts, delay.TotalMilliseconds, ex.Message);

				await Task.Delay (delay, cancellationToken);
			}
		}

		throw lastException;
	}

	public async Task ExecuteAsync (Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
	{
		await ExecuteAsync (async ct => {
			await operation (ct);
			return true;
		}, cancellationToken);
	}

	TimeSpan CalculateDelay (int attempt)
	{
		var delay = TimeSpan.FromMilliseconds (_options.BaseDelay.TotalMilliseconds * Math.Pow (_options.BackoffMultiplier, attempt - 1));
		return delay > _options.MaxDelay ? _options.MaxDelay : delay;
	}
}