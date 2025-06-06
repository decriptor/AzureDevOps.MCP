using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Moq;

namespace AzureDevOps.MCP.Tests.Tools;

[TestClass]
public class SafeWriteToolsTests
{
	Mock<IAzureDevOpsService> _mockAzureDevOpsService = null!;
	Mock<IAuditService> _mockAuditService = null!;
	Mock<ILogger<SafeWriteTools>> _mockLogger = null!;
	AzureDevOpsConfiguration _config = null!;
	SafeWriteTools _safeWriteTools = null!;

	[TestInitialize]
	public void Setup ()
	{
		_mockAzureDevOpsService = new Mock<IAzureDevOpsService> ();
		_mockAuditService = new Mock<IAuditService> ();
		_mockLogger = new Mock<ILogger<SafeWriteTools>> ();

		_config = new AzureDevOpsConfiguration {
			OrganizationUrl = "https://dev.azure.com/test",
			PersonalAccessToken = "test-pat",
			EnabledWriteOperations = new List<string>
			{
				SafeWriteOperations.CreateDraftPullRequest,
				SafeWriteOperations.UpdateWorkItemTags
			},
			RequireConfirmation = true,
			EnableAuditLogging = true
		};

		var options = Options.Create (_config);
		_safeWriteTools = new SafeWriteTools (_mockAzureDevOpsService.Object, _mockAuditService.Object, options, _mockLogger.Object);
	}

	[TestMethod]
	public async Task CreateDraftPullRequestAsync_WithoutConfirmation_ReturnsConfirmationRequired ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo-id";
		const string sourceBranch = "feature/test";
		const string targetBranch = "main";
		const string title = "Test PR";
		const string description = "Test description";

		// Act
		var result = await _safeWriteTools.CreateDraftPullRequestAsync (
			projectName, repositoryId, sourceBranch, targetBranch, title, description, confirm: false);

		// Assert
		var response = result as object;
		response.Should ().NotBeNull ();

