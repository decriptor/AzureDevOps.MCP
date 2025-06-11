namespace AzureDevOps.MCP.Services.Infrastructure;

public record ExceptionInfo
{
    public string Type { get; init; } = "";
    public string Message { get; init; } = "";
    public string? StackTrace { get; init; }
    public Dictionary<string, string?>? Data { get; init; }
    public List<ExceptionInfo>? InnerExceptions { get; init; }
}