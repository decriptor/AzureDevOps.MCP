using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Tools;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace AzureDevOps.MCP.Tests.Integration;

[TestClass]
public class McpToolsIntegrationTests
{
	ServiceProvider _serviceProvider = null!;
	Mock<IAzureDevOpsService> _mockAzureDevOpsService = null!;

	[TestInitialize]
	public void Setup ()
	{
		var services = new ServiceCollection ();

		// Add logging
		services.AddLogging (builder => builder.AddConsole ());

		// Add configuration
		var config = new AzureDevOpsConfiguration {
			OrganizationUrl = "https://dev.azure.com/test",
			PersonalAccessToken = "test-pat",
			EnabledWriteOperations =
			[
				SafeWriteOperations.CreateDraftPullRequest,
				SafeWriteOperations.UpdateWorkItemTags,
				SafeWriteOperations.PullRequestComments
			],
			RequireConfirmation = true,
			EnableAuditLogging = true
		};

		services.Configure<AzureDevOpsConfiguration> (options => {
			options.OrganizationUrl = config.OrganizationUrl;
			options.PersonalAccessToken = config.PersonalAccessToken;
			options.EnabledWriteOperations = config.EnabledWriteOperations;
			options.RequireConfirmation = config.RequireConfirmation;
			options.EnableAuditLogging = config.EnableAuditLogging;
		});

		// Add services
		services.AddMemoryCache ();
		services.AddSingleton<ICacheService, CacheService> ();
		services.AddSingleton<IPerformanceService, PerformanceService> ();
		services.AddSingleton<IAuditService, AuditService> ();

		// Mock Azure DevOps service
		_mockAzureDevOpsService = new Mock<IAzureDevOpsService> ();
		services.AddSingleton (_mockAzureDevOpsService.Object);

		// Add tools
		services.AddSingleton<AzureDevOpsTools> ();
		services.AddSingleton<SafeWriteTools> ();
		services.AddSingleton<BatchTools> ();
		services.AddSingleton<PerformanceTools> ();

		_serviceProvider = services.BuildServiceProvider ();
	}

	[TestCleanup]
	public void Cleanup ()
	{
		_serviceProvider?.Dispose ();
	}
	[TestMethod]
	public void AzureDevOpsTools_DependencyInjection_ResolvesCorrectly ()
	{
		// Act
		var tools = _serviceProvider.GetRequiredService<AzureDevOpsTools> ();

		// Assert
		tools.Should ().NotBeNull ();
		tools.Should ().BeOfType<AzureDevOpsTools> ();
	}
	[TestMethod]
	public void SafeWriteTools_DependencyInjection_ResolvesCorrectly ()
	{
		// Act
		var tools = _serviceProvider.GetRequiredService<SafeWriteTools> ();

		// Assert
		tools.Should ().NotBeNull ();
		tools.Should ().BeOfType<SafeWriteTools> ();
	}
	[TestMethod]
	public void BatchTools_DependencyInjection_ResolvesCorrectly ()
	{
		// Act
		var tools = _serviceProvider.GetRequiredService<BatchTools> ();

		// Assert
		tools.Should ().NotBeNull ();
		tools.Should ().BeOfType<BatchTools> ();
	}
	[TestMethod]
	public void PerformanceTools_DependencyInjection_ResolvesCorrectly ()
	{
		// Act
		var tools = _serviceProvider.GetRequiredService<PerformanceTools> ();

		// Assert
		tools.Should ().NotBeNull ();
		tools.Should ().BeOfType<PerformanceTools> ();
	}
	[TestMethod]
	public void AllServices_DependencyInjection_ResolvesCorrectly ()
	{
		// Act & Assert
		var cacheService = _serviceProvider.GetRequiredService<ICacheService> ();
		cacheService.Should ().NotBeNull ();
		cacheService.Should ().BeOfType<CacheService> ();

		var performanceService = _serviceProvider.GetRequiredService<IPerformanceService> ();
		performanceService.Should ().NotBeNull ();
		performanceService.Should ().BeOfType<PerformanceService> ();

		var auditService = _serviceProvider.GetRequiredService<IAuditService> ();
		auditService.Should ().NotBeNull ();
		auditService.Should ().BeOfType<AuditService> ();

		var azureDevOpsService = _serviceProvider.GetRequiredService<IAzureDevOpsService> ();
		azureDevOpsService.Should ().NotBeNull ();
		azureDevOpsService.Should ().Be (_mockAzureDevOpsService.Object);
	}
	[TestMethod]
	public void Configuration_BindsCorrectly ()
	{
		// Act
		var config = _serviceProvider.GetRequiredService<IOptions<AzureDevOpsConfiguration>> ().Value;

		// Assert
		config.Should ().NotBeNull ();
		config.OrganizationUrl.Should ().Be ("https://dev.azure.com/test");
		config.PersonalAccessToken.Should ().Be ("test-pat");
		config.EnabledWriteOperations.Should ().Contain (SafeWriteOperations.CreateDraftPullRequest);
		config.EnabledWriteOperations.Should ().Contain (SafeWriteOperations.UpdateWorkItemTags);
		config.EnabledWriteOperations.Should ().Contain (SafeWriteOperations.PullRequestComments);
		config.RequireConfirmation.Should ().BeTrue ();
		config.EnableAuditLogging.Should ().BeTrue ();
	}

