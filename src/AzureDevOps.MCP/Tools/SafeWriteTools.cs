using System.ComponentModel;
using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace AzureDevOps.MCP.Tools;

[McpServerToolType]
public class SafeWriteTools
{
	readonly IAzureDevOpsService _azureDevOpsService;
	readonly IAuditService _auditService;
	readonly AzureDevOpsConfiguration _config;
	readonly ILogger<SafeWriteTools> _logger;

	public SafeWriteTools (
		IAzureDevOpsService azureDevOpsService,
		IAuditService auditService,
		IOptions<AzureDevOpsConfiguration> config,
		ILogger<SafeWriteTools> logger)
	{
		_azureDevOpsService = azureDevOpsService;
		_auditService = auditService;
		_config = config.Value;
		_logger = logger;
	}

	[McpServerTool (Name = "add_pull_request_comment", ReadOnly = false, OpenWorld = false)]
	[Description ("Adds a comment to a pull request. Requires 'PullRequestComments' to be enabled in configuration.")]
	public async Task<object> AddPullRequestCommentAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the repository")] string repositoryId,
		[Description ("The ID of the pull request")] int pullRequestId,
		[Description ("The comment content to add")] string content,
		[Description ("Parent comment ID for replies (optional)")] int? parentCommentId = null,
		[Description ("Set to true to confirm the operation (required when confirmations are enabled)")] bool confirm = false)
	{
		// Check if operation is enabled
		if (!_config.EnabledWriteOperations.Contains (SafeWriteOperations.PullRequestComments)) {
			throw new InvalidOperationException (
				"Pull request comments are not enabled. Add 'PullRequestComments' to EnabledWriteOperations in configuration.");
		}

		// Prepare audit entry
		var auditEntry = new WriteOperationAuditEntry {
			Operation = SafeWriteOperations.PullRequestComments,
			TargetResource = $"PR #{pullRequestId} in {repositoryId}",
			ProjectName = projectName,
			AdditionalContext = $"Comment length: {content.Length} chars",
			PersonalAccessTokenHash = AuditService.HashToken (_config.PersonalAccessToken)
		};

		try {
			// Check confirmation requirement
			if (_config.RequireConfirmation && !confirm) {
				return new {
					requiresConfirmation = true,
					operation = "AddPullRequestComment",
					details = new {
						projectName,
						repositoryId,
						pullRequestId,
						commentLength = content.Length,
						isReply = parentCommentId.HasValue
					},
					message = "This operation requires confirmation. Set 'confirm' parameter to true to proceed.",
					preview = new {
						content = content.Length > 200 ? string.Concat (content.AsSpan (0, 200), "...") : content
					}
				};
			}

			// Perform the operation
			var comment = await _azureDevOpsService.AddPullRequestCommentAsync (
				projectName, repositoryId, pullRequestId, content, parentCommentId);

			auditEntry.Success = true;

			// Log audit
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			return new {
				success = true,
				commentId = comment.Id,
				author = comment.Author?.DisplayName ?? "Unknown",
				publishedDate = comment.PublishedDate,
				content = comment.Content,
				parentCommentId = comment.ParentCommentId,
				message = "Comment added successfully"
			};
		} catch (Exception ex) {
			auditEntry.Success = false;
			auditEntry.ErrorMessage = ex.Message;

			// Log audit even for failures
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			_logger.LogError (ex, "Failed to add pull request comment");
			throw new InvalidOperationException ($"Failed to add comment: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "add_work_item_comment", ReadOnly = false, OpenWorld = false)]
	[Description ("Adds a comment to a work item. Requires 'WorkItemComments' to be enabled in configuration.")]
	public async Task<object> AddWorkItemCommentAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the work item")] int workItemId,
		[Description ("The comment content to add")] string content,
		[Description ("Set to true to confirm the operation (required when confirmations are enabled)")] bool confirm = false)
	{
		// Check if operation is enabled
		if (!_config.EnabledWriteOperations.Contains (SafeWriteOperations.WorkItemComments)) {
			throw new InvalidOperationException (
				"Work item comments are not enabled. Add 'WorkItemComments' to EnabledWriteOperations in configuration.");
		}

		// Prepare audit entry
		var auditEntry = new WriteOperationAuditEntry {
			Operation = SafeWriteOperations.WorkItemComments,
			TargetResource = $"Work Item #{workItemId}",
			ProjectName = projectName,
			AdditionalContext = $"Comment length: {content.Length} chars",
			PersonalAccessTokenHash = AuditService.HashToken (_config.PersonalAccessToken)
		};

		// Require confirmation if enabled
		if (_config.RequireConfirmation && !confirm) {
			auditEntry.Success = false;
			auditEntry.ErrorMessage = "Operation requires confirmation";
			await _auditService.LogWriteOperationAsync (auditEntry);
			throw new InvalidOperationException (
				"This operation requires confirmation. Set confirm=true to proceed.");
		}

		try {
			_logger.LogInformation ("Adding comment to work item {WorkItemId} in project {ProjectName}",
				workItemId, projectName);

			// Add the comment using Azure DevOps service
			var result = await _azureDevOpsService.AddWorkItemCommentAsync (projectName, workItemId, content);

			// Log successful operation
			auditEntry.Success = true;
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			_logger.LogInformation ("Successfully added comment to work item {WorkItemId}", workItemId);

			return new {
				success = true,
				workItemId,
				commentId = result.Id,
				message = "Comment added successfully"
			};
		} catch (Exception ex) {
			// Log failed operation
			auditEntry.Success = false;
			auditEntry.ErrorMessage = ex.Message;
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			_logger.LogError (ex, "Failed to add work item comment");
			throw new InvalidOperationException ($"Failed to add comment: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "get_audit_logs", ReadOnly = true, OpenWorld = false)]
	[Description ("Retrieves audit logs for write operations")]
	public async Task<object> GetAuditLogsAsync (
		[Description ("Get logs since this date (optional)")] DateTime? since = null)
	{
		var logs = await _auditService.GetAuditLogsAsync (since);
		return logs.Select (log => new {
			timestamp = log.Timestamp,
			operation = log.Operation,
			targetResource = log.TargetResource,
			projectName = log.ProjectName,
			success = log.Success,
			errorMessage = log.ErrorMessage,
			additionalContext = log.AdditionalContext,
			tokenHash = log.PersonalAccessTokenHash
		}).ToList ();
	}

	[McpServerTool (Name = "create_draft_pull_request", ReadOnly = false, OpenWorld = false)]
	[Description ("Creates a draft pull request. Requires 'CreateDraftPullRequest' to be enabled in configuration.")]
	public async Task<object> CreateDraftPullRequestAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the repository")] string repositoryId,
		[Description ("The source branch name (without refs/heads/)")] string sourceBranch,
		[Description ("The target branch name (without refs/heads/)")] string targetBranch,
		[Description ("The title of the pull request")] string title,
		[Description ("The description of the pull request")] string description,
		[Description ("Set to true to confirm the operation (required when confirmations are enabled)")] bool confirm = false)
	{
		// Check if operation is enabled
		if (!_config.EnabledWriteOperations.Contains (SafeWriteOperations.CreateDraftPullRequest)) {
			throw new InvalidOperationException (
				"Create draft pull request is not enabled. Add 'CreateDraftPullRequest' to EnabledWriteOperations in configuration.");
		}

		// Prepare audit entry
		var auditEntry = new WriteOperationAuditEntry {
			Operation = SafeWriteOperations.CreateDraftPullRequest,
			TargetResource = $"Draft PR from {sourceBranch} to {targetBranch} in {repositoryId}",
			ProjectName = projectName,
			AdditionalContext = $"Title: {title}",
			PersonalAccessTokenHash = AuditService.HashToken (_config.PersonalAccessToken)
		};

		try {
			// Check confirmation requirement
			if (_config.RequireConfirmation && !confirm) {
				return new {
					requiresConfirmation = true,
					operation = "CreateDraftPullRequest",
					details = new {
						projectName,
						repositoryId,
						sourceBranch,
						targetBranch,
						title,
						descriptionLength = description.Length
					},
					message = "This operation requires confirmation. Set 'confirm' parameter to true to proceed.",
					preview = new {
						action = $"Create draft PR: {sourceBranch} → {targetBranch}",
						title,
						description = description.Length > 100 ? string.Concat (description.AsSpan (0, 100), "...") : description
					}
				};
			}

			// Perform the operation
			var pullRequest = await _azureDevOpsService.CreateDraftPullRequestAsync (
				projectName, repositoryId, sourceBranch, targetBranch, title, description);

			auditEntry.Success = true;

			// Log audit
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			return new {
				success = true,
				pullRequestId = pullRequest.PullRequestId,
				title = pullRequest.Title,
				description = pullRequest.Description,
				sourceBranch = pullRequest.SourceRefName,
				targetBranch = pullRequest.TargetRefName,
				isDraft = pullRequest.IsDraft,
				createdBy = pullRequest.CreatedBy?.DisplayName,
				creationDate = pullRequest.CreationDate,
				url = pullRequest.Url,
				message = "Draft pull request created successfully"
			};
		} catch (Exception ex) {
			auditEntry.Success = false;
			auditEntry.ErrorMessage = ex.Message;

			// Log audit even for failures
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			_logger.LogError (ex, "Failed to create draft pull request");
			throw new InvalidOperationException ($"Failed to create draft pull request: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "update_work_item_tags", ReadOnly = false, OpenWorld = false)]
	[Description ("Updates tags on a work item by adding and/or removing specified tags. Requires 'UpdateWorkItemTags' to be enabled in configuration.")]
	public async Task<object> UpdateWorkItemTagsAsync (
		[Description ("The ID of the work item to update")] int workItemId,
		[Description ("Array of tags to add (optional)")] string[] tagsToAdd = null!,
		[Description ("Array of tags to remove (optional)")] string[] tagsToRemove = null!,
		[Description ("Set to true to confirm the operation (required when confirmations are enabled)")] bool confirm = false)
	{
		// Check if operation is enabled
		if (!_config.EnabledWriteOperations.Contains (SafeWriteOperations.UpdateWorkItemTags)) {
			throw new InvalidOperationException (
				"Update work item tags is not enabled. Add 'UpdateWorkItemTags' to EnabledWriteOperations in configuration.");
		}

		// Validate input
		tagsToAdd ??= Array.Empty<string> ();
		tagsToRemove ??= Array.Empty<string> ();

		if (tagsToAdd.Length == 0 && tagsToRemove.Length == 0) {
			throw new InvalidOperationException ("At least one tag must be specified to add or remove.");
		}

		// Prepare audit entry
		var auditEntry = new WriteOperationAuditEntry {
			Operation = SafeWriteOperations.UpdateWorkItemTags,
			TargetResource = $"Work Item #{workItemId}",
			ProjectName = "Unknown", // Will be updated after getting work item
			AdditionalContext = $"Add: [{string.Join (", ", tagsToAdd)}], Remove: [{string.Join (", ", tagsToRemove)}]",
			PersonalAccessTokenHash = AuditService.HashToken (_config.PersonalAccessToken)
		};

		try {
			// Get current work item to show preview
			var currentWorkItem = await _azureDevOpsService.GetWorkItemAsync (workItemId) ?? throw new InvalidOperationException ($"Work item {workItemId} not found");

			// Update project name in audit
			auditEntry.ProjectName = currentWorkItem.Fields.TryGetValue ("System.TeamProject", out var value)
				? value?.ToString () ?? "Unknown"
				: "Unknown";

			// Get current tags for preview
			var currentTagsString = currentWorkItem.Fields.TryGetValue ("System.Tags", out var tagsValue)
				? tagsValue?.ToString () ?? ""
				: "";
			var currentTags = currentTagsString.Split (';', StringSplitOptions.RemoveEmptyEntries)
				.Select (t => t.Trim ())
				.Where (t => !string.IsNullOrEmpty (t))
				.ToArray ();

			// Check confirmation requirement
			if (_config.RequireConfirmation && !confirm) {
				return new {
					requiresConfirmation = true,
					operation = "UpdateWorkItemTags",
					details = new {
						workItemId,
						workItemTitle = currentWorkItem.Fields.TryGetValue ("System.Title", out var titleValue)
							? titleValue?.ToString ()
							: "Unknown",
						currentTags,
						tagsToAdd,
						tagsToRemove
					},
					message = "This operation requires confirmation. Set 'confirm' parameter to true to proceed.",
					preview = new {
						action = "Update work item tags",
						currentTags,
						willAdd = tagsToAdd,
						willRemove = tagsToRemove
					}
				};
			}

			// Perform the operation
			var updatedWorkItem = await _azureDevOpsService.UpdateWorkItemTagsAsync (workItemId, tagsToAdd, tagsToRemove);

			auditEntry.Success = true;

			// Log audit
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			// Get updated tags for response
			var updatedTagsString = updatedWorkItem.Fields.TryGetValue ("System.Tags", out var updatedTagsValue)
				? updatedTagsValue?.ToString () ?? ""
				: "";
			var updatedTags = updatedTagsString.Split (';', StringSplitOptions.RemoveEmptyEntries)
				.Select (t => t.Trim ())
				.Where (t => !string.IsNullOrEmpty (t))
				.ToArray ();

			return new {
				success = true,
				workItemId = updatedWorkItem.Id,
				title = updatedWorkItem.Fields.TryGetValue ("System.Title", out var finalTitleValue)
					? finalTitleValue?.ToString ()
					: "Unknown",
				previousTags = currentTags,
				updatedTags,
				tagsAdded = tagsToAdd,
				tagsRemoved = tagsToRemove,
				revision = updatedWorkItem.Rev,
				message = "Work item tags updated successfully"
			};
		} catch (Exception ex) {
			auditEntry.Success = false;
			auditEntry.ErrorMessage = ex.Message;

			// Log audit even for failures
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			_logger.LogError (ex, "Failed to update work item tags for work item {WorkItemId}", workItemId);
			throw new InvalidOperationException ($"Failed to update work item tags: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "create_work_item", ReadOnly = false, OpenWorld = false)]
	[Description ("Creates a new work item. Requires 'CreateWorkItem' to be enabled in configuration.")]
	public async Task<object> CreateWorkItemAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The type of work item to create (e.g., 'User Story', 'Task', 'Bug', 'Feature')")] string workItemType,
		[Description ("The title of the work item")] string title,
		[Description ("The description/details of the work item")] string description,
		[Description ("Optional tags to add (semicolon-separated)")] string? tags = null,
		[Description ("Set to true to confirm the operation (required when confirmations are enabled)")] bool confirm = false)
	{
		// Check if operation is enabled
		if (!_config.EnabledWriteOperations.Contains (SafeWriteOperations.CreateWorkItem)) {
			throw new InvalidOperationException (
				"Work item creation is not enabled. Add 'CreateWorkItem' to EnabledWriteOperations in configuration.");
		}

		// Prepare audit entry
		var auditEntry = new WriteOperationAuditEntry {
			Operation = SafeWriteOperations.CreateWorkItem,
			TargetResource = $"New {workItemType} in {projectName}",
			ProjectName = projectName,
			AdditionalContext = $"Title: {title}, Type: {workItemType}",
			PersonalAccessTokenHash = AuditService.HashToken (_config.PersonalAccessToken)
		};

		// Require confirmation if enabled
		if (_config.RequireConfirmation && !confirm) {
			auditEntry.Success = false;
			auditEntry.ErrorMessage = "Operation requires confirmation";
			await _auditService.LogWriteOperationAsync (auditEntry);
			throw new InvalidOperationException (
				"This operation requires confirmation. Set confirm=true to proceed.");
		}

		try {
			_logger.LogInformation ("Creating {WorkItemType} in project {ProjectName} with title {Title}",
				workItemType, projectName, title);

			// Create the work item using Azure DevOps service
			var result = await _azureDevOpsService.CreateWorkItemAsync (projectName, workItemType, title, description, tags);

			// Log successful operation
			auditEntry.Success = true;
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			_logger.LogInformation ("Successfully created work item #{WorkItemId}", result.Id);

			return new {
				success = true,
				workItemId = result.Id,
				workItemType,
				title,
				url = result.Url,
				message = "Work item created successfully"
			};
		} catch (Exception ex) {
			// Log failed operation
			auditEntry.Success = false;
			auditEntry.ErrorMessage = ex.Message;
			if (_config.EnableAuditLogging) {
				await _auditService.LogWriteOperationAsync (auditEntry);
			}

			_logger.LogError (ex, "Failed to create work item");
			throw new InvalidOperationException ($"Failed to create work item: {ex.Message}", ex);
		}
	}
}