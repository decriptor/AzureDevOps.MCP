using Sentry;

namespace AzureDevOps.MCP.Services;

public interface ISentryPerformanceService : IPerformanceService
{
    ITransaction? StartTransaction(string name, string operation);
    ISpan? StartSpan(string operation, string? description = null);
    void SetUser(string? userId, string? organizationUrl = null);
    void SetTag(string key, string value);
    void SetContext(string key, object context);
    void CaptureException(Exception exception, string? operation = null);
    void AddBreadcrumb(string message, string category = "default", BreadcrumbLevel level = BreadcrumbLevel.Info);
}

public class SentryPerformanceService : ISentryPerformanceService
{
    private readonly IPerformanceService _basePerformanceService;
    private readonly ILogger<SentryPerformanceService> _logger;
    private readonly bool _sentryEnabled;

    public SentryPerformanceService(
        IPerformanceService basePerformanceService,
        ILogger<SentryPerformanceService> logger,
        IConfiguration configuration)
    {
        _basePerformanceService = basePerformanceService;
        _logger = logger;
        
        var config = configuration.GetSection("AzureDevOps").Get<AzureDevOpsConfiguration>();
        _sentryEnabled = !string.IsNullOrEmpty(config?.Monitoring.Sentry.Dsn) && config.Monitoring.EnablePerformanceTracking;
    }

    public IDisposable TrackOperation(string operationName, Dictionary<string, object>? metadata = null)
    {
        return new SentryOperationTracker(this, _basePerformanceService, operationName, metadata, _sentryEnabled);
    }

    public async Task<PerformanceMetrics> GetMetricsAsync()
    {
        return await _basePerformanceService.GetMetricsAsync();
    }

    public void RecordApiCall(string apiName, long durationMs, bool success)
    {
        _basePerformanceService.RecordApiCall(apiName, durationMs, success);
        
        if (_sentryEnabled)
        {
            // Add breadcrumb for API calls
            SentrySdk.AddBreadcrumb(
                message: $"API call: {apiName}",
                category: "api",
                level: success ? BreadcrumbLevel.Info : BreadcrumbLevel.Error,
                data: new Dictionary<string, string>
                {
                    ["duration"] = $"{durationMs}ms",
                    ["success"] = success.ToString()
                });

            // Track slow API calls
            if (durationMs > 2000)
            {
                SentrySdk.AddBreadcrumb(
                    message: $"Slow API call detected: {apiName}",
                    category: "performance",
                    level: BreadcrumbLevel.Warning,
                    data: new Dictionary<string, string>
                    {
                        ["duration"] = $"{durationMs}ms",
                        ["threshold"] = "2000ms"
                    });
            }
        }
    }

    public ITransaction? StartTransaction(string name, string operation)
    {
        if (!_sentryEnabled) return null;
        
        return SentrySdk.StartTransaction(name, operation);
    }

    public ISpan? StartSpan(string operation, string? description = null)
    {
        if (!_sentryEnabled) return null;
        
        return SentrySdk.GetSpan()?.StartChild(operation, description);
    }

    public void SetUser(string? userId, string? organizationUrl = null)
    {
        if (!_sentryEnabled) return;
        
        SentrySdk.ConfigureScope(scope =>
        {
            scope.User = new SentryUser
            {
                Id = userId,
                Other = organizationUrl != null ? new Dictionary<string, string> { ["organization"] = organizationUrl } : null
            };
        });
    }

    public void SetTag(string key, string value)
    {
        if (!_sentryEnabled) return;
        
        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag(key, value);
        });
    }

    public void SetContext(string key, object context)
    {
        if (!_sentryEnabled) return;
        
        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetExtra(key, context);
        });
    }

    public void CaptureException(Exception exception, string? operation = null)
    {
        if (!_sentryEnabled)
        {
            _logger.LogError(exception, "Error in operation: {Operation}", operation ?? "Unknown");
            return;
        }
        
        SentrySdk.ConfigureScope(scope =>
        {
            if (operation != null)
            {
                scope.SetTag("operation", operation);
            }
        });
        
        SentrySdk.CaptureException(exception);
    }

    public void AddBreadcrumb(string message, string category = "default", BreadcrumbLevel level = BreadcrumbLevel.Info)
    {
        if (!_sentryEnabled) return;
        
        SentrySdk.AddBreadcrumb(message, category, level: level);
    }

    private class SentryOperationTracker : IDisposable
    {
        private readonly ISentryPerformanceService _sentryService;
        private readonly IDisposable _baseTracker;
        private readonly ITransaction? _transaction;
        private readonly ISpan? _span;
        private readonly string _operationName;
        private readonly bool _sentryEnabled;

        public SentryOperationTracker(
            ISentryPerformanceService sentryService,
            IPerformanceService baseService,
            string operationName,
            Dictionary<string, object>? metadata,
            bool sentryEnabled)
        {
            _sentryService = sentryService;
            _operationName = operationName;
            _sentryEnabled = sentryEnabled;
            _baseTracker = baseService.TrackOperation(operationName, metadata);

            if (_sentryEnabled)
            {
                // Create transaction for major operations
                if (IsTransactionWorthy(operationName))
                {
                    _transaction = SentrySdk.StartTransaction(operationName, "mcp.operation");
                    _transaction?.SetTag("operation.type", GetOperationType(operationName));
                    
                    if (metadata != null)
                    {
                        foreach (var item in metadata)
                        {
                            _transaction?.SetExtra($"metadata.{item.Key}", item.Value);
                        }
                    }
                }
                else
                {
                    // Create span for smaller operations
                    _span = SentrySdk.GetSpan()?.StartChild("mcp.operation", operationName);
                    _span?.SetTag("operation.name", operationName);
                }

                // Add breadcrumb
                _sentryService.AddBreadcrumb(
                    message: $"Started operation: {operationName}",
                    category: "operation",
                    level: BreadcrumbLevel.Debug);
            }
        }

        public void Dispose()
        {
            if (_sentryEnabled)
            {
                _transaction?.Finish();
                _span?.Finish();

                _sentryService.AddBreadcrumb(
                    message: $"Completed operation: {_operationName}",
                    category: "operation",
                    level: BreadcrumbLevel.Debug);
            }

            _baseTracker.Dispose();
        }

        private static bool IsTransactionWorthy(string operationName)
        {
            // Major operations that deserve their own transaction
            var transactionOperations = new[]
            {
                "CreateDraftPullRequest",
                "UpdateWorkItemTags",
                "AddPullRequestComment",
                "SearchCode",
                "BatchGetWorkItems",
                "BatchGetFileContents"
            };

            return transactionOperations.Contains(operationName);
        }

        private static string GetOperationType(string operationName)
        {
            return operationName switch
            {
                var op when op.StartsWith("Get") => "read",
                var op when op.StartsWith("List") => "read",
                var op when op.StartsWith("Search") => "search",
                var op when op.StartsWith("Batch") => "batch",
                var op when op.StartsWith("Create") => "write",
                var op when op.StartsWith("Update") => "write",
                var op when op.StartsWith("Add") => "write",
                var op when op.StartsWith("Download") => "download",
                _ => "unknown"
            };
        }
    }
}