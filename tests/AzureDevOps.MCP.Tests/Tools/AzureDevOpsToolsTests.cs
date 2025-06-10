using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Moq;

namespace AzureDevOps.MCP.Tests.Tools;

[TestClass]
public class AzureDevOpsToolsTests
{
	Mock<IAzureDevOpsService> _mockAzureDevOpsService = null!;
	Mock<ILogger<AzureDevOpsTools>> _mockLogger = null!;
	AzureDevOpsTools _azureDevOpsTools = null!;

	[TestInitialize]
	public void Setup ()
	{
		_mockAzureDevOpsService = new Mock<IAzureDevOpsService> ();
		_mockLogger = new Mock<ILogger<AzureDevOpsTools>> ();
		_azureDevOpsTools = new AzureDevOpsTools (_mockAzureDevOpsService.Object, _mockLogger.Object);
	}

	[TestMethod]
	public async Task ListProjectsAsync_ReturnsProjects ()
	{
		// Arrange
		var mockProjects = new[]
		{
			new TeamProjectReference
			{
				Id = Guid.NewGuid(),
				Name = "Project1",
				Description = "Test Project 1",
				Url = "https://dev.azure.com/test/Project1",
				State = ProjectState.WellFormed,
				Visibility = ProjectVisibility.Private
			},
			new TeamProjectReference
			{
				Id = Guid.NewGuid(),
				Name = "Project2",
				Description = "Test Project 2",
				Url = "https://dev.azure.com/test/Project2",
				State = ProjectState.WellFormed,
				Visibility = ProjectVisibility.Public
			}
		};

		_mockAzureDevOpsService
			.Setup (x => x.GetProjectsAsync ())
			.ReturnsAsync (mockProjects);

		// Act
		var result = await _azureDevOpsTools.ListProjectsAsync ();

		// Assert
		result.Should ().NotBeNull ();
		_mockAzureDevOpsService.Verify (x => x.GetProjectsAsync (), Times.Once);
	}

	[TestMethod]
	public async Task ListProjectsAsync_ServiceThrowsException_ThrowsInvalidOperationException ()
	{
		// Arrange
		_mockAzureDevOpsService
			.Setup (x => x.GetProjectsAsync ())
			.ThrowsAsync (new UnauthorizedAccessException ("Access denied"));

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException> (
			() => _azureDevOpsTools.ListProjectsAsync ());

		exception.Message.Should ().Contain ("Failed to list projects");
		exception.InnerException.Should ().BeOfType<UnauthorizedAccessException> ();
	}

	[TestMethod]
	public async Task ListRepositoriesAsync_ValidProject_ReturnsRepositories ()
	{
		// Arrange
		const string projectName = "TestProject";
		var mockRepositories = new[]
		{
			new GitRepository
			{
				Id = Guid.NewGuid(),
				Name = "Repo1",
				RemoteUrl = "https://dev.azure.com/test/TestProject/_git/Repo1",
				WebUrl = "https://dev.azure.com/test/TestProject/_git/Repo1",
				DefaultBranch = "refs/heads/main",
				Size = 1024
			},
			new GitRepository
			{
				Id = Guid.NewGuid(),
				Name = "Repo2",
				RemoteUrl = "https://dev.azure.com/test/TestProject/_git/Repo2",
				WebUrl = "https://dev.azure.com/test/TestProject/_git/Repo2",
				DefaultBranch = "refs/heads/master",
				Size = 2048
			}
		};

		_mockAzureDevOpsService
			.Setup (x => x.GetRepositoriesAsync (projectName))
			.ReturnsAsync (mockRepositories);

		// Act
		var result = await _azureDevOpsTools.ListRepositoriesAsync (projectName);

		// Assert
		result.Should ().NotBeNull ();
		_mockAzureDevOpsService.Verify (x => x.GetRepositoriesAsync (projectName), Times.Once);
	}

	[TestMethod]
	public async Task ListRepositoryItemsAsync_ValidParameters_ReturnsItems ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo-id";
		const string path = "/src";

		var mockItems = new[]
		{
			new GitItem
			{
				Path = "/src/file1.cs",
				IsFolder = false,
				CommitId = "abc123",
				Url = "https://dev.azure.com/test/TestProject/_git/test-repo/blob/main/src/file1.cs"
			},
			new GitItem
			{
				Path = "/src/subfolder",
				IsFolder = true,
				CommitId = "abc123",
				Url = "https://dev.azure.com/test/TestProject/_git/test-repo/tree/main/src/subfolder"
			}
		};

		_mockAzureDevOpsService
			.Setup (x => x.GetRepositoryItemsAsync (projectName, repositoryId, path))
			.ReturnsAsync (mockItems);

		// Act
		var result = await _azureDevOpsTools.ListRepositoryItemsAsync (projectName, repositoryId, path);

