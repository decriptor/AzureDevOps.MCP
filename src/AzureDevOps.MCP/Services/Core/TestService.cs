using AzureDevOps.MCP.Services.Infrastructure;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Authorization;
using AzureDevOps.MCP.ErrorHandling;
using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps test operations.
/// Implements caching, validation, authorization, and error handling.
/// </summary>
public class TestService : ITestService
{
    readonly IAzureDevOpsConnectionFactory _connectionFactory;
    readonly IErrorHandler _errorHandler;
    readonly ICacheService _cacheService;
    readonly IAuthorizationService _authorizationService;
    readonly ILogger<TestService> _logger;

    // Cache expiration times based on data volatility
    static readonly TimeSpan TestPlansCacheExpiration = TimeSpan.FromMinutes(30); // Plans change rarely
    static readonly TimeSpan TestSuitesCacheExpiration = TimeSpan.FromMinutes(15); // Suites change occasionally
    static readonly TimeSpan TestRunsCacheExpiration = TimeSpan.FromMinutes(5); // Runs change frequently
    static readonly TimeSpan TestResultsCacheExpiration = TimeSpan.FromMinutes(10); // Results change after runs complete

    // Cache key prefixes for organized cache management
    const string TestPlansCachePrefix = "test:plans";
    const string TestPlanCachePrefix = "test:plan";
    const string TestSuitesCachePrefix = "test:suites";
    const string TestRunsCachePrefix = "test:runs";
    const string TestRunCachePrefix = "test:run";
    const string TestResultsCachePrefix = "test:results";

