using System.ComponentModel;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Services.Core;
using ModelContextProtocol.Server;

namespace AzureDevOps.MCP.Tools;

[McpServerToolType]
public class TestPlanTools
{
	readonly ITestService _testService;
	readonly IPerformanceService _performanceService;
	readonly ILogger<TestPlanTools> _logger;

	public TestPlanTools (
		ITestService testService,
		IPerformanceService performanceService,
		ILogger<TestPlanTools> logger)
	{
		_testService = testService;
		_performanceService = performanceService;
		_logger = logger;
	}

	[McpServerTool (Name = "get_test_plans", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets test plans for a specific Azure DevOps project")]
	public async Task<object> GetTestPlansAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("Maximum number of test plans to return (default: 20)")] int limit = 20)
	{
		using var _ = _performanceService.TrackOperation ("GetTestPlans", new Dictionary<string, object> { ["project"] = projectName, ["limit"] = limit });
		try {
			var testPlans = await _testService.GetTestPlansAsync (projectName, limit, CancellationToken.None);

			return new {
				success = true,
				projectName,
				totalCount = testPlans.Count (),
				testPlans = testPlans.Select (plan => new {
					id = plan.Id,
					name = plan.Name,
					description = plan.Description,
					state = plan.State,
					areaPath = plan.AreaPath,
					startDate = plan.StartDate,
					endDate = plan.EndDate,
					owner = plan.Owner,
					revision = plan.Revision,
					createdDate = plan.CreatedDate,
					modifiedDate = plan.ModifiedDate,
					project = new {
						id = plan.Project?.Id,
						name = plan.Project?.Name
					}
				})
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting test plans for project {ProjectName}", projectName);
			throw new InvalidOperationException ($"Failed to get test plans: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "get_test_plan", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets detailed information about a specific test plan")]
	public async Task<object> GetTestPlanAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the test plan")] int planId)
	{
		using var _ = _performanceService.TrackOperation ("GetTestPlan", new Dictionary<string, object> { ["project"] = projectName, ["planId"] = planId });
		try {
			var testPlan = await _testService.GetTestPlanAsync (projectName, planId, CancellationToken.None);

			if (testPlan == null) {
				return new {
					success = false,
					message = $"Test plan {planId} not found in project {projectName}"
				};
			}

			return new {
				success = true,
				projectName,
				testPlan = new {
					id = testPlan.Id,
					name = testPlan.Name,
					description = testPlan.Description,
					state = testPlan.State,
					areaPath = testPlan.AreaPath,
					startDate = testPlan.StartDate,
					endDate = testPlan.EndDate,
					owner = testPlan.Owner,
					revision = testPlan.Revision,
					createdDate = testPlan.CreatedDate,
					modifiedDate = testPlan.ModifiedDate,
					project = new {
						id = testPlan.Project?.Id,
						name = testPlan.Project?.Name
					}
				}
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting test plan {PlanId} for project {ProjectName}", planId, projectName);
			throw new InvalidOperationException ($"Failed to get test plan: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "get_test_suites", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets test suites for a specific test plan")]
	public async Task<object> GetTestSuitesAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the test plan")] int planId)
	{
		using var _ = _performanceService.TrackOperation ("GetTestSuites", new Dictionary<string, object> { ["project"] = projectName, ["planId"] = planId });
		try {
			var testSuites = await _testService.GetTestSuitesAsync (projectName, planId, CancellationToken.None);

			return new {
				success = true,
				projectName,
				planId,
				totalCount = testSuites.Count (),
				testSuites = testSuites.Select (suite => new {
					id = suite.Id,
					name = suite.Name,
					suiteType = suite.SuiteType,
					planId = suite.PlanId,
					parentSuiteId = suite.ParentSuiteId,
					state = suite.State,
					testCaseCount = suite.TestCaseCount,
					lastUpdated = suite.LastUpdated,
					requirementId = suite.RequirementId,
					queryString = suite.QueryString
				})
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting test suites for plan {PlanId} in project {ProjectName}", planId, projectName);
			throw new InvalidOperationException ($"Failed to get test suites: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "get_test_runs", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets test runs for a specific Azure DevOps project")]
	public async Task<object> GetTestRunsAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("Maximum number of test runs to return (default: 20)")] int limit = 20)
	{
		using var _ = _performanceService.TrackOperation ("GetTestRuns", new Dictionary<string, object> { ["project"] = projectName, ["limit"] = limit });
		try {
			var testRuns = await _testService.GetTestRunsAsync (projectName, limit: limit, cancellationToken: CancellationToken.None);

			return new {
				success = true,
				projectName,
				totalCount = testRuns.Count (),
				testRuns = testRuns.Select (run => new {
					id = run.Id,
					name = run.Name,
					state = run.State,
					planId = run.PlanId,
					totalTests = run.TotalTests,
					passedTests = run.PassedTests,
					failedTests = run.FailedTests,
					notExecutedTests = run.NotExecutedTests,
					startedDate = run.StartedDate,
					completedDate = run.CompletedDate,
					owner = run.Owner,
					buildNumber = run.BuildNumber,
					releaseUri = run.ReleaseUri,
					passPercentage = run.PassPercentage,
					comment = run.Comment,
					project = new {
						id = run.Project?.Id,
						name = run.Project?.Name
					}
				})
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting test runs for project {ProjectName}", projectName);
			throw new InvalidOperationException ($"Failed to get test runs: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "get_test_run", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets detailed information about a specific test run")]
	public async Task<object> GetTestRunAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the test run")] int runId)
	{
		using var _ = _performanceService.TrackOperation ("GetTestRun", new Dictionary<string, object> { ["project"] = projectName, ["runId"] = runId });
		try {
			var testRun = await _testService.GetTestRunAsync (projectName, runId, CancellationToken.None);

			if (testRun == null) {
				return new {
					success = false,
					message = $"Test run {runId} not found in project {projectName}"
				};
			}

			return new {
				success = true,
				projectName,
				testRun = new {
					id = testRun.Id,
					name = testRun.Name,
					state = testRun.State,
					planId = testRun.PlanId,
					totalTests = testRun.TotalTests,
					passedTests = testRun.PassedTests,
					failedTests = testRun.FailedTests,
					notExecutedTests = testRun.NotExecutedTests,
					startedDate = testRun.StartedDate,
					completedDate = testRun.CompletedDate,
					owner = testRun.Owner,
					buildNumber = testRun.BuildNumber,
					releaseUri = testRun.ReleaseUri,
					passPercentage = testRun.PassPercentage,
					comment = testRun.Comment,
					project = new {
						id = testRun.Project?.Id,
						name = testRun.Project?.Name
					}
				}
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting test run {RunId} for project {ProjectName}", runId, projectName);
			throw new InvalidOperationException ($"Failed to get test run: {ex.Message}", ex);
		}
	}

	[McpServerTool (Name = "get_test_results", ReadOnly = true, OpenWorld = false)]
	[Description ("Gets test results for a specific test run")]
	public async Task<object> GetTestResultsAsync (
		[Description ("The name of the Azure DevOps project")] string projectName,
		[Description ("The ID of the test run")] int runId)
	{
		using var _ = _performanceService.TrackOperation ("GetTestResults", new Dictionary<string, object> { ["project"] = projectName, ["runId"] = runId });
		try {
			var testResults = await _testService.GetTestResultsAsync (projectName, runId, CancellationToken.None);

			return new {
				success = true,
				projectName,
				runId,
				totalCount = testResults.Count (),
				testResults = testResults.Select (result => new {
					id = result.Id,
					testCaseId = result.TestCaseId,
					testCaseTitle = result.TestCaseTitle,
					outcome = result.Outcome,
					state = result.State,
					priority = result.Priority,
					startedDate = result.StartedDate,
					completedDate = result.CompletedDate,
					durationMs = result.Duration.TotalMilliseconds,
					runBy = result.RunBy,
					failureType = result.FailureType,
					errorMessage = result.ErrorMessage,
					stackTrace = result.StackTrace,
					computerName = result.ComputerName,
					automatedTestName = result.AutomatedTestName,
					testMethod = result.TestMethod
				})
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting test results for run {RunId} in project {ProjectName}", runId, projectName);
			throw new InvalidOperationException ($"Failed to get test results: {ex.Message}", ex);
		}
	}
}