using AzureDevOps.MCP.Services;
using Microsoft.Model.Context;
using Microsoft.Model.Context.WebApi;

namespace AzureDevOps.MCP.Controllers.MCP
{
    [McpController("repositories")]
    public class RepositoriesController : McpControllerBase
    {
        private readonly IAzureDevOpsService _azureDevOpsService;
        private readonly ILogger<RepositoriesController> _logger;

        public RepositoriesController(
            IAzureDevOpsService azureDevOpsService,
            ILogger<RepositoriesController> logger)
        {
            _azureDevOpsService = azureDevOpsService;
            _logger = logger;
        }

        [McpGet("list")]
        public async Task<McpActionResult> ListRepositoriesAsync([McpFromQuery] string projectName)
        {
            try
            {
                var repos = await _azureDevOpsService.GetRepositoriesAsync(projectName);
                return McpJson(repos.Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    url = r.RemoteUrl
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing repositories for project {ProjectName}", projectName);
                return McpProblem(ex.Message);
            }
        }

        [McpGet("files")]
        public async Task<McpActionResult> ListFilesAsync(
            [McpFromQuery] string projectName,
            [McpFromQuery] string repositoryId,
            [McpFromQuery] string path = "/")
        {
            try
            {
                var items = await _azureDevOpsService.GetRepositoryItemsAsync(
                    projectName, repositoryId, path);
                
                return McpJson(items.Select(i => new
                {
                    path = i.Path,
                    isFolder = i.IsFolder,
                    size = i.Size,
                    commitId = i.CommitId
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files for repository {RepositoryId} in project {ProjectName}", 
                    repositoryId, projectName);
                return McpProblem(ex.Message);
            }
        }

        [McpGet("file")]
        public async Task<McpActionResult> GetFileContentAsync(
            [McpFromQuery] string projectName,
            [McpFromQuery] string repositoryId,
            [McpFromQuery] string path)
        {
            try
            {
                var content = await _azureDevOpsService.GetFileContentAsync(
                    projectName, repositoryId, path);
                
                return McpText(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file {Path} from repository {RepositoryId} in project {ProjectName}", 
                    path, repositoryId, projectName);
                return McpProblem(ex.Message);
            }
        }
    }
}
