using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Environment-specific configuration.
/// </summary>
public class EnvironmentConfiguration
{
    /// <summary>
    /// Environment name (Development, Staging, Production).
    /// </summary>
    [Required]
    public string Name { get; set; } = "Production";

    /// <summary>
    /// Application version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Build number or commit hash.
    /// </summary>
    public string? Build { get; set; }

    /// <summary>
    /// Deployment timestamp.
    /// </summary>
    public DateTime? DeployedAt { get; set; }

    /// <summary>
    /// Instance identifier for tracking.
    /// </summary>
    public string InstanceId { get; set; } = Environment.MachineName;

    /// <summary>
    /// Enable development features.
    /// </summary>
    public bool EnableDevelopmentFeatures { get; set; } = false;

    /// <summary>
    /// Enable debug endpoints.
    /// </summary>
    public bool EnableDebugEndpoints { get; set; } = false;

    /// <summary>
    /// Enable metrics endpoints.
    /// </summary>
    public bool EnableMetricsEndpoints { get; set; } = true;

    /// <summary>
    /// Data protection key ring path.
    /// </summary>
    public string? DataProtectionKeyPath { get; set; }

    /// <summary>
    /// Custom configuration values for environment-specific needs.
    /// </summary>
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}