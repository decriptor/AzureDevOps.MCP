namespace AzureDevOps.MCP.Services.Infrastructure;

public interface IMemoryPressureManager
{
	bool IsMemoryPressureHigh { get; }
	void CheckMemoryPressure ();
	event EventHandler<MemoryPressureEventArgs>? MemoryPressureChanged;
}

public class MemoryPressureEventArgs : EventArgs
{
	public bool IsHighPressure { get; }
	public long MemoryUsageBytes { get; }

	public MemoryPressureEventArgs (bool isHighPressure, long memoryUsageBytes)
	{
		IsHighPressure = isHighPressure;
		MemoryUsageBytes = memoryUsageBytes;
	}
}

public class MemoryPressureManager : IMemoryPressureManager, IDisposable
{
	readonly Timer _memoryCheckTimer;
	readonly long _highPressureThresholdBytes;
	readonly ILogger<MemoryPressureManager> _logger;
	bool _isHighPressure;
	bool _disposed;

	public event EventHandler<MemoryPressureEventArgs>? MemoryPressureChanged;

	public bool IsMemoryPressureHigh => _isHighPressure;

	public MemoryPressureManager (ILogger<MemoryPressureManager> logger, long highPressureThresholdBytes = 1_000_000_000) // 1GB default
	{
		_logger = logger;
		_highPressureThresholdBytes = highPressureThresholdBytes;
		_memoryCheckTimer = new Timer (CheckMemoryPressureCallback, null, TimeSpan.Zero, TimeSpan.FromMinutes (1));
	}

	public void CheckMemoryPressure ()
	{
		if (_disposed) {
			return;
		}

		try {
			var memoryUsage = GC.GetTotalMemory (false);
			var wasHighPressure = _isHighPressure;
			_isHighPressure = memoryUsage > _highPressureThresholdBytes;

			if (wasHighPressure != _isHighPressure) {
				_logger.LogInformation ("Memory pressure changed: {IsHigh} (Usage: {Usage:N0} bytes)",
					_isHighPressure ? "HIGH" : "NORMAL", memoryUsage);

				MemoryPressureChanged?.Invoke (this, new MemoryPressureEventArgs (_isHighPressure, memoryUsage));
			}
		} catch (Exception ex) {
			_logger.LogError (ex, "Error checking memory pressure");
		}
	}

	void CheckMemoryPressureCallback (object? state)
	{
		CheckMemoryPressure ();
	}

	public void Dispose ()
	{
		if (!_disposed) {
			_memoryCheckTimer?.Dispose ();
			_disposed = true;
		}
	}
}