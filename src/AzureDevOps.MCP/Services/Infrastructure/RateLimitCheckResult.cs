namespace AzureDevOps.MCP.Services.Infrastructure;

public record RateLimitCheckResult (
	string RuleName,
	int RequestLimit,
	int RequestCount,
	bool IsAllowed,
	DateTimeOffset ResetTime)
{
	public int RemainingRequests => Math.Max (0, RequestLimit - RequestCount);
}