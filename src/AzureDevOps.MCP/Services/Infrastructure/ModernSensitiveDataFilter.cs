using System.Collections.Frozen;
using System.Text.RegularExpressions;
using AzureDevOps.MCP.Configuration;
using Microsoft.Extensions.Options;

namespace AzureDevOps.MCP.Services.Infrastructure;

public sealed class ModernSensitiveDataFilter : ISensitiveDataFilter
{
	readonly LoggingConfiguration _config;

	// Use frozen dictionary for compiled regex patterns
	static readonly FrozenDictionary<string, Regex> SensitivePatterns = new Dictionary<string, Regex> {
		["email"] = new Regex (@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
		["creditcard"] = new Regex (@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled),
		["ssn"] = new Regex (@"\b\d{3}-?\d{2}-?\d{4}\b", RegexOptions.Compiled),
		["phone"] = new Regex (@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled),
		["token"] = new Regex (@"\b[A-Za-z0-9]{20,}\b", RegexOptions.Compiled),
		["password"] = new Regex (@"(password|pwd|pass|secret|key|token)[\""\s]*[:=\s][\""\s]*[^\s\"""",;]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
		["bearer"] = new Regex (@"Bearer\s+[A-Za-z0-9\-_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
		["jwt"] = new Regex (@"\b[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\b", RegexOptions.Compiled)
	}.ToFrozenDictionary ();

	const string RedactionReplacement = "[REDACTED]";

	public ModernSensitiveDataFilter (IOptions<LoggingConfiguration> config)
	{
		_config = config?.Value ?? throw new ArgumentNullException (nameof (config));
	}

	public StructuredLogEntry FilterSensitiveData (StructuredLogEntry entry)
	{
		if (!_config.EnableSensitiveDataFiltering) {
			return entry;
		}

		return entry with {
			Message = FilterString (entry.Message),
			Properties = FilterProperties (entry.Properties),
			Exception = entry.Exception != null ? FilterException (entry.Exception) : null
		};
	}

	string FilterString (string input)
	{
		if (string.IsNullOrEmpty (input)) {
			return input;
		}

		var filtered = input;
		foreach (var (_, pattern) in SensitivePatterns) {
			filtered = pattern.Replace (filtered, RedactionReplacement);
		}
		return filtered;
	}

	Dictionary<string, object?> FilterProperties (Dictionary<string, object?> properties)
	{
		var filtered = new Dictionary<string, object?> ();

		foreach (var (key, value) in properties) {
			// Check if the key itself indicates sensitive data
			var filteredKey = IsSensitiveKey (key) ? $"{key}_{RedactionReplacement}" : key;

			// Filter the value
			var filteredValue = value switch {
				string str => FilterString (str),
				null => null,
				_ => value.ToString () is { } strValue ? FilterString (strValue) : value
			};

			filtered[filteredKey] = filteredValue;
		}

		return filtered;
	}

	ExceptionInfo FilterException (ExceptionInfo exception)
	{
		return exception with {
			Message = FilterString (exception.Message),
			StackTrace = FilterString (exception.StackTrace ?? ""),
			Data = exception.Data?.ToDictionary (
				kvp => IsSensitiveKey (kvp.Key) ? $"{kvp.Key}_{RedactionReplacement}" : kvp.Key,
				kvp => kvp.Value != null ? FilterString (kvp.Value) : null
			),
			InnerExceptions = exception.InnerExceptions?.Select (FilterException).ToList ()
		};
	}

	static bool IsSensitiveKey (string key)
	{
		var sensitiveKeywords = new[] { "password", "secret", "key", "token", "auth", "credential", "pat" };
		return sensitiveKeywords.Any (keyword => key.Contains (keyword, StringComparison.OrdinalIgnoreCase));
	}
}