using System.Collections.Frozen;

namespace AzureDevOps.MCP.Services.Infrastructure;

public record RateLimitResult(
    bool IsAllowed,
    string ClientId,
    int RequestCount,
    int RequestLimit,
    int RemainingRequests,
    DateTimeOffset ResetTime,
    TimeSpan RetryAfter,
    FrozenDictionary<string, RateLimitCheckResult> Rules);