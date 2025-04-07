namespace AzureDevOps.MCP.Security;

public interface ISecretManager
{
	Task<string> GetSecretAsync (string secretName, CancellationToken cancellationToken = default);
	Task<bool> SecretExistsAsync (string secretName, CancellationToken cancellationToken = default);
	Task RefreshSecretsAsync (CancellationToken cancellationToken = default);
}

public class EnvironmentSecretManager : ISecretManager
{
	readonly ILogger<EnvironmentSecretManager> _logger;
	readonly Dictionary<string, string> _secretCache = [];
	readonly SemaphoreSlim _cacheLock = new (1, 1);
	DateTime _lastRefresh = DateTime.MinValue;
	readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes (30);

	public EnvironmentSecretManager (ILogger<EnvironmentSecretManager> logger)
	{
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
	}

	public async Task<string> GetSecretAsync (string secretName, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace (secretName)) {
			throw new ArgumentException ("Secret name cannot be null or empty", nameof (secretName));
		}

		await _cacheLock.WaitAsync (cancellationToken);
		try {
			// Refresh cache if expired
			if (DateTime.UtcNow - _lastRefresh > _cacheExpiry) {
				await RefreshSecretsInternalAsync ();
			}

			if (_secretCache.TryGetValue (secretName, out var secret)) {
				return secret;
			}

			// Try direct environment variable lookup
			var envVarName = $"AZDO_{secretName.ToUpperInvariant ()}";
			var envSecret = Environment.GetEnvironmentVariable (envVarName);

			if (!string.IsNullOrEmpty (envSecret)) {
				_secretCache[secretName] = envSecret;
				return envSecret;
			}

			throw new InvalidOperationException ($"Secret '{secretName}' not found in environment variables. Expected: {envVarName}");
		} finally {
			_cacheLock.Release ();
		}
	}

	public async Task<bool> SecretExistsAsync (string secretName, CancellationToken cancellationToken = default)
	{
		try {
			await GetSecretAsync (secretName, cancellationToken);
			return true;
		} catch (InvalidOperationException) {
			return false;
		}
	}

	public async Task RefreshSecretsAsync (CancellationToken cancellationToken = default)
	{
		await _cacheLock.WaitAsync (cancellationToken);
		try {
			await RefreshSecretsInternalAsync ();
		} finally {
			_cacheLock.Release ();
		}
	}

	Task RefreshSecretsInternalAsync ()
	{
		_secretCache.Clear ();
		_lastRefresh = DateTime.UtcNow;

		_logger.LogDebug ("Secret cache refreshed at {Timestamp}", _lastRefresh);

		return Task.CompletedTask;
	}
}

public class SecretValidationException : Exception
{
	public string SecretName { get; }

	public SecretValidationException (string secretName, string message)
		: base ($"Secret validation failed for '{secretName}': {message}")
	{
		SecretName = secretName;
	}

	public SecretValidationException (string secretName, string message, Exception innerException)
		: base ($"Secret validation failed for '{secretName}': {message}", innerException)
	{
		SecretName = secretName;
	}
}