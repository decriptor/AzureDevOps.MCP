using System.ComponentModel;
using AzureDevOps.MCP.Services;
using ModelContextProtocol.Server;

namespace AzureDevOps.MCP.Tools;

[McpServerToolType]
public class PerformanceTools
{
	readonly IPerformanceService _performanceService;
	readonly ICacheService _cacheService;
	readonly ILogger<PerformanceTools> _logger;

	public PerformanceTools (
		IPerformanceService performanceService,
		ICacheService cacheService,
		ILogger<PerformanceTools> logger)
	{
		_performanceService = performanceService;
		_cacheService = cacheService;
		_logger = logger;
	}

	[McpServerTool (Name = "get_performance_metrics", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets performance metrics for the MCP server including operation timings and API call statistics")]
	public async Task<object> GetPerformanceMetricsAsync ()
	{
		try {
			var metrics = await _performanceService.GetMetricsAsync ();
			var uptime = DateTime.UtcNow - metrics.StartTime;

			return new {
				uptime = new {
					days = uptime.Days,
					hours = uptime.Hours,
					minutes = uptime.Minutes,
					totalMinutes = uptime.TotalMinutes
				},
				summary = new {
					totalOperations = metrics.TotalOperations,
					totalApiCalls = metrics.TotalApiCalls,
					operationTypes = metrics.Operations.Count,
					apiTypes = metrics.ApiCalls.Count
				},
				operations = metrics.Operations.Select (op => new {
					name = op.Key,
					count = op.Value.Count,
					averageDurationMs = Math.Round (op.Value.AverageDurationMs, 2),
					minDurationMs = op.Value.MinDurationMs,
					maxDurationMs = op.Value.MaxDurationMs,
					totalDurationMs = op.Value.TotalDurationMs
				}).OrderByDescending (op => op.count),
				apiCalls = metrics.ApiCalls.Select (api => new {
					name = api.Key,
					successCount = api.Value.SuccessCount,
					failureCount = api.Value.FailureCount,
					successRate = api.Value.SuccessCount + api.Value.FailureCount > 0
						? Math.Round (100.0 * api.Value.SuccessCount / (api.Value.SuccessCount + api.Value.FailureCount), 2)
						: 0,
					averageDurationMs = Math.Round (api.Value.AverageDurationMs, 2),
					totalDurationMs = api.Value.TotalDurationMs
				}).OrderByDescending (api => api.successCount + api.failureCount)
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting performance metrics");
			throw new InvalidOperationException ($"Failed to get performance metrics: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "clear_cache", ReadOnly = false, OpenWorld = false)]
	[Description ("Clears all cached data to force fresh API calls")]
	public async Task<object> ClearCacheAsync ()
	{
		try {
			await _cacheService.ClearAsync ();
			return new {
				success = true,
				message = "Cache cleared successfully",
				timestamp = DateTime.UtcNow
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error clearing cache");
			throw new InvalidOperationException ($"Failed to clear cache: {ex.Message}", ex);
		}
	}
}