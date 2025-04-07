using System.ComponentModel;
using AzureDevOps.MCP.Services;
using ModelContextProtocol.Server;

namespace AzureDevOps.MCP.Tools;

[McpServerToolType]
public class BatchTools
{
	readonly IAzureDevOpsService _azureDevOpsService;
	readonly IPerformanceService _performanceService;
	readonly ILogger<BatchTools> _logger;

	public BatchTools (
		IAzureDevOpsService azureDevOpsService,
		IPerformanceService performanceService,
		ILogger<BatchTools> logger)
	{
		_azureDevOpsService = azureDevOpsService;
		_performanceService = performanceService;
		_logger = logger;
	}

	[McpServerTool (Name = "batch_get_work_items", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets multiple work items by their IDs in a single batch operation")]
	public async Task<object> BatchGetWorkItemsAsync (
		[Description ("Array of work item IDs to retrieve")] int[] workItemIds)
	{
		using var _ = _performanceService.TrackOperation ("BatchGetWorkItems", new Dictionary<string, object> { ["count"] = workItemIds.Length });

		try {
			var tasks = workItemIds.Select (id => GetWorkItemWithErrorHandling (id));
			var results = await Task.WhenAll (tasks);

			return new {
				success = true,
				totalRequested = workItemIds.Length,
				totalRetrieved = results.Count (r => r.workItem != null),
				workItems = results.Where (r => r.workItem != null).Select (r => new {
					id = r.workItem!.Id,
					title = r.workItem.Fields.TryGetValue ("System.Title", out var titleValue) ? titleValue?.ToString () : "No title",
					state = r.workItem.Fields.TryGetValue ("System.State", out var stateValue) ? stateValue?.ToString () : "Unknown",
					type = r.workItem.Fields.TryGetValue ("System.WorkItemType", out var typeValue) ? typeValue?.ToString () : "Unknown",
					url = r.workItem.Url
				}),
				errors = results.Where (r => r.error != null).Select (r => new {
					r.id,
					r.error
				})
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error in batch work item retrieval");
			throw new InvalidOperationException ($"Failed to batch retrieve work items: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "batch_get_file_contents", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets contents of multiple files in a single batch operation")]
	public async Task<object> BatchGetFileContentsAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the repository")] string repositoryId,
		[Description ("Array of file paths to retrieve")] string[] paths)
	{
		using var _ = _performanceService.TrackOperation ("BatchGetFileContents", new Dictionary<string, object> { ["count"] = paths.Length });

		try {
			var tasks = paths.Select (path => GetFileContentWithErrorHandling (projectName, repositoryId, path));
			var results = await Task.WhenAll (tasks);

			return new {
				success = true,
				projectName,
				repositoryId,
				totalRequested = paths.Length,
				totalRetrieved = results.Count (r => r.content != null),
				files = results.Where (r => r.content != null).Select (r => new {
					r.path,
					r.content,
					contentLength = r.content!.Length
				}),
				errors = results.Where (r => r.error != null).Select (r => new {
					r.path,
					r.error
				})
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error in batch file content retrieval");
			throw new InvalidOperationException ($"Failed to batch retrieve file contents: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "batch_list_repository_items", ReadOnly = true, OpenWorld = false)]
	[Description ("Lists items from multiple repository paths in a single batch operation")]
	public async Task<object> BatchListRepositoryItemsAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the repository")] string repositoryId,
		[Description ("Array of paths to list items from")] string[] paths)
	{
		using var _ = _performanceService.TrackOperation ("BatchListRepositoryItems", new Dictionary<string, object> { ["count"] = paths.Length });

		try {
			var tasks = paths.Select (path => ListItemsWithErrorHandling (projectName, repositoryId, path));
			var results = await Task.WhenAll (tasks);

			return new {
				success = true,
				projectName,
				repositoryId,
				totalRequested = paths.Length,
				totalRetrieved = results.Count (r => r.items != null),
				paths = results.Where (r => r.items != null).Select (r => new {
					r.path,
					itemCount = r.items!.Count (),
					items = r.items!.Select (i => new {
						path = i.Path,
						isFolder = i.IsFolder,
						commitId = i.CommitId
					})
				}),
				errors = results.Where (r => r.error != null).Select (r => new {
					r.path,
					r.error
				})
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error in batch repository items listing");
			throw new InvalidOperationException ($"Failed to batch list repository items: {ex.Message}", ex);
		}
	}

	async Task<(int id, Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem? workItem, string? error)> GetWorkItemWithErrorHandling (int id)
	{
		try {
			var workItem = await _azureDevOpsService.GetWorkItemAsync (id);
			return (id, workItem, null);
		} catch (Exception ex) {
			_logger.LogWarning (ex, "Failed to get work item {Id}", id);
			return (id, null, ex.Message);
		}
	}

	async Task<(string path, string? content, string? error)> GetFileContentWithErrorHandling (string projectName, string repositoryId, string path)
	{
		try {
			var content = await _azureDevOpsService.GetFileContentAsync (projectName, repositoryId, path);
			return (path, content, null);
		} catch (Exception ex) {
			_logger.LogWarning (ex, "Failed to get file content for {Path}", path);
			return (path, null, ex.Message);
		}
	}

	async Task<(string path, IEnumerable<Microsoft.TeamFoundation.SourceControl.WebApi.GitItem>? items, string? error)> ListItemsWithErrorHandling (string projectName, string repositoryId, string path)
	{
		try {
			var items = await _azureDevOpsService.GetRepositoryItemsAsync (projectName, repositoryId, path);
			return (path, items, null);
		} catch (Exception ex) {
			_logger.LogWarning (ex, "Failed to list items for {Path}", path);
			return (path, null, ex.Message);
		}
	}
}