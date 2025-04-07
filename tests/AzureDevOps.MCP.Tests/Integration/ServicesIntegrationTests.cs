using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Services.Core;
using AzureDevOps.MCP.Services.Infrastructure;

using Microsoft.Extensions.Logging;

namespace AzureDevOps.MCP.Tests.Integration;

/// <summary>
/// Integration tests for core services that test against a real Azure DevOps instance.
/// These tests require environment variables to be set:
/// - TEST_AZDO_ORGANIZATION_URL: Azure DevOps organization URL
/// - TEST_AZDO_PAT: Personal Access Token with appropriate permissions
/// - TEST_PROJECT_NAME: Name of test project (optional, defaults to TestProject)
/// </summary>
[TestClass]
public class ServicesIntegrationTests : IntegrationTestBase
{
	[ClassInitialize]
	public static void ClassInitialize (TestContext context)
	{
		_fixture = new IntegrationTestFixture ();
	}

	[ClassCleanup]
	public static async Task ClassCleanup ()
	{
		if (_fixture != null) {
			await _fixture.DisposeAsync ();
		}
	}
	[TestMethod]
	[Ignore ("Integration test - requires Azure DevOps credentials (TEST_AZDO_ORGANIZATION_URL and TEST_AZDO_PAT environment variables)")]
	public async Task ProjectService_GetProjectsAsync_ReturnsProjects ()
	{
		// Arrange
		SkipIfNotIntegrationTest ();
		var projectService = GetService<IProjectService> ();

		// Act
		var projects = await projectService.GetProjectsAsync ();

		// Assert
		Assert.IsNotNull (projects);
		var projectList = projects.ToList ();
		Assert.IsTrue (projectList.Count > 0);

		Logger.LogInformation ("Retrieved {ProjectCount} projects", projectList.Count);

		// Verify project structure
		var firstProject = projectList.First ();
		Assert.IsNotNull (firstProject.Id);
		Assert.IsFalse (string.IsNullOrEmpty (firstProject.Name));

		Logger.LogInformation ("First project: {ProjectName} ({ProjectId})", firstProject.Name, firstProject.Id);
	}
	[TestMethod]
	[Ignore ("Integration test - requires Azure DevOps credentials (TEST_AZDO_ORGANIZATION_URL and TEST_AZDO_PAT environment variables)")]
	public async Task ProjectService_GetProjectAsync_ReturnsProjectDetails ()
	{
		// Arrange
		SkipIfNotIntegrationTest ();
		var projectService = GetService<IProjectService> ();

		// First get a project to test with
		var projects = await projectService.GetProjectsAsync ();
		var testProject = projects.FirstOrDefault ();
		Skip.If (testProject == null, "No projects available for testing");

		// Act
		Assert.IsNotNull (testProject, "Test project should not be null after skip check");
		var project = await projectService.GetProjectAsync (testProject.Name);

		// Assert
		Assert.IsNotNull (project);
		Assert.AreEqual (testProject.Name, project.Name);
		Assert.AreEqual (testProject.Id, project.Id);

		Logger.LogInformation ("Retrieved project details: {ProjectName} - {Description}",
			project.Name, project.Description);
	}
	[TestMethod]
	[Ignore ("Integration test - requires Azure DevOps credentials (TEST_AZDO_ORGANIZATION_URL and TEST_AZDO_PAT environment variables)")]
	public async Task RepositoryService_GetRepositoriesAsync_ReturnsRepositories ()
	{
		// Arrange
		SkipIfNotIntegrationTest ();
		var projectService = GetService<IProjectService> ();
		var repositoryService = GetService<IRepositoryService> ();

		// Get a test project
		var projects = await projectService.GetProjectsAsync ();
		var testProject = projects.FirstOrDefault ();
		Skip.If (testProject == null, "No projects available for testing");

		// Act
		Assert.IsNotNull (testProject, "Test project should not be null after skip check");
		var repositories = await repositoryService.GetRepositoriesAsync (testProject.Name);

		// Assert
		Assert.IsNotNull (repositories);
		var repoList = repositories.ToList ();

		Logger.LogInformation ("Retrieved {RepositoryCount} repositories for project {ProjectName}",
			repoList.Count, testProject.Name);

		if (repoList.Count != 0) {
			var firstRepo = repoList.First ();
			Assert.IsNotNull (firstRepo.Id);
			Assert.IsFalse (string.IsNullOrEmpty (firstRepo.Name));
			Assert.IsNotNull (firstRepo.ProjectReference);

			Logger.LogInformation ("First repository: {RepoName} ({RepoId})", firstRepo.Name, firstRepo.Id);
		}
	}
	[TestMethod]
	[Ignore ("Integration test - requires Azure DevOps credentials (TEST_AZDO_ORGANIZATION_URL and TEST_AZDO_PAT environment variables)")]
	public async Task WorkItemService_GetWorkItemsAsync_ReturnsWorkItems ()
	{
		// Arrange
		SkipIfNotIntegrationTest ();
		var projectService = GetService<IProjectService> ();
		var workItemService = GetService<IWorkItemService> ();

		// Get a test project
		var projects = await projectService.GetProjectsAsync ();
		var testProject = projects.FirstOrDefault ();
		Skip.If (testProject == null, "No projects available for testing");

		// Act
		Assert.IsNotNull (testProject, "Test project should not be null after skip check");
		var workItems = await workItemService.GetWorkItemsAsync (testProject.Name, limit: 10);

		// Assert
		Assert.IsNotNull (workItems);
		var workItemList = workItems.ToList ();

		Logger.LogInformation ("Retrieved {WorkItemCount} work items for project {ProjectName}",
			workItemList.Count, testProject.Name);

		if (workItemList.Count != 0) {
			var firstWorkItem = workItemList.First ();
			Assert.IsNotNull (firstWorkItem.Id);
			Assert.IsTrue (firstWorkItem.Fields?.ContainsKey ("System.Title"));

			var title = firstWorkItem.Fields?["System.Title"]?.ToString ();
			Logger.LogInformation ("First work item: {WorkItemId} - {Title}", firstWorkItem.Id, title);
		}
	}