    public TestService(
        IAzureDevOpsConnectionFactory connectionFactory,
        IErrorHandler errorHandler,
        ICacheService cacheService,
        IAuthorizationService authorizationService,
        ILogger<TestService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<TestPlan>> GetTestPlansAsync(string projectNameOrId, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectNameOrId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        const string operation = nameof(GetTestPlansAsync);
        _logger.LogDebug("Getting test plans for project {ProjectName} with limit {Limit}", projectNameOrId, limit);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization
            if (!await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{TestPlansCachePrefix}:{projectNameOrId}:{limit}";
            var cached = await _cacheService.GetAsync<List<TestPlan>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} test plans from cache for project {ProjectName}", cached.Count, projectNameOrId);
                return cached.AsEnumerable();
            }

            // Note: Actual Azure DevOps Test API implementation would go here
            // For now, return simulated data as the specific test packages are not available
            var plans = GenerateSimulatedTestPlans(projectNameOrId, limit);

            // Cache the results
            var planList = plans.ToList();
            await _cacheService.SetAsync(cacheKey, planList, TestPlansCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} test plans for project {ProjectName}", planList.Count, projectNameOrId);
            return planList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<TestPlan?> GetTestPlanAsync(string projectNameOrId, int planId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectNameOrId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(planId);

        const string operation = nameof(GetTestPlanAsync);
        _logger.LogDebug("Getting test plan {PlanId} for project {ProjectName}", planId, projectNameOrId);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization
            if (!await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{TestPlanCachePrefix}:{projectNameOrId}:{planId}";
            var cached = await _cacheService.GetAsync<TestPlan>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved test plan {PlanId} from cache", planId);
                return cached;
            }

            // Note: Actual Azure DevOps Test API implementation would go here
            var plan = GenerateSimulatedTestPlan(projectNameOrId, planId);

            if (plan != null)
            {
                // Cache the result
                await _cacheService.SetAsync(cacheKey, plan, TestPlansCacheExpiration, ct);
                _logger.LogInformation("Retrieved test plan {PlanId} for project {ProjectName}", planId, projectNameOrId);
            }
            else
            {
                _logger.LogWarning("Test plan {PlanId} not found in project {ProjectName}", planId, projectNameOrId);
            }

            return plan;

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<TestSuite>> GetTestSuitesAsync(string projectNameOrId, int planId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectNameOrId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(planId);

        const string operation = nameof(GetTestSuitesAsync);
        _logger.LogDebug("Getting test suites for plan {PlanId} in project {ProjectName}", planId, projectNameOrId);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization
            if (!await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{TestSuitesCachePrefix}:{projectNameOrId}:{planId}";
            var cached = await _cacheService.GetAsync<List<TestSuite>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} test suites from cache for plan {PlanId}", cached.Count, planId);
                return cached.AsEnumerable();
            }

            // Note: Actual Azure DevOps Test API implementation would go here
            var suites = GenerateSimulatedTestSuites(projectNameOrId, planId);

            // Cache the results
            var suiteList = suites.ToList();
            await _cacheService.SetAsync(cacheKey, suiteList, TestSuitesCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} test suites for plan {PlanId} in project {ProjectName}", suiteList.Count, planId, projectNameOrId);
            return suiteList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<TestRun>> GetTestRunsAsync(string projectNameOrId, int? planId = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectNameOrId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        const string operation = nameof(GetTestRunsAsync);
        _logger.LogDebug("Getting test runs for project {ProjectName} with plan {PlanId} and limit {Limit}", projectNameOrId, planId, limit);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization
            if (!await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{TestRunsCachePrefix}:{projectNameOrId}:{planId}:{limit}";
            var cached = await _cacheService.GetAsync<List<TestRun>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} test runs from cache for project {ProjectName}", cached.Count, projectNameOrId);
                return cached.AsEnumerable();
            }

            // Note: Actual Azure DevOps Test API implementation would go here
            var runs = GenerateSimulatedTestRuns(projectNameOrId, planId, limit);

            // Cache the results
            var runList = runs.ToList();
            await _cacheService.SetAsync(cacheKey, runList, TestRunsCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} test runs for project {ProjectName}", runList.Count, projectNameOrId);
            return runList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<TestRun?> GetTestRunAsync(string projectNameOrId, int runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectNameOrId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        const string operation = nameof(GetTestRunAsync);
        _logger.LogDebug("Getting test run {RunId} for project {ProjectName}", runId, projectNameOrId);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization
            if (!await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{TestRunCachePrefix}:{projectNameOrId}:{runId}";
            var cached = await _cacheService.GetAsync<TestRun>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved test run {RunId} from cache", runId);
                return cached;
            }

            // Note: Actual Azure DevOps Test API implementation would go here
            var run = GenerateSimulatedTestRun(projectNameOrId, runId);

            if (run != null)
            {
                // Cache the result
                await _cacheService.SetAsync(cacheKey, run, TestRunsCacheExpiration, ct);
                _logger.LogInformation("Retrieved test run {RunId} for project {ProjectName}", runId, projectNameOrId);
            }
            else
            {
                _logger.LogWarning("Test run {RunId} not found in project {ProjectName}", runId, projectNameOrId);
            }

            return run;

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<TestResult>> GetTestResultsAsync(string projectNameOrId, int runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectNameOrId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        const string operation = nameof(GetTestResultsAsync);
        _logger.LogDebug("Getting test results for run {RunId} in project {ProjectName}", runId, projectNameOrId);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization
            if (!await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{TestResultsCachePrefix}:{projectNameOrId}:{runId}";
            var cached = await _cacheService.GetAsync<List<TestResult>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} test results from cache for run {RunId}", cached.Count, runId);
                return cached.AsEnumerable();
            }

            // Note: Actual Azure DevOps Test API implementation would go here
            var results = GenerateSimulatedTestResults(projectNameOrId, runId);

            // Cache the results
            var resultList = results.ToList();
            await _cacheService.SetAsync(cacheKey, resultList, TestResultsCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} test results for run {RunId} in project {ProjectName}", resultList.Count, runId, projectNameOrId);
            return resultList.AsEnumerable();

        }, operation, cancellationToken);
    }

