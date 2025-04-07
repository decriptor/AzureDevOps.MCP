using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using AzureDevOps.MCP.Configuration;
using Microsoft.Extensions.Options;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Modern structured logging service using .NET 9 features and high-performance patterns.
/// </summary>
public sealed class ModernStructuredLogger : ILogger
{
	readonly ILogger _innerLogger;
	readonly LoggingConfiguration _config;
	readonly ISensitiveDataFilter _sensitiveDataFilter;

	// Use frozen set for efficient log level checking
	static readonly FrozenSet<LogLevel> PerformanceLogLevels = new HashSet<LogLevel>
	{
		LogLevel.Debug,
		LogLevel.Trace
	}.ToFrozenSet ();

	public ModernStructuredLogger (
		ILogger innerLogger,
		IOptions<LoggingConfiguration> config,
		ISensitiveDataFilter sensitiveDataFilter)
	{
		_innerLogger = innerLogger ?? throw new ArgumentNullException (nameof (innerLogger));
		_config = config?.Value ?? throw new ArgumentNullException (nameof (config));
		_sensitiveDataFilter = sensitiveDataFilter ?? throw new ArgumentNullException (nameof (sensitiveDataFilter));
	}

	public IDisposable? BeginScope<TState> (TState state) where TState : notnull
	{
		return _innerLogger.BeginScope (state);
	}

	public bool IsEnabled (LogLevel logLevel)
	{
		if (!_config.EnableStructuredLogging) {
			return _innerLogger.IsEnabled (logLevel);
		}

		// Performance optimization: skip expensive logging for disabled levels
		if (!_config.EnablePerformanceLogging && PerformanceLogLevels.Contains (logLevel)) {
			return false;
		}

		return _innerLogger.IsEnabled (logLevel);
	}

	public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled (logLevel)) {
			return;
		}

		var structuredEntry = CreateStructuredLogEntry (logLevel, eventId, state, exception, formatter);

		if (_config.EnableSensitiveDataFiltering) {
			structuredEntry = _sensitiveDataFilter.FilterSensitiveData (structuredEntry);
		}

		// Log the structured entry using modern string interpolation
		_innerLogger.Log (logLevel, eventId, structuredEntry, exception,
			(entry, ex) => JsonSerializer.Serialize (entry, ModernJsonOptions.Default));
	}

	StructuredLogEntry CreateStructuredLogEntry<TState> (
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		var timestamp = DateTimeOffset.UtcNow;
		var activity = Activity.Current;

		return new StructuredLogEntry {
			Timestamp = timestamp,
			Level = logLevel.ToString (),
			EventId = eventId.Id,
			EventName = eventId.Name,
			Message = formatter (state, exception),
			Exception = exception != null ? CreateExceptionInfo (exception) : null,
			TraceId = activity?.TraceId.ToString (),
			SpanId = activity?.SpanId.ToString (),
			ParentId = activity?.ParentId,
			Properties = ExtractProperties (state),
			MachineName = Environment.MachineName,
			ProcessId = Environment.ProcessId,
			ThreadId = Environment.CurrentManagedThreadId,
			Scope = GetCurrentScope ()
		};
	}

	static ExceptionInfo CreateExceptionInfo (Exception exception)
	{
		var innerExceptions = new List<ExceptionInfo> ();
		var current = exception.InnerException;

		while (current != null) {
			innerExceptions.Add (new ExceptionInfo {
				Type = current.GetType ().FullName ?? current.GetType ().Name,
				Message = current.Message,
				StackTrace = current.StackTrace,
				Data = current.Data.Count > 0
					? current.Data.Cast<System.Collections.DictionaryEntry> ()
						.ToDictionary (de => de.Key.ToString () ?? "", de => de.Value?.ToString ())
					: null
			});
			current = current.InnerException;
		}

		return new ExceptionInfo {
			Type = exception.GetType ().FullName ?? exception.GetType ().Name,
			Message = exception.Message,
			StackTrace = exception.StackTrace,
			Data = exception.Data.Count > 0
				? exception.Data.Cast<System.Collections.DictionaryEntry> ()
					.ToDictionary (de => de.Key.ToString () ?? "", de => de.Value?.ToString ())
				: null,
			InnerExceptions = innerExceptions.Count > 0 ? innerExceptions : null
		};
	}

	static Dictionary<string, object?> ExtractProperties<TState> (TState state)
	{
		var properties = new Dictionary<string, object?> ();

		// Handle structured logging state (like from LoggerMessage.Define)
		if (state is IEnumerable<KeyValuePair<string, object?>> keyValuePairs) {
			foreach (var kvp in keyValuePairs.Where (kvp => kvp.Key != "{OriginalFormat}")) {
				properties[kvp.Key] = kvp.Value;
			}
		}

		// Handle anonymous objects and other types
		else if (state != null && !state.GetType ().IsPrimitive && state is not string) {
			try {
				var json = JsonSerializer.Serialize (state, ModernJsonOptions.Default);
				var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>> (json, ModernJsonOptions.Default);
				if (parsed != null) {
					foreach (var kvp in parsed) {
						properties[kvp.Key] = kvp.Value;
					}
				}
			} catch {
				// Fallback to string representation
				properties["state"] = state.ToString ();
			}
		}

		return properties;
	}

	string? GetCurrentScope ()
	{
		// Try to extract scope information from the current activity
		var activity = Activity.Current;
		if (activity?.Tags != null) {
			var scopeTags = activity.Tags
				.Where (tag => tag.Key.StartsWith ("scope.", StringComparison.OrdinalIgnoreCase))
				.ToDictionary (tag => tag.Key, tag => tag.Value);

			if (scopeTags.Count > 0) {
				return JsonSerializer.Serialize (scopeTags, ModernJsonOptions.Default);
			}
		}

		return null;
	}
}