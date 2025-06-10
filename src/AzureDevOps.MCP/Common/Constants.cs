namespace AzureDevOps.MCP.Common;

public static class Constants
{
	public static class Cache
	{
		public static readonly TimeSpan ProjectsCacheExpiration = TimeSpan.FromMinutes (10);
		public static readonly TimeSpan RepositoriesCacheExpiration = TimeSpan.FromMinutes (5);
		public static readonly TimeSpan FileContentCacheExpiration = TimeSpan.FromMinutes (5);
		public static readonly TimeSpan WorkItemsCacheExpiration = TimeSpan.FromMinutes (1);
		public static readonly TimeSpan BuildsCacheExpiration = TimeSpan.FromMinutes (1);
		public static readonly TimeSpan TestRunsCacheExpiration = TimeSpan.FromMinutes (2);
		public static readonly TimeSpan WikiCacheExpiration = TimeSpan.FromMinutes (5);

		public const int MaxCacheEntries = 1000;
		public const int MaxCacheKeysToTrack = 10000;
	}

	public static class Performance
	{
		public const long SlowOperationThresholdMs = 1000;
		public const long VerySlowOperationThresholdMs = 2000;
		public const int MaxRetryAttempts = 3;
		public const int BaseRetryDelayMs = 500;
		public const int MaxConcurrentOperations = 50;
		public const int MaxOperationHistoryEntries = 1000;
	}

	public static class AzureDevOps
	{
		public const string DefaultBranchPrefix = "refs/heads/";
		public const string TagPrefix = "refs/tags/";
		public const int DefaultWorkItemLimit = 100;
		public const int MaxWorkItemLimit = 1000;
		public const int DefaultBuildLimit = 20;
		public const int MaxBuildLimit = 100;
		public const int DefaultCommitLimit = 50;
		public const int MaxCommitLimit = 500;
		public const int DefaultTestRunLimit = 20;
		public const int MaxTestRunLimit = 100;
		public const int DefaultCodeSearchLimit = 50;
		public const int MaxCodeSearchLimit = 200;
	}

	public static class Validation
	{
		public const int MaxProjectNameLength = 64;
		public const int MaxRepositoryIdLength = 36; // GUID length
		public const int MaxFilePathLength = 260;
		public const int MaxBranchNameLength = 250;
		public const int MaxTagLength = 100;
		public const int MaxCommentLength = 8000;
		public const int MaxPullRequestTitleLength = 400;
		public const int MaxPullRequestDescriptionLength = 4000;
		public const int MaxSearchQueryLength = 200;

		public static readonly string[] ForbiddenPathChars = { "..", "//", "\\", "<", ">", "|", "?", "*", ":" };
		public static readonly string[] ValidImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg" };
		public static readonly string[] ValidDocumentExtensions = { ".md", ".txt", ".pdf", ".doc", ".docx" };
	}

	public static class Security
	{
		public const int TokenHashLength = 8;
		public const int MaxAuditLogEntries = 1000;
		public const int SessionTimeoutMinutes = 60;
		public const int MaxFailedAttemptsBeforeBlock = 5;
		public const int BlockDurationMinutes = 15;
	}

	public static class OperationTypes
	{
		public const string Read = "read";
		public const string Write = "write";
		public const string Search = "search";
		public const string Batch = "batch";
		public const string Download = "download";
		public const string Cache = "cache";
		public const string Performance = "performance";
	}

	public static class TracingCategories
	{
		public const string AzureDevOpsRead = "azure_devops.read";
		public const string AzureDevOpsWrite = "azure_devops.write";
		public const string AzureDevOpsSearch = "azure_devops.search";
		public const string AzureDevOpsBatch = "azure_devops.batch";
		public const string AzureDevOpsDownload = "azure_devops.download";
		public const string McpOperation = "mcp.operation";
		public const string CacheOperation = "cache.operation";
		public const string ApiCall = "azure_devops.api_call";
	}
}