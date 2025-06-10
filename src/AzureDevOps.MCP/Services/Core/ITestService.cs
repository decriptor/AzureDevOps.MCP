using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps test operations.
/// Follows Single Responsibility Principle - only handles test-related operations.
/// </summary>
public interface ITestService
{
    /// <summary>
    /// Retrieves test plans for a project.
    /// </summary>
    /// <param name="projectNameOrId">The project name or ID</param>
    /// <param name="limit">Maximum number of test plans to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test plans</returns>
    Task<IEnumerable<TestPlan>> GetTestPlansAsync(string projectNameOrId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific test plan.
    /// </summary>
    /// <param name="projectNameOrId">The project name or ID</param>
    /// <param name="planId">The test plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test plan details or null if not found</returns>
    Task<TestPlan?> GetTestPlanAsync(string projectNameOrId, int planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves test suites for a test plan.
    /// </summary>
    /// <param name="projectNameOrId">The project name or ID</param>
    /// <param name="planId">The test plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test suites</returns>
    Task<IEnumerable<TestSuite>> GetTestSuitesAsync(string projectNameOrId, int planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves test runs for a project.
    /// </summary>
    /// <param name="projectNameOrId">The project name or ID</param>
    /// <param name="planId">Optional test plan ID to filter by</param>
    /// <param name="limit">Maximum number of test runs to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test runs</returns>
    Task<IEnumerable<TestRun>> GetTestRunsAsync(string projectNameOrId, int? planId = null, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific test run.
    /// </summary>
    /// <param name="projectNameOrId">The project name or ID</param>
    /// <param name="runId">The test run ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test run details or null if not found</returns>
    Task<TestRun?> GetTestRunAsync(string projectNameOrId, int runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves test results for a test run.
    /// </summary>
    /// <param name="projectNameOrId">The project name or ID</param>
    /// <param name="runId">The test run ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test results</returns>
    Task<IEnumerable<TestResult>> GetTestResultsAsync(string projectNameOrId, int runId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Simplified test plan model.
/// </summary>
public class TestPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TeamProjectReference? Project { get; set; }
    public string State { get; set; } = string.Empty;
    public string AreaPath { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Owner { get; set; } = string.Empty;
    public int Revision { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Simplified test suite model.
/// </summary>
public class TestSuite
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SuiteType { get; set; } = string.Empty;
    public int PlanId { get; set; }
    public int? ParentSuiteId { get; set; }
    public string State { get; set; } = string.Empty;
    public int TestCaseCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public string RequirementId { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
}

/// <summary>
/// Simplified test run model.
/// </summary>
public class TestRun
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public TeamProjectReference? Project { get; set; }
    public int? PlanId { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Owner { get; set; } = string.Empty;
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int NotExecutedTests { get; set; }
    public string BuildNumber { get; set; } = string.Empty;
    public string ReleaseUri { get; set; } = string.Empty;
    public double PassPercentage { get; set; }
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// Simplified test result model.
/// </summary>
public class TestResult
{
    public int Id { get; set; }
    public int TestCaseId { get; set; }
    public string TestCaseTitle { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public TimeSpan Duration { get; set; }
    public string RunBy { get; set; } = string.Empty;
    public string FailureType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string AutomatedTestName { get; set; } = string.Empty;
    public string TestMethod { get; set; } = string.Empty;
}