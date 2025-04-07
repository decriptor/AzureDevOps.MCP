using System.Collections.Concurrent;
using Sentry;
using Sentry.Protocol;

namespace AzureDevOps.MCP.Services;

public interface ISentryPerformanceService
{
	Task StartTransactionAsync (string operationName, CancellationToken cancellationToken = default);
	Task FinishTransactionAsync (bool success = true, CancellationToken cancellationToken = default);
	Task RecordSpanAsync (string spanName, TimeSpan duration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Production implementation for Sentry performance tracking.
/// Integrates with Sentry SDK for real performance monitoring.
/// </summary>
public class SentryPerformanceService : ISentryPerformanceService
{
	readonly ILogger<SentryPerformanceService> _logger;
	readonly ThreadLocal<ITransactionTracer?> _currentTransaction = new ();
	readonly ConcurrentDictionary<string, ISpan> _activeSpans = new ();

	public SentryPerformanceService (ILogger<SentryPerformanceService> logger)
	{
		_logger = logger;
	}

	public Task StartTransactionAsync (string operationName, CancellationToken cancellationToken = default)
	{
		try {
			var transaction = SentrySdk.StartTransaction (operationName, "operation");
			_currentTransaction.Value = transaction;
			return Task.CompletedTask;
		} catch (Exception ex) {
			_logger.LogWarning (ex, "Failed to start Sentry transaction for {OperationName}", operationName);
			return Task.CompletedTask;
		}
	}

	public Task FinishTransactionAsync (bool success = true, CancellationToken cancellationToken = default)
	{
		try {
			var transaction = _currentTransaction.Value;
			if (transaction != null) {
				transaction.Status = success ? SpanStatus.Ok : SpanStatus.InternalError;
				transaction.Finish ();
				_currentTransaction.Value = null;
			}
		} catch (Exception ex) {
			_logger.LogWarning (ex, "Failed to finish Sentry transaction");
		}
		return Task.CompletedTask;
	}

	public Task RecordSpanAsync (string spanName, TimeSpan duration, CancellationToken cancellationToken = default)
	{
		try {
			var transaction = _currentTransaction.Value;
			if (transaction != null) {
				var span = transaction.StartChild ("operation", spanName);
				span.Finish (SpanStatus.Ok);
			}
		} catch (Exception ex) {
			_logger.LogWarning (ex, "Failed to record Sentry span {SpanName}", spanName);
		}
		return Task.CompletedTask;
	}
}