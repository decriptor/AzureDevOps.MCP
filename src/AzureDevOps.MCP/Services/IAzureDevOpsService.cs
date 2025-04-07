using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.MCP.Services;

public interface IAzureDevOpsService
{
	Task<VssConnection> GetConnectionAsync ();
	Task<IEnumerable<TeamProjectReference>> GetProjectsAsync ();
	Task<IEnumerable<GitRepository>> GetRepositoriesAsync (string projectName);
	Task<IEnumerable<GitItem>> GetRepositoryItemsAsync (string projectName, string repositoryId, string path);
	Task<string> GetFileContentAsync (string projectName, string repositoryId, string path);
	Task<IEnumerable<WorkItem>> GetWorkItemsAsync (string projectName, int limit = 100);
	Task<WorkItem?> GetWorkItemAsync (int id);

	// Git operations
	Task<IEnumerable<GitCommitRef>> GetCommitsAsync (string projectName, string repositoryId, string? branch = null, int limit = 50);
	Task<IEnumerable<GitPullRequest>> GetPullRequestsAsync (string projectName, string repositoryId, string? status = null);
	Task<IEnumerable<GitRef>> GetBranchesAsync (string projectName, string repositoryId);
	Task<IEnumerable<GitRef>> GetTagsAsync (string projectName, string repositoryId);

	// Write operations (when enabled)
	Task<Microsoft.TeamFoundation.SourceControl.WebApi.Comment> AddPullRequestCommentAsync (string projectName, string repositoryId, int pullRequestId, string content, int? parentCommentId = null);
	Task<WorkItemComment> AddWorkItemCommentAsync (string projectName, int workItemId, string content);
	Task<GitPullRequest> CreateDraftPullRequestAsync (string projectName, string repositoryId, string sourceBranch, string targetBranch, string title, string description);
	Task<WorkItem> UpdateWorkItemTagsAsync (int workItemId, string[] tagsToAdd, string[] tagsToRemove);
	Task<WorkItem> CreateWorkItemAsync (string projectName, string workItemType, string title, string description, string? tags = null);

	// Search operations - Commented out due to missing APIs
	// Task<IEnumerable<CodeSearchResult>> SearchCodeAsync(string searchText, string? projectName = null, string? repositoryName = null, int limit = 50);

	// Wiki operations - Commented out due to missing APIs  
	// Task<IEnumerable<WikiReference>> GetWikisAsync(string projectName);
	// Task<WikiPage?> GetWikiPageAsync(string projectName, string wikiIdentifier, string path);

	// Test operations - Commented out until proper Azure DevOps test packages are available
	// Task<IEnumerable<TestPlan>> GetTestPlansAsync(string projectName, int limit = 20);
	// Task<TestPlan?> GetTestPlanAsync(string projectName, int planId);
	// Task<IEnumerable<TestSuite>> GetTestSuitesAsync(string projectName, int planId, int limit = 20);
	// Task<IEnumerable<TestRun>> GetTestRunsAsync(string projectName, int limit = 20);
	// Task<TestRun?> GetTestRunAsync(string projectName, int runId);
	// Task<IEnumerable<TestCaseResult>> GetTestResultsAsync(string projectName, int runId);

	// Artifact operations - commenting out as BuildHttpClient is not available
	// Task<Stream> DownloadBuildArtifactAsync(string projectName, int buildId, string artifactName);
}

public class CodeSearchResult
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public string Repository { get; set; } = string.Empty;
	public string Project { get; set; } = string.Empty;
	public Dictionary<int, string> Matches { get; set; } = [];
}

public class WikiReference
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
}

public class WikiPage
{
	public string Path { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public int Order { get; set; }
	public bool IsParentPage { get; set; }
	public List<WikiPage> SubPages { get; set; } = [];
}

public class WorkItemComment
{
	public int Id { get; set; }
	public string Text { get; set; } = string.Empty;
	public DateTime CreatedDate { get; set; }
	public string CreatedBy { get; set; } = string.Empty;
	public int WorkItemId { get; set; }
}
