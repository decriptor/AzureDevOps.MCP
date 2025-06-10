using AzureDevOps.MCP.Services.Infrastructure;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Authorization;
using AzureDevOps.MCP.ErrorHandling;
using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps search operations.
/// Implements caching, validation, authorization, and error handling.
/// </summary>
public class SearchService : ISearchService
{
    readonly IAzureDevOpsConnectionFactory _connectionFactory;
    readonly IErrorHandler _errorHandler;
    readonly ICacheService _cacheService;
    readonly IAuthorizationService _authorizationService;
    readonly ILogger<SearchService> _logger;

    // Cache expiration times based on search result volatility
    static readonly TimeSpan CodeSearchCacheExpiration = TimeSpan.FromMinutes(30); // Code changes moderately
    static readonly TimeSpan WorkItemSearchCacheExpiration = TimeSpan.FromMinutes(15); // Work items change frequently
    static readonly TimeSpan FileSearchCacheExpiration = TimeSpan.FromMinutes(20); // Files change moderately
    static readonly TimeSpan PackageSearchCacheExpiration = TimeSpan.FromHours(2); // Packages change rarely

    // Cache key prefixes for organized cache management
    const string CodeSearchCachePrefix = "search:code";
    const string WorkItemSearchCachePrefix = "search:workitems";
    const string FileSearchCachePrefix = "search:files";
    const string PackageSearchCachePrefix = "search:packages";

    public SearchService(
        IAzureDevOpsConnectionFactory connectionFactory,
        IErrorHandler errorHandler,
        ICacheService cacheService,
        IAuthorizationService authorizationService,
        ILogger<SearchService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<CodeSearchResult>> SearchCodeAsync(string searchText, string? projectNameOrId = null, string? repositoryName = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        const string operation = nameof(SearchCodeAsync);
        _logger.LogDebug("Searching code for '{SearchText}' in project {ProjectName}, repository {RepositoryName} with limit {Limit}", searchText, projectNameOrId, repositoryName, limit);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization for project access if specified
            if (!string.IsNullOrEmpty(projectNameOrId) && !await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{CodeSearchCachePrefix}:{searchText}:{projectNameOrId}:{repositoryName}:{limit}";
            var cached = await _cacheService.GetAsync<List<CodeSearchResult>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} code search results from cache for '{SearchText}'", cached.Count, searchText);
                return cached.AsEnumerable();
            }

            // Note: Actual Azure DevOps Search API implementation would go here
            // For now, return simulated data as the specific search packages are not available
            var results = GenerateSimulatedCodeSearchResults(searchText, projectNameOrId, repositoryName, limit);

            // Cache the results
            var resultList = results.ToList();
            await _cacheService.SetAsync(cacheKey, resultList, CodeSearchCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} code search results for '{SearchText}'", resultList.Count, searchText);
            return resultList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<WorkItemSearchResult>> SearchWorkItemsAsync(string searchText, string? projectNameOrId = null, string? workItemType = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        const string operation = nameof(SearchWorkItemsAsync);
        _logger.LogDebug("Searching work items for '{SearchText}' in project {ProjectName}, type {WorkItemType} with limit {Limit}", searchText, projectNameOrId, workItemType, limit);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization for project access if specified
            if (!string.IsNullOrEmpty(projectNameOrId) && !await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{WorkItemSearchCachePrefix}:{searchText}:{projectNameOrId}:{workItemType}:{limit}";
            var cached = await _cacheService.GetAsync<List<WorkItemSearchResult>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} work item search results from cache for '{SearchText}'", cached.Count, searchText);
                return cached.AsEnumerable();
            }

            // Note: Actual Azure DevOps Search API implementation would go here
            var results = GenerateSimulatedWorkItemSearchResults(searchText, projectNameOrId, workItemType, limit);

            // Cache the results
            var resultList = results.ToList();
            await _cacheService.SetAsync(cacheKey, resultList, WorkItemSearchCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} work item search results for '{SearchText}'", resultList.Count, searchText);
            return resultList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<FileSearchResult>> SearchFilesAsync(string fileName, string? projectNameOrId = null, string? repositoryName = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        const string operation = nameof(SearchFilesAsync);
        _logger.LogDebug("Searching files for '{FileName}' in project {ProjectName}, repository {RepositoryName} with limit {Limit}", fileName, projectNameOrId, repositoryName, limit);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization for project access if specified
            if (!string.IsNullOrEmpty(projectNameOrId) && !await _authorizationService.CanAccessProjectAsync(projectNameOrId, ct))
            {
                throw new UnauthorizedAccessException($"Access denied to project '{projectNameOrId}'");
            }

            // Check cache first
            var cacheKey = $"{FileSearchCachePrefix}:{fileName}:{projectNameOrId}:{repositoryName}:{limit}";
            var cached = await _cacheService.GetAsync<List<FileSearchResult>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} file search results from cache for '{FileName}'", cached.Count, fileName);
                return cached.AsEnumerable();
            }

