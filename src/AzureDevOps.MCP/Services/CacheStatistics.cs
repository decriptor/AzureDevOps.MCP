namespace AzureDevOps.MCP.Services;

public class CacheStatistics
{
	public int EntryCount { get; set; }
	public double HitRate { get; set; }
	public long TotalSizeBytes { get; set; }
	public long HitCount { get; set; }
	public long MissCount { get; set; }
}
