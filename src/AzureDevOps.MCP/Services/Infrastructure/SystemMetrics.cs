namespace AzureDevOps.MCP.Services.Infrastructure;

public record SystemMetrics(
    long TotalMemoryBytes,
    long UsedMemoryBytes,
    int GCGen0Collections,
    int GCGen1Collections,
    int GCGen2Collections,
    int ThreadPoolWorkerThreads,
    int ThreadPoolCompletionPortThreads,
    int ProcessorCount,
    long WorkingSetBytes
);