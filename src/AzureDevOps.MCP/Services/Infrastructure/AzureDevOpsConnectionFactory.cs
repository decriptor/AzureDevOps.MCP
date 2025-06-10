using System.Collections.Concurrent;
using AzureDevOps.MCP.Common;
using AzureDevOps.MCP.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.MCP.Services.Infrastructure;

public class AzureDevOpsConnectionFactory : IAzureDevOpsConnectionFactory
{
	readonly AzureDevOpsConfiguration _config;
	readonly ILogger<AzureDevOpsConnectionFactory> _logger;
	readonly SemaphoreSlim _connectionLock = new (1, 1);
	readonly ConcurrentDictionary<Type, object> _clientCache = new ();

	VssConnection? _connection;
	DateTime _lastConnectionTest = DateTime.MinValue;
	bool _disposed;

	public AzureDevOpsConnectionFactory (
		IOptions<AzureDevOpsConfiguration> config,
		ILogger<AzureDevOpsConnectionFactory> logger)
	{
		_config = config.Value ?? throw new ArgumentNullException (nameof (config));
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));

		ValidateConfiguration ();
	}

	public async Task<VssConnection> GetConnectionAsync (CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed ();

		if (_connection != null && IsConnectionValid ()) {
			return _connection;
		}

		await _connectionLock.WaitAsync (cancellationToken);
		try {
			// Double-check pattern
			if (_connection != null && IsConnectionValid ()) {
				return _connection;
			}

			await CreateNewConnectionAsync (cancellationToken);
			return _connection!;
		} finally {
			_connectionLock.Release ();
		}
	}

	public async Task<T> GetClientAsync<T> (CancellationToken cancellationToken = default) where T : VssHttpClientBase
	{
		ThrowIfDisposed ();

		var clientType = typeof (T);

		// Check cache first
		if (_clientCache.TryGetValue (clientType, out var cachedClient) && cachedClient is T client) {
			return client;
		}

		var connection = await GetConnectionAsync (cancellationToken);

		try {
			var newClient = await connection.GetClientAsync<T> (cancellationToken);
			_clientCache.TryAdd (clientType, newClient);
			return newClient;
		} catch (Exception ex) {
			_logger.LogError (ex, "Failed to get client of type {ClientType}", clientType.Name);
			throw;
		}
	}

	public async Task<bool> TestConnectionAsync (CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed ();

		try {
			var connection = await GetConnectionAsync (cancellationToken);

			// Test with a lightweight API call
			using var projectClient = await connection.GetClientAsync<Microsoft.TeamFoundation.Core.WebApi.ProjectHttpClient> (cancellationToken);
			await projectClient.GetProjects (top: 1);

			_lastConnectionTest = DateTime.UtcNow;
			_logger.LogDebug ("Connection test successful");
			return true;
		} catch (Exception ex) {
			_logger.LogWarning (ex, "Connection test failed");
			InvalidateConnection ();
			return false;
		}
	}

	public void InvalidateConnection ()
	{
		_connectionLock.Wait ();
		try {
			_connection?.Dispose ();
			_connection = null;
			_clientCache.Clear ();
			_lastConnectionTest = DateTime.MinValue;

			_logger.LogInformation ("Connection invalidated");
		} finally {
			_connectionLock.Release ();
		}
	}

	async Task CreateNewConnectionAsync (CancellationToken cancellationToken)
	{
		try {
			_logger.LogDebug ("Creating new Azure DevOps connection to {OrganizationUrl}", _config.OrganizationUrl);

			var credentials = new VssBasicCredential (string.Empty, _config.PersonalAccessToken);
			var newConnection = new VssConnection (new Uri (_config.OrganizationUrl), credentials);

			// Test the connection
			await newConnection.ConnectAsync (cancellationToken);

			// Dispose old connection if exists
			_connection?.Dispose ();

			_connection = newConnection;
			_clientCache.Clear (); // Clear client cache when connection changes
			_lastConnectionTest = DateTime.UtcNow;

			_logger.LogInformation ("Successfully connected to Azure DevOps");
		} catch (Exception ex) {
			_logger.LogError (ex, "Failed to create Azure DevOps connection");
			throw new InvalidOperationException ("Failed to establish connection to Azure DevOps", ex);
		}
	}

	bool IsConnectionValid ()
	{
		if (_connection == null) {
			return false;
		}

		// Test connection every 5 minutes
		var testInterval = TimeSpan.FromMinutes (5);
		return DateTime.UtcNow - _lastConnectionTest < testInterval;
	}

	void ValidateConfiguration ()
	{
		if (string.IsNullOrWhiteSpace (_config.OrganizationUrl)) {
			throw new InvalidOperationException ("Azure DevOps organization URL is not configured");
		}

		if (string.IsNullOrWhiteSpace (_config.PersonalAccessToken)) {
			throw new InvalidOperationException ("Azure DevOps personal access token is not configured");
		}

		if (!Uri.TryCreate (_config.OrganizationUrl, UriKind.Absolute, out var uri) || !uri.Scheme.StartsWith ("http")) {
			throw new InvalidOperationException ("Azure DevOps organization URL is not a valid HTTP/HTTPS URL");
		}
	}

	void ThrowIfDisposed ()
	{
		if (_disposed) {
			throw new ObjectDisposedException (nameof (AzureDevOpsConnectionFactory));
		}
	}

	public void Dispose ()
	{
		Dispose (true);
		GC.SuppressFinalize (this);
	}

	public async ValueTask DisposeAsync ()
	{
		await DisposeAsyncCore ();
		Dispose (false);
		GC.SuppressFinalize (this);
	}

	protected virtual void Dispose (bool disposing)
	{
		if (!_disposed && disposing) {
			_connection?.Dispose ();
			_connectionLock?.Dispose ();
			_clientCache?.Clear ();
			_disposed = true;
		}
	}

	protected virtual ValueTask DisposeAsyncCore ()
	{
		if (_connection != null) {
			_connection.Dispose ();
			_connection = null;
		}

		if (_connectionLock != null) {
			_connectionLock.Dispose ();
		}

		_clientCache?.Clear ();
		_disposed = true;

		return ValueTask.CompletedTask;
	}
}