		// Verify that the Azure DevOps service was not called
		_mockAzureDevOpsService.Verify (x => x.CreateDraftPullRequestAsync (
			It.IsAny<string> (), It.IsAny<string> (), It.IsAny<string> (),
			It.IsAny<string> (), It.IsAny<string> (), It.IsAny<string> ()), Times.Never);
	}

	[TestMethod]
	public async Task CreateDraftPullRequestAsync_WithConfirmation_CreatesPullRequest ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo-id";
		const string sourceBranch = "feature/test";
		const string targetBranch = "main";
		const string title = "Test PR";
		const string description = "Test description";

		var mockPr = new GitPullRequest {
			PullRequestId = 123,
			Title = title,
			Description = description,
			SourceRefName = $"refs/heads/{sourceBranch}",
			TargetRefName = $"refs/heads/{targetBranch}",
			IsDraft = true,
			Url = "https://dev.azure.com/test/project/_git/repo/pullrequest/123"
		};

		_mockAzureDevOpsService
			.Setup (x => x.CreateDraftPullRequestAsync (projectName, repositoryId, sourceBranch, targetBranch, title, description))
			.ReturnsAsync (mockPr);

		// Act
		var result = await _safeWriteTools.CreateDraftPullRequestAsync (
			projectName, repositoryId, sourceBranch, targetBranch, title, description, confirm: true);

		// Assert
		result.Should ().NotBeNull ();

		// Verify the Azure DevOps service was called
		_mockAzureDevOpsService.Verify (x => x.CreateDraftPullRequestAsync (
			projectName, repositoryId, sourceBranch, targetBranch, title, description), Times.Once);

		// Verify audit was logged
		_mockAuditService.Verify (x => x.LogWriteOperationAsync (
			It.Is<WriteOperationAuditEntry> (entry =>
				entry.Operation == SafeWriteOperations.CreateDraftPullRequest &&
				entry.Success)), Times.Once);
	}

	[TestMethod]
	public async Task UpdateWorkItemTagsAsync_WithoutConfirmation_ReturnsConfirmationRequired ()
	{
		// Arrange
		const int workItemId = 123;
		var tagsToAdd = new[] { "bug", "urgent" };
		var tagsToRemove = new[] { "backlog" };

		var mockWorkItem = new WorkItem {
			Id = workItemId,
			Fields = new Dictionary<string, object> {
				["System.Title"] = "Test Work Item",
				["System.TeamProject"] = "TestProject",
				["System.Tags"] = "existing; backlog"
			}
		};

		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemAsync (workItemId))
			.ReturnsAsync (mockWorkItem);

		// Act
		var result = await _safeWriteTools.UpdateWorkItemTagsAsync (
			workItemId, tagsToAdd, tagsToRemove, confirm: false);

		// Assert
		result.Should ().NotBeNull ();

		// Verify that the Azure DevOps service update was not called
		_mockAzureDevOpsService.Verify (x => x.UpdateWorkItemTagsAsync (
			It.IsAny<int> (), It.IsAny<string[]> (), It.IsAny<string[]> ()), Times.Never);
	}

	[TestMethod]
	public async Task UpdateWorkItemTagsAsync_WithConfirmation_UpdatesTags ()
	{
		// Arrange
		const int workItemId = 123;
		var tagsToAdd = new[] { "bug", "urgent" };
		var tagsToRemove = new[] { "backlog" };

		var currentWorkItem = new WorkItem {
			Id = workItemId,
			Fields = new Dictionary<string, object> {
				["System.Title"] = "Test Work Item",
				["System.TeamProject"] = "TestProject",
				["System.Tags"] = "existing; backlog"
			}
		};

		var updatedWorkItem = new WorkItem {
			Id = workItemId,
			Rev = 2,
			Fields = new Dictionary<string, object> {
				["System.Title"] = "Test Work Item",
				["System.TeamProject"] = "TestProject",
				["System.Tags"] = "existing; bug; urgent"
			}
		};

		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemAsync (workItemId))
			.ReturnsAsync (currentWorkItem);

		_mockAzureDevOpsService
			.Setup (x => x.UpdateWorkItemTagsAsync (workItemId, tagsToAdd, tagsToRemove))
			.ReturnsAsync (updatedWorkItem);

		// Act
		var result = await _safeWriteTools.UpdateWorkItemTagsAsync (
			workItemId, tagsToAdd, tagsToRemove, confirm: true);

		// Assert
		result.Should ().NotBeNull ();

		// Verify the Azure DevOps service was called
		_mockAzureDevOpsService.Verify (x => x.UpdateWorkItemTagsAsync (
			workItemId, tagsToAdd, tagsToRemove), Times.Once);

		// Verify audit was logged
		_mockAuditService.Verify (x => x.LogWriteOperationAsync (
			It.Is<WriteOperationAuditEntry> (entry =>
				entry.Operation == SafeWriteOperations.UpdateWorkItemTags &&
				entry.Success)), Times.Once);
	}

	[TestMethod]
	public async Task UpdateWorkItemTagsAsync_WithOperationDisabled_ThrowsException ()
	{
		// Arrange
		_config.EnabledWriteOperations.Remove (SafeWriteOperations.UpdateWorkItemTags);

		const int workItemId = 123;
		var tagsToAdd = new[] { "bug" };
		var tagsToRemove = Array.Empty<string> ();

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException> (
			() => _safeWriteTools.UpdateWorkItemTagsAsync (workItemId, tagsToAdd, tagsToRemove, confirm: true));

		exception.Message.Should ().Contain ("Update work item tags is not enabled");
	}

	[TestMethod]
	public async Task UpdateWorkItemTagsAsync_WithNoTagsSpecified_ThrowsException ()
	{
		// Arrange
		const int workItemId = 123;
		var tagsToAdd = Array.Empty<string> ();
		var tagsToRemove = Array.Empty<string> ();

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException> (
			() => _safeWriteTools.UpdateWorkItemTagsAsync (workItemId, tagsToAdd, tagsToRemove, confirm: true));

		exception.Message.Should ().Contain ("At least one tag must be specified");
	}
}