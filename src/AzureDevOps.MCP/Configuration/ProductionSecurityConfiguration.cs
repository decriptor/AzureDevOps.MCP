namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Security configuration for production deployments.
/// </summary>
public class ProductionSecurityConfiguration
{
    /// <summary>
    /// Enable Azure Key Vault for secrets management.
    /// </summary>
    public bool EnableKeyVault { get; set; } = false;

    /// <summary>
    /// Azure Key Vault URL.
    /// </summary>
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// Managed identity client ID for Key Vault access.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Enable API key authentication.
    /// </summary>
    public bool EnableApiKeyAuth { get; set; } = false;

    /// <summary>
    /// Valid API keys for authentication (stored as hashes).
    /// </summary>
    public List<string> ApiKeyHashes { get; set; } = new();

    /// <summary>
    /// Enable IP whitelisting.
    /// </summary>
    public bool EnableIpWhitelist { get; set; } = false;

    /// <summary>
    /// Allowed IP addresses or CIDR ranges.
    /// </summary>
    public List<string> AllowedIpRanges { get; set; } = new();

    /// <summary>
    /// Enable request signing for additional security.
    /// </summary>
    public bool EnableRequestSigning { get; set; } = false;

    /// <summary>
    /// CORS origins for web access.
    /// </summary>
    public List<string> CorsOrigins { get; set; } = new();
}