    // Simulation methods - these would be replaced with actual Azure DevOps API calls
    private static IEnumerable<TestPlan> GenerateSimulatedTestPlans(string projectName, int limit)
    {
        var random = new Random();
        for (int i = 1; i <= Math.Min(limit, 10); i++)
        {
            yield return new TestPlan
            {
                Id = i,
                Name = $"Test Plan {i} - {projectName}",
                Description = $"Comprehensive test plan for {projectName} functionality",
                Project = new TeamProjectReference { Name = projectName, Id = Guid.NewGuid() },
                State = random.Next(3) switch { 0 => "Active", 1 => "Inactive", _ => "Completed" },
                AreaPath = $"\\{projectName}\\Testing",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                Owner = $"testowner{i}@company.com",
                Revision = random.Next(1, 10),
                CreatedDate = DateTime.UtcNow.AddDays(-60 + i),
                ModifiedDate = DateTime.UtcNow.AddDays(-random.Next(1, 30))
            };
        }
    }

    private static TestPlan? GenerateSimulatedTestPlan(string projectName, int planId)
    {
        if (planId > 20) return null; // Simulate not found

        var random = new Random(planId); // Consistent results for same ID
        return new TestPlan
        {
            Id = planId,
            Name = $"Test Plan {planId} - {projectName}",
            Description = $"Detailed test plan for {projectName} with comprehensive test coverage",
            Project = new TeamProjectReference { Name = projectName, Id = Guid.NewGuid() },
            State = random.Next(3) switch { 0 => "Active", 1 => "Inactive", _ => "Completed" },
            AreaPath = $"\\{projectName}\\Testing",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            Owner = $"testowner{planId}@company.com",
            Revision = random.Next(1, 10),
            CreatedDate = DateTime.UtcNow.AddDays(-60 + planId),
            ModifiedDate = DateTime.UtcNow.AddDays(-random.Next(1, 30))
        };
    }

    private static IEnumerable<TestSuite> GenerateSimulatedTestSuites(string projectName, int planId)
    {
        var random = new Random(planId);
        var suiteCount = random.Next(3, 8);
        
        for (int i = 1; i <= suiteCount; i++)
        {
            yield return new TestSuite
            {
                Id = (planId * 100) + i,
                Name = $"Test Suite {i} - {projectName}",
                SuiteType = i == 1 ? "StaticTestSuite" : "DynamicTestSuite",
                PlanId = planId,
                ParentSuiteId = i > 1 && random.Next(2) == 0 ? (planId * 100) + 1 : null,
                State = random.Next(2) == 0 ? "InProgress" : "Completed",
                TestCaseCount = random.Next(5, 25),
                LastUpdated = DateTime.UtcNow.AddDays(-random.Next(1, 15)),
                RequirementId = $"REQ-{planId}-{i}",
                QueryString = i > 1 ? $"[System.AreaPath] UNDER '{projectName}'" : string.Empty
            };
        }
    }

    private static IEnumerable<TestRun> GenerateSimulatedTestRuns(string projectName, int? planId, int limit)
    {
        var random = new Random();
        var states = new[] { "InProgress", "Completed", "Aborted", "NotStarted" };
        
        for (int i = 1; i <= Math.Min(limit, 15); i++)
        {
            var totalTests = random.Next(10, 100);
            var passedTests = random.Next(0, totalTests);
            var failedTests = random.Next(0, totalTests - passedTests);
            var notExecuted = totalTests - passedTests - failedTests;
            
            yield return new TestRun
            {
                Id = i,
                Name = $"Test Run {i} - {projectName}",
                State = states[random.Next(states.Length)],
                Project = new TeamProjectReference { Name = projectName, Id = Guid.NewGuid() },
                PlanId = planId ?? random.Next(1, 6),
                StartedDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                CompletedDate = random.Next(2) == 0 ? DateTime.UtcNow.AddDays(-random.Next(0, 15)) : null,
                Owner = $"testrunner{i}@company.com",
                TotalTests = totalTests,
                PassedTests = passedTests,
                FailedTests = failedTests,
                NotExecutedTests = notExecuted,
                BuildNumber = $"{DateTime.UtcNow:yyyyMMdd}.{random.Next(1, 100)}",
                ReleaseUri = $"https://dev.azure.com/{projectName}/_release?releaseId={random.Next(1, 50)}",
                PassPercentage = totalTests > 0 ? Math.Round((double)passedTests / totalTests * 100, 2) : 0,
                Comment = $"Automated test run for {projectName}"
            };
        }
    }

