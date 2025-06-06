using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using System.Diagnostics;
using Sentry;

namespace AzureDevOps.MCP.Services;

public partial class CachedAzureDevOpsService : IAzureDevOpsService
{
    private readonly IAzureDevOpsService _innerService;
    private readonly ICacheService _cache;
    private readonly IPerformanceService _performance;
    private readonly ILogger<CachedAzureDevOpsService> _logger;

    public CachedAzureDevOpsService(
        AzureDevOpsService innerService,
        ICacheService cache,
        IPerformanceService performance,
        ILogger<CachedAzureDevOpsService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _performance = performance;
        _logger = logger;
    }

    public async Task<VssConnection> GetConnectionAsync()
    {
        using var _ = _performance.TrackOperation("GetConnection");
        return await _innerService.GetConnectionAsync();
    }

    public async Task<IEnumerable<TeamProjectReference>> GetProjectsAsync()
    {
        using var transaction = SentrySdk.StartTransaction("GetProjects", "azure_devops.read");
        using var _ = _performance.TrackOperation("GetProjects");
        
        transaction?.SetTag("cache.enabled", "true");
        transaction?.SetTag("operation.type", "read");
        
        return await _cache.GetOrSetAsync("projects", async () =>
        {
            using var span = transaction?.StartChild("azure_devops.api_call", "GetProjects");
            var sw = Stopwatch.StartNew();
            try
            {
                span?.SetTag("cache.hit", "false");
                var result = await _innerService.GetProjectsAsync();
                _performance.RecordApiCall("GetProjects", sw.ElapsedMilliseconds, true);
                
                span?.SetTag("projects.count", result.Count().ToString());
                span?.SetTag("duration.ms", sw.ElapsedMilliseconds.ToString());
                transaction?.SetExtra("projects.count", result.Count());
                
                return result.ToList();
            }
            catch (Exception ex)
            {
                _performance.RecordApiCall("GetProjects", sw.ElapsedMilliseconds, false);
                span?.SetTag("error", "true");
                transaction?.SetTag("error", "true");
                SentrySdk.CaptureException(ex);
                throw;
            }
        }, TimeSpan.FromMinutes(10));
    }

    public async Task<IEnumerable<GitRepository>> GetRepositoriesAsync(string projectName)
    {
        using var _ = _performance.TrackOperation("GetRepositories");
        
        var cacheKey = $"repos_{projectName}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetRepositoriesAsync(projectName);
                _performance.RecordApiCall("GetRepositories", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetRepositories", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<IEnumerable<GitItem>> GetRepositoryItemsAsync(string projectName, string repositoryId, string path)
    {
        using var _ = _performance.TrackOperation("GetRepositoryItems");
        
        var cacheKey = $"items_{projectName}_{repositoryId}_{path.Replace('/', '_')}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetRepositoryItemsAsync(projectName, repositoryId, path);
                _performance.RecordApiCall("GetRepositoryItems", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetRepositoryItems", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(2));
    }

    public async Task<string> GetFileContentAsync(string projectName, string repositoryId, string path)
    {
        using var _ = _performance.TrackOperation("GetFileContent");
        
        var cacheKey = $"file_{projectName}_{repositoryId}_{path.Replace('/', '_')}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetFileContentAsync(projectName, repositoryId, path);
                _performance.RecordApiCall("GetFileContent", sw.ElapsedMilliseconds, true);
                return result;
            }
            catch
            {
                _performance.RecordApiCall("GetFileContent", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(string projectName, int limit = 100)
    {
        using var _ = _performance.TrackOperation("GetWorkItems");
        
        var cacheKey = $"workitems_{projectName}_{limit}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetWorkItemsAsync(projectName, limit);
                _performance.RecordApiCall("GetWorkItems", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetWorkItems", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(1));
    }

    public async Task<WorkItem?> GetWorkItemAsync(int id)
    {
        using var _ = _performance.TrackOperation("GetWorkItem");
        
        var cacheKey = $"workitem_{id}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetWorkItemAsync(id);
                _performance.RecordApiCall("GetWorkItem", sw.ElapsedMilliseconds, true);
                return result;
            }
            catch
            {
                _performance.RecordApiCall("GetWorkItem", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(2));
    }

    public async Task<IEnumerable<GitCommitRef>> GetCommitsAsync(string projectName, string repositoryId, string? branch = null, int limit = 50)
    {
        using var _ = _performance.TrackOperation("GetCommits");
        
        var cacheKey = $"commits_{projectName}_{repositoryId}_{branch ?? "default"}_{limit}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetCommitsAsync(projectName, repositoryId, branch, limit);
                _performance.RecordApiCall("GetCommits", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetCommits", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(1));
    }

    public async Task<IEnumerable<GitPullRequest>> GetPullRequestsAsync(string projectName, string repositoryId, string? status = null)
    {
        using var _ = _performance.TrackOperation("GetPullRequests");
        
        var cacheKey = $"prs_{projectName}_{repositoryId}_{status ?? "all"}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetPullRequestsAsync(projectName, repositoryId, status);
                _performance.RecordApiCall("GetPullRequests", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetPullRequests", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromSeconds(30));
    }

    public async Task<IEnumerable<GitRef>> GetBranchesAsync(string projectName, string repositoryId)
    {
        using var _ = _performance.TrackOperation("GetBranches");
        
        var cacheKey = $"branches_{projectName}_{repositoryId}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetBranchesAsync(projectName, repositoryId);
                _performance.RecordApiCall("GetBranches", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetBranches", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<IEnumerable<GitRef>> GetTagsAsync(string projectName, string repositoryId)
    {
        using var _ = _performance.TrackOperation("GetTags");
        
        var cacheKey = $"tags_{projectName}_{repositoryId}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetTagsAsync(projectName, repositoryId);
                _performance.RecordApiCall("GetTags", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetTags", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<Comment> AddPullRequestCommentAsync(string projectName, string repositoryId, int pullRequestId, string content, int? parentCommentId = null)
    {
        using var _ = _performance.TrackOperation("AddPullRequestComment");
        
        // Don't cache write operations
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _innerService.AddPullRequestCommentAsync(projectName, repositoryId, pullRequestId, content, parentCommentId);
            _performance.RecordApiCall("AddPullRequestComment", sw.ElapsedMilliseconds, true);
            
            // Invalidate PR cache after adding comment
            await _cache.RemoveAsync($"prs_{projectName}_{repositoryId}_all");
            
            return result;
        }
        catch
        {
            _performance.RecordApiCall("AddPullRequestComment", sw.ElapsedMilliseconds, false);
            throw;
        }
    }
}