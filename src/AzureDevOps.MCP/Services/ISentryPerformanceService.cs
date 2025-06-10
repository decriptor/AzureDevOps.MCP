namespace AzureDevOps.MCP.Services;

public interface ISentryPerformanceService
{
	Task StartTransactionAsync(string operationName, CancellationToken cancellationToken = default);
	Task FinishTransactionAsync(bool success = true, CancellationToken cancellationToken = default);
	Task RecordSpanAsync(string spanName, TimeSpan duration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Simple stub implementation for Sentry performance tracking.
/// This can be replaced with a full Sentry implementation when needed.
/// </summary>
public class SentryPerformanceService : ISentryPerformanceService
{
	readonly ILogger<SentryPerformanceService> _logger;

	public SentryPerformanceService(ILogger<SentryPerformanceService> logger)
	{
		_logger = logger;
	}

	public Task StartTransactionAsync(string operationName, CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Started transaction: {OperationName}", operationName);
		return Task.CompletedTask;
	}

	public Task FinishTransactionAsync(bool success = true, CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Finished transaction: Success={Success}", success);
		return Task.CompletedTask;
	}

	public Task RecordSpanAsync(string spanName, TimeSpan duration, CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Recorded span: {SpanName} Duration={Duration}ms", spanName, duration.TotalMilliseconds);
		return Task.CompletedTask;
	}
}