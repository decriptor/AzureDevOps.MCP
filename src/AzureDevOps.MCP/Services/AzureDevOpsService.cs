using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.MCP.Services
{
    public class AzureDevOpsService : IAzureDevOpsService
    {
        private readonly IConfiguration _configuration;
        private VssConnection? _connection;

        public AzureDevOpsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<VssConnection> GetConnectionAsync()
        {
            if (_connection != null)
                return _connection;

            var orgUrl = _configuration["AzureDevOps:OrganizationUrl"];
            var pat = _configuration["AzureDevOps:PersonalAccessToken"];

            if (string.IsNullOrEmpty(orgUrl) || string.IsNullOrEmpty(pat))
                throw new InvalidOperationException("Azure DevOps organization URL and PAT must be configured");

            var credentials = new VssBasicCredential(string.Empty, pat);
            _connection = new VssConnection(new Uri(orgUrl), credentials);
            await _connection.ConnectAsync();
            return _connection;
        }

        public async Task<IEnumerable<TeamProjectReference>> GetProjectsAsync()
        {
            var connection = await GetConnectionAsync();
            var projectClient = connection.GetClient<ProjectHttpClient>();
            return await projectClient.GetProjects();
        }

        public async Task<IEnumerable<GitRepository>> GetRepositoriesAsync(string projectName)
        {
            var connection = await GetConnectionAsync();
            var gitClient = connection.GetClient<GitHttpClient>();
            return await gitClient.GetRepositoriesAsync(projectName);
        }

        public async Task<IEnumerable<GitItem>> GetRepositoryItemsAsync(string projectName, string repositoryId, string path)
        {
            var connection = await GetConnectionAsync();
            var gitClient = connection.GetClient<GitHttpClient>();
            var items = await gitClient.GetItemsAsync(
                repositoryId,
                recursionLevel: VersionControlRecursionType.OneLevel,
                scopePath: path,
                project: projectName);

            return items;
        }

        public async Task<string> GetFileContentAsync(string projectName, string repositoryId, string path)
        {
            var connection = await GetConnectionAsync();
            var gitClient = connection.GetClient<GitHttpClient>();
            var item = await gitClient.GetItemAsync(
                repositoryId,
                path,
                includeContent: true,
                project: projectName);

            return item.Content;
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(string projectName, int limit = 100)
        {
            var connection = await GetConnectionAsync();
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
            
            // Create a WIQL query to get work items
            var wiql = new Wiql
            {
                Query = $"SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.TeamProject] = '{projectName}' ORDER BY [System.ChangedDate] DESC"
            };

            var result = await witClient.QueryByWiqlAsync(wiql);
            
            // If no work items are found, return empty list
            if (result.WorkItems.Count() == 0)
                return new List<WorkItem>();

            // Get the actual work items with fields
            var ids = result.WorkItems.Select(wi => wi.Id).Take(limit);
            return await witClient.GetWorkItemsAsync(ids, expand: WorkItemExpand.All);
        }

        public async Task<WorkItem?> GetWorkItemAsync(int id)
        {
            var connection = await GetConnectionAsync();
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
            
            try 
            {
                return await witClient.GetWorkItemAsync(id, expand: WorkItemExpand.All);
            }
            catch (VssServiceException)
            {
                return null;
            }
        }
    }
}
