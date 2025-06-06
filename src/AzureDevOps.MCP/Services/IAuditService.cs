namespace AzureDevOps.MCP.Services;

public interface IAuditService
{
    Task LogWriteOperationAsync(WriteOperationAuditEntry entry);
    Task<IEnumerable<WriteOperationAuditEntry>> GetAuditLogsAsync(DateTime? since = null);
}

public class WriteOperationAuditEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public required string Operation { get; set; }
    public required string TargetResource { get; set; }
    public required string ProjectName { get; set; }
    public string? AdditionalContext { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PersonalAccessTokenHash { get; set; }
}