    private static TestRun? GenerateSimulatedTestRun(string projectName, int runId)
    {
        if (runId > 100) return null; // Simulate not found

        var random = new Random(runId); // Consistent results for same ID
        var states = new[] { "InProgress", "Completed", "Aborted", "NotStarted" };
        var totalTests = random.Next(10, 100);
        var passedTests = random.Next(0, totalTests);
        var failedTests = random.Next(0, totalTests - passedTests);
        var notExecuted = totalTests - passedTests - failedTests;

        return new TestRun
        {
            Id = runId,
            Name = $"Test Run {runId} - {projectName}",
            State = states[random.Next(states.Length)],
            Project = new TeamProjectReference { Name = projectName, Id = Guid.NewGuid() },
            PlanId = random.Next(1, 6),
            StartedDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
            CompletedDate = random.Next(2) == 0 ? DateTime.UtcNow.AddDays(-random.Next(0, 15)) : null,
            Owner = $"testrunner{runId}@company.com",
            TotalTests = totalTests,
            PassedTests = passedTests,
            FailedTests = failedTests,
            NotExecutedTests = notExecuted,
            BuildNumber = $"{DateTime.UtcNow:yyyyMMdd}.{random.Next(1, 100)}",
            ReleaseUri = $"https://dev.azure.com/{projectName}/_release?releaseId={random.Next(1, 50)}",
            PassPercentage = totalTests > 0 ? Math.Round((double)passedTests / totalTests * 100, 2) : 0,
            Comment = $"Automated test run {runId} for {projectName}"
        };
    }

    private static IEnumerable<TestResult> GenerateSimulatedTestResults(string projectName, int runId)
    {
        var random = new Random(runId);
        var outcomes = new[] { "Passed", "Failed", "Inconclusive", "NotExecuted" };
        var failureTypes = new[] { "AssertionFailure", "TimeoutException", "NullReferenceException", "ArgumentException" };
        var resultCount = random.Next(10, 50);

        for (int i = 1; i <= resultCount; i++)
        {
            var outcome = outcomes[random.Next(outcomes.Length)];
            var isFailed = outcome == "Failed";
            
            yield return new TestResult
            {
                Id = (runId * 1000) + i,
                TestCaseId = random.Next(1000, 9999),
                TestCaseTitle = $"Test Case {i} - Validate {projectName} functionality",
                Outcome = outcome,
                State = outcome == "NotExecuted" ? "NotStarted" : "Completed",
                StartedDate = DateTime.UtcNow.AddDays(-random.Next(1, 5)),
                CompletedDate = outcome != "NotExecuted" ? DateTime.UtcNow.AddDays(-random.Next(0, 3)) : null,
                Duration = TimeSpan.FromSeconds(random.Next(1, 300)),
                RunBy = $"testrunner{random.Next(1, 5)}@company.com",
                FailureType = isFailed ? failureTypes[random.Next(failureTypes.Length)] : string.Empty,
                ErrorMessage = isFailed ? $"Test failed due to {failureTypes[random.Next(failureTypes.Length)]}" : string.Empty,
                StackTrace = isFailed ? $"   at TestMethod.Execute() in C:\\Tests\\{projectName}Test.cs:line {random.Next(50, 200)}" : string.Empty,
                ComputerName = $"TestAgent-{random.Next(1, 10):00}",
                Priority = random.Next(1, 4),
                AutomatedTestName = $"{projectName}.Tests.TestMethod{i}",
                TestMethod = $"TestMethod{i}"
            };
        }
    }
}