using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Moq;

namespace AzureDevOps.MCP.Tests.Tools;

[TestClass]
public class BatchToolsTests
{
	Mock<IAzureDevOpsService> _mockAzureDevOpsService = null!;
	Mock<IPerformanceService> _mockPerformanceService = null!;
	Mock<ILogger<BatchTools>> _mockLogger = null!;
	BatchTools _batchTools = null!;

	[TestInitialize]
	public void Setup ()
	{
		_mockAzureDevOpsService = new Mock<IAzureDevOpsService> ();
		_mockPerformanceService = new Mock<IPerformanceService> ();
		_mockLogger = new Mock<ILogger<BatchTools>> ();

		_mockPerformanceService
			.Setup (x => x.TrackOperation (It.IsAny<string> (), It.IsAny<Dictionary<string, object>> ()))
			.Returns (Mock.Of<IDisposable> ());

		_batchTools = new BatchTools (_mockAzureDevOpsService.Object, _mockPerformanceService.Object, _mockLogger.Object);
	}

	[TestMethod]
	public async Task BatchGetWorkItemsAsync_ValidIds_ReturnsWorkItems ()
	{
		// Arrange
		var workItemIds = new[] { 1, 2, 3 };
		var mockWorkItems = workItemIds.Select (id => new WorkItem {
			Id = id,
			Fields = new Dictionary<string, object> {
				["System.Title"] = $"Work Item {id}",
				["System.State"] = "Active",
				["System.WorkItemType"] = "Bug"
			},
			Url = $"https://dev.azure.com/test/_workitems/edit/{id}"
		}).ToArray ();

		for (int i = 0; i < workItemIds.Length; i++) {
			_mockAzureDevOpsService
				.Setup (x => x.GetWorkItemAsync (workItemIds[i]))
				.ReturnsAsync (mockWorkItems[i]);
		}

		// Act
		var result = await _batchTools.BatchGetWorkItemsAsync (workItemIds);

		// Assert
		result.Should ().NotBeNull ();

		// Verify all work items were requested
		foreach (var id in workItemIds) {
			_mockAzureDevOpsService.Verify (x => x.GetWorkItemAsync (id), Times.Once);
		}

		// Verify performance tracking
		_mockPerformanceService.Verify (x => x.TrackOperation (
			"BatchGetWorkItems",
			It.Is<Dictionary<string, object>> (d => d.ContainsKey ("count") && (int)d["count"] == workItemIds.Length)),
			Times.Once);
	}

