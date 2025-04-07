using AzureDevOps.MCP.Authorization;
using AzureDevOps.MCP.ErrorHandling;
using AzureDevOps.MCP.Services.Infrastructure;

using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps build operations.
/// Implements caching, validation, authorization, and error handling.
/// </summary>
public class BuildService : IBuildService
{
	readonly IAzureDevOpsConnectionFactory _connectionFactory;
	readonly IErrorHandler _errorHandler;
	readonly ICacheService _cacheService;
	readonly IAuthorizationService _authorizationService;
	readonly ILogger<BuildService> _logger;

	// Cache expiration times based on data volatility
	static readonly TimeSpan BuildDefinitionsCacheExpiration = TimeSpan.FromMinutes (15); // Definitions change rarely
	static readonly TimeSpan BuildsCacheExpiration = TimeSpan.FromMinutes (2); // Builds change frequently
	static readonly TimeSpan AgentPoolsCacheExpiration = TimeSpan.FromMinutes (30); // Pools change rarely

	// Cache key prefixes for organized cache management
	const string BuildDefinitionsCachePrefix = "build:definitions";
	const string BuildDefinitionCachePrefix = "build:definition";
	const string BuildsCachePrefix = "build:builds";
	const string BuildCachePrefix = "build:build";
	const string AgentPoolsCachePrefix = "build:agent-pools";
	const string AgentsCachePrefix = "build:agents";

	public BuildService (
		IAzureDevOpsConnectionFactory connectionFactory,
		IErrorHandler errorHandler,
		ICacheService cacheService,
		IAuthorizationService authorizationService,
		ILogger<BuildService> logger)
	{
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException (nameof (connectionFactory));
		_errorHandler = errorHandler ?? throw new ArgumentNullException (nameof (errorHandler));
		_cacheService = cacheService ?? throw new ArgumentNullException (nameof (cacheService));
		_authorizationService = authorizationService ?? throw new ArgumentNullException (nameof (authorizationService));
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
	}

