using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps work items.
/// Follows Single Responsibility Principle - only handles work item operations.
/// </summary>
public interface IWorkItemService
{
	/// <summary>
	/// Retrieves work items from a project with optional filtering.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="limit">Maximum number of work items to return</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of work items</returns>
	Task<IEnumerable<WorkItem>> GetWorkItemsAsync (
		string projectName, int limit = 100, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves a specific work item by ID.
	/// </summary>
	/// <param name="id">The work item ID</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>The work item if found, null otherwise</returns>
	Task<WorkItem?> GetWorkItemAsync (
		int id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes a WIQL query to retrieve work items.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="wiql">The WIQL query string</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of work items matching the query</returns>
	Task<IEnumerable<WorkItem>> QueryWorkItemsAsync (
		string projectName, string wiql, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves work items by type in a project.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="workItemType">The work item type (e.g., "Bug", "User Story", "Task")</param>
	/// <param name="limit">Maximum number of work items to return</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of work items of the specified type</returns>
	Task<IEnumerable<WorkItem>> GetWorkItemsByTypeAsync (
		string projectName, string workItemType, int limit = 100, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves work items assigned to a specific user.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="assignedTo">The user identifier (email or display name)</param>
	/// <param name="limit">Maximum number of work items to return</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of work items assigned to the user</returns>
	Task<IEnumerable<WorkItem>> GetWorkItemsByAssigneeAsync (
		string projectName, string assignedTo, int limit = 100, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves the revision history of a work item.
	/// </summary>
	/// <param name="id">The work item ID</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of work item revisions</returns>
	Task<IEnumerable<WorkItem>> GetWorkItemRevisionsAsync (
		int id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a work item exists.
	/// </summary>
	/// <param name="id">The work item ID</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>True if the work item exists, false otherwise</returns>
	Task<bool> WorkItemExistsAsync (
		int id, CancellationToken cancellationToken = default);
}