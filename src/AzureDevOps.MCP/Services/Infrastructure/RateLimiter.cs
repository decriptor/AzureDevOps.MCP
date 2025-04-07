using System.Collections.Concurrent;

namespace AzureDevOps.MCP.Services.Infrastructure;

public class SlidingWindowRateLimiter : IRateLimiter
{
	readonly RateLimitOptions _options;
	readonly ILogger<SlidingWindowRateLimiter> _logger;
	readonly ConcurrentDictionary<string, RequestWindow> _windows = new ();
	readonly Timer _cleanupTimer;

	public SlidingWindowRateLimiter (RateLimitOptions options, ILogger<SlidingWindowRateLimiter> logger)
	{
		_options = options ?? throw new ArgumentNullException (nameof (options));
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));

		_cleanupTimer = new Timer (CleanupExpiredWindows, null, TimeSpan.FromMinutes (1), TimeSpan.FromMinutes (1));
	}

	public Task<bool> TryAcquireAsync (string identifier, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty (identifier)) {
			throw new ArgumentException ("Identifier cannot be null or empty", nameof (identifier));
		}

		var window = _windows.GetOrAdd (identifier, _ => new RequestWindow (_options.WindowSize));
		var now = DateTime.UtcNow;

		lock (window) {
			// Remove old requests outside the window
			window.Requests.RemoveAll (time => now - time > _options.WindowSize);

			if (window.Requests.Count >= _options.RequestsPerWindow) {
				_logger.LogWarning ("Rate limit exceeded for identifier: {Identifier} ({Count}/{Limit})",
					identifier, window.Requests.Count, _options.RequestsPerWindow);

				if (_options.ThrowOnLimit) {
					var status = GetStatusInternal (identifier, window, now);
					throw new RateLimitExceededException (status);
				}

				return Task.FromResult (false);
			}

			window.Requests.Add (now);
			_logger.LogTrace ("Request allowed for identifier: {Identifier} ({Count}/{Limit})",
				identifier, window.Requests.Count, _options.RequestsPerWindow);

			return Task.FromResult (true);
		}
	}

	public Task<RateLimitStatus> GetStatusAsync (string identifier)
	{
		if (string.IsNullOrEmpty (identifier)) {
			throw new ArgumentException ("Identifier cannot be null or empty", nameof (identifier));
		}

		var window = _windows.GetOrAdd (identifier, _ => new RequestWindow (_options.WindowSize));
		var now = DateTime.UtcNow;

		lock (window) {
			return Task.FromResult (GetStatusInternal (identifier, window, now));
		}
	}

	public void Reset (string identifier)
	{
		if (string.IsNullOrEmpty (identifier)) {
			return;
		}

		if (_windows.TryGetValue (identifier, out var window)) {
			lock (window) {
				window.Requests.Clear ();
			}
		}

		_logger.LogDebug ("Rate limit reset for identifier: {Identifier}", identifier);
	}

	public void ResetAll ()
	{
		foreach (var kvp in _windows) {
			lock (kvp.Value) {
				kvp.Value.Requests.Clear ();
			}
		}

		_logger.LogInformation ("All rate limits reset");
	}

	RateLimitStatus GetStatusInternal (string identifier, RequestWindow window, DateTime now)
	{
		// Remove old requests
		window.Requests.RemoveAll (time => now - time > _options.WindowSize);

		var requestsInWindow = window.Requests.Count;
		var requestsRemaining = Math.Max (0, _options.RequestsPerWindow - requestsInWindow);

		var oldestRequest = window.Requests.Count > 0 ? window.Requests.Min () : now;
		var windowStart = now - _options.WindowSize;
		var nextReset = requestsRemaining == 0 && window.Requests.Count > 0
			? window.Requests.Min () + _options.WindowSize
			: (DateTime?)null;

		return new RateLimitStatus {
			RequestsRemaining = requestsRemaining,
			RequestsPerWindow = _options.RequestsPerWindow,
			WindowSize = _options.WindowSize,
			WindowStart = windowStart,
			NextReset = nextReset
		};
	}

	void CleanupExpiredWindows (object? state)
	{
		var now = DateTime.UtcNow;
		var keysToRemove = new List<string> ();

		foreach (var kvp in _windows) {
			lock (kvp.Value) {
				kvp.Value.Requests.RemoveAll (time => now - time > _options.WindowSize);

				// Remove windows that haven't been used recently
				if (kvp.Value.Requests.Count == 0 && now - kvp.Value.LastAccess > TimeSpan.FromMinutes (10)) {
					keysToRemove.Add (kvp.Key);
				}
			}
		}

		foreach (var key in keysToRemove) {
			_windows.TryRemove (key, out _);
		}

		if (keysToRemove.Count > 0) {
			_logger.LogDebug ("Cleaned up {Count} expired rate limit windows", keysToRemove.Count);
		}
	}

	class RequestWindow
	{
		public List<DateTime> Requests { get; } = [];
		public DateTime LastAccess { get; set; }

		public RequestWindow (TimeSpan windowSize)
		{
			LastAccess = DateTime.UtcNow;
		}
	}
}