	public async Task<IEnumerable<BuildDefinitionReference>> GetBuildDefinitionsAsync (string projectNameOrId, int limit = 100, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace (projectNameOrId);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (limit);

		const string operation = nameof (GetBuildDefinitionsAsync);
		_logger.LogDebug ("Getting build definitions for project {ProjectName} with limit {Limit}", projectNameOrId, limit);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async (ct) => {
			// Check authorization
			if (!await _authorizationService.CanAccessProjectAsync (projectNameOrId, ct)) {
				throw new UnauthorizedAccessException ($"Access denied to project '{projectNameOrId}'");
			}

			// Check cache first
			var cacheKey = $"{BuildDefinitionsCachePrefix}:{projectNameOrId}:{limit}";
			var cached = await _cacheService.GetAsync<List<BuildDefinitionReference>> (cacheKey, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} build definitions from cache for project {ProjectName}", cached.Count, projectNameOrId);
				return cached.AsEnumerable ();
			}           // NOTE: Using simulated data for demonstration purposes
						// In a production environment, replace with actual Azure DevOps Build API calls:
						// var buildClient = await _connectionFactory.GetClientAsync<BuildHttpClient>(ct);
						// var definitions = await buildClient.GetDefinitionsAsync(project: projectNameOrId, top: limit, cancellationToken: ct);
			var definitions = GenerateSimulatedBuildDefinitions (projectNameOrId, limit);

			// Cache the results
			var definitionList = definitions.ToList ();
			await _cacheService.SetAsync (cacheKey, definitionList, BuildDefinitionsCacheExpiration, ct);

			_logger.LogInformation ("Retrieved {Count} build definitions for project {ProjectName}", definitionList.Count, projectNameOrId);
			return definitionList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<BuildDefinition?> GetBuildDefinitionAsync (string projectNameOrId, int definitionId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace (projectNameOrId);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (definitionId);

		const string operation = nameof (GetBuildDefinitionAsync);
		_logger.LogDebug ("Getting build definition {DefinitionId} for project {ProjectName}", definitionId, projectNameOrId);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async (ct) => {
			// Check authorization
			if (!await _authorizationService.CanAccessProjectAsync (projectNameOrId, ct)) {
				throw new UnauthorizedAccessException ($"Access denied to project '{projectNameOrId}'");
			}

			// Check cache first
			var cacheKey = $"{BuildDefinitionCachePrefix}:{projectNameOrId}:{definitionId}";
			var cached = await _cacheService.GetAsync<BuildDefinition> (cacheKey, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved build definition {DefinitionId} from cache", definitionId);
				return cached;
			}           // NOTE: Using simulated data for demonstration purposes
						// In a production environment, replace with actual Azure DevOps Build API calls:
						// var buildClient = await _connectionFactory.GetClientAsync<BuildHttpClient>(ct);
						// var definition = await buildClient.GetDefinitionAsync(project: projectNameOrId, definitionId: definitionId, cancellationToken: ct);
			var definition = GenerateSimulatedBuildDefinition (projectNameOrId, definitionId);

			if (definition != null) {
				// Cache the result
				await _cacheService.SetAsync (cacheKey, definition, BuildDefinitionsCacheExpiration, ct);
				_logger.LogInformation ("Retrieved build definition {DefinitionId} for project {ProjectName}", definitionId, projectNameOrId);
			} else {
				_logger.LogWarning ("Build definition {DefinitionId} not found in project {ProjectName}", definitionId, projectNameOrId);
			}

			return definition;

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<Build>> GetBuildsAsync (string projectNameOrId, int? definitionId = null, int limit = 50, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace (projectNameOrId);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (limit);

		const string operation = nameof (GetBuildsAsync);
		_logger.LogDebug ("Getting builds for project {ProjectName} with definition {DefinitionId} and limit {Limit}", projectNameOrId, definitionId, limit);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async (ct) => {
			// Check authorization
			if (!await _authorizationService.CanAccessProjectAsync (projectNameOrId, ct)) {
				throw new UnauthorizedAccessException ($"Access denied to project '{projectNameOrId}'");
			}

			// Check cache first
			var cacheKey = $"{BuildsCachePrefix}:{projectNameOrId}:{definitionId}:{limit}";
			var cached = await _cacheService.GetAsync<List<Build>> (cacheKey, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} builds from cache for project {ProjectName}", cached.Count, projectNameOrId);
				return cached.AsEnumerable ();
			}

			// Note: Actual Azure DevOps Build API implementation would go here
			var builds = GenerateSimulatedBuilds (projectNameOrId, definitionId, limit);

			// Cache the results
			var buildList = builds.ToList ();
			await _cacheService.SetAsync (cacheKey, buildList, BuildsCacheExpiration, ct);

			_logger.LogInformation ("Retrieved {Count} builds for project {ProjectName}", buildList.Count, projectNameOrId);
			return buildList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<Build?> GetBuildAsync (string projectNameOrId, int buildId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace (projectNameOrId);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (buildId);

		const string operation = nameof (GetBuildAsync);
		_logger.LogDebug ("Getting build {BuildId} for project {ProjectName}", buildId, projectNameOrId);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async (ct) => {
			// Check authorization
			if (!await _authorizationService.CanAccessProjectAsync (projectNameOrId, ct)) {
				throw new UnauthorizedAccessException ($"Access denied to project '{projectNameOrId}'");
			}

			// Check cache first
			var cacheKey = $"{BuildCachePrefix}:{projectNameOrId}:{buildId}";
			var cached = await _cacheService.GetAsync<Build> (cacheKey, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved build {BuildId} from cache", buildId);
				return cached;
			}

			// Note: Actual Azure DevOps Build API implementation would go here
			var build = GenerateSimulatedBuild (projectNameOrId, buildId);

			if (build != null) {
				// Cache the result
				await _cacheService.SetAsync (cacheKey, build, BuildsCacheExpiration, ct);
				_logger.LogInformation ("Retrieved build {BuildId} for project {ProjectName}", buildId, projectNameOrId);
			} else {
				_logger.LogWarning ("Build {BuildId} not found in project {ProjectName}", buildId, projectNameOrId);
			}

			return build;

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<TaskAgentPool>> GetAgentPoolsAsync (CancellationToken cancellationToken = default)
	{
		const string operation = nameof (GetAgentPoolsAsync);
		_logger.LogDebug ("Getting agent pools");

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async (ct) => {
			// Check authorization for system-level operation
			if (!await _authorizationService.CanPerformOperationAsync ("GetAgentPools", ct)) {
				throw new UnauthorizedAccessException ("Access denied to agent pools");
			}

			// Check cache first
			const string cacheKey = AgentPoolsCachePrefix;
			var cached = await _cacheService.GetAsync<List<TaskAgentPool>> (cacheKey, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} agent pools from cache", cached.Count);
				return cached.AsEnumerable ();
			}

			// Note: Actual Azure DevOps Agent API implementation would go here
			var pools = GenerateSimulatedAgentPools ();

			// Cache the results
			var poolList = pools.ToList ();
			await _cacheService.SetAsync (cacheKey, poolList, AgentPoolsCacheExpiration, ct);

			_logger.LogInformation ("Retrieved {Count} agent pools", poolList.Count);
			return poolList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<TaskAgent>> GetAgentsAsync (int poolId, CancellationToken cancellationToken = default)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (poolId);

		const string operation = nameof (GetAgentsAsync);
		_logger.LogDebug ("Getting agents for pool {PoolId}", poolId);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async (ct) => {
			// Check authorization for system-level operation
			if (!await _authorizationService.CanPerformOperationAsync ("GetAgents", ct)) {
				throw new UnauthorizedAccessException ("Access denied to agents");
			}

			// Check cache first
			var cacheKey = $"{AgentsCachePrefix}:{poolId}";
			var cached = await _cacheService.GetAsync<List<TaskAgent>> (cacheKey, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} agents from cache for pool {PoolId}", cached.Count, poolId);
				return cached.AsEnumerable ();
			}

			// Note: Actual Azure DevOps Agent API implementation would go here
			var agents = GenerateSimulatedAgents (poolId);

			// Cache the results
			var agentList = agents.ToList ();
			await _cacheService.SetAsync (cacheKey, agentList, AgentPoolsCacheExpiration, ct);

			_logger.LogInformation ("Retrieved {Count} agents for pool {PoolId}", agentList.Count, poolId);
			return agentList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	// Simulation methods - these would be replaced with actual Azure DevOps API calls
	static IEnumerable<BuildDefinitionReference> GenerateSimulatedBuildDefinitions (string projectName, int limit)
	{
		var random = new Random ();
		for (int i = 1; i <= Math.Min (limit, 10); i++) {
			yield return new BuildDefinitionReference {
				Id = i,
				Name = $"Build-{projectName}-{i}",
				Path = $"\\Builds\\{projectName}",
				Project = new TeamProjectReference { Name = projectName, Id = Guid.NewGuid () },
				QueueStatus = random.Next (2) == 0 ? "Enabled" : "Paused",
				Type = "Build",
				Url = $"https://dev.azure.com/{projectName}/_build/definition?definitionId={i}"
			};
		}
	}

	static BuildDefinition? GenerateSimulatedBuildDefinition (string projectName, int definitionId)
	{
		if (definitionId > 10) {
			return null; // Simulate not found
		}

		return new BuildDefinition {
			Id = definitionId,
			Name = $"Build-{projectName}-{definitionId}",
			Path = $"\\Builds\\{projectName}",
			Project = new TeamProjectReference { Name = projectName, Id = Guid.NewGuid () },
			Description = $"Build definition for {projectName}",
			Repository = $"{projectName}-repo",
			DefaultBranch = "refs/heads/main",
			Variables = new Dictionary<string, object> { ["BuildConfiguration"] = "Release" },
			Steps =
			[
				new() { DisplayName = "Restore packages", Task = "DotNetCoreCLI@2", Inputs = new Dictionary<string, object> { ["command"] = "restore" } },
				new() { DisplayName = "Build solution", Task = "DotNetCoreCLI@2", Inputs = new Dictionary<string, object> { ["command"] = "build" } },
				new() { DisplayName = "Run tests", Task = "DotNetCoreCLI@2", Inputs = new Dictionary<string, object> { ["command"] = "test" } }
			],
			CreatedDate = DateTime.UtcNow.AddDays (-30),
			ModifiedDate = DateTime.UtcNow.AddDays (-7)
		};
	}

	static IEnumerable<Build> GenerateSimulatedBuilds (string projectName, int? definitionId, int limit)
	{
		var random = new Random ();
		var statuses = new[] { "InProgress", "Completed", "Cancelled" };
		var results = new[] { "Succeeded", "Failed", "PartiallySucceeded" };

		for (int i = 1; i <= Math.Min (limit, 20); i++) {
			var status = statuses[random.Next (statuses.Length)];
			yield return new Build {
				Id = i,
				BuildNumber = $"{DateTime.UtcNow:yyyyMMdd}.{i}",
				Status = status,
				Result = status == "Completed" ? results[random.Next (results.Length)] : string.Empty,
				Definition = new BuildDefinitionReference {
					Id = definitionId ?? random.Next (1, 6),
					Name = $"Build-{projectName}",
					Project = new TeamProjectReference { Name = projectName, Id = Guid.NewGuid () }
				},
				SourceBranch = "refs/heads/main",
				SourceVersion = Guid.NewGuid ().ToString ()[..8],
				StartTime = DateTime.UtcNow.AddHours (-random.Next (1, 24)),
				FinishTime = status == "Completed" ? DateTime.UtcNow.AddHours (-random.Next (0, 12)) : null,
				RequestedBy = "user@domain.com",
				Url = $"https://dev.azure.com/{projectName}/_build/results?buildId={i}"
			};
		}
	}

	static Build? GenerateSimulatedBuild (string projectName, int buildId)
	{
		if (buildId > 100) {
			return null; // Simulate not found
		}

		var random = new Random (buildId); // Consistent results for same ID
		var statuses = new[] { "InProgress", "Completed", "Cancelled" };
		var results = new[] { "Succeeded", "Failed", "PartiallySucceeded" };
		var status = statuses[random.Next (statuses.Length)];

		return new Build {
			Id = buildId,
			BuildNumber = $"{DateTime.UtcNow:yyyyMMdd}.{buildId}",
			Status = status,
			Result = status == "Completed" ? results[random.Next (results.Length)] : string.Empty,
			Definition = new BuildDefinitionReference {
				Id = random.Next (1, 6),
				Name = $"Build-{projectName}",
				Project = new TeamProjectReference { Name = projectName, Id = Guid.NewGuid () }
			},
			SourceBranch = "refs/heads/main",
			SourceVersion = Guid.NewGuid ().ToString ()[..8],
			StartTime = DateTime.UtcNow.AddHours (-random.Next (1, 24)),
			FinishTime = status == "Completed" ? DateTime.UtcNow.AddHours (-random.Next (0, 12)) : null,
			RequestedBy = "user@domain.com",
			Url = $"https://dev.azure.com/{projectName}/_build/results?buildId={buildId}"
		};
	}

	static IEnumerable<TaskAgentPool> GenerateSimulatedAgentPools ()
	{
		yield return new TaskAgentPool {
			Id = 1,
			Name = "Azure Pipelines",
			PoolType = "Hosted",
			Size = 0,
			IsHosted = true,
			CreatedOn = DateTime.UtcNow.AddYears (-1)
		};

		yield return new TaskAgentPool {
			Id = 2,
			Name = "Default",
			PoolType = "SelfHosted",
			Size = 3,
			IsHosted = false,
			CreatedOn = DateTime.UtcNow.AddMonths (-6)
		};

		yield return new TaskAgentPool {
			Id = 3,
			Name = "Production",
			PoolType = "SelfHosted",
			Size = 5,
			IsHosted = false,
			CreatedOn = DateTime.UtcNow.AddMonths (-3)
		};
	}

	static IEnumerable<TaskAgent> GenerateSimulatedAgents (int poolId)
	{
		if (poolId == 1) {
			yield break; // Hosted pool has no visible agents
		}

		var agentCount = poolId == 2 ? 3 : 5;
		for (int i = 1; i <= agentCount; i++) {
			yield return new TaskAgent {
				Id = (poolId * 10) + i,
				Name = $"Agent-{poolId}-{i:00}",
				Version = "3.230.0",
				Status = i % 4 == 0 ? "Offline" : "Online",
				Enabled = true,
				CreatedOn = DateTime.UtcNow.AddDays (-30 + i),
				OperatingSystem = i % 2 == 0 ? "Windows" : "Linux"
			};
		}
	}
}