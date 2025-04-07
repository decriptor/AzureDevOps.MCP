namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Sensitive data filter using modern .NET 9 regex patterns.
/// </summary>
public interface ISensitiveDataFilter
{
	StructuredLogEntry FilterSensitiveData (StructuredLogEntry entry);
}