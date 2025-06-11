namespace AzureDevOps.MCP.Services.Infrastructure;

// Modern record types for structured data
public record StructuredLogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Level { get; init; } = "";
    public int EventId { get; init; }
    public string? EventName { get; init; }
    public string Message { get; init; } = "";
    public ExceptionInfo? Exception { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? ParentId { get; init; }
    public Dictionary<string, object?> Properties { get; init; } = new();
    public string MachineName { get; init; } = "";
    public int ProcessId { get; init; }
    public int ThreadId { get; init; }
    public string? Scope { get; init; }
}