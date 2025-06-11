namespace AzureDevOps.MCP.Services.Infrastructure;

public record CacheWarmingOperation(
    string OperationName,
    WarmingPriority Priority,
    double EstimatedBenefit,
    TimeSpan RecommendedPreloadTime
);