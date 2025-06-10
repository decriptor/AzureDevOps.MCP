namespace AzureDevOps.MCP.Services;

public interface IPerformanceService
{
	IDisposable TrackOperation (string operationName, Dictionary<string, object>? metadata = null);
	Task<PerformanceMetrics> GetMetricsAsync ();
	void RecordApiCall (string apiName, long durationMs, bool success);
}

public class PerformanceMetrics
{
	public Dictionary<string, OperationMetrics> Operations { get; set; } = new ();
	public Dictionary<string, ApiCallMetrics> ApiCalls { get; set; } = new ();
	public DateTime StartTime { get; set; }
	public long TotalOperations { get; set; }
	public long TotalApiCalls { get; set; }
}

public class OperationMetrics
{
	public long Count { get; set; }
	public double AverageDurationMs { get; set; }
	public long MinDurationMs { get; set; }
	public long MaxDurationMs { get; set; }
	public long TotalDurationMs { get; set; }
}

public class ApiCallMetrics
{
	public long SuccessCount { get; set; }
	public long FailureCount { get; set; }
	public double AverageDurationMs { get; set; }
	public long TotalDurationMs { get; set; }
}