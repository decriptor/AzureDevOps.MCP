namespace AzureDevOps.MCP.Services.Infrastructure;

public record RateLimitStoreResult(int RequestCount, bool IsAllowed, DateTimeOffset ResetTime);