	[TestMethod]
	public async Task BatchGetWorkItemsAsync_SomeWorkItemsNotFound_ReturnsPartialResults ()
	{
		// Arrange
		var workItemIds = new[] { 1, 2, 999 }; // 999 doesn't exist

		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemAsync (1))
			.ReturnsAsync (new WorkItem { Id = 1, Fields = new Dictionary<string, object> { ["System.Title"] = "Item 1" } });

		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemAsync (2))
			.ReturnsAsync (new WorkItem { Id = 2, Fields = new Dictionary<string, object> { ["System.Title"] = "Item 2" } });

		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemAsync (999))
			.ThrowsAsync (new InvalidOperationException ("Work item not found"));

		// Act
		var result = await _batchTools.BatchGetWorkItemsAsync (workItemIds);

		// Assert
		result.Should ().NotBeNull ();

		// Should handle errors gracefully and return partial results
		foreach (var id in workItemIds) {
			_mockAzureDevOpsService.Verify (x => x.GetWorkItemAsync (id), Times.Once);
		}
	}

	[TestMethod]
	public async Task BatchGetFileContentsAsync_ValidPaths_ReturnsFileContents ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo";
		var filePaths = new[] { "file1.txt", "file2.txt", "file3.txt" };

		for (int i = 0; i < filePaths.Length; i++) {
			_mockAzureDevOpsService
				.Setup (x => x.GetFileContentAsync (projectName, repositoryId, filePaths[i]))
				.ReturnsAsync ($"Content of {filePaths[i]}");
		}

		// Act
		var result = await _batchTools.BatchGetFileContentsAsync (projectName, repositoryId, filePaths);

		// Assert
		result.Should ().NotBeNull ();

		// Verify all files were requested
		foreach (var path in filePaths) {
			_mockAzureDevOpsService.Verify (x => x.GetFileContentAsync (projectName, repositoryId, path), Times.Once);
		}

		// Verify performance tracking
		_mockPerformanceService.Verify (x => x.TrackOperation (
			"BatchGetFileContents",
			It.Is<Dictionary<string, object>> (d => d.ContainsKey ("count") && (int)d["count"] == filePaths.Length)),
			Times.Once);
	}

	[TestMethod]
	public async Task BatchGetFileContentsAsync_SomeFilesNotFound_ReturnsPartialResults ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo";
		var filePaths = new[] { "existing.txt", "missing.txt" };

		_mockAzureDevOpsService
			.Setup (x => x.GetFileContentAsync (projectName, repositoryId, "existing.txt"))
			.ReturnsAsync ("File content");

		_mockAzureDevOpsService
			.Setup (x => x.GetFileContentAsync (projectName, repositoryId, "missing.txt"))
			.ThrowsAsync (new FileNotFoundException ("File not found"));

		// Act
		var result = await _batchTools.BatchGetFileContentsAsync (projectName, repositoryId, filePaths);

		// Assert
		result.Should ().NotBeNull ();

		// Should handle errors gracefully
		foreach (var path in filePaths) {
			_mockAzureDevOpsService.Verify (x => x.GetFileContentAsync (projectName, repositoryId, path), Times.Once);
		}
	}

	[TestMethod]
	public async Task BatchListRepositoryItemsAsync_ValidPaths_ReturnsItems ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo";
		var paths = new[] { "/", "/src", "/docs" };

		foreach (var path in paths) {
			var mockItems = new[]
			{
				new GitItem { Path = $"{path}/item1", IsFolder = false },
				new GitItem { Path = $"{path}/item2", IsFolder = true }
			};

			_mockAzureDevOpsService
				.Setup (x => x.GetRepositoryItemsAsync (projectName, repositoryId, path))
				.ReturnsAsync (mockItems);
		}

		// Act
		var result = await _batchTools.BatchListRepositoryItemsAsync (projectName, repositoryId, paths);

		// Assert
		result.Should ().NotBeNull ();

		// Verify all paths were requested
		foreach (var path in paths) {
			_mockAzureDevOpsService.Verify (x => x.GetRepositoryItemsAsync (projectName, repositoryId, path), Times.Once);
		}

		// Verify performance tracking
		_mockPerformanceService.Verify (x => x.TrackOperation (
			"BatchListRepositoryItems",
			It.Is<Dictionary<string, object>> (d => d.ContainsKey ("count") && (int)d["count"] == paths.Length)),
			Times.Once);
	}

	[TestMethod]
	public async Task BatchListRepositoryItemsAsync_EmptyPathArray_HandlesGracefully ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo";
		var emptyPaths = Array.Empty<string> ();

		// Act
		var result = await _batchTools.BatchListRepositoryItemsAsync (projectName, repositoryId, emptyPaths);

		// Assert
		result.Should ().NotBeNull ();

		// Should not make any service calls
		_mockAzureDevOpsService.Verify (x => x.GetRepositoryItemsAsync (
			It.IsAny<string> (), It.IsAny<string> (), It.IsAny<string> ()), Times.Never);
	}

	[TestMethod]
	public async Task BatchGetWorkItemsAsync_EmptyIdArray_HandlesGracefully ()
	{
		// Arrange
		var emptyIds = Array.Empty<int> ();

		// Act
		var result = await _batchTools.BatchGetWorkItemsAsync (emptyIds);

		// Assert
		result.Should ().NotBeNull ();

		// Should not make any service calls
		_mockAzureDevOpsService.Verify (x => x.GetWorkItemAsync (It.IsAny<int> ()), Times.Never);
	}

	[TestMethod]
	public async Task BatchGetFileContentsAsync_EmptyPathArray_HandlesGracefully ()
	{
		// Arrange
		const string projectName = "TestProject";
		const string repositoryId = "test-repo";
		var emptyPaths = Array.Empty<string> ();

		// Act
		var result = await _batchTools.BatchGetFileContentsAsync (projectName, repositoryId, emptyPaths);

		// Assert
		result.Should ().NotBeNull ();

		// Should not make any service calls
		_mockAzureDevOpsService.Verify (x => x.GetFileContentAsync (
			It.IsAny<string> (), It.IsAny<string> (), It.IsAny<string> ()), Times.Never);
	}
}