using AzureDevOps.MCP.Services;
using Microsoft.Model.Context;
using Microsoft.Model.Context.WebApi;

namespace AzureDevOps.MCP.Controllers.MCP
{
    [McpController("workitems")]
    public class WorkItemsController : McpControllerBase
    {
        private readonly IAzureDevOpsService _azureDevOpsService;
        private readonly ILogger<WorkItemsController> _logger;

        public WorkItemsController(
            IAzureDevOpsService azureDevOpsService,
            ILogger<WorkItemsController> logger)
        {
            _azureDevOpsService = azureDevOpsService;
            _logger = logger;
        }

        [McpGet("list")]
        public async Task<McpActionResult> ListWorkItemsAsync(
            [McpFromQuery] string projectName,
            [McpFromQuery] int limit = 100)
        {
            try
            {
                var workItems = await _azureDevOpsService.GetWorkItemsAsync(projectName, limit);
                
                var result = workItems.Select(wi => new
                {
                    id = wi.Id,
                    title = wi.Fields.ContainsKey("System.Title") ? wi.Fields["System.Title"] : "No title",
                    state = wi.Fields.ContainsKey("System.State") ? wi.Fields["System.State"] : "Unknown",
                    type = wi.Fields.ContainsKey("System.WorkItemType") ? wi.Fields["System.WorkItemType"] : "Unknown",
                    assignedTo = wi.Fields.ContainsKey("System.AssignedTo") ? wi.Fields["System.AssignedTo"] : "Unassigned"
                });

                return McpJson(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing work items for project {ProjectName}", projectName);
                return McpProblem(ex.Message);
            }
        }

        [McpGet("get/{id}")]
        public async Task<McpActionResult> GetWorkItemAsync(int id)
        {
            try
            {
                var workItem = await _azureDevOpsService.GetWorkItemAsync(id);
                
                if (workItem == null)
                    return McpNotFound($"Work item {id} not found");

                return McpJson(new
                {
                    id = workItem.Id,
                    fields = workItem.Fields
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting work item {Id}", id);
                return McpProblem(ex.Message);
            }
        }
    }
}
