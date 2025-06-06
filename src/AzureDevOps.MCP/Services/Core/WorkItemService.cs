using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOps.MCP.Services.Core;

public class WorkItemService : IWorkItemService
{
    private readonly Infrastructure.IConnectionFactory _connectionFactory;
    private readonly ErrorHandling.IErrorHandler _errorHandler;
    private readonly Infrastructure.ICacheService _cacheService;
    private readonly ILogger<WorkItemService> _logger;

    private const string CacheKeyPrefix = "workitems";
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(2);

    public WorkItemService(
        Infrastructure.IConnectionFactory connectionFactory,
        ErrorHandling.IErrorHandler errorHandler,
        Infrastructure.ICacheService cacheService,
        ILogger<WorkItemService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(
        string projectName, int limit = 100, CancellationToken cancellationToken = default)
    {
        // Validate input
        var projectValidation = Validation.ValidationHelper.ValidateProjectName(projectName);
        projectValidation.ThrowIfInvalid();

        var limitValidation = Validation.ValidationHelper.ValidateLimit(limit, 1000);
        limitValidation.ThrowIfInvalid();

        const string operation = nameof(GetWorkItemsAsync);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async ct =>
        {
            var cacheKey = $"{CacheKeyPrefix}:{projectName.ToLowerInvariant()}:all:{limit}";

            // Check cache first
            var cached = await _cacheService.GetAsync<List<WorkItem>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} work items for project {ProjectName} from cache", 
                    cached.Count, projectName);
                return cached;
            }

            // Build WIQL query to get recent work items
            var wiql = $@"
                SELECT [System.Id], [System.Title], [System.State], [System.AssignedTo], 
                       [System.CreatedDate], [System.ChangedDate], [System.WorkItemType]
                FROM WorkItems 
                WHERE [System.TeamProject] = '{projectName}' 
                ORDER BY [System.ChangedDate] DESC";

            var workItems = await QueryWorkItemsInternalAsync(projectName, wiql, limit, ct);
            var workItemsList = workItems.ToList();

            // Cache the results
            await _cacheService.SetAsync(cacheKey, workItemsList, DefaultCacheExpiration, ct);

