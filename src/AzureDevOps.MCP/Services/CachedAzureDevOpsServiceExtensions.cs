using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Diagnostics;

namespace AzureDevOps.MCP.Services;

// Extension methods for CachedAzureDevOpsService to handle new operations
public partial class CachedAzureDevOpsService
{
    public async Task<IEnumerable<CodeSearchResult>> SearchCodeAsync(string searchText, string? projectName = null, string? repositoryName = null, int limit = 50)
    {
        using var _ = _performance.TrackOperation("SearchCode");
        
        // Don't cache search results as they should be fresh
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _innerService.SearchCodeAsync(searchText, projectName, repositoryName, limit);
            _performance.RecordApiCall("SearchCode", sw.ElapsedMilliseconds, true);
            return result;
        }
        catch
        {
            _performance.RecordApiCall("SearchCode", sw.ElapsedMilliseconds, false);
            throw;
        }
    }

    public async Task<IEnumerable<WikiReference>> GetWikisAsync(string projectName)
    {
        using var _ = _performance.TrackOperation("GetWikis");
        
        var cacheKey = $"wikis_{projectName}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetWikisAsync(projectName);
                _performance.RecordApiCall("GetWikis", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetWikis", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(10));
    }

    public async Task<WikiPage?> GetWikiPageAsync(string projectName, string wikiIdentifier, string path)
    {
        using var _ = _performance.TrackOperation("GetWikiPage");
        
        var cacheKey = $"wikipage_{projectName}_{wikiIdentifier}_{path.Replace('/', '_')}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetWikiPageAsync(projectName, wikiIdentifier, path);
                _performance.RecordApiCall("GetWikiPage", sw.ElapsedMilliseconds, true);
                return result;
            }
            catch
            {
                _performance.RecordApiCall("GetWikiPage", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<IEnumerable<Build>> GetBuildsAsync(string projectName, int? definitionId = null, int limit = 20)
    {
        using var _ = _performance.TrackOperation("GetBuilds");
        
        var cacheKey = $"builds_{projectName}_{definitionId ?? 0}_{limit}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetBuildsAsync(projectName, definitionId, limit);
                _performance.RecordApiCall("GetBuilds", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetBuilds", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(1)); // Short cache for builds
    }

    public async Task<IEnumerable<TestRun>> GetTestRunsAsync(string projectName, int limit = 20)
    {
        using var _ = _performance.TrackOperation("GetTestRuns");
        
        var cacheKey = $"testruns_{projectName}_{limit}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetTestRunsAsync(projectName, limit);
                _performance.RecordApiCall("GetTestRuns", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetTestRuns", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(2));
    }

    public async Task<IEnumerable<TestCaseResult>> GetTestResultsAsync(string projectName, int runId)
    {
        using var _ = _performance.TrackOperation("GetTestResults");
        
        var cacheKey = $"testresults_{projectName}_{runId}";
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.GetTestResultsAsync(projectName, runId);
                _performance.RecordApiCall("GetTestResults", sw.ElapsedMilliseconds, true);
                return result.ToList();
            }
            catch
            {
                _performance.RecordApiCall("GetTestResults", sw.ElapsedMilliseconds, false);
                throw;
            }
        }, TimeSpan.FromMinutes(10)); // Longer cache for completed test results
    }

    public async Task<Stream> DownloadBuildArtifactAsync(string projectName, int buildId, string artifactName)
    {
        using var _ = _performance.TrackOperation("DownloadBuildArtifact");
        
        // Don't cache artifact streams
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _innerService.DownloadBuildArtifactAsync(projectName, buildId, artifactName);
            _performance.RecordApiCall("DownloadBuildArtifact", sw.ElapsedMilliseconds, true);
            return result;
        }
        catch
        {
            _performance.RecordApiCall("DownloadBuildArtifact", sw.ElapsedMilliseconds, false);
            throw;
        }
    }

    public async Task<GitPullRequest> CreateDraftPullRequestAsync(string projectName, string repositoryId, string sourceBranch, string targetBranch, string title, string description)
    {
        using var _ = _performance.TrackOperation("CreateDraftPullRequest");
        
        // Don't cache write operations
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _innerService.CreateDraftPullRequestAsync(projectName, repositoryId, sourceBranch, targetBranch, title, description);
            _performance.RecordApiCall("CreateDraftPullRequest", sw.ElapsedMilliseconds, true);
            
            // Invalidate PR cache after creating new PR
            await _cache.RemoveAsync($"prs_{projectName}_{repositoryId}_all");
            await _cache.RemoveAsync($"prs_{projectName}_{repositoryId}_draft");
            
            return result;
        }
        catch
        {
            _performance.RecordApiCall("CreateDraftPullRequest", sw.ElapsedMilliseconds, false);
            throw;
        }
    }

    public async Task<WorkItem> UpdateWorkItemTagsAsync(int workItemId, string[] tagsToAdd, string[] tagsToRemove)
    {
        using var _ = _performance.TrackOperation("UpdateWorkItemTags");
        
        // Don't cache write operations
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _innerService.UpdateWorkItemTagsAsync(workItemId, tagsToAdd, tagsToRemove);
            _performance.RecordApiCall("UpdateWorkItemTags", sw.ElapsedMilliseconds, true);
            
            // Invalidate work item cache after updating tags
            await _cache.RemoveAsync($"workitem_{workItemId}");
            
            return result;
        }
        catch
        {
            _performance.RecordApiCall("UpdateWorkItemTags", sw.ElapsedMilliseconds, false);
            throw;
        }
    }
}