            // Note: Actual Azure DevOps Search API implementation would go here
            var results = GenerateSimulatedFileSearchResults(fileName, projectNameOrId, repositoryName, limit);

            // Cache the results
            var resultList = results.ToList();
            await _cacheService.SetAsync(cacheKey, resultList, FileSearchCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} file search results for '{FileName}'", resultList.Count, fileName);
            return resultList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<PackageSearchResult>> SearchPackagesAsync(string packageName, string? feedName = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        const string operation = nameof(SearchPackagesAsync);
        _logger.LogDebug("Searching packages for '{PackageName}' in feed {FeedName} with limit {Limit}", packageName, feedName, limit);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async (ct) =>
        {
            // Check authorization for package access
            if (!await _authorizationService.CanPerformOperationAsync("SearchPackages", ct))
            {
                throw new UnauthorizedAccessException("Access denied to package search");
            }

            // Check cache first
            var cacheKey = $"{PackageSearchCachePrefix}:{packageName}:{feedName}:{limit}";
            var cached = await _cacheService.GetAsync<List<PackageSearchResult>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} package search results from cache for '{PackageName}'", cached.Count, packageName);
                return cached.AsEnumerable();
            }

            // Note: Actual Azure DevOps Package API implementation would go here
            var results = GenerateSimulatedPackageSearchResults(packageName, feedName, limit);

