using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AzureDevOps.MCP.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AzureDevOps.MCP.Security;

/// <summary>
/// Production-ready secret manager with Azure Key Vault integration.
/// Supports multiple secret sources with fallback mechanisms.
/// </summary>
public class ProductionSecretManager : ISecretManager, IDisposable
{
    readonly ILogger<ProductionSecretManager> _logger;
    readonly ProductionSecurityConfiguration _config;
    readonly SecretClient? _keyVaultClient;
    readonly ConcurrentDictionary<string, CachedSecret> _secretCache = new();
    readonly SemaphoreSlim _refreshLock = new(1, 1);
    readonly Timer _refreshTimer;
    
    readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);
    readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(10);
    
    bool _disposed = false;

    public ProductionSecretManager(
        ILogger<ProductionSecretManager> logger,
        IOptions<ProductionConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value?.Security ?? throw new ArgumentNullException(nameof(config));

        // Initialize Key Vault client if enabled
        if (_config.EnableKeyVault && !string.IsNullOrWhiteSpace(_config.KeyVaultUrl))
        {
            try
            {
                var keyVaultUri = new Uri(_config.KeyVaultUrl);
                var credential = CreateCredential();
                _keyVaultClient = new SecretClient(keyVaultUri, credential);
                
                _logger.LogInformation("Azure Key Vault client initialized for {KeyVaultUrl}", _config.KeyVaultUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Key Vault client for {KeyVaultUrl}", _config.KeyVaultUrl);
                throw;
            }
        }

        // Start periodic refresh timer
        _refreshTimer = new Timer(RefreshExpiredSecrets, null, _refreshInterval, _refreshInterval);
    }

    /// <summary>
    /// Retrieves a secret from the configured sources with caching.
    /// </summary>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));

        _logger.LogDebug("Retrieving secret {SecretName}", secretName);

        // Check cache first
        if (_secretCache.TryGetValue(secretName, out var cachedSecret) && !cachedSecret.IsExpired)
        {
            _logger.LogDebug("Secret {SecretName} found in cache", secretName);
            return cachedSecret.Value;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring lock
            if (_secretCache.TryGetValue(secretName, out cachedSecret) && !cachedSecret.IsExpired)
            {
                return cachedSecret.Value;
            }

            var secret = await RetrieveSecretFromSourcesAsync(secretName, cancellationToken);
            
            // Cache the secret
            _secretCache[secretName] = new CachedSecret(secret, DateTime.UtcNow.Add(_cacheExpiry));
            
            _logger.LogInformation("Secret {SecretName} retrieved and cached", secretName);
            return secret;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Checks if a secret exists in any of the configured sources.
    /// </summary>
    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            await GetSecretAsync(secretName, cancellationToken);
            return true;
        }
        catch (SecretNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if secret {SecretName} exists", secretName);
            return false;
        }
    }

    /// <summary>
    /// Forces a refresh of all cached secrets.
    /// </summary>
    public async Task RefreshSecretsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting manual refresh of all cached secrets");

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var secretNames = _secretCache.Keys.ToList();
            var refreshTasks = secretNames.Select(async secretName =>
            {
                try
                {
                    var secret = await RetrieveSecretFromSourcesAsync(secretName, cancellationToken);
                    _secretCache[secretName] = new CachedSecret(secret, DateTime.UtcNow.Add(_cacheExpiry));
                    _logger.LogDebug("Refreshed secret {SecretName}", secretName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh secret {SecretName}", secretName);
                    // Remove failed secret from cache
                    _secretCache.TryRemove(secretName, out _);
                }
            });

            await Task.WhenAll(refreshTasks);
            _logger.LogInformation("Completed refresh of {Count} cached secrets", secretNames.Count);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Retrieves multiple secrets in parallel for efficiency.
    /// </summary>
    public async Task<Dictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> secretNames, 
        CancellationToken cancellationToken = default)
    {
        var secretTasks = secretNames.Select(async name =>
        {
            try
            {
                var value = await GetSecretAsync(name, cancellationToken);
                return new KeyValuePair<string, string>(name, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve secret {SecretName}", name);
                throw;
            }
        });

        var results = await Task.WhenAll(secretTasks);
        return results.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Validates that all required secrets are available.
    /// </summary>
    public async Task ValidateRequiredSecretsAsync(
        IEnumerable<string> requiredSecrets, 
        CancellationToken cancellationToken = default)
    {
        var validationTasks = requiredSecrets.Select(async secretName =>
        {
            if (!await SecretExistsAsync(secretName, cancellationToken))
            {
                throw new SecretValidationException(secretName, "Required secret is not available");
            }
        });

        await Task.WhenAll(validationTasks);
        _logger.LogInformation("All required secrets validated successfully");
    }

    /// <summary>
    /// Creates credentials for Azure Key Vault access.
    /// </summary>
    DefaultAzureCredential CreateCredential()
    {
        var options = new DefaultAzureCredentialOptions();
        
        // Use managed identity if specified
        if (!string.IsNullOrWhiteSpace(_config.ManagedIdentityClientId))
        {
            options.ManagedIdentityClientId = _config.ManagedIdentityClientId;
            _logger.LogDebug("Using managed identity with client ID {ClientId}", _config.ManagedIdentityClientId);
        }

        // Exclude interactive browser credential in production
        options.ExcludeInteractiveBrowserCredential = true;
        options.ExcludeVisualStudioCodeCredential = true;
        options.ExcludeVisualStudioCredential = true;

        return new DefaultAzureCredential(options);
    }

    /// <summary>
    /// Retrieves a secret from configured sources with fallback logic.
    /// </summary>
    async Task<string> RetrieveSecretFromSourcesAsync(string secretName, CancellationToken cancellationToken)
    {
        var lastException = default(Exception);

        // Try Azure Key Vault first (if enabled)
        if (_keyVaultClient != null)
        {
            try
            {
                var response = await _keyVaultClient.GetSecretAsync(secretName, null, cancellationToken);
                _logger.LogDebug("Secret {SecretName} retrieved from Azure Key Vault", secretName);
                return response.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve secret {SecretName} from Azure Key Vault, trying fallback sources", secretName);
                lastException = ex;
            }
        }

        // Fallback to environment variables
        var envVarName = $"AZDO_{secretName.ToUpperInvariant()}";
        var envSecret = Environment.GetEnvironmentVariable(envVarName);

        if (!string.IsNullOrEmpty(envSecret))
        {
            _logger.LogDebug("Secret {SecretName} retrieved from environment variable {EnvVar}", secretName, envVarName);
            return envSecret;
        }

        // Try alternative environment variable naming
        var altEnvVarName = secretName.ToUpperInvariant().Replace("-", "_");
        var altEnvSecret = Environment.GetEnvironmentVariable(altEnvVarName);

        if (!string.IsNullOrEmpty(altEnvSecret))
        {
            _logger.LogDebug("Secret {SecretName} retrieved from alternative environment variable {EnvVar}", secretName, altEnvVarName);
            return altEnvSecret;
        }

        // If all sources fail, throw a comprehensive exception
        var errorMessage = $"Secret '{secretName}' not found in any configured source. " +
                          $"Tried: Azure Key Vault (enabled: {_keyVaultClient != null}), " +
                          $"Environment variables: {envVarName}, {altEnvVarName}";

        if (lastException != null)
        {
            throw new SecretNotFoundException(secretName, errorMessage, lastException);
        }

        throw new SecretNotFoundException(secretName, errorMessage);
    }

    /// <summary>
    /// Timer callback to refresh expired secrets.
    /// </summary>
    void RefreshExpiredSecrets(object? state)
    {
        if (_disposed) return;

        Task.Run(async () =>
        {
            try
            {
                var expiredSecrets = _secretCache
                    .Where(kv => kv.Value.IsExpired)
                    .Select(kv => kv.Key)
                    .ToList();

                if (expiredSecrets.Any())
                {
                    _logger.LogDebug("Refreshing {Count} expired secrets", expiredSecrets.Count);
                    
                    foreach (var secretName in expiredSecrets)
                    {
                        try
                        {
                            var secret = await RetrieveSecretFromSourcesAsync(secretName, CancellationToken.None);
                            _secretCache[secretName] = new CachedSecret(secret, DateTime.UtcNow.Add(_cacheExpiry));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to refresh expired secret {SecretName}", secretName);
                            _secretCache.TryRemove(secretName, out _);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled secret refresh");
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        _refreshTimer?.Dispose();
        _refreshLock?.Dispose();
        _disposed = true;

        _logger.LogDebug("ProductionSecretManager disposed");
    }
}

/// <summary>
/// Represents a cached secret with expiration.
/// </summary>
internal record CachedSecret(string Value, DateTime ExpiresAt)
{
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Exception thrown when a secret cannot be found in any configured source.
/// </summary>
public class SecretNotFoundException : Exception
{
    public string SecretName { get; }

    public SecretNotFoundException(string secretName, string message) 
        : base(message)
    {
        SecretName = secretName;
    }

    public SecretNotFoundException(string secretName, string message, Exception innerException) 
        : base(message, innerException)
    {
        SecretName = secretName;
    }
}

/// <summary>
/// Secret manager factory for dependency injection.
/// </summary>
public static class SecretManagerFactory
{
    /// <summary>
    /// Creates the appropriate secret manager based on configuration.
    /// </summary>
    public static ISecretManager CreateSecretManager(
        IServiceProvider serviceProvider,
        IOptions<ProductionConfiguration> config)
    {
        var securityConfig = config.Value.Security;
        
        if (securityConfig.EnableKeyVault)
        {
            return serviceProvider.GetRequiredService<ProductionSecretManager>();
        }

        return serviceProvider.GetRequiredService<EnvironmentSecretManager>();
    }
}