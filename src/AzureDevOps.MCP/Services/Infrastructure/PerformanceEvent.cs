namespace AzureDevOps.MCP.Services.Infrastructure;

public record PerformanceEvent (
	DateTimeOffset Timestamp,
	string OperationName,
	TimeSpan Duration,
	bool Success,
	long MemoryDelta
);