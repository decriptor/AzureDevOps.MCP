using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.MCP.Infrastructure;

public interface IConnectionFactory : IAsyncDisposable, IDisposable
{
    Task<VssConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default) where T : VssHttpClientBase;
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    void InvalidateConnection();
    bool IsHealthy { get; }
}

public class SafeConnectionFactory : IConnectionFactory
{
    private readonly ILogger<SafeConnectionFactory> _logger;
    private readonly Security.ISecretManager _secretManager;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<Type, object> _clientCache = new();
    
    private VssConnection? _connection;
    private DateTime _lastConnectionTime = DateTime.MinValue;
    private DateTime _lastHealthCheck = DateTime.MinValue;
    private bool _isHealthy = false;
    private bool _disposed = false;

    private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(5);

    public bool IsHealthy => _isHealthy && DateTime.UtcNow - _lastHealthCheck < _healthCheckInterval;

    public SafeConnectionFactory(
        ILogger<SafeConnectionFactory> logger,
        Security.ISecretManager secretManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretManager = secretManager ?? throw new ArgumentNullException(nameof(secretManager));
    }

    public async Task<VssConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection?.HasAuthenticated == true && IsConnectionValid())
            {
                return _connection;
            }

            await CreateNewConnectionAsync(cancellationToken);
            return _connection!;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default) where T : VssHttpClientBase
    {
        var clientType = typeof(T);
        
        // Try to get from cache first
        if (_clientCache.TryGetValue(clientType, out var cachedClient) && cachedClient is T typedClient)
        {
            return typedClient;
        }

        var connection = await GetConnectionAsync(cancellationToken);
        var client = connection.GetClient<T>();
        
        // Cache the client for reuse
        _clientCache.AddOrUpdate(clientType, client, (_, _) => client);
        
        return client;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken);
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_connectionTimeout);
            
            // Test with a simple API call
            var client = await GetClientAsync<Microsoft.TeamFoundation.Core.WebApi.ProjectHttpClient>(cts.Token);
            await client.GetProjects(top: 1, cancellationToken: cts.Token);
            
            _isHealthy = true;
            _lastHealthCheck = DateTime.UtcNow;
            
            _logger.LogDebug("Connection test successful");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Connection test cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _isHealthy = false;
            _lastHealthCheck = DateTime.UtcNow;
            
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    public void InvalidateConnection()
    {
        _connectionLock.Wait();
        try
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
                _clientCache.Clear();
                _isHealthy = false;
                
                _logger.LogInformation("Connection invalidated and cleared");
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task CreateNewConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Clean up existing connection
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
                _clientCache.Clear();
            }

            // Get fresh credentials
            var organizationUrl = await _secretManager.GetSecretAsync("OrganizationUrl", cancellationToken);
            var personalAccessToken = await _secretManager.GetSecretAsync("PersonalAccessToken", cancellationToken);

            // Validate configuration
            var urlValidation = Validation.ValidationHelper.ValidateOrganizationUrl(organizationUrl);
            urlValidation.ThrowIfInvalid();

            var tokenValidation = Validation.ValidationHelper.ValidatePersonalAccessToken(personalAccessToken);
            tokenValidation.ThrowIfInvalid();

            // Create new connection
            var credentials = new Microsoft.VisualStudio.Services.Common.VssBasicCredential(string.Empty, personalAccessToken);
            _connection = new VssConnection(new Uri(organizationUrl), credentials);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_connectionTimeout);

            await _connection.ConnectAsync(cts.Token);
            
            _lastConnectionTime = DateTime.UtcNow;
            _isHealthy = true;
            _lastHealthCheck = DateTime.UtcNow;

            _logger.LogInformation("Successfully created new Azure DevOps connection");
        }
        catch (Exception ex)
        {
            _isHealthy = false;
            _logger.LogError(ex, "Failed to create Azure DevOps connection");
            throw new InvalidOperationException("Failed to establish connection to Azure DevOps", ex);
        }
    }

    private bool IsConnectionValid()
    {
        if (_connection == null || !_connection.HasAuthenticated)
            return false;

        // Consider connection stale after 30 minutes
        return DateTime.UtcNow - _lastConnectionTime < TimeSpan.FromMinutes(30);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SafeConnectionFactory));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _connection?.Dispose();
            _connectionLock.Dispose();
            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
        }
        
        _clientCache.Clear();
    }
}