namespace AzureDevOps.MCP.Services.Infrastructure;

public record RateLimitEntry (int RequestCount, DateTimeOffset ResetTime);