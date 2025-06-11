namespace AzureDevOps.MCP.Services.Infrastructure;

public record PerformanceRecommendation(
    string Category,
    string Description,
    string Suggestion,
    RecommendationPriority Priority
);