		// Assert
		result.Should ().NotBeNull ();
		_mockAzureDevOpsService.Verify (x => x.GetRepositoryItemsAsync (projectName, repositoryId, path), Times.Once);
	}

	[TestMethod]
	public async Task GetFileContentAsync_ValidParameters_ReturnsContent ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo-id";
		const string path = "/src/Program.cs";
		const string expectedContent = "using System;\nnamespace Test { }";

		_mockAzureDevOpsService
			.Setup (x => x.GetFileContentAsync (projectName, repositoryId, path))
			.ReturnsAsync (expectedContent);

		// Act
		var result = await _azureDevOpsTools.GetFileContentAsync (projectName, repositoryId, path);

		// Assert
		result.Should ().NotBeNull ();
		_mockAzureDevOpsService.Verify (x => x.GetFileContentAsync (projectName, repositoryId, path), Times.Once);
	}

	[TestMethod]
	public async Task ListWorkItemsAsync_ValidProject_ReturnsWorkItems ()
	{
		// Arrange
		const string projectName = "TestProject";
		const int limit = 50;

		var mockWorkItems = new[]
		{
			new WorkItem
			{
				Id = 1,
				Fields = new Dictionary<string, object>
				{
					["System.Title"] = "Bug in login",
					["System.State"] = "Active",
					["System.WorkItemType"] = "Bug",
					["System.AssignedTo"] = "john.doe@company.com",
					["System.CreatedDate"] = DateTime.UtcNow.AddDays(-5),
					["System.ChangedDate"] = DateTime.UtcNow.AddDays(-1)
				},
				Url = "https://dev.azure.com/test/TestProject/_workitems/edit/1"
			},
			new WorkItem
			{
				Id = 2,
				Fields = new Dictionary<string, object>
				{
					["System.Title"] = "Feature request",
					["System.State"] = "New",
					["System.WorkItemType"] = "User Story",
					["System.CreatedDate"] = DateTime.UtcNow.AddDays(-3)
				},
				Url = "https://dev.azure.com/test/TestProject/_workitems/edit/2"
			}
		};

		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemsAsync (projectName, limit))
			.ReturnsAsync (mockWorkItems);

		// Act
		var result = await _azureDevOpsTools.ListWorkItemsAsync (projectName, limit);

		// Assert
		result.Should ().NotBeNull ();
		_mockAzureDevOpsService.Verify (x => x.GetWorkItemsAsync (projectName, limit), Times.Once);
	}

	[TestMethod]
	public async Task GetWorkItemAsync_ValidId_ReturnsWorkItem ()
	{
		// Arrange
		const int workItemId = 123;
		var mockWorkItem = new WorkItem {
			Id = workItemId,
			Rev = 5,
			Fields = new Dictionary<string, object> {
				["System.Title"] = "Test Work Item",
				["System.State"] = "Active",
				["System.WorkItemType"] = "Task"
			},
			Relations = new[]
			{
				new WorkItemRelation
				{
					Rel = "System.LinkTypes.Hierarchy-Forward",
					Url = "https://dev.azure.com/test/_apis/wit/workItems/124",
					Attributes = new Dictionary<string, object> { ["name"] = "Child" }
				}
			},
			Url = "https://dev.azure.com/test/TestProject/_workitems/edit/123"
		};

		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemAsync (workItemId))
			.ReturnsAsync (mockWorkItem);

		// Act
		var result = await _azureDevOpsTools.GetWorkItemAsync (workItemId);

		// Assert
		result.Should ().NotBeNull ();
		_mockAzureDevOpsService.Verify (x => x.GetWorkItemAsync (workItemId), Times.Once);
	}

	[TestMethod]
	public async Task GetWorkItemAsync_WorkItemNotFound_ThrowsInvalidOperationException ()
	{
		// Arrange
		const int workItemId = 999;

		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemAsync (workItemId))
			.ReturnsAsync ((WorkItem?)null);

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException> (
			() => _azureDevOpsTools.GetWorkItemAsync (workItemId));

		exception.Message.Should ().Contain ($"Work item {workItemId} not found");
	}

	[TestMethod]
	public async Task ListRepositoriesAsync_ServiceThrowsException_ThrowsInvalidOperationException ()
	{
		// Arrange
		const string projectName = "InvalidProject";

		_mockAzureDevOpsService
			.Setup (x => x.GetRepositoriesAsync (projectName))
			.ThrowsAsync (new ArgumentException ("Project not found"));

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException> (
			() => _azureDevOpsTools.ListRepositoriesAsync (projectName));

		exception.Message.Should ().Contain ($"Failed to list repositories for project '{projectName}'");
		exception.InnerException.Should ().BeOfType<ArgumentException> ();
	}

	[TestMethod]
	public async Task GetFileContentAsync_FileNotFound_ThrowsInvalidOperationException ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo-id";
		const string path = "/nonexistent/file.txt";

		_mockAzureDevOpsService
			.Setup (x => x.GetFileContentAsync (projectName, repositoryId, path))
			.ThrowsAsync (new FileNotFoundException ("File not found"));

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException> (
			() => _azureDevOpsTools.GetFileContentAsync (projectName, repositoryId, path));

		exception.Message.Should ().Contain ("Failed to get file content");
		exception.InnerException.Should ().BeOfType<FileNotFoundException> ();
	}
}