using Sentry;
using System.Diagnostics;

namespace AzureDevOps.MCP.Services;

public static class TracingHelper
{
    public static async Task<T> TraceOperation<T>(
        string operationName,
        Func<Task<T>> operation,
        IPerformanceService performance,
        Dictionary<string, object>? metadata = null,
        bool isTransaction = false)
    {
        using var perfTracker = performance.TrackOperation(operationName, metadata);
        using var sentryOperation = isTransaction 
            ? SentrySdk.StartTransaction(operationName, GetOperationCategory(operationName))
            : SentrySdk.GetSpan()?.StartChild("mcp.operation", operationName);

        sentryOperation?.SetTag("operation.name", operationName);
        sentryOperation?.SetTag("operation.type", GetOperationType(operationName));

        if (metadata != null)
        {
            foreach (var item in metadata)
            {
                sentryOperation?.SetExtra($"metadata.{item.Key}", item.Value);
            }
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            
            sentryOperation?.SetTag("success", "true");
            sentryOperation?.SetTag("duration.ms", sw.ElapsedMilliseconds.ToString());
            
            performance.RecordApiCall(operationName, sw.ElapsedMilliseconds, true);
            
            return result;
        }
        catch (Exception ex)
        {
            sentryOperation?.SetTag("success", "false");
            sentryOperation?.SetTag("error.type", ex.GetType().Name);
            sentryOperation?.SetTag("duration.ms", sw.ElapsedMilliseconds.ToString());
            
            performance.RecordApiCall(operationName, sw.ElapsedMilliseconds, false);
            
            SentrySdk.CaptureException(ex);
            throw;
        }
        finally
        {
            sentryOperation?.Finish();
        }
    }

    public static async Task<T> TraceCachedOperation<T>(
        string operationName,
        string cacheKey,
        Func<Task<T>> operation,
        ICacheService cache,
        IPerformanceService performance,
        TimeSpan? expiration = null,
        Dictionary<string, object>? metadata = null) where T : class
    {
        using var transaction = SentrySdk.StartTransaction(operationName, GetOperationCategory(operationName));
        using var perfTracker = performance.TrackOperation(operationName, metadata);

        transaction?.SetTag("cache.enabled", "true");
        transaction?.SetTag("cache.key", cacheKey);
        transaction?.SetTag("operation.type", GetOperationType(operationName));

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            using var span = transaction?.StartChild("azure_devops.api_call", operationName);
            var sw = Stopwatch.StartNew();
            
            try
            {
                span?.SetTag("cache.hit", "false");
                var result = await operation();
                
                // Add result metadata
                if (result is IEnumerable<object> enumerable)
                {
                    var count = enumerable.Count();
                    span?.SetTag("result.count", count.ToString());
                    transaction?.SetExtra("result.count", count);
                }
                
                span?.SetTag("duration.ms", sw.ElapsedMilliseconds.ToString());
                performance.RecordApiCall(operationName, sw.ElapsedMilliseconds, true);
                
                return result;
            }
            catch (Exception ex)
            {
                span?.SetTag("error", "true");
                span?.SetTag("error.type", ex.GetType().Name);
                transaction?.SetTag("error", "true");
                
                performance.RecordApiCall(operationName, sw.ElapsedMilliseconds, false);
                SentrySdk.CaptureException(ex);
                throw;
            }
        }, expiration);
    }

    public static async Task<T> TraceWriteOperation<T>(
        string operationName,
        Func<Task<T>> operation,
        IPerformanceService performance,
        ICacheService cache,
        string[]? cacheKeysToInvalidate = null,
        Dictionary<string, object>? metadata = null)
    {
        using var transaction = SentrySdk.StartTransaction(operationName, "azure_devops.write");
        using var perfTracker = performance.TrackOperation(operationName, metadata);

        transaction?.SetTag("operation.type", "write");
        transaction?.SetTag("operation.name", operationName);

        if (metadata != null)
        {
            foreach (var item in metadata)
            {
                transaction?.SetExtra($"metadata.{item.Key}", item.Value);
            }
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            
            // Invalidate cache after successful write
            if (cacheKeysToInvalidate != null)
            {
                var invalidationTasks = cacheKeysToInvalidate.Select(cache.RemoveAsync);
                await Task.WhenAll(invalidationTasks);
                
                transaction?.SetTag("cache.invalidated", "true");
                transaction?.SetExtra("cache.keys_invalidated", cacheKeysToInvalidate);
            }
            
            transaction?.SetTag("success", "true");
            transaction?.SetTag("duration.ms", sw.ElapsedMilliseconds.ToString());
            performance.RecordApiCall(operationName, sw.ElapsedMilliseconds, true);
            
            return result;
        }
        catch (Exception ex)
        {
            transaction?.SetTag("success", "false");
            transaction?.SetTag("error.type", ex.GetType().Name);
            transaction?.SetTag("duration.ms", sw.ElapsedMilliseconds.ToString());
            
            performance.RecordApiCall(operationName, sw.ElapsedMilliseconds, false);
            SentrySdk.CaptureException(ex);
            throw;
        }
    }

    private static string GetOperationCategory(string operationName)
    {
        return operationName switch
        {
            var op when op.StartsWith("Get") || op.StartsWith("List") => "azure_devops.read",
            var op when op.StartsWith("Search") => "azure_devops.search",
            var op when op.StartsWith("Batch") => "azure_devops.batch",
            var op when op.StartsWith("Create") || op.StartsWith("Update") || op.StartsWith("Add") => "azure_devops.write",
            var op when op.StartsWith("Download") => "azure_devops.download",
            _ => "azure_devops.other"
        };
    }

    private static string GetOperationType(string operationName)
    {
        return operationName switch
        {
            var op when op.StartsWith("Get") || op.StartsWith("List") => "read",
            var op when op.StartsWith("Search") => "search",
            var op when op.StartsWith("Batch") => "batch",
            var op when op.StartsWith("Create") || op.StartsWith("Update") || op.StartsWith("Add") => "write",
            var op when op.StartsWith("Download") => "download",
            _ => "other"
        };
    }

    public static void TrackSlowOperation(string operationName, long durationMs, long threshold = 1000)
    {
        if (durationMs > threshold)
        {
            SentrySdk.AddBreadcrumb(
                message: $"Slow operation detected: {operationName}",
                category: "performance",
                level: BreadcrumbLevel.Warning,
                data: new Dictionary<string, string>
                {
                    ["operation"] = operationName,
                    ["duration"] = $"{durationMs}ms",
                    ["threshold"] = $"{threshold}ms"
                });
                
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("performance.slow_operation", operationName);
                scope.SetExtra("performance.duration_ms", durationMs);
            });
        }
    }

    public static void SetUserContext(string? organizationUrl, string? projectName = null)
    {
        SentrySdk.ConfigureScope(scope =>
        {
            scope.User = new SentryUser
            {
                Other = new Dictionary<string, string>()
            };
            
            if (!string.IsNullOrEmpty(organizationUrl))
            {
                scope.User.Other["organization_url"] = organizationUrl;
                scope.SetTag("azure_devops.organization", ExtractOrgName(organizationUrl));
            }
            
            if (!string.IsNullOrEmpty(projectName))
            {
                scope.User.Other["project_name"] = projectName;
                scope.SetTag("azure_devops.project", projectName);
            }
        });
    }

    private static string ExtractOrgName(string organizationUrl)
    {
        try
        {
            var uri = new Uri(organizationUrl);
            return uri.Segments.LastOrDefault()?.TrimEnd('/') ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}