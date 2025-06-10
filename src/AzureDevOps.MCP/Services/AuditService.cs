using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AzureDevOps.MCP.Services;

public class AuditService : IAuditService
{
	readonly ILogger<AuditService> _logger;
	readonly string _auditFilePath;

	public AuditService (ILogger<AuditService> logger)
	{
		_logger = logger;
		_auditFilePath = Path.Combine (Directory.GetCurrentDirectory (), "audit", "write-operations.json");
		Directory.CreateDirectory (Path.GetDirectoryName (_auditFilePath)!);
	}

	public async Task LogWriteOperationAsync (WriteOperationAuditEntry entry)
	{
		try {
			// Log to structured logging
			_logger.LogInformation ("Write operation performed: {Operation} on {TargetResource} in project {ProjectName}. Success: {Success}",
				entry.Operation, entry.TargetResource, entry.ProjectName, entry.Success);

			// Also persist to file for audit trail
			var entries = await LoadAuditEntriesAsync ();
			entries.Add (entry);

			// Keep only last 1000 entries
			if (entries.Count > 1000) {
				entries = entries.OrderByDescending (e => e.Timestamp).Take (1000).ToList ();
			}

			var json = JsonSerializer.Serialize (entries, new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync (_auditFilePath, json);
		} catch (Exception ex) {
			_logger.LogError (ex, "Failed to write audit log entry");
		}
	}

	public async Task<IEnumerable<WriteOperationAuditEntry>> GetAuditLogsAsync (DateTime? since = null)
	{
		var entries = await LoadAuditEntriesAsync ();

		if (since.HasValue) {
			entries = entries.Where (e => e.Timestamp >= since.Value).ToList ();
		}

		return entries.OrderByDescending (e => e.Timestamp);
	}

	async Task<List<WriteOperationAuditEntry>> LoadAuditEntriesAsync ()
	{
		if (!File.Exists (_auditFilePath)) {
			return new List<WriteOperationAuditEntry> ();
		}

		try {
			var json = await File.ReadAllTextAsync (_auditFilePath);
			return JsonSerializer.Deserialize<List<WriteOperationAuditEntry>> (json) ?? new List<WriteOperationAuditEntry> ();
		} catch (Exception ex) {
			_logger.LogError (ex, "Failed to load audit entries");
			return new List<WriteOperationAuditEntry> ();
		}
	}

	public static string HashToken (string token)
	{
		using var sha256 = SHA256.Create ();
		var bytes = sha256.ComputeHash (Encoding.UTF8.GetBytes (token));
		return Convert.ToBase64String (bytes).Substring (0, 8); // First 8 chars for identification
	}
}