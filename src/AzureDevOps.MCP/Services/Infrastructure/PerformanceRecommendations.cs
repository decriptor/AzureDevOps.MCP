using System.Collections.Frozen;

namespace AzureDevOps.MCP.Services.Infrastructure;

public record PerformanceRecommendations(
    DateTimeOffset GeneratedAt,
    FrozenSet<PerformanceRecommendation> Recommendations,
    SystemMetrics SystemMetrics,
    FrozenDictionary<string, PerformanceMetrics> OperationMetrics
);