namespace AzureDevOps.MCP.Configuration;

public class AzureDevOpsConfiguration
{
    public required string OrganizationUrl { get; set; }
    public required string PersonalAccessToken { get; set; }
    public List<string> EnabledWriteOperations { get; set; } = new();
    public bool RequireConfirmation { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
    public MonitoringConfiguration Monitoring { get; set; } = new();
}

public class MonitoringConfiguration
{
    public SentryConfiguration Sentry { get; set; } = new();
    public bool EnablePerformanceTracking { get; set; } = true;
    public bool EnableErrorTracking { get; set; } = true;
}

public class SentryConfiguration
{
    public string? Dsn { get; set; }
    public bool Debug { get; set; } = false;
    public double SampleRate { get; set; } = 1.0;
    public double TracesSampleRate { get; set; } = 0.1;
    public string Environment { get; set; } = "production";
    public string Release { get; set; } = "1.0.0";
    public bool AttachStacktrace { get; set; } = true;
    public bool SendDefaultPii { get; set; } = false;
}

public static class SafeWriteOperations
{
    public const string PullRequestComments = "PullRequestComments";
    public const string WorkItemComments = "WorkItemComments";
    public const string CreateDraftPullRequest = "CreateDraftPullRequest";
    public const string UpdateWorkItemTags = "UpdateWorkItemTags";
    
    public static readonly HashSet<string> AllOperations = new()
    {
        PullRequestComments,
        WorkItemComments,
        CreateDraftPullRequest,
        UpdateWorkItemTags
    };
    
    public static readonly Dictionary<string, string> OperationDescriptions = new()
    {
        [PullRequestComments] = "Add comments to pull requests",
        [WorkItemComments] = "Add comments to work items",
        [CreateDraftPullRequest] = "Create draft pull requests (not auto-published)",
        [UpdateWorkItemTags] = "Add or remove tags from work items"
    };
}