            _logger.LogInformation("Retrieved {Count} work items for project {ProjectName} from Azure DevOps", 
                workItemsList.Count, projectName);
            return workItemsList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<WorkItem?> GetWorkItemAsync(
        int id, CancellationToken cancellationToken = default)
    {
        // Validate input
        var idValidation = Validation.ValidationHelper.ValidateWorkItemId(id);
        idValidation.ThrowIfInvalid();

        const string operation = nameof(GetWorkItemAsync);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async ct =>
        {
            var cacheKey = $"{CacheKeyPrefix}:item:{id}";

            // Check cache first
            var cached = await _cacheService.GetAsync<WorkItem>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved work item {WorkItemId} from cache", id);
                return cached;
            }

            try
            {
                var client = await _connectionFactory.GetClientAsync<WorkItemTrackingHttpClient>(ct);
                var workItem = await client.GetWorkItemAsync(id, expand: WorkItemExpand.All, cancellationToken: ct);

                if (workItem != null)
                {
                    // Cache the result
                    await _cacheService.SetAsync(cacheKey, workItem, DefaultCacheExpiration, ct);
                    
                    _logger.LogDebug("Retrieved work item {WorkItemId} from Azure DevOps", id);
                }

                return workItem;
            }
            catch (Microsoft.VisualStudio.Services.WebApi.VssServiceException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Work item {WorkItemId} not found", id);
                return null;
            }

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<WorkItem>> QueryWorkItemsAsync(
        string projectName, string wiql, CancellationToken cancellationToken = default)
    {
        // Validate input
        var projectValidation = Validation.ValidationHelper.ValidateProjectName(projectName);
        projectValidation.ThrowIfInvalid();

        var wiqlValidation = Validation.ValidationHelper.ValidateWiql(wiql);
        wiqlValidation.ThrowIfInvalid();

        const string operation = nameof(QueryWorkItemsAsync);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async ct =>
        {
            return await QueryWorkItemsInternalAsync(projectName, wiql, null, ct);

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<WorkItem>> GetWorkItemsByTypeAsync(
        string projectName, string workItemType, int limit = 100, CancellationToken cancellationToken = default)
    {
        // Validate input
        var projectValidation = Validation.ValidationHelper.ValidateProjectName(projectName);
        projectValidation.ThrowIfInvalid();

        ArgumentException.ThrowIfNullOrWhiteSpace(workItemType);

        var limitValidation = Validation.ValidationHelper.ValidateLimit(limit, 1000);
        limitValidation.ThrowIfInvalid();

        const string operation = nameof(GetWorkItemsByTypeAsync);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async ct =>
        {
            var cacheKey = $"{CacheKeyPrefix}:type:{projectName.ToLowerInvariant()}:{workItemType.ToLowerInvariant()}:{limit}";

            // Check cache first
            var cached = await _cacheService.GetAsync<List<WorkItem>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} {WorkItemType} work items for project {ProjectName} from cache", 
                    cached.Count, workItemType, projectName);
                return cached;
            }

            // Build WIQL query for specific work item type
            var wiql = $@"
                SELECT [System.Id], [System.Title], [System.State], [System.AssignedTo], 
                       [System.CreatedDate], [System.ChangedDate], [System.WorkItemType]
                FROM WorkItems 
                WHERE [System.TeamProject] = '{projectName}' 
                AND [System.WorkItemType] = '{workItemType}'
                ORDER BY [System.ChangedDate] DESC";

            var workItems = await QueryWorkItemsInternalAsync(projectName, wiql, limit, ct);
            var workItemsList = workItems.ToList();

            // Cache the results
            await _cacheService.SetAsync(cacheKey, workItemsList, DefaultCacheExpiration, ct);

            _logger.LogDebug("Retrieved {Count} {WorkItemType} work items for project {ProjectName} from Azure DevOps", 
                workItemsList.Count, workItemType, projectName);
            return workItemsList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<WorkItem>> GetWorkItemsByAssigneeAsync(
        string projectName, string assignedTo, int limit = 100, CancellationToken cancellationToken = default)
    {
        // Validate input
        var projectValidation = Validation.ValidationHelper.ValidateProjectName(projectName);
        projectValidation.ThrowIfInvalid();

        ArgumentException.ThrowIfNullOrWhiteSpace(assignedTo);

        var limitValidation = Validation.ValidationHelper.ValidateLimit(limit, 1000);
        limitValidation.ThrowIfInvalid();

        const string operation = nameof(GetWorkItemsByAssigneeAsync);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async ct =>
        {
            var cacheKey = $"{CacheKeyPrefix}:assignee:{projectName.ToLowerInvariant()}:{assignedTo.ToLowerInvariant()}:{limit}";

            // Check cache first
            var cached = await _cacheService.GetAsync<List<WorkItem>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} work items assigned to {AssignedTo} for project {ProjectName} from cache", 
                    cached.Count, assignedTo, projectName);
                return cached;
            }

            // Build WIQL query for specific assignee
            var wiql = $@"
                SELECT [System.Id], [System.Title], [System.State], [System.AssignedTo], 
                       [System.CreatedDate], [System.ChangedDate], [System.WorkItemType]
                FROM WorkItems 
                WHERE [System.TeamProject] = '{projectName}' 
                AND [System.AssignedTo] = '{assignedTo}'
                ORDER BY [System.ChangedDate] DESC";

            var workItems = await QueryWorkItemsInternalAsync(projectName, wiql, limit, ct);
            var workItemsList = workItems.ToList();

            // Cache the results
            await _cacheService.SetAsync(cacheKey, workItemsList, DefaultCacheExpiration, ct);

            _logger.LogDebug("Retrieved {Count} work items assigned to {AssignedTo} for project {ProjectName} from Azure DevOps", 
                workItemsList.Count, assignedTo, projectName);
            return workItemsList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<IEnumerable<WorkItem>> GetWorkItemRevisionsAsync(
        int id, CancellationToken cancellationToken = default)
    {
        // Validate input
        var idValidation = Validation.ValidationHelper.ValidateWorkItemId(id);
        idValidation.ThrowIfInvalid();

        const string operation = nameof(GetWorkItemRevisionsAsync);

        return await _errorHandler.ExecuteWithErrorHandlingAsync(async ct =>
        {
            var cacheKey = $"{CacheKeyPrefix}:revisions:{id}";

            // Check cache first (longer expiration for revisions as they're historical)
            var cached = await _cacheService.GetAsync<List<WorkItem>>(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved {Count} revisions for work item {WorkItemId} from cache", 
                    cached.Count, id);
                return cached;
            }

            var client = await _connectionFactory.GetClientAsync<WorkItemTrackingHttpClient>(ct);
            var revisions = await client.GetRevisionsAsync(id, expand: WorkItemExpand.All, cancellationToken: ct);

            // Convert to list for caching
            var revisionsList = revisions.ToList();

            // Cache with longer expiration for revisions
            await _cacheService.SetAsync(cacheKey, revisionsList, TimeSpan.FromMinutes(10), ct);

            _logger.LogDebug("Retrieved {Count} revisions for work item {WorkItemId} from Azure DevOps", 
                revisionsList.Count, id);
            return revisionsList.AsEnumerable();

        }, operation, cancellationToken);
    }

    public async Task<bool> WorkItemExistsAsync(
        int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var workItem = await GetWorkItemAsync(id, cancellationToken);
            return workItem != null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if work item {WorkItemId} exists", id);
            return false;
        }
    }

    private async Task<IEnumerable<WorkItem>> QueryWorkItemsInternalAsync(
        string projectName, string wiql, int? limit, CancellationToken cancellationToken)
    {
        var client = await _connectionFactory.GetClientAsync<WorkItemTrackingHttpClient>(cancellationToken);
        
        // Execute the WIQL query
        var query = new Wiql { Query = wiql };
        var queryResult = await client.QueryByWiqlAsync(query, projectName, cancellationToken: cancellationToken);

        if (queryResult?.WorkItems == null || !queryResult.WorkItems.Any())
        {
            return [];
        }

        // Get work item IDs and apply limit if specified
        var workItemIds = queryResult.WorkItems.Select(wi => wi.Id);
        if (limit.HasValue)
        {
            workItemIds = workItemIds.Take(limit.Value);
        }

        // Batch retrieve work items (Azure DevOps API supports up to 200 IDs per batch)
        var allWorkItems = new List<WorkItem>();
        var batchSize = 200;
        var workItemIdsList = workItemIds.ToList();

        for (int i = 0; i < workItemIdsList.Count; i += batchSize)
        {
            var batch = workItemIdsList.Skip(i).Take(batchSize);
            var workItems = await client.GetWorkItemsAsync(
                batch, 
                expand: WorkItemExpand.All, 
                cancellationToken: cancellationToken);
            
            allWorkItems.AddRange(workItems);
        }

        return allWorkItems;
    }
}