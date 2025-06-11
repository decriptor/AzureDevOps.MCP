using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Services.Core;
using AzureDevOps.MCP.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Moq;
using FluentAssertions;
using TestResult = AzureDevOps.MCP.Services.Core.TestResult;

namespace AzureDevOps.MCP.Tests.Tools;

[TestClass]
public class TestPlanToolsTests
{
	Mock<ITestService> _mockTestService;
	Mock<IPerformanceService> _mockPerformanceService;
	Mock<ILogger<TestPlanTools>> _mockLogger;
	Mock<IDisposable> _mockTracker;
	TestPlanTools _testPlanTools;

	[TestInitialize]
	public void Setup()
	{
		_mockTestService = new Mock<ITestService>();
		_mockPerformanceService = new Mock<IPerformanceService>();
		_mockLogger = new Mock<ILogger<TestPlanTools>>();
		_mockTracker = new Mock<IDisposable>();

		_mockPerformanceService
			.Setup(x => x.TrackOperation(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
			.Returns(_mockTracker.Object);

		_testPlanTools = new TestPlanTools(
			_mockTestService.Object,
			_mockPerformanceService.Object,
			_mockLogger.Object);
	}

	[TestMethod]
	public async Task GetTestPlansAsync_ShouldReturnTestPlans_WhenServiceReturnsData()
	{
		// Arrange
		var projectName = "TestProject";
		var limit = 10;
		var testPlans = new List<TestPlan>
		{
			new TestPlan
			{
				Id = 1,
				Name = "Test Plan 1",
				Description = "Description 1",
				State = "Active",
				AreaPath = "TestProject\\Area1",
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddDays(30),
				Owner = "user1@example.com",
				Revision = 1,
				CreatedDate = DateTime.Now.AddDays(-10),
				ModifiedDate = DateTime.Now.AddDays(-1),
				Project = new TeamProjectReference { Id = Guid.NewGuid(), Name = projectName }
			},
			new TestPlan
			{
				Id = 2,
				Name = "Test Plan 2", 
				Description = "Description 2",
				State = "Inactive",
				AreaPath = "TestProject\\Area2",
				StartDate = DateTime.Now.AddDays(10),
				EndDate = DateTime.Now.AddDays(40),
				Owner = "user2@example.com",
				Revision = 2,
				CreatedDate = DateTime.Now.AddDays(-5),
				ModifiedDate = DateTime.Now,
				Project = new TeamProjectReference { Id = Guid.NewGuid(), Name = projectName }
			}
		};

		_mockTestService
			.Setup(x => x.GetTestPlansAsync(projectName, limit, It.IsAny<CancellationToken>()))
			.ReturnsAsync(testPlans);

		// Act
		var result = await _testPlanTools.GetTestPlansAsync(projectName, limit);

		// Assert
		result.Should().NotBeNull();
		var resultData = result as dynamic;
		((bool)resultData.success).Should().BeTrue();
		((string)resultData.projectName).Should().Be(projectName);
		((int)resultData.totalCount).Should().Be(2);

		_mockPerformanceService.Verify(x => x.TrackOperation("GetTestPlans", 
			It.Is<Dictionary<string, object>>(d => (string)d["project"] == projectName && (int)d["limit"] == limit)), 
			Times.Once);
	}

	[TestMethod]
	public async Task GetTestPlanAsync_ShouldReturnTestPlan_WhenPlanExists()
	{
		// Arrange
		var projectName = "TestProject";
		var planId = 123;
		var testPlan = new TestPlan
		{
			Id = planId,
			Name = "Specific Test Plan",
			Description = "Specific Description",
			State = "Active",
			AreaPath = "TestProject\\Area1",
			StartDate = DateTime.Now,
			EndDate = DateTime.Now.AddDays(30),
			Owner = "owner@example.com",
			Revision = 5,
			CreatedDate = DateTime.Now.AddDays(-20),
			ModifiedDate = DateTime.Now.AddDays(-2),
			Project = new TeamProjectReference { Id = Guid.NewGuid(), Name = projectName }
		};

		_mockTestService
			.Setup(x => x.GetTestPlanAsync(projectName, planId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(testPlan);

		// Act
		var result = await _testPlanTools.GetTestPlanAsync(projectName, planId);

		// Assert
		result.Should().NotBeNull();
		var resultData = result as dynamic;
		((bool)resultData.success).Should().BeTrue();
		((string)resultData.projectName).Should().Be(projectName);
		((int)resultData.testPlan.id).Should().Be(planId);
		((string)resultData.testPlan.name).Should().Be("Specific Test Plan");
	}

	[TestMethod]
	public async Task GetTestPlanAsync_ShouldReturnNotFound_WhenPlanDoesNotExist()
	{
		// Arrange
		var projectName = "TestProject";
		var planId = 999;

		_mockTestService
			.Setup(x => x.GetTestPlanAsync(projectName, planId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((TestPlan?)null);

		// Act
		var result = await _testPlanTools.GetTestPlanAsync(projectName, planId);

		// Assert
		result.Should().NotBeNull();
		var resultData = result as dynamic;
		((bool)resultData.success).Should().BeFalse();
		((string)resultData.message).Should().Contain($"Test plan {planId} not found");
	}

	[TestMethod]
	public async Task GetTestSuitesAsync_ShouldReturnTestSuites_WhenServiceReturnsData()
	{
		// Arrange
		var projectName = "TestProject";
		var planId = 123;
		var testSuites = new List<TestSuite>
		{
			new TestSuite
			{
				Id = 1,
				Name = "Suite 1",
				SuiteType = "StaticTestSuite",
				PlanId = planId,
				ParentSuiteId = null,
				State = "Active",
				TestCaseCount = 5,
				LastUpdated = DateTime.Now.AddDays(-1),
				RequirementId = "REQ-001",
				QueryString = ""
			},
			new TestSuite
			{
				Id = 2,
				Name = "Suite 2",
				SuiteType = "DynamicTestSuite",
				PlanId = planId,
				ParentSuiteId = 1,
				State = "Active",
				TestCaseCount = 3,
				LastUpdated = DateTime.Now,
				RequirementId = "REQ-002",
				QueryString = "[State] = 'Active'"
			}
		};

		_mockTestService
			.Setup(x => x.GetTestSuitesAsync(projectName, planId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(testSuites);

		// Act
		var result = await _testPlanTools.GetTestSuitesAsync(projectName, planId);

		// Assert
		result.Should().NotBeNull();
		var resultData = result as dynamic;
		((bool)resultData.success).Should().BeTrue();
		((string)resultData.projectName).Should().Be(projectName);
		((int)resultData.planId).Should().Be(planId);
		((int)resultData.totalCount).Should().Be(2);
	}

	[TestMethod]
	public async Task GetTestRunsAsync_ShouldReturnTestRuns_WhenServiceReturnsData()
	{
		// Arrange
		var projectName = "TestProject";
		var limit = 10;
		var testRuns = new List<TestRun>
		{
			new TestRun
			{
				Id = 1,
				Name = "Test Run 1",
				State = "Completed",
				PlanId = 123,
				TotalTests = 10,
				PassedTests = 8,
				FailedTests = 2,
				NotExecutedTests = 0,
				StartedDate = DateTime.Now.AddDays(-1),
				CompletedDate = DateTime.Now.AddHours(-2),
				Owner = "tester@example.com",
				BuildNumber = "Build-456",
				ReleaseUri = "http://release.example.com/456",
				PassPercentage = 80.0,
				Comment = "Automated test run",
				Project = new TeamProjectReference { Id = Guid.NewGuid(), Name = projectName }
			}
		};

		_mockTestService
			.Setup(x => x.GetTestRunsAsync(projectName, null, limit, It.IsAny<CancellationToken>()))
			.ReturnsAsync(testRuns);

		// Act
		var result = await _testPlanTools.GetTestRunsAsync(projectName, limit);

		// Assert
		result.Should().NotBeNull();
		var resultData = result as dynamic;
		((bool)resultData.success).Should().BeTrue();
		((string)resultData.projectName).Should().Be(projectName);
		((int)resultData.totalCount).Should().Be(1);
	}

	[TestMethod]
	public async Task GetTestRunAsync_ShouldReturnTestRun_WhenRunExists()
	{
		// Arrange
		var projectName = "TestProject";
		var runId = 456;
		var testRun = new TestRun
		{
			Id = runId,
			Name = "Specific Test Run",
			State = "InProgress",
			PlanId = 123,
			TotalTests = 15,
			PassedTests = 10,
			FailedTests = 3,
			NotExecutedTests = 2,
			StartedDate = DateTime.Now.AddHours(-3),
			CompletedDate = null,
			Owner = "runner@example.com",
			BuildNumber = "Build-789",
			ReleaseUri = "http://release.example.com/789",
			PassPercentage = 66.7,
			Comment = "Manual test run",
			Project = new TeamProjectReference { Id = Guid.NewGuid(), Name = projectName }
		};

		_mockTestService
			.Setup(x => x.GetTestRunAsync(projectName, runId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(testRun);

		// Act
		var result = await _testPlanTools.GetTestRunAsync(projectName, runId);

		// Assert
		result.Should().NotBeNull();
		var resultData = result as dynamic;
		((bool)resultData.success).Should().BeTrue();
		((string)resultData.projectName).Should().Be(projectName);
		((int)resultData.testRun.id).Should().Be(runId);
		((string)resultData.testRun.name).Should().Be("Specific Test Run");
	}

	[TestMethod]
	public async Task GetTestRunAsync_ShouldReturnNotFound_WhenRunDoesNotExist()
	{
		// Arrange
		var projectName = "TestProject";
		var runId = 999;

		_mockTestService
			.Setup(x => x.GetTestRunAsync(projectName, runId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((TestRun?)null);

		// Act
		var result = await _testPlanTools.GetTestRunAsync(projectName, runId);

		// Assert
		result.Should().NotBeNull();
		var resultData = result as dynamic;
		((bool)resultData.success).Should().BeFalse();
		((string)resultData.message).Should().Contain($"Test run {runId} not found");
	}

	[TestMethod]
	public async Task GetTestResultsAsync_ShouldReturnTestResults_WhenServiceReturnsData()
	{
		// Arrange
		var projectName = "TestProject";
		var runId = 456;
		var testResults = new List<TestResult>
		{
			new TestResult
			{
				Id = 1,
				TestCaseId = 101,
				TestCaseTitle = "Test Case 1",
				Outcome = "Passed",
				State = "Completed",
				Priority = 1,
				StartedDate = DateTime.Now.AddHours(-2),
				CompletedDate = DateTime.Now.AddHours(-1),
				Duration = TimeSpan.FromMinutes(5),
				RunBy = "tester@example.com",
				FailureType = "",
				ErrorMessage = "",
				StackTrace = "",
				ComputerName = "TEST-MACHINE-01",
				AutomatedTestName = "UnitTest.TestCase1",
				TestMethod = "TestMethod1"
			},
			new TestResult
			{
				Id = 2,
				TestCaseId = 102,
				TestCaseTitle = "Test Case 2",
				Outcome = "Failed",
				State = "Completed",
				Priority = 2,
				StartedDate = DateTime.Now.AddHours(-2),
				CompletedDate = DateTime.Now.AddHours(-1),
				Duration = TimeSpan.FromMinutes(3),
				RunBy = "tester@example.com",
				FailureType = "Assertion",
				ErrorMessage = "Expected value was not found",
				StackTrace = "at TestMethod2() line 42",
				ComputerName = "TEST-MACHINE-01",
				AutomatedTestName = "UnitTest.TestCase2",
				TestMethod = "TestMethod2"
			}
		};

		_mockTestService
			.Setup(x => x.GetTestResultsAsync(projectName, runId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(testResults);

		// Act
		var result = await _testPlanTools.GetTestResultsAsync(projectName, runId);

		// Assert
		result.Should().NotBeNull();
		var resultData = result as dynamic;
		((bool)resultData.success).Should().BeTrue();
		((string)resultData.projectName).Should().Be(projectName);
		((int)resultData.runId).Should().Be(runId);
		((int)resultData.totalCount).Should().Be(2);
	}

	[TestMethod]
	public async Task GetTestPlansAsync_ShouldThrowInvalidOperationException_WhenServiceThrows()
	{
		// Arrange
		var projectName = "TestProject";
		var limit = 10;
		var exception = new Exception("Service error");

		_mockTestService
			.Setup(x => x.GetTestPlansAsync(projectName, limit, It.IsAny<CancellationToken>()))
			.ThrowsAsync(exception);

		// Act & Assert
		var thrownException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
			() => _testPlanTools.GetTestPlansAsync(projectName, limit));

		thrownException.Message.Should().Contain("Failed to get test plans");
		thrownException.InnerException.Should().Be(exception);
	}

	[TestMethod]
	public async Task GetTestSuitesAsync_ShouldThrowInvalidOperationException_WhenServiceThrows()
	{
		// Arrange
		var projectName = "TestProject";
		var planId = 123;
		var exception = new Exception("Service error");

		_mockTestService
			.Setup(x => x.GetTestSuitesAsync(projectName, planId, It.IsAny<CancellationToken>()))
			.ThrowsAsync(exception);

		// Act & Assert
		var thrownException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
			() => _testPlanTools.GetTestSuitesAsync(projectName, planId));

		thrownException.Message.Should().Contain("Failed to get test suites");
		thrownException.InnerException.Should().Be(exception);
	}

	[TestMethod]
	public async Task GetTestResultsAsync_ShouldThrowInvalidOperationException_WhenServiceThrows()
	{
		// Arrange
		var projectName = "TestProject";
		var runId = 456;
		var exception = new Exception("Service error");

		_mockTestService
			.Setup(x => x.GetTestResultsAsync(projectName, runId, It.IsAny<CancellationToken>()))
			.ThrowsAsync(exception);

		// Act & Assert
		var thrownException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
			() => _testPlanTools.GetTestResultsAsync(projectName, runId));

		thrownException.Message.Should().Contain("Failed to get test results");
		thrownException.InnerException.Should().Be(exception);
	}
}