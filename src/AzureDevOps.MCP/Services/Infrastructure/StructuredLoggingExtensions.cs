using AzureDevOps.MCP.Configuration;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Extension methods for structured logging.
/// </summary>
public static class StructuredLoggingExtensions
{
    public static IServiceCollection AddModernStructuredLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LoggingConfiguration>(configuration.GetSection("Logging"));
        services.AddSingleton<ISensitiveDataFilter, ModernSensitiveDataFilter>();
        
        // Register structured logger factory
        services.AddSingleton<IStructuredLoggerFactory, StructuredLoggerFactory>();
        
        return services;
    }

    public static PerformanceLogger LogPerformance(this ILogger logger, string operationName, Dictionary<string, object?>? properties = null)
    {
        return new PerformanceLogger(logger, operationName, properties);
    }

    public static IDisposable BeginOperationScope(this ILogger logger, string operationName, Dictionary<string, object?>? properties = null)
    {
        var scopeProperties = new Dictionary<string, object?>
        {
            ["operation"] = operationName,
            ["start_time"] = DateTimeOffset.UtcNow.ToString("O")
        };

        if (properties != null)
        {
            foreach (var (key, value) in properties)
            {
                scopeProperties[key] = value;
            }
        }

        return logger.BeginScope(scopeProperties) ?? NullScope.Instance;
    }
}

/// <summary>
/// Null object pattern implementation for disposable scope.
/// </summary>
internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();
    private NullScope() { }
    public void Dispose() { }
}