	[TestMethod]
	public async Task SafeWriteTools_WithDisabledOperation_ThrowsExpectedException ()
	{
		// Arrange
		var tools = _serviceProvider.GetRequiredService<SafeWriteTools> ();

		// Update configuration to disable the operation
		var configOptions = _serviceProvider.GetRequiredService<IOptions<AzureDevOpsConfiguration>> ();
		configOptions.Value.EnabledWriteOperations.Clear (); // Disable all operations

		// Act & Assert
		var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException> (
			() => tools.CreateDraftPullRequestAsync (
				"TestProject", "test-repo", "feature", "main", "Test PR", "Description", true));

		exception.Message.Should ().Contain ("Create draft pull request is not enabled");
	}

	[TestMethod]
	public async Task CacheService_IntegrationWithPerformanceTracking_WorksCorrectly ()
	{
		// Arrange
		var cacheService = _serviceProvider.GetRequiredService<ICacheService> ();
		var performanceService = _serviceProvider.GetRequiredService<IPerformanceService> ();

		const string testKey = "integration_test_key";
		const string testValue = "integration_test_value";

		// Act
		using (performanceService.TrackOperation ("CacheIntegrationTest")) {
			await cacheService.SetAsync (testKey, testValue);
			var retrievedValue = await cacheService.GetAsync<string> (testKey);

			// Assert
			retrievedValue.Should ().Be (testValue);
		}

		// Verify performance was tracked
		var metrics = await performanceService.GetMetricsAsync ();
		metrics.TotalOperations.Should ().BeGreaterThan (0);
		metrics.Operations.Should ().ContainKey ("CacheIntegrationTest");
	}

	[TestMethod]
	public async Task AuditService_IntegrationWithFileSystem_WorksCorrectly ()
	{
		// Arrange
		var auditService = _serviceProvider.GetRequiredService<IAuditService> ();

		var auditEntry = new WriteOperationAuditEntry {
			Operation = "IntegrationTest",
			TargetResource = "TestResource",
			ProjectName = "TestProject",
			Success = true,
			AdditionalContext = "Integration test audit entry"
		};

		// Act
		await auditService.LogWriteOperationAsync (auditEntry);
		var logs = await auditService.GetAuditLogsAsync ();

		// Assert
		logs.Should ().NotBeEmpty ();
		logs.Should ().Contain (entry =>
			entry.Operation == "IntegrationTest" &&
			entry.TargetResource == "TestResource" &&
			entry.ProjectName == "TestProject");
	}

	[TestMethod]
	public async Task MultipleTools_ConcurrentExecution_HandledCorrectly ()
	{
		// Arrange
		var azureDevOpsTools = _serviceProvider.GetRequiredService<AzureDevOpsTools> ();
		var performanceTools = _serviceProvider.GetRequiredService<PerformanceTools> ();
		var batchTools = _serviceProvider.GetRequiredService<BatchTools> ();

		// Setup mock responses
		_mockAzureDevOpsService
			.Setup (x => x.GetWorkItemAsync (It.IsAny<int> ()))
			.ReturnsAsync (new Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem {
				Id = 1,
				Fields = new Dictionary<string, object> { ["System.Title"] = "Test" }
			});

		// Act
		var tasks = new[]
		{
			Task.Run(() => performanceTools.GetPerformanceMetricsAsync()),
			Task.Run(() => batchTools.BatchGetWorkItemsAsync([1, 2, 3])),
			Task.Run(() => performanceTools.GetPerformanceMetricsAsync())
		};

		await Task.WhenAll (tasks);

		// Assert
		// If we reach here without exceptions, concurrent execution worked
		tasks.All (t => t.IsCompletedSuccessfully).Should ().BeTrue ();
	}
}