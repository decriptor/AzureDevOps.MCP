namespace AzureDevOps.MCP.Services.Infrastructure;

// Modern record types for data structures
public record RateLimitRule (TimeSpan WindowSize, int RequestLimit);