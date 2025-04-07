namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Interface for performance optimization services.
/// </summary>
public interface IPerformanceOptimizationService : IDisposable
{
	TimeSpan OptimizeCacheDuration (string operationType, string dataType);
	Task<T> TrackPerformanceAsync<T> (string operationName, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
	PerformanceRecommendations GetPerformanceRecommendations ();
	T OptimizeAllocation<T> (Func<T> factory) where T : class;
	CacheWarmingPlan CreateCacheWarmingPlan ();
}