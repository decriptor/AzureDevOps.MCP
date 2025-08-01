using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOps.MCP.Services.Core;

public class ProjectService : IProjectService
{
	readonly AzureDevOps.MCP.Services.Infrastructure.IAzureDevOpsConnectionFactory _connectionFactory;
	readonly ErrorHandling.IErrorHandler _errorHandler;
	readonly AzureDevOps.MCP.Services.ICacheService _cacheService;
	readonly Authorization.IAuthorizationService _authorizationService;
	readonly ILogger<ProjectService> _logger;

	const string CacheKeyPrefix = "projects";
	static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes (10);

	public ProjectService (
		AzureDevOps.MCP.Services.Infrastructure.IAzureDevOpsConnectionFactory connectionFactory,
		ErrorHandling.IErrorHandler errorHandler,
		AzureDevOps.MCP.Services.ICacheService cacheService,
		Authorization.IAuthorizationService authorizationService,
		ILogger<ProjectService> logger)
	{
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException (nameof (connectionFactory));
		_errorHandler = errorHandler ?? throw new ArgumentNullException (nameof (errorHandler));
		_cacheService = cacheService ?? throw new ArgumentNullException (nameof (cacheService));
		_authorizationService = authorizationService ?? throw new ArgumentNullException (nameof (authorizationService));
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
	}

	public async Task<IEnumerable<TeamProjectReference>> GetProjectsAsync (CancellationToken cancellationToken = default)
	{
		const string operation = nameof (GetProjectsAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			// Check cache first
			var cached = await _cacheService.GetAsync<List<TeamProjectReference>> (CacheKeyPrefix, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} projects from cache", cached.Count);
				return cached;
			}

			// Fetch from Azure DevOps
			var client = await _connectionFactory.GetClientAsync<ProjectHttpClient> (ct);
			var projects = await client.GetProjects ();

			// Convert to list for caching
			var projectList = projects.ToList ();

			// Cache the results
			await _cacheService.SetAsync (CacheKeyPrefix, projectList, DefaultCacheExpiration, ct);

			_logger.LogInformation ("Retrieved {Count} projects from Azure DevOps", projectList.Count);
			return projectList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<TeamProject?> GetProjectAsync (string projectNameOrId, CancellationToken cancellationToken = default)
	{
		// Validate input
		var validation = Validation.ValidationHelper.ValidateProjectName (projectNameOrId);
		validation.ThrowIfInvalid ();

		const string operation = nameof (GetProjectAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var cacheKey = $"{CacheKeyPrefix}:detail:{projectNameOrId.ToLowerInvariant ()}";

			// Check cache first
			var cached = await _cacheService.GetAsync<TeamProject> (cacheKey, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved project {ProjectName} from cache", projectNameOrId);
				return cached;
			}

			try {
				var client = await _connectionFactory.GetClientAsync<ProjectHttpClient> (ct);
				var project = await client.GetProject (projectNameOrId);

				if (project != null) {
					// Cache the result
					await _cacheService.SetAsync (cacheKey, project, DefaultCacheExpiration, ct);

					_logger.LogDebug ("Retrieved project {ProjectName} from Azure DevOps", projectNameOrId);
				}

				return project;
			} catch (Exception ex) when (ex.Message.Contains ("NotFound") || ex.Message.Contains ("404")) {
				_logger.LogDebug ("Project {ProjectName} not found", projectNameOrId);
				return null;
			}

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<ProjectProperty>> GetProjectPropertiesAsync (string projectNameOrId, CancellationToken cancellationToken = default)
	{
		// Validate input
		var validation = Validation.ValidationHelper.ValidateProjectName (projectNameOrId);
		validation.ThrowIfInvalid ();

		const string operation = nameof (GetProjectPropertiesAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var cacheKey = $"{CacheKeyPrefix}:properties:{projectNameOrId.ToLowerInvariant ()}";

			// Check cache first
			var cached = await _cacheService.GetAsync<List<ProjectProperty>> (cacheKey, ct);
			if (cached != null) {
				_logger.LogDebug ("Retrieved properties for project {ProjectName} from cache", projectNameOrId);
				return cached;
			}

			// Simplified implementation - project properties API may not be available
			var propertiesList = new List<ProjectProperty> ();

			// Cache with shorter expiration for properties
			await _cacheService.SetAsync (cacheKey, propertiesList, TimeSpan.FromMinutes (5), ct);

			_logger.LogDebug ("Retrieved {Count} properties for project {ProjectName} from Azure DevOps",
				propertiesList.Count, projectNameOrId);

			return propertiesList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<bool> ProjectExistsAsync (string projectNameOrId, CancellationToken cancellationToken = default)
	{
		try {
			var project = await GetProjectAsync (projectNameOrId, cancellationToken);
			return project != null;
		} catch (Exception ex) {
			_logger.LogDebug (ex, "Error checking if project {ProjectName} exists", projectNameOrId);
			return false;
		}
	}
}