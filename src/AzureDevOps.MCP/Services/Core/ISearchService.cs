using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps search operations.
/// Follows Single Responsibility Principle - only handles search-related operations.
/// </summary>
public interface ISearchService
{
	/// <summary>
	/// Searches for code across repositories.
	/// </summary>
	/// <param name="searchText">The text to search for</param>
	/// <param name="projectNameOrId">Optional project to limit search scope</param>
	/// <param name="repositoryName">Optional repository to limit search scope</param>
	/// <param name="limit">Maximum number of results to return</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of code search results</returns>
	Task<IEnumerable<CodeSearchResult>> SearchCodeAsync (string searchText, string? projectNameOrId = null, string? repositoryName = null, int limit = 50, CancellationToken cancellationToken = default);

	/// <summary>
	/// Searches for work items.
	/// </summary>
	/// <param name="searchText">The text to search for</param>
	/// <param name="projectNameOrId">Optional project to limit search scope</param>
	/// <param name="workItemType">Optional work item type to filter by</param>
	/// <param name="limit">Maximum number of results to return</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of work item search results</returns>
	Task<IEnumerable<WorkItemSearchResult>> SearchWorkItemsAsync (string searchText, string? projectNameOrId = null, string? workItemType = null, int limit = 50, CancellationToken cancellationToken = default);

	/// <summary>
	/// Searches for files by name or path.
	/// </summary>
	/// <param name="fileName">The file name or pattern to search for</param>
	/// <param name="projectNameOrId">Optional project to limit search scope</param>
	/// <param name="repositoryName">Optional repository to limit search scope</param>
	/// <param name="limit">Maximum number of results to return</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of file search results</returns>
	Task<IEnumerable<FileSearchResult>> SearchFilesAsync (string fileName, string? projectNameOrId = null, string? repositoryName = null, int limit = 50, CancellationToken cancellationToken = default);

	/// <summary>
	/// Searches for packages in feeds.
	/// </summary>
	/// <param name="packageName">The package name to search for</param>
	/// <param name="feedName">Optional feed to limit search scope</param>
	/// <param name="limit">Maximum number of results to return</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of package search results</returns>
	Task<IEnumerable<PackageSearchResult>> SearchPackagesAsync (string packageName, string? feedName = null, int limit = 50, CancellationToken cancellationToken = default);
}

/// <summary>
/// Code search result model.
/// </summary>
public class CodeSearchResult
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public string Repository { get; set; } = string.Empty;
	public string Project { get; set; } = string.Empty;
	public string Branch { get; set; } = string.Empty;
	public List<CodeMatch> Matches { get; set; } = [];
	public string ContentType { get; set; } = string.Empty;
	public long FileSize { get; set; }
	public DateTime LastModified { get; set; }
	public string Author { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Code match details within a file.
/// </summary>
public class CodeMatch
{
	public int LineNumber { get; set; }
	public string LineContent { get; set; } = string.Empty;
	public int ColumnStart { get; set; }
	public int ColumnEnd { get; set; }
	public string MatchedText { get; set; } = string.Empty;
	public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Work item search result model.
/// </summary>
public class WorkItemSearchResult
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string WorkItemType { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string AssignedTo { get; set; } = string.Empty;
	public TeamProjectReference? Project { get; set; }
	public string AreaPath { get; set; } = string.Empty;
	public string IterationPath { get; set; } = string.Empty;
	public List<string> Tags { get; set; } = [];
	public List<WorkItemMatch> Matches { get; set; } = [];
	public DateTime CreatedDate { get; set; }
	public DateTime ChangedDate { get; set; }
	public string CreatedBy { get; set; } = string.Empty;
	public string ChangedBy { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Work item field match details.
/// </summary>
public class WorkItemMatch
{
	public string FieldName { get; set; } = string.Empty;
	public string FieldValue { get; set; } = string.Empty;
	public List<string> HighlightedTerms { get; set; } = [];
}

/// <summary>
/// File search result model.
/// </summary>
public class FileSearchResult
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public string Repository { get; set; } = string.Empty;
	public string Project { get; set; } = string.Empty;
	public string Branch { get; set; } = string.Empty;
	public string ContentType { get; set; } = string.Empty;
	public long FileSize { get; set; }
	public DateTime LastModified { get; set; }
	public string Author { get; set; } = string.Empty;
	public string CommitId { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Package search result model.
/// </summary>
public class PackageSearchResult
{
	public string PackageName { get; set; } = string.Empty;
	public string Version { get; set; } = string.Empty;
	public string FeedName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public List<string> Authors { get; set; } = [];
	public List<string> Tags { get; set; } = [];
	public string PackageType { get; set; } = string.Empty;
	public long DownloadCount { get; set; }
	public DateTime PublishedDate { get; set; }
	public string LicenseUrl { get; set; } = string.Empty;
	public string ProjectUrl { get; set; } = string.Empty;
	public string IconUrl { get; set; } = string.Empty;
	public bool IsPrerelease { get; set; }
	public bool IsListed { get; set; }
}