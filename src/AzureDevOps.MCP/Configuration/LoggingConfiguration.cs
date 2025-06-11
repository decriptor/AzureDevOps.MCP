using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Comprehensive logging configuration.
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Minimum log level (default: Information).
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Enable structured logging with JSON format.
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// Enable console logging.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Enable file logging.
    /// </summary>
    public bool EnableFileLogging { get; set; } = false;

    /// <summary>
    /// Log file path (when file logging is enabled).
    /// </summary>
    public string? LogFilePath { get; set; }

    /// <summary>
    /// Maximum log file size in MB (default: 100).
    /// </summary>
    [Range(10, 1000)]
    public int MaxLogFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Number of log files to retain (default: 10).
    /// </summary>
    [Range(1, 50)]
    public int RetainedLogFileCount { get; set; } = 10;

    /// <summary>
    /// Enable sensitive data filtering in logs.
    /// </summary>
    public bool EnableSensitiveDataFiltering { get; set; } = true;

    /// <summary>
    /// Enable performance logging for detailed operation tracking.
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = false;

    /// <summary>
    /// Enable SQL query logging.
    /// </summary>
    public bool EnableSqlLogging { get; set; } = false;

    /// <summary>
    /// Patterns to filter from logs (regex patterns).
    /// </summary>
    public List<string> SensitiveDataPatterns { get; set; } = new()
    {
        @"pat_[a-zA-Z0-9]{52}",  // Azure DevOps PAT
        @"Authorization:\s*Bearer\s+[a-zA-Z0-9\-._~+/]+=*",  // Bearer tokens
        @"password['""][^'""]+['""]",  // Password fields
        @"secret['""][^'""]+['""]"     // Secret fields
    };
}