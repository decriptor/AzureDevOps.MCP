using AzureDevOps.MCP.Services;
using Microsoft.Model.Context;
using Microsoft.Model.Context.WebApi;

namespace AzureDevOps.MCP.Controllers.MCP
{
    [McpController("projects")]
    public class ProjectsController : McpControllerBase
    {
        private readonly IAzureDevOpsService _azureDevOpsService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            IAzureDevOpsService azureDevOpsService,
            ILogger<ProjectsController> logger)
        {
            _azureDevOpsService = azureDevOpsService;
            _logger = logger;
        }

        [McpGet("list")]
        public async Task<McpActionResult> ListProjectsAsync()
        {
            try
            {
                var projects = await _azureDevOpsService.GetProjectsAsync();
                return McpJson(projects.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing projects");
                return McpProblem(ex.Message);
            }
        }
    }
}
