using System.Diagnostics;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Performance-aware logger for high-frequency operations.
/// </summary>
public sealed class PerformanceLogger : IDisposable
{
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch;
    private readonly string _operationName;
    private readonly Dictionary<string, object?> _properties;
    private readonly Activity? _activity;

    public PerformanceLogger(ILogger logger, string operationName, Dictionary<string, object?>? properties = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _properties = properties ?? new Dictionary<string, object?>();
        _stopwatch = Stopwatch.StartNew();
        
        // Start activity for distributed tracing
        _activity = new Activity(operationName);
        _activity.Start();
        
        // Add properties as tags
        foreach (var (key, value) in _properties)
        {
            _activity.SetTag(key, value?.ToString());
        }

        _logger.LogDebug("Operation {OperationName} started", operationName);
    }

    public void AddProperty(string key, object? value)
    {
        _properties[key] = value;
        _activity?.SetTag(key, value?.ToString());
    }

    public void LogMilestone(string milestone, Dictionary<string, object?>? additionalProperties = null)
    {
        var allProperties = new Dictionary<string, object?>(_properties)
        {
            ["milestone"] = milestone,
            ["elapsed_ms"] = _stopwatch.ElapsedMilliseconds
        };

        if (additionalProperties != null)
        {
            foreach (var (key, value) in additionalProperties)
            {
                allProperties[key] = value;
            }
        }

        _logger.LogDebug("Operation {OperationName} milestone: {Milestone} at {ElapsedMs}ms", 
            _operationName, milestone, _stopwatch.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _activity?.Stop();

        var finalProperties = new Dictionary<string, object?>(_properties)
        {
            ["duration_ms"] = _stopwatch.ElapsedMilliseconds,
            ["success"] = true
        };

        _logger.LogInformation("Operation {OperationName} completed in {DurationMs}ms", 
            _operationName, _stopwatch.ElapsedMilliseconds);
        
        _activity?.Dispose();
    }

    public void Fail(Exception? exception = null, string? reason = null)
    {
        _stopwatch.Stop();
        
        var finalProperties = new Dictionary<string, object?>(_properties)
        {
            ["duration_ms"] = _stopwatch.ElapsedMilliseconds,
            ["success"] = false,
            ["failure_reason"] = reason
        };

        if (exception != null)
        {
            _logger.LogError(exception, "Operation {OperationName} failed after {DurationMs}ms: {Reason}", 
                _operationName, _stopwatch.ElapsedMilliseconds, reason ?? exception.Message);
        }
        else
        {
            _logger.LogWarning("Operation {OperationName} failed after {DurationMs}ms: {Reason}", 
                _operationName, _stopwatch.ElapsedMilliseconds, reason ?? "Unknown reason");
        }
        
        _activity?.SetStatus(ActivityStatusCode.Error, reason ?? exception?.Message);
        _activity?.Stop();
        _activity?.Dispose();
    }
}