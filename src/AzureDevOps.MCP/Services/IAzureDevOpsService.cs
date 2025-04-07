using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.MCP.Services
{
    public interface IAzureDevOpsService
    {
        Task<VssConnection> GetConnectionAsync();
        Task<IEnumerable<TeamProjectReference>> GetProjectsAsync();
        Task<IEnumerable<GitRepository>> GetRepositoriesAsync(string projectName);
        Task<IEnumerable<GitItem>> GetRepositoryItemsAsync(string projectName, string repositoryId, string path);
        Task<string> GetFileContentAsync(string projectName, string repositoryId, string path);
        Task<IEnumerable<WorkItem>> GetWorkItemsAsync(string projectName, int limit = 100);
        Task<WorkItem?> GetWorkItemAsync(int id);
    }
}
