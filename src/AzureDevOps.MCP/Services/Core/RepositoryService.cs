using System.Text;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureDevOps.MCP.Services.Core;

public class RepositoryService : IRepositoryService
{
	readonly AzureDevOps.MCP.Services.Infrastructure.IAzureDevOpsConnectionFactory _connectionFactory;
	readonly ErrorHandling.IErrorHandler _errorHandler;
	readonly AzureDevOps.MCP.Services.ICacheService _cacheService;
	readonly ILogger<RepositoryService> _logger;

	const string CacheKeyPrefix = "repositories";
	static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes (5);
	static readonly TimeSpan FileContentCacheExpiration = TimeSpan.FromMinutes (15);

	public RepositoryService (
		AzureDevOps.MCP.Services.Infrastructure.IAzureDevOpsConnectionFactory connectionFactory,
		ErrorHandling.IErrorHandler errorHandler,
		AzureDevOps.MCP.Services.ICacheService cacheService,
		ILogger<RepositoryService> logger)
	{
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException (nameof (connectionFactory));
		_errorHandler = errorHandler ?? throw new ArgumentNullException (nameof (errorHandler));
		_cacheService = cacheService ?? throw new ArgumentNullException (nameof (cacheService));
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
	}

	public async Task<IEnumerable<GitRepository>> GetRepositoriesAsync (
		string projectName, CancellationToken cancellationToken = default)
	{
		// Validate input
		var validation = Validation.ValidationHelper.ValidateProjectName (projectName);
		validation.ThrowIfInvalid ();

		const string operation = nameof (GetRepositoriesAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var cacheKey = $"{CacheKeyPrefix}:{projectName.ToLowerInvariant ()}";

			// Check cache first
			var cached = await _cacheService.GetAsync<List<GitRepository>> (cacheKey);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} repositories for project {ProjectName} from cache",
					cached.Count, projectName);
				return cached;
			}

			// Fetch from Azure DevOps
			var client = await _connectionFactory.GetClientAsync<GitHttpClient> (ct);
			var repositories = await client.GetRepositoriesAsync (projectName, cancellationToken: ct);

			// Convert to list for caching
			var repositoriesList = repositories.ToList ();

			// Cache the results
			await _cacheService.SetAsync (cacheKey, repositoriesList, DefaultCacheExpiration);

			_logger.LogInformation ("Retrieved {Count} repositories for project {ProjectName} from Azure DevOps",
				repositoriesList.Count, projectName);
			return repositoriesList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<GitItem>> GetRepositoryItemsAsync (
		string projectName, string repositoryId, string path, CancellationToken cancellationToken = default)
	{
		// Validate input
		var projectValidation = Validation.ValidationHelper.ValidateProjectName (projectName);
		projectValidation.ThrowIfInvalid ();

		var repoValidation = Validation.ValidationHelper.ValidateRepositoryId (repositoryId);
		repoValidation.ThrowIfInvalid ();

		if (!string.IsNullOrEmpty (path)) {
			var pathValidation = Validation.ValidationHelper.ValidateFilePath (path);
			pathValidation.ThrowIfInvalid ();
		}

		const string operation = nameof (GetRepositoryItemsAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var normalizedPath = string.IsNullOrEmpty (path) ? "/" : path;
			var cacheKey = $"{CacheKeyPrefix}:items:{projectName.ToLowerInvariant ()}:{repositoryId.ToLowerInvariant ()}:{normalizedPath.ToLowerInvariant ()}";

			// Check cache first
			var cached = await _cacheService.GetAsync<List<GitItem>> (cacheKey);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} items for {ProjectName}/{RepositoryId}/{Path} from cache",
					cached.Count, projectName, repositoryId, normalizedPath);
				return cached;
			}

			// Fetch from Azure DevOps
			var client = await _connectionFactory.GetClientAsync<GitHttpClient> (ct);
			var items = await client.GetItemsAsync (
				project: projectName,
				repositoryId: repositoryId,
				scopePath: normalizedPath,
				recursionLevel: VersionControlRecursionType.OneLevel,
				cancellationToken: ct);

			// Convert to list for caching
			var itemsList = items.ToList ();

			// Cache the results
			await _cacheService.SetAsync (cacheKey, itemsList, DefaultCacheExpiration);

			_logger.LogDebug ("Retrieved {Count} items for {ProjectName}/{RepositoryId}/{Path} from Azure DevOps",
				itemsList.Count, projectName, repositoryId, normalizedPath);
			return itemsList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<string> GetFileContentAsync (
		string projectName, string repositoryId, string path, CancellationToken cancellationToken = default)
	{
		// Validate input
		var projectValidation = Validation.ValidationHelper.ValidateProjectName (projectName);
		projectValidation.ThrowIfInvalid ();

		var repoValidation = Validation.ValidationHelper.ValidateRepositoryId (repositoryId);
		repoValidation.ThrowIfInvalid ();

		var pathValidation = Validation.ValidationHelper.ValidateFilePath (path);
		pathValidation.ThrowIfInvalid ();

		const string operation = nameof (GetFileContentAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var cacheKey = $"{CacheKeyPrefix}:content:{projectName.ToLowerInvariant ()}:{repositoryId.ToLowerInvariant ()}:{path.ToLowerInvariant ()}";

			// Check cache first
			var cached = await _cacheService.GetAsync<string> (cacheKey);
			if (cached != null) {
				_logger.LogDebug ("Retrieved file content for {ProjectName}/{RepositoryId}/{Path} from cache",
					projectName, repositoryId, path);
				return cached;
			}

			// Fetch from Azure DevOps
			var client = await _connectionFactory.GetClientAsync<GitHttpClient> (ct);

			try {
				using var stream = await client.GetItemContentAsync (
					project: projectName,
					repositoryId: repositoryId,
					path: path,
					cancellationToken: ct);

				using var reader = new StreamReader (stream, Encoding.UTF8);
				var content = await reader.ReadToEndAsync (ct);

				// Cache the content (only if it's not too large)
				if (content.Length <= 1024 * 1024) // 1MB limit
				{
					await _cacheService.SetAsync (cacheKey, content, FileContentCacheExpiration);
				}

				_logger.LogDebug ("Retrieved file content for {ProjectName}/{RepositoryId}/{Path} from Azure DevOps ({Size} chars)",
					projectName, repositoryId, path, content.Length);
				return content;
			} catch (Exception ex) when (ex.Message.Contains ("NotFound") || ex.Message.Contains ("404")) {
				_logger.LogWarning ("File not found: {ProjectName}/{RepositoryId}/{Path}", projectName, repositoryId, path);
				return string.Empty;
			}

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<GitCommitRef>> GetCommitsAsync (
		string projectName, string repositoryId, string? branch = null, int limit = 50,
		CancellationToken cancellationToken = default)
	{
		// Validate input
		var projectValidation = Validation.ValidationHelper.ValidateProjectName (projectName);
		projectValidation.ThrowIfInvalid ();

		var repoValidation = Validation.ValidationHelper.ValidateRepositoryId (repositoryId);
		repoValidation.ThrowIfInvalid ();

		var limitValidation = Validation.ValidationHelper.ValidateLimit (limit, 1000);
		limitValidation.ThrowIfInvalid ();

		if (!string.IsNullOrEmpty (branch)) {
			var branchValidation = Validation.ValidationHelper.ValidateBranchName (branch);
			branchValidation.ThrowIfInvalid ();
		}

		const string operation = nameof (GetCommitsAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var normalizedBranch = string.IsNullOrEmpty (branch) ? "main" : branch;
			var cacheKey = $"{CacheKeyPrefix}:commits:{projectName.ToLowerInvariant ()}:{repositoryId.ToLowerInvariant ()}:{normalizedBranch.ToLowerInvariant ()}:{limit}";

			// Check cache first (shorter expiration for commits as they change frequently)
			var cached = await _cacheService.GetAsync<List<GitCommitRef>> (cacheKey);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} commits for {ProjectName}/{RepositoryId}/{Branch} from cache",
					cached.Count, projectName, repositoryId, normalizedBranch);
				return cached;
			}

			// Fetch from Azure DevOps
			var client = await _connectionFactory.GetClientAsync<GitHttpClient> (ct);

			var searchCriteria = new GitQueryCommitsCriteria {
				ItemVersion = new GitVersionDescriptor {
					Version = normalizedBranch,
					VersionType = GitVersionType.Branch
				},
				Top = limit
			};

			var commits = await client.GetCommitsAsync (
				project: projectName,
				repositoryId: repositoryId,
				searchCriteria: searchCriteria,
				cancellationToken: ct);

			// Convert to list for caching
			var commitsList = commits.ToList ();

			// Cache with shorter expiration (1 minute for commits)
			await _cacheService.SetAsync (cacheKey, commitsList, TimeSpan.FromMinutes (1));

			_logger.LogDebug ("Retrieved {Count} commits for {ProjectName}/{RepositoryId}/{Branch} from Azure DevOps",
				commitsList.Count, projectName, repositoryId, normalizedBranch);
			return commitsList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<GitPullRequest>> GetPullRequestsAsync (
		string projectName, string repositoryId, string? status = null,
		CancellationToken cancellationToken = default)
	{
		// Validate input
		var projectValidation = Validation.ValidationHelper.ValidateProjectName (projectName);
		projectValidation.ThrowIfInvalid ();

		var repoValidation = Validation.ValidationHelper.ValidateRepositoryId (repositoryId);
		repoValidation.ThrowIfInvalid ();

		const string operation = nameof (GetPullRequestsAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var normalizedStatus = status ?? "all";
			var cacheKey = $"{CacheKeyPrefix}:pullrequests:{projectName.ToLowerInvariant ()}:{repositoryId.ToLowerInvariant ()}:{normalizedStatus.ToLowerInvariant ()}";

			// Check cache first (shorter expiration as PRs change frequently)
			var cached = await _cacheService.GetAsync<List<GitPullRequest>> (cacheKey);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} pull requests for {ProjectName}/{RepositoryId}/{Status} from cache",
					cached.Count, projectName, repositoryId, normalizedStatus);
				return cached;
			}

			// Fetch from Azure DevOps
			var client = await _connectionFactory.GetClientAsync<GitHttpClient> (ct);

			var searchCriteria = new GitPullRequestSearchCriteria {
				Status = string.IsNullOrEmpty (status) ? null : Enum.Parse<PullRequestStatus> (status, true)
			};

			var pullRequests = await client.GetPullRequestsAsync (
				project: projectName,
				repositoryId: repositoryId,
				searchCriteria: searchCriteria,
				cancellationToken: ct);

			// Convert to list for caching
			var pullRequestsList = pullRequests.ToList ();

			// Cache with shorter expiration (1 minute for PRs)
			await _cacheService.SetAsync (cacheKey, pullRequestsList, TimeSpan.FromMinutes (1));

			_logger.LogDebug ("Retrieved {Count} pull requests for {ProjectName}/{RepositoryId}/{Status} from Azure DevOps",
				pullRequestsList.Count, projectName, repositoryId, normalizedStatus);
			return pullRequestsList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<GitRef>> GetBranchesAsync (
		string projectName, string repositoryId, CancellationToken cancellationToken = default)
	{
		// Validate input
		var projectValidation = Validation.ValidationHelper.ValidateProjectName (projectName);
		projectValidation.ThrowIfInvalid ();

		var repoValidation = Validation.ValidationHelper.ValidateRepositoryId (repositoryId);
		repoValidation.ThrowIfInvalid ();

		const string operation = nameof (GetBranchesAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var cacheKey = $"{CacheKeyPrefix}:branches:{projectName.ToLowerInvariant ()}:{repositoryId.ToLowerInvariant ()}";

			// Check cache first
			var cached = await _cacheService.GetAsync<List<GitRef>> (cacheKey);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} branches for {ProjectName}/{RepositoryId} from cache",
					cached.Count, projectName, repositoryId);
				return cached;
			}

			// Fetch from Azure DevOps
			var client = await _connectionFactory.GetClientAsync<GitHttpClient> (ct);
			var branches = await client.GetRefsAsync (
				project: projectName,
				repositoryId: repositoryId,
				filter: "heads/", // Only branches
				cancellationToken: ct);

			// Convert to list for caching
			var branchesList = branches.ToList ();

			// Cache the results
			await _cacheService.SetAsync (cacheKey, branchesList, DefaultCacheExpiration);

			_logger.LogDebug ("Retrieved {Count} branches for {ProjectName}/{RepositoryId} from Azure DevOps",
				branchesList.Count, projectName, repositoryId);
			return branchesList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<IEnumerable<GitRef>> GetTagsAsync (
		string projectName, string repositoryId, CancellationToken cancellationToken = default)
	{
		// Validate input
		var projectValidation = Validation.ValidationHelper.ValidateProjectName (projectName);
		projectValidation.ThrowIfInvalid ();

		var repoValidation = Validation.ValidationHelper.ValidateRepositoryId (repositoryId);
		repoValidation.ThrowIfInvalid ();

		const string operation = nameof (GetTagsAsync);

		return await _errorHandler.ExecuteWithErrorHandlingAsync (async ct => {
			var cacheKey = $"{CacheKeyPrefix}:tags:{projectName.ToLowerInvariant ()}:{repositoryId.ToLowerInvariant ()}";

			// Check cache first
			var cached = await _cacheService.GetAsync<List<GitRef>> (cacheKey);
			if (cached != null) {
				_logger.LogDebug ("Retrieved {Count} tags for {ProjectName}/{RepositoryId} from cache",
					cached.Count, projectName, repositoryId);
				return cached;
			}

			// Fetch from Azure DevOps
			var client = await _connectionFactory.GetClientAsync<GitHttpClient> (ct);
			var tags = await client.GetRefsAsync (
				project: projectName,
				repositoryId: repositoryId,
				filter: "tags/", // Only tags
				cancellationToken: ct);

			// Convert to list for caching
			var tagsList = tags.ToList ();

			// Cache the results (longer expiration for tags as they change less frequently)
			await _cacheService.SetAsync (cacheKey, tagsList, TimeSpan.FromMinutes (30));

			_logger.LogDebug ("Retrieved {Count} tags for {ProjectName}/{RepositoryId} from Azure DevOps",
				tagsList.Count, projectName, repositoryId);
			return tagsList.AsEnumerable ();

		}, operation, cancellationToken);
	}

	public async Task<bool> RepositoryExistsAsync (
		string projectName, string repositoryId, CancellationToken cancellationToken = default)
	{
		try {
			var repositories = await GetRepositoriesAsync (projectName, cancellationToken);
			return repositories.Any (r =>
				r.Id.ToString ().Equals (repositoryId, StringComparison.OrdinalIgnoreCase) ||
				r.Name.Equals (repositoryId, StringComparison.OrdinalIgnoreCase));
		} catch (Exception ex) {
			_logger.LogDebug (ex, "Error checking if repository {RepositoryId} exists in project {ProjectName}",
				repositoryId, projectName);
			return false;
		}
	}
}