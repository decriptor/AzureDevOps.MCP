namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Factory for creating structured loggers.
/// </summary>
public interface IStructuredLoggerFactory
{
	ILogger CreateLogger (string categoryName);
}