	// BuildService integration test removed - service was removed in Phase 2

	// TestService integration test removed - service was removed in Phase 2
	[TestMethod]
	[Ignore ("Integration test - requires Azure DevOps credentials (TEST_AZDO_ORGANIZATION_URL and TEST_AZDO_PAT environment variables)")]
	public async Task CacheService_SetAndGet_WorksCorrectly ()
	{
		// Arrange
		SkipIfNotIntegrationTest ();
		var cacheService = GetService<ICacheService> ();

		var testKey = $"integration-test-{Guid.NewGuid ()}";
		var testValue = new { Message = "Test Value", Timestamp = DateTime.UtcNow };

		try {
			// Act - Set value
			await cacheService.SetAsync (testKey, testValue, TimeSpan.FromMinutes (1));

			// Act - Get value
			var retrievedValue = await cacheService.GetAsync<object> (testKey);

			// Assert
			Assert.IsNotNull (retrievedValue);
			Logger.LogInformation ("Successfully cached and retrieved test value with key {TestKey}", testKey);
		} finally {
			// Cleanup
			await cacheService.RemoveAsync (testKey);
		}
	}
	[TestMethod]
	[Ignore ("Integration test - requires Azure DevOps credentials (TEST_AZDO_ORGANIZATION_URL and TEST_AZDO_PAT environment variables)")]
	public async Task ConnectionFactory_GetConnectionAsync_CreatesValidConnection ()
	{
		// Arrange
		SkipIfNotIntegrationTest ();
		var connectionFactory = GetService<IAzureDevOpsConnectionFactory> ();

		// Act
		var connection = await connectionFactory.GetConnectionAsync ();

		// Assert
		Assert.IsNotNull (connection);

		// Test authentication
		if (!connection.HasAuthenticated) {
			await connection.ConnectAsync ();
		}

		Assert.IsTrue (connection.HasAuthenticated, "Connection should be authenticated");

		Logger.LogInformation ("Successfully created and authenticated Azure DevOps connection");
	}

	// SearchService integration test removed - service was removed in Phase 2
	[TestMethod]
	[Ignore ("Integration test - requires Azure DevOps credentials (TEST_AZDO_ORGANIZATION_URL and TEST_AZDO_PAT environment variables)")]
	public async Task FullServicePipeline_ProjectToRepositoryToCommits_WorksEndToEnd ()
	{
		// Arrange
		SkipIfNotIntegrationTest ();
		var projectService = GetService<IProjectService> ();
		var repositoryService = GetService<IRepositoryService> ();

		// Act & Assert - Full pipeline

		// 1. Get projects
		var projects = await projectService.GetProjectsAsync ();
		Assert.IsTrue (projects.Any ());
		var testProject = projects.First ();

		Logger.LogInformation ("Step 1: Retrieved project {ProjectName}", testProject.Name);

		// 2. Get repositories in project
		var repositories = await repositoryService.GetRepositoriesAsync (testProject.Name);
		Assert.IsNotNull (repositories);
		var repoList = repositories.ToList ();

		Logger.LogInformation ("Step 2: Retrieved {RepositoryCount} repositories", repoList.Count);

		if (repoList.Count != 0) {
			var testRepo = repoList.First ();

			// 3. Get commits from repository
			var commits = await repositoryService.GetCommitsAsync (testProject.Name, testRepo.Id.ToString (), limit: 5);
			Assert.IsNotNull (commits);
			var commitList = commits.ToList ();

			Logger.LogInformation ("Step 3: Retrieved {CommitCount} commits from repository {RepoName}",
				commitList.Count, testRepo.Name);

			if (commitList.Count != 0) {
				var firstCommit = commitList.First ();
				Assert.IsFalse (string.IsNullOrEmpty (firstCommit.CommitId));
				Assert.IsNotNull (firstCommit.Author);

				Logger.LogInformation ("First commit: {CommitId} by {Author}",
					firstCommit.CommitId, firstCommit.Author?.Name);
			}
		}

		Logger.LogInformation ("End-to-end service pipeline completed successfully");
	}
}