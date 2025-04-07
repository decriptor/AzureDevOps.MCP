namespace AzureDevOps.MCP.Security;

/// <summary>
/// Represents a cached secret with expiration.
/// </summary>
record CachedSecret (string Value, DateTime ExpiresAt)
{
	/// <summary>
	/// Indicates whether the cached secret has expired.
	/// </summary>
	public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}