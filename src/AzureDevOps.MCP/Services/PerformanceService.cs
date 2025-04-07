using System.Collections.Concurrent;
using System.Diagnostics;

namespace AzureDevOps.MCP.Services;

public class PerformanceService : IPerformanceService
{
	readonly ILogger<PerformanceService> _logger;
	readonly ConcurrentDictionary<string, List<long>> _operationDurations = new ();
	readonly ConcurrentDictionary<string, (long success, long failure, long totalDuration)> _apiCallStats = new ();
	readonly DateTime _startTime = DateTime.UtcNow;
	long _totalOperations;
	long _totalApiCalls;

	public PerformanceService (ILogger<PerformanceService> logger)
	{
		_logger = logger;
	}

	public IDisposable TrackOperation (string operationName, Dictionary<string, object>? metadata = null)
	{
		return new OperationTracker (this, operationName, metadata);
	}

	public Task<PerformanceMetrics> GetMetricsAsync ()
	{
		var metrics = new PerformanceMetrics {
			StartTime = _startTime,
			TotalOperations = _totalOperations,
			TotalApiCalls = _totalApiCalls
		};

		foreach (var (operation, durations) in _operationDurations) {
			if (durations.Count != 0) {
				metrics.Operations[operation] = new OperationMetrics {
					Count = durations.Count,
					AverageDurationMs = durations.Average (),
					MinDurationMs = durations.Min (),
					MaxDurationMs = durations.Max (),
					TotalDurationMs = durations.Sum ()
				};
			}
		}

		foreach (var (api, stats) in _apiCallStats) {
			var totalCalls = stats.success + stats.failure;
			metrics.ApiCalls[api] = new ApiCallMetrics {
				SuccessCount = stats.success,
				FailureCount = stats.failure,
				AverageDurationMs = totalCalls > 0 ? (double)stats.totalDuration / totalCalls : 0,
				TotalDurationMs = stats.totalDuration
			};
		}

		return Task.FromResult (metrics);
	}

	public void RecordApiCall (string apiName, long durationMs, bool success)
	{
		_apiCallStats.AddOrUpdate (apiName,
			success ? (1, 0, durationMs) : (0, 1, durationMs),
			(key, current) => {
				return success
					? (current.success + 1, current.failure, current.totalDuration + durationMs)
					: (current.success, current.failure + 1, current.totalDuration + durationMs);
			});

		Interlocked.Increment (ref _totalApiCalls);
	}

	internal void RecordOperation (string operationName, long durationMs, Dictionary<string, object>? metadata)
	{
		_operationDurations.AddOrUpdate (operationName,
			[durationMs],
			(key, list) => {
				list.Add (durationMs);
				return list;
			});

		Interlocked.Increment (ref _totalOperations);

		if (durationMs > 1000) {
			_logger.LogWarning ("Slow operation detected: {Operation} took {Duration}ms", operationName, durationMs);
		}
	}

	class OperationTracker : IDisposable
	{
		readonly PerformanceService _service;
		readonly string _operationName;
		readonly Dictionary<string, object>? _metadata;
		readonly Stopwatch _stopwatch;

		public OperationTracker (PerformanceService service, string operationName, Dictionary<string, object>? metadata)
		{
			_service = service;
			_operationName = operationName;
			_metadata = metadata;
			_stopwatch = Stopwatch.StartNew ();
		}

		public void Dispose ()
		{
			_stopwatch.Stop ();
			_service.RecordOperation (_operationName, _stopwatch.ElapsedMilliseconds, _metadata);
		}
	}
}