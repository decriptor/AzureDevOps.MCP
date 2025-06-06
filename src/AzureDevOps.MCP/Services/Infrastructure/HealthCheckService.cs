using System.Collections.Concurrent;
using System.Diagnostics;

namespace AzureDevOps.MCP.Services.Infrastructure;

public class HealthCheckService : IHealthCheckService, IDisposable
{
	readonly ConcurrentDictionary<string, Func<CancellationToken, Task<HealthStatus>>> _checks = new ();
	readonly ConcurrentDictionary<string, HealthStatus> _lastResults = new ();
	readonly ILogger<HealthCheckService> _logger;
	readonly Timer _periodicCheckTimer;
	bool _disposed;

	public event EventHandler<HealthChangedEventArgs>? HealthChanged;

	public HealthCheckService (ILogger<HealthCheckService> logger)
	{
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
		_periodicCheckTimer = new Timer (PeriodicHealthCheck, null, TimeSpan.FromMinutes (1), TimeSpan.FromMinutes (1));
	}

	public async Task<HealthStatus> CheckHealthAsync (CancellationToken cancellationToken = default)
	{
		var allResults = await CheckAllHealthAsync (cancellationToken);

		var isHealthy = allResults.Values.All (status => status.IsHealthy);
		var unhealthyChecks = allResults.Where (kvp => !kvp.Value.IsHealthy).ToList ();

		if (isHealthy) {
			return HealthStatus.Healthy ("All health checks passed", new Dictionary<string, object> {
				["totalChecks"] = allResults.Count,
				["details"] = allResults
			});
		}

		var description = $"{unhealthyChecks.Count} of {allResults.Count} health checks failed";
		return HealthStatus.Unhealthy (description, data: new Dictionary<string, object> {
			["totalChecks"] = allResults.Count,
			["failedChecks"] = unhealthyChecks.Count,
			["details"] = allResults
		});
	}

	public async Task<Dictionary<string, HealthStatus>> CheckAllHealthAsync (CancellationToken cancellationToken = default)
	{
		var results = new Dictionary<string, HealthStatus> ();
		var tasks = _checks.Select (async kvp => {
			var (name, check) = kvp;
			var sw = Stopwatch.StartNew ();

			try {
				var result = await check (cancellationToken);
				sw.Stop ();

				var statusWithTiming = new HealthStatus {
					IsHealthy = result.IsHealthy,
					Description = result.Description,
					Data = result.Data,
					Exception = result.Exception,
					ResponseTime = sw.Elapsed
				};

				return (name, status: statusWithTiming);
			} catch (Exception ex) {
				sw.Stop ();
				_logger.LogError (ex, "Health check failed for {CheckName}", name);

				return (name, status: HealthStatus.Unhealthy (
					$"Health check threw exception: {ex.Message}",
					ex,
					responseTime: sw.Elapsed));
			}
		});

		var completedTasks = await Task.WhenAll (tasks);

		foreach (var (name, status) in completedTasks) {
			results[name] = status;

			// Check if status changed
			if (_lastResults.TryGetValue (name, out var previousStatus) &&
				previousStatus.IsHealthy != status.IsHealthy) {
				HealthChanged?.Invoke (this, new HealthChangedEventArgs (name, status, previousStatus));
			}

			_lastResults[name] = status;
		}

		return results;
	}

	public void RegisterCheck (string name, Func<CancellationToken, Task<HealthStatus>> check)
	{
		if (string.IsNullOrEmpty (name)) {
			throw new ArgumentException ("Check name cannot be null or empty", nameof (name));
		}

		if (check == null) {
			throw new ArgumentNullException (nameof (check));
		}

		_checks.AddOrUpdate (name, check, (_, _) => check);
		_logger.LogDebug ("Registered health check: {CheckName}", name);
	}

	async void PeriodicHealthCheck (object? state)
	{
		if (_disposed) {
			return;
		}

		try {
			await CheckAllHealthAsync ();
		} catch (Exception ex) {
			_logger.LogError (ex, "Error during periodic health check");
		}
	}

	public void Dispose ()
	{
		if (!_disposed) {
			_periodicCheckTimer?.Dispose ();
			_disposed = true;
		}
	}
}