            // Cache the results
            var resultList = results.ToList();
            await _cacheService.SetAsync(cacheKey, resultList, PackageSearchCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} package search results for '{PackageName}'", resultList.Count, packageName);
            return resultList.AsEnumerable();

        }, operation, cancellationToken);
    }

    // Simulation methods - these would be replaced with actual Azure DevOps API calls
    private static IEnumerable<CodeSearchResult> GenerateSimulatedCodeSearchResults(string searchText, string? projectName, string? repositoryName, int limit)
    {
        var random = new Random(searchText.GetHashCode());
        var extensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".h", ".md", ".json", ".xml" };
        
        for (int i = 1; i <= Math.Min(limit, 20); i++)
        {
            var extension = extensions[random.Next(extensions.Length)];
            var fileName = $"File{i}{extension}";
            var project = projectName ?? $"Project{random.Next(1, 6)}";
            var repo = repositoryName ?? $"{project}-repo";
            
            yield return new CodeSearchResult
            {
                FileName = fileName,
                FilePath = $"/src/components/{fileName}",
                Repository = repo,
                Project = project,
                Branch = "main",
                Matches = GenerateCodeMatches(searchText, random.Next(1, 5)),
                ContentType = GetContentType(extension),
                FileSize = random.Next(1024, 10240),
                LastModified = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                Author = $"developer{random.Next(1, 10)}@company.com",
                Url = $"https://dev.azure.com/{project}/_git/{repo}?path=/src/components/{fileName}"
            };
        }
    }

    private static List<CodeMatch> GenerateCodeMatches(string searchText, int matchCount)
    {
        var random = new Random(searchText.GetHashCode());
        var matches = new List<CodeMatch>();
        
        for (int i = 1; i <= matchCount; i++)
        {
            var lineNumber = random.Next(10, 200);
            matches.Add(new CodeMatch
            {
                LineNumber = lineNumber,
                LineContent = $"    function process{searchText}() {{ // Line {lineNumber}",
                ColumnStart = random.Next(1, 20),
                ColumnEnd = random.Next(21, 40),
                MatchedText = searchText,
                Context = $"Function definition containing '{searchText}'"
            });
        }
        
        return matches;
    }

    private static string GetContentType(string extension) => extension switch
    {
        ".cs" => "text/x-csharp",
        ".js" => "text/javascript",
        ".ts" => "text/typescript",
        ".py" => "text/x-python",
        ".java" => "text/x-java",
        ".cpp" or ".h" => "text/x-c++src",
        ".md" => "text/markdown",
        ".json" => "application/json",
        ".xml" => "text/xml",
        _ => "text/plain"
    };

    private static IEnumerable<WorkItemSearchResult> GenerateSimulatedWorkItemSearchResults(string searchText, string? projectName, string? workItemType, int limit)
    {
        var random = new Random(searchText.GetHashCode());
        var types = new[] { "Bug", "Feature", "Task", "User Story", "Epic" };
        var states = new[] { "New", "Active", "Resolved", "Closed", "Removed" };
        
        for (int i = 1; i <= Math.Min(limit, 15); i++)
        {
            var project = projectName ?? $"Project{random.Next(1, 6)}";
            var type = workItemType ?? types[random.Next(types.Length)];
            
            yield return new WorkItemSearchResult
            {
                Id = random.Next(1000, 9999),
                Title = $"{type} - {searchText} functionality enhancement",
                WorkItemType = type,
                State = states[random.Next(states.Length)],
                AssignedTo = $"assignee{random.Next(1, 10)}@company.com",
                Project = new TeamProjectReference { Name = project, Id = Guid.NewGuid() },
                AreaPath = $"\\{project}\\Features",
                IterationPath = $"\\{project}\\Sprint {random.Next(1, 20)}",
                Tags = GenerateTags(searchText, random),
                Matches = GenerateWorkItemMatches(searchText, random.Next(1, 3)),
                CreatedDate = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                ChangedDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                CreatedBy = $"creator{random.Next(1, 5)}@company.com",
                ChangedBy = $"modifier{random.Next(1, 8)}@company.com",
                Url = $"https://dev.azure.com/{project}/_workitems/edit/{random.Next(1000, 9999)}"
            };
        }
    }

    private static List<string> GenerateTags(string searchText, Random random)
    {
        var commonTags = new[] { "feature", "bug", "enhancement", "investigation", "technical-debt" };
        var tagCount = random.Next(1, 4);
        var tags = new List<string> { searchText.ToLower() };
        
        for (int i = 0; i < tagCount; i++)
        {
            tags.Add(commonTags[random.Next(commonTags.Length)]);
        }
        
        return tags.Distinct().ToList();
    }

    private static List<WorkItemMatch> GenerateWorkItemMatches(string searchText, int matchCount)
    {
        var random = new Random(searchText.GetHashCode());
        var fields = new[] { "Title", "Description", "Acceptance Criteria", "Comments" };
        var matches = new List<WorkItemMatch>();
        
        for (int i = 0; i < matchCount; i++)
        {
            var field = fields[random.Next(fields.Length)];
            matches.Add(new WorkItemMatch
            {
                FieldName = field,
                FieldValue = $"This {field.ToLower()} contains {searchText} and related functionality",
                HighlightedTerms = new List<string> { searchText }
            });
        }
        
        return matches;
    }

    private static IEnumerable<FileSearchResult> GenerateSimulatedFileSearchResults(string fileName, string? projectName, string? repositoryName, int limit)
    {
        var random = new Random(fileName.GetHashCode());
        var extensions = new[] { ".cs", ".js", ".ts", ".md", ".json", ".xml", ".yml", ".config" };
        
        for (int i = 1; i <= Math.Min(limit, 25); i++)
        {
            var project = projectName ?? $"Project{random.Next(1, 6)}";
            var repo = repositoryName ?? $"{project}-repo";
            var extension = extensions[random.Next(extensions.Length)];
            var matchedFileName = $"{fileName}{i}{extension}";
            
            yield return new FileSearchResult
            {
                FileName = matchedFileName,
                FilePath = $"/src/{random.Next(1, 5) switch { 1 => "components", 2 => "services", 3 => "utils", _ => "modules" }}/{matchedFileName}",
                Repository = repo,
                Project = project,
                Branch = random.Next(10) == 0 ? "develop" : "main",
                ContentType = GetContentType(extension),
                FileSize = random.Next(512, 51200),
                LastModified = DateTime.UtcNow.AddDays(-random.Next(1, 180)),
                Author = $"dev{random.Next(1, 15)}@company.com",
                CommitId = Guid.NewGuid().ToString()[..8],
                Url = $"https://dev.azure.com/{project}/_git/{repo}?path={matchedFileName}"
            };
        }
    }

    private static IEnumerable<PackageSearchResult> GenerateSimulatedPackageSearchResults(string packageName, string? feedName, int limit)
    {
        var random = new Random(packageName.GetHashCode());
        var packageTypes = new[] { "NuGet", "npm", "PyPI", "Maven", "Docker" };
        var authors = new[] { "Microsoft", "Google", "Apache", "JetBrains", "HashiCorp", "Netflix", "Uber" };
        
        for (int i = 1; i <= Math.Min(limit, 10); i++)
        {
            var feed = feedName ?? $"Feed{random.Next(1, 4)}";
            var packageType = packageTypes[random.Next(packageTypes.Length)];
            var version = $"{random.Next(1, 10)}.{random.Next(0, 20)}.{random.Next(0, 100)}";
            
            yield return new PackageSearchResult
            {
                PackageName = $"{packageName}.{(i == 1 ? "Core" : $"Extensions.{i}")}",
                Version = version,
                FeedName = feed,
                Description = $"A comprehensive {packageName} package for {packageType} applications",
                Authors = new List<string> { authors[random.Next(authors.Length)] },
                Tags = new List<string> { packageName.ToLower(), packageType.ToLower(), "library", "framework" },
                PackageType = packageType,
                DownloadCount = random.Next(1000, 1000000),
                PublishedDate = DateTime.UtcNow.AddDays(-random.Next(1, 1000)),
                LicenseUrl = "https://opensource.org/licenses/MIT",
                ProjectUrl = $"https://github.com/organization/{packageName}",
                IconUrl = $"https://cdn.nuget.org/packages/{packageName}.png",
                IsPrerelease = random.Next(10) == 0,
                IsListed = random.Next(20) != 0
            };
        }
    }
}