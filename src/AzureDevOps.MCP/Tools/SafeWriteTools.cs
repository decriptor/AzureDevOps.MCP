using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AzureDevOps.MCP.Tools;

[McpServerToolType]
public class SafeWriteTools
{
    private readonly IAzureDevOpsService _azureDevOpsService;
    private readonly IAuditService _auditService;
    private readonly AzureDevOpsConfiguration _config;
    private readonly ILogger<SafeWriteTools> _logger;

    public SafeWriteTools(
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

    [McpServerTool(Name = "add_pull_request_comment", ReadOnly = false, OpenWorld = false)]
    [Description("Adds a comment to a pull request. Requires 'PullRequestComments' to be enabled in configuration.")]
    public async Task<object> AddPullRequestCommentAsync(
        [Description("The name of the Azure DevOps project")] string projectName,
        [Description("The ID of the repository")] string repositoryId,
        [Description("The ID of the pull request")] int pullRequestId,
        [Description("The comment content to add")] string content,
        [Description("Parent comment ID for replies (optional)")] int? parentCommentId = null,
        [Description("Set to true to confirm the operation (required when confirmations are enabled)")] bool confirm = false)
    {
        // Check if operation is enabled
        if (!_config.EnabledWriteOperations.Contains(SafeWriteOperations.PullRequestComments))
        {
            throw new InvalidOperationException(
                "Pull request comments are not enabled. Add 'PullRequestComments' to EnabledWriteOperations in configuration.");
        }

        // Prepare audit entry
        var auditEntry = new WriteOperationAuditEntry
        {
            Operation = SafeWriteOperations.PullRequestComments,
            TargetResource = $"PR #{pullRequestId} in {repositoryId}",
            ProjectName = projectName,
            AdditionalContext = $"Comment length: {content.Length} chars",
            PersonalAccessTokenHash = AuditService.HashToken(_config.PersonalAccessToken)
        };

        try
        {
            // Check confirmation requirement
            if (_config.RequireConfirmation && !confirm)
            {
                return new
                {
                    requiresConfirmation = true,
                    operation = "AddPullRequestComment",
                    details = new
                    {
                        projectName,
                        repositoryId,
                        pullRequestId,
                        commentLength = content.Length,
                        isReply = parentCommentId.HasValue
                    },
                    message = "This operation requires confirmation. Set 'confirm' parameter to true to proceed.",
                    preview = new
                    {
                        content = content.Length > 200 ? content.Substring(0, 200) + "..." : content
                    }
                };
            }

            // Perform the operation
            var comment = await _azureDevOpsService.AddPullRequestCommentAsync(
                projectName, repositoryId, pullRequestId, content, parentCommentId);

            auditEntry.Success = true;

            // Log audit
            if (_config.EnableAuditLogging)
            {
                await _auditService.LogWriteOperationAsync(auditEntry);
            }

            return new
            {
                success = true,
                commentId = comment.Id,
                author = comment.Author?.DisplayName ?? "Unknown",
                publishedDate = comment.PublishedDate,
                content = comment.Content,
                parentCommentId = comment.ParentCommentId,
                message = "Comment added successfully"
            };
        }
        catch (Exception ex)
        {
            auditEntry.Success = false;
            auditEntry.ErrorMessage = ex.Message;

            // Log audit even for failures
            if (_config.EnableAuditLogging)
            {
                await _auditService.LogWriteOperationAsync(auditEntry);
            }

            _logger.LogError(ex, "Failed to add pull request comment");
            throw new InvalidOperationException($"Failed to add comment: {ex.Message}", ex);
        }
    }

    [McpServerTool(Name = "get_audit_logs", ReadOnly = true, OpenWorld = false)]
    [Description("Retrieves audit logs for write operations")]
    public async Task<object> GetAuditLogsAsync(
        [Description("Get logs since this date (optional)")] DateTime? since = null)
    {
        var logs = await _auditService.GetAuditLogsAsync(since);
        return logs.Select(log => new
        {
            timestamp = log.Timestamp,
            operation = log.Operation,
            targetResource = log.TargetResource,
            projectName = log.ProjectName,
            success = log.Success,
            errorMessage = log.ErrorMessage,
            additionalContext = log.AdditionalContext,
            tokenHash = log.PersonalAccessTokenHash
        }).ToList();
    }

    [McpServerTool(Name = "create_draft_pull_request", ReadOnly = false, OpenWorld = false)]
    [Description("Creates a draft pull request. Requires 'CreateDraftPullRequest' to be enabled in configuration.")]
    public async Task<object> CreateDraftPullRequestAsync(
        [Description("The name of the Azure DevOps project")] string projectName,
        [Description("The ID of the repository")] string repositoryId,
        [Description("The source branch name (without refs/heads/)")] string sourceBranch,
        [Description("The target branch name (without refs/heads/)")] string targetBranch,
        [Description("The title of the pull request")] string title,
        [Description("The description of the pull request")] string description,
        [Description("Set to true to confirm the operation (required when confirmations are enabled)")] bool confirm = false)
    {
        // Check if operation is enabled
        if (!_config.EnabledWriteOperations.Contains(SafeWriteOperations.CreateDraftPullRequest))
        {
            throw new InvalidOperationException(
                "Create draft pull request is not enabled. Add 'CreateDraftPullRequest' to EnabledWriteOperations in configuration.");
        }

        // Prepare audit entry
        var auditEntry = new WriteOperationAuditEntry
        {
            Operation = SafeWriteOperations.CreateDraftPullRequest,
            TargetResource = $"Draft PR from {sourceBranch} to {targetBranch} in {repositoryId}",
            ProjectName = projectName,
            AdditionalContext = $"Title: {title}",
            PersonalAccessTokenHash = AuditService.HashToken(_config.PersonalAccessToken)
        };

        try
        {
            // Check confirmation requirement
            if (_config.RequireConfirmation && !confirm)
            {
                return new
                {
                    requiresConfirmation = true,
                    operation = "CreateDraftPullRequest",
                    details = new
                    {
                        projectName,
                        repositoryId,
                        sourceBranch,
                        targetBranch,
                        title,
                        descriptionLength = description.Length
                    },
                    message = "This operation requires confirmation. Set 'confirm' parameter to true to proceed.",
                    preview = new
                    {
                        action = $"Create draft PR: {sourceBranch} → {targetBranch}",
                        title,
                        description = description.Length > 100 ? description.Substring(0, 100) + "..." : description
                    }
                };
            }

            // Perform the operation
            var pullRequest = await _azureDevOpsService.CreateDraftPullRequestAsync(
                projectName, repositoryId, sourceBranch, targetBranch, title, description);

            auditEntry.Success = true;

            // Log audit
            if (_config.EnableAuditLogging)
            {
                await _auditService.LogWriteOperationAsync(auditEntry);
            }

            return new
            {
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
        }
        catch (Exception ex)
        {
            auditEntry.Success = false;
            auditEntry.ErrorMessage = ex.Message;

            // Log audit even for failures
            if (_config.EnableAuditLogging)
            {
                await _auditService.LogWriteOperationAsync(auditEntry);
            }

            _logger.LogError(ex, "Failed to create draft pull request");
            throw new InvalidOperationException($"Failed to create draft pull request: {ex.Message}", ex);
        }
    }

    [McpServerTool(Name = "update_work_item_tags", ReadOnly = false, OpenWorld = false)]
    [Description("Updates tags on a work item by adding and/or removing specified tags. Requires 'UpdateWorkItemTags' to be enabled in configuration.")]
    public async Task<object> UpdateWorkItemTagsAsync(
        [Description("The ID of the work item to update")] int workItemId,
        [Description("Array of tags to add (optional)")] string[] tagsToAdd = null!,
        [Description("Array of tags to remove (optional)")] string[] tagsToRemove = null!,
        [Description("Set to true to confirm the operation (required when confirmations are enabled)")] bool confirm = false)
    {
        // Check if operation is enabled
        if (!_config.EnabledWriteOperations.Contains(SafeWriteOperations.UpdateWorkItemTags))
        {
            throw new InvalidOperationException(
                "Update work item tags is not enabled. Add 'UpdateWorkItemTags' to EnabledWriteOperations in configuration.");
        }

        // Validate input
        tagsToAdd ??= Array.Empty<string>();
        tagsToRemove ??= Array.Empty<string>();

        if (tagsToAdd.Length == 0 && tagsToRemove.Length == 0)
        {
            throw new InvalidOperationException("At least one tag must be specified to add or remove.");
        }

        // Prepare audit entry
        var auditEntry = new WriteOperationAuditEntry
        {
            Operation = SafeWriteOperations.UpdateWorkItemTags,
            TargetResource = $"Work Item #{workItemId}",
            ProjectName = "Unknown", // Will be updated after getting work item
            AdditionalContext = $"Add: [{string.Join(", ", tagsToAdd)}], Remove: [{string.Join(", ", tagsToRemove)}]",
            PersonalAccessTokenHash = AuditService.HashToken(_config.PersonalAccessToken)
        };

        try
        {
            // Get current work item to show preview
            var currentWorkItem = await _azureDevOpsService.GetWorkItemAsync(workItemId);
            if (currentWorkItem == null)
            {
                throw new InvalidOperationException($"Work item {workItemId} not found");
            }

            // Update project name in audit
            auditEntry.ProjectName = currentWorkItem.Fields.ContainsKey("System.TeamProject") 
                ? currentWorkItem.Fields["System.TeamProject"]?.ToString() ?? "Unknown"
                : "Unknown";

            // Get current tags for preview
            var currentTagsString = currentWorkItem.Fields.ContainsKey("System.Tags") 
                ? currentWorkItem.Fields["System.Tags"]?.ToString() ?? ""
                : "";
            var currentTags = currentTagsString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            // Check confirmation requirement
            if (_config.RequireConfirmation && !confirm)
            {
                return new
                {
                    requiresConfirmation = true,
                    operation = "UpdateWorkItemTags",
                    details = new
                    {
                        workItemId,
                        workItemTitle = currentWorkItem.Fields.ContainsKey("System.Title") 
                            ? currentWorkItem.Fields["System.Title"]?.ToString() 
                            : "Unknown",
                        currentTags,
                        tagsToAdd,
                        tagsToRemove
                    },
                    message = "This operation requires confirmation. Set 'confirm' parameter to true to proceed.",
                    preview = new
                    {
                        action = "Update work item tags",
                        currentTags,
                        willAdd = tagsToAdd,
                        willRemove = tagsToRemove
                    }
                };
            }

            // Perform the operation
            var updatedWorkItem = await _azureDevOpsService.UpdateWorkItemTagsAsync(workItemId, tagsToAdd, tagsToRemove);

            auditEntry.Success = true;

            // Log audit
            if (_config.EnableAuditLogging)
            {
                await _auditService.LogWriteOperationAsync(auditEntry);
            }

            // Get updated tags for response
            var updatedTagsString = updatedWorkItem.Fields.ContainsKey("System.Tags") 
                ? updatedWorkItem.Fields["System.Tags"]?.ToString() ?? ""
                : "";
            var updatedTags = updatedTagsString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            return new
            {
                success = true,
                workItemId = updatedWorkItem.Id,
                title = updatedWorkItem.Fields.ContainsKey("System.Title") 
                    ? updatedWorkItem.Fields["System.Title"]?.ToString() 
                    : "Unknown",
                previousTags = currentTags,
                updatedTags = updatedTags,
                tagsAdded = tagsToAdd,
                tagsRemoved = tagsToRemove,
                revision = updatedWorkItem.Rev,
                message = "Work item tags updated successfully"
            };
        }
        catch (Exception ex)
        {
            auditEntry.Success = false;
            auditEntry.ErrorMessage = ex.Message;

            // Log audit even for failures
            if (_config.EnableAuditLogging)
            {
                await _auditService.LogWriteOperationAsync(auditEntry);
            }

            _logger.LogError(ex, "Failed to update work item tags for work item {WorkItemId}", workItemId);
            throw new InvalidOperationException($"Failed to update work item tags: {ex.Message}", ex);
        }
    }
}