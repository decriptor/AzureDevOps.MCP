using Microsoft.Extensions.Options;
using AzureDevOps.MCP.Configuration;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Implementation of structured logger factory.
/// </summary>
public sealed class StructuredLoggerFactory : IStructuredLoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<LoggingConfiguration> _config;
    private readonly ISensitiveDataFilter _sensitiveDataFilter;

    public StructuredLoggerFactory(
        ILoggerFactory loggerFactory,
        IOptions<LoggingConfiguration> config,
        ISensitiveDataFilter sensitiveDataFilter)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _sensitiveDataFilter = sensitiveDataFilter ?? throw new ArgumentNullException(nameof(sensitiveDataFilter));
    }

    public ILogger CreateLogger(string categoryName)
    {
        var innerLogger = _loggerFactory.CreateLogger(categoryName);
        return new ModernStructuredLogger(innerLogger, _config, _sensitiveDataFilter);
    }
}