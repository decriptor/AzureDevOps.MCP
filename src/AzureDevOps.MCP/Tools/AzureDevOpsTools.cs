using System.ComponentModel;
using AzureDevOps.MCP.Services;
using ModelContextProtocol.Server;

namespace AzureDevOps.MCP.Tools;

[McpServerToolType]
public class AzureDevOpsTools
{
	readonly IAzureDevOpsService _azureDevOpsService;
	readonly ILogger<AzureDevOpsTools> _logger;

	public AzureDevOpsTools (IAzureDevOpsService azureDevOpsService, ILogger<AzureDevOpsTools> logger)
	{
		_azureDevOpsService = azureDevOpsService;
		_logger = logger;
	}

	[McpServerTool (Name = "list_projects", ReadOnly = true, OpenWorld = false)]
	[Description ("Lists all projects in the Azure DevOps organization")]
	public async Task<object> ListProjectsAsync ()
	{
		try {
			var projects = await _azureDevOpsService.GetProjectsAsync (); return projects.Select (p => new {
				id = p.Id,
				name = p.Name,
				description = p.Description,
				url = p.Url,
				state = p.State.ToString (),
				visibility = p.Visibility.ToString ()
			}).ToList ();
		} catch (Exception ex) {
			_logger.LogError (ex, "Error listing projects");
			throw new InvalidOperationException ($"Failed to list projects: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "list_repositories", ReadOnly = true, OpenWorld = false)]
	[Description ("Lists all repositories in a specific Azure DevOps project")]
	public async Task<object> ListRepositoriesAsync (
		[Description ("The name of the Azure DevOps project")] string projectName)
	{
		try {
			var repos = await _azureDevOpsService.GetRepositoriesAsync (projectName);
			return repos.Select (r => new {
				id = r.Id,
				name = r.Name,
				url = r.RemoteUrl,
				webUrl = r.WebUrl,
				defaultBranch = r.DefaultBranch,
				size = r.Size
			}).ToList ();
		} catch (Exception ex) {
			_logger.LogError (ex, "Error listing repositories for project {ProjectName}", projectName);
			throw new InvalidOperationException ($"Failed to list repositories for project '{projectName}': {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "list_repository_items", ReadOnly = true, OpenWorld = false)]
	[Description ("Lists files and folders in a repository path")]
	public async Task<object> ListRepositoryItemsAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the repository")] string repositoryId,
		[Description ("The path to list items from (default: root)")] string path = "/")
	{
		try {
			var items = await _azureDevOpsService.GetRepositoryItemsAsync (projectName, repositoryId, path); return items.Select (i => new {
				path = i.Path,
				isFolder = i.IsFolder,
				commitId = i.CommitId,
				url = i.Url
			}).ToList ();
		} catch (Exception ex) {
			_logger.LogError (ex, "Error listing repository items for {RepositoryId} at path {Path}", repositoryId, path);
			throw new InvalidOperationException ($"Failed to list repository items: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "get_file_content", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets the content of a specific file from a repository")]
	public async Task<object> GetFileContentAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the repository")] string repositoryId,
		[Description ("The path to the file")] string path)
	{
		try {
			var content = await _azureDevOpsService.GetFileContentAsync (projectName, repositoryId, path);
			return new {
				path,
				content,
				repositoryId,
				projectName
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting file content for {Path} in repository {RepositoryId}", path, repositoryId);
			throw new InvalidOperationException ($"Failed to get file content: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "list_work_items", ReadOnly = true, OpenWorld = false)]
	[Description ("Lists work items in a specific Azure DevOps project")]
	public async Task<object> ListWorkItemsAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("Maximum number of work items to return (default: 100)")] int limit = 100)
	{
		try {
			var workItems = await _azureDevOpsService.GetWorkItemsAsync (projectName, limit);
			return workItems.Select (wi => new {
				id = wi.Id,
				title = wi.Fields.TryGetValue ("System.Title", out var titleValue) ? titleValue?.ToString () : "No title",
				state = wi.Fields.TryGetValue ("System.State", out var stateValue) ? stateValue?.ToString () : "Unknown",
				type = wi.Fields.TryGetValue ("System.WorkItemType", out var typeValue) ? typeValue?.ToString () : "Unknown",
				assignedTo = wi.Fields.TryGetValue ("System.AssignedTo", out var assignedValue) ? assignedValue?.ToString () : "Unassigned",
				createdDate = wi.Fields.TryGetValue ("System.CreatedDate", out var createdValue) ? createdValue : null,
				changedDate = wi.Fields.TryGetValue ("System.ChangedDate", out var changedValue) ? changedValue : null,
				url = wi.Url
			}).ToList ();
		} catch (Exception ex) {
			_logger.LogError (ex, "Error listing work items for project {ProjectName}", projectName);
			throw new InvalidOperationException ($"Failed to list work items for project '{projectName}': {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "get_work_item", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets detailed information about a specific work item")]
	public async Task<object> GetWorkItemAsync (
		[Description ("The ID of the work item")] int id)
	{
		try {
			var workItem = await _azureDevOpsService.GetWorkItemAsync (id) ?? throw new InvalidOperationException ($"Work item {id} not found");
			return new {
				id = workItem.Id,
				rev = workItem.Rev,
				fields = workItem.Fields,
				relations = workItem.Relations?.Select (r => new {
					rel = r.Rel,
					url = r.Url,
					attributes = r.Attributes
				}).ToList (),
				url = workItem.Url
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting work item {Id}", id);
			throw new InvalidOperationException ($"Failed to get work item {id}: {ex.Message}", ex);
		}
	}
}
