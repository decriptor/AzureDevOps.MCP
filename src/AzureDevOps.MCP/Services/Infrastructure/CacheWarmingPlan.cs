using System.Collections.Frozen;

namespace AzureDevOps.MCP.Services.Infrastructure;

public record CacheWarmingPlan (
	DateTimeOffset CreatedAt,
	FrozenSet<CacheWarmingOperation> Operations,
	double EstimatedTotalBenefit,
	TimeSpan EstimatedPreloadDuration
);