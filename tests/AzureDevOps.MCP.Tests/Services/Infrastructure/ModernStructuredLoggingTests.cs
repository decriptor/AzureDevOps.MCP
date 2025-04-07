using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Services.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace AzureDevOps.MCP.Tests.Services.Infrastructure;

[TestClass]
public class ModernStructuredLoggerTests
{
	Mock<ILogger> _mockInnerLogger = null!;
	Mock<ISensitiveDataFilter> _mockSensitiveDataFilter = null!;
	Mock<IOptions<LoggingConfiguration>> _mockOptions = null!;
	LoggingConfiguration _config = null!;
	ModernStructuredLogger _logger = null!;

	[TestInitialize]
	public void Setup ()
	{
		_mockInnerLogger = new Mock<ILogger> ();
		_mockSensitiveDataFilter = new Mock<ISensitiveDataFilter> ();
		_mockOptions = new Mock<IOptions<LoggingConfiguration>> ();

		_config = new LoggingConfiguration {
			EnableStructuredLogging = true,
			EnablePerformanceLogging = true,
			EnableSensitiveDataFiltering = true
		};

		_mockOptions.Setup (x => x.Value).Returns (_config);
		_logger = new ModernStructuredLogger (_mockInnerLogger.Object, _mockOptions.Object, _mockSensitiveDataFilter.Object);
	}

	[TestMethod]
	public void IsEnabled_WhenStructuredLoggingDisabled_DelegatesToInnerLogger ()
	{
		// Arrange
		_config.EnableStructuredLogging = false;
		_mockInnerLogger.Setup (x => x.IsEnabled (LogLevel.Information)).Returns (true);

		// Act
		var result = _logger.IsEnabled (LogLevel.Information);

		// Assert
		Assert.IsTrue (result);
		_mockInnerLogger.Verify (x => x.IsEnabled (LogLevel.Information), Times.Once);
	}

	[TestMethod]
	public void IsEnabled_WhenPerformanceLoggingDisabled_ReturnsFalseForDebugLevels ()
	{
		// Arrange
		_config.EnablePerformanceLogging = false;
		_mockInnerLogger.Setup (x => x.IsEnabled (LogLevel.Debug)).Returns (true);

		// Act
		var result = _logger.IsEnabled (LogLevel.Debug);

		// Assert
		Assert.IsFalse (result);
	}

	[TestMethod]
	public void IsEnabled_WhenPerformanceLoggingEnabled_AllowsDebugLevels ()
	{
		// Arrange
		_config.EnablePerformanceLogging = true;
		_mockInnerLogger.Setup (x => x.IsEnabled (LogLevel.Debug)).Returns (true);

		// Act
		var result = _logger.IsEnabled (LogLevel.Debug);

		// Assert
		Assert.IsTrue (result);
		_mockInnerLogger.Verify (x => x.IsEnabled (LogLevel.Debug), Times.Once);
	}

	[TestMethod]
	public void Log_CreatesStructuredLogEntry ()
	{
		// Arrange
		_mockInnerLogger.Setup (x => x.IsEnabled (LogLevel.Information)).Returns (true);
		_mockSensitiveDataFilter.Setup (x => x.FilterSensitiveData (It.IsAny<StructuredLogEntry> ()))
			.Returns<StructuredLogEntry> (entry => entry);

		StructuredLogEntry? capturedEntry = null;
		_mockInnerLogger.Setup (x => x.Log (
			LogLevel.Information,
			It.IsAny<EventId> (),
			It.IsAny<StructuredLogEntry> (),
			null,
			It.IsAny<Func<StructuredLogEntry, Exception?, string>> ()))
			.Callback<LogLevel, EventId, StructuredLogEntry, Exception?, Func<StructuredLogEntry, Exception?, string>> (
				(level, eventId, state, ex, formatter) => capturedEntry = state);

		var eventId = new EventId (123, "TestEvent");
		var testData = new { Property1 = "Value1", Property2 = 42 };

		// Act
		_logger.LogInformation (eventId, "Test message with {Property1} and {Property2}", "Value1", 42);

		// Assert
		Assert.IsNotNull (capturedEntry);
		Assert.AreEqual ("Information", capturedEntry.Level);
		Assert.AreEqual (123, capturedEntry.EventId);
		Assert.AreEqual ("TestEvent", capturedEntry.EventName);
		Assert.IsTrue (capturedEntry.Message.Contains ("Test message"));
		Assert.IsTrue (capturedEntry.Timestamp != default);
		Assert.AreEqual (Environment.MachineName, capturedEntry.MachineName);
		Assert.AreEqual (Environment.ProcessId, capturedEntry.ProcessId);
	}

	[TestMethod]
	public void Log_WithException_IncludesExceptionInfo ()
	{
		// Arrange
		_mockInnerLogger.Setup (x => x.IsEnabled (LogLevel.Error)).Returns (true);
		_mockSensitiveDataFilter.Setup (x => x.FilterSensitiveData (It.IsAny<StructuredLogEntry> ()))
			.Returns<StructuredLogEntry> (entry => entry);

		StructuredLogEntry? capturedEntry = null;
		_mockInnerLogger.Setup (x => x.Log (
			LogLevel.Error,
			It.IsAny<EventId> (),
			It.IsAny<StructuredLogEntry> (),
			It.IsAny<Exception> (),
			It.IsAny<Func<StructuredLogEntry, Exception?, string>> ()))
			.Callback<LogLevel, EventId, StructuredLogEntry, Exception?, Func<StructuredLogEntry, Exception?, string>> (
				(level, eventId, state, ex, formatter) => capturedEntry = state);

		var exception = new InvalidOperationException ("Test exception",
			new ArgumentException ("Inner exception"));

		// Act
		_logger.LogError (exception, "Error occurred");

		// Assert
		Assert.IsNotNull (capturedEntry);
		Assert.IsNotNull (capturedEntry.Exception);
		Assert.AreEqual ("System.InvalidOperationException", capturedEntry.Exception.Type);
		Assert.AreEqual ("Test exception", capturedEntry.Exception.Message);
		Assert.IsNotNull (capturedEntry.Exception.InnerExceptions);
		Assert.AreEqual (1, capturedEntry.Exception.InnerExceptions.Count);
		Assert.AreEqual ("System.ArgumentException", capturedEntry.Exception.InnerExceptions[0].Type);
	}

	[TestMethod]
	public void Log_WithActivityContext_IncludesTraceInformation ()
	{
		// Arrange
		_mockInnerLogger.Setup (x => x.IsEnabled (LogLevel.Information)).Returns (true);
		_mockSensitiveDataFilter.Setup (x => x.FilterSensitiveData (It.IsAny<StructuredLogEntry> ()))
			.Returns<StructuredLogEntry> (entry => entry);

		StructuredLogEntry? capturedEntry = null;
		_mockInnerLogger.Setup (x => x.Log (
			It.IsAny<LogLevel> (),
			It.IsAny<EventId> (),
			It.IsAny<StructuredLogEntry> (),
			It.IsAny<Exception?> (),
			It.IsAny<Func<StructuredLogEntry, Exception?, string>> ()))
			.Callback<LogLevel, EventId, StructuredLogEntry, Exception?, Func<StructuredLogEntry, Exception?, string>> (
				(level, eventId, state, ex, formatter) => capturedEntry = state);

		// Act
		using var activity = new Activity ("TestActivity");
		activity.Start ();

		_logger.LogInformation ("Test message");

		// Assert
		Assert.IsNotNull (capturedEntry);
		Assert.IsNotNull (capturedEntry.TraceId);
		Assert.IsNotNull (capturedEntry.SpanId);
		Assert.AreEqual (activity.TraceId.ToString (), capturedEntry.TraceId);
		Assert.AreEqual (activity.SpanId.ToString (), capturedEntry.SpanId);
	}

	[TestMethod]
	public void Log_WithSensitiveDataFiltering_CallsFilter ()
	{
		// Arrange
		_mockInnerLogger.Setup (x => x.IsEnabled (LogLevel.Information)).Returns (true);
		var filteredEntry = new StructuredLogEntry { Message = "Filtered message" };
		_mockSensitiveDataFilter.Setup (x => x.FilterSensitiveData (It.IsAny<StructuredLogEntry> ()))
			.Returns (filteredEntry);

		// Act
		_logger.LogInformation ("Message with sensitive data");

		// Assert
		_mockSensitiveDataFilter.Verify (x => x.FilterSensitiveData (It.IsAny<StructuredLogEntry> ()), Times.Once);
	}

	[TestMethod]
	public void BeginScope_DelegatesToInnerLogger ()
	{
		// Arrange
		var scope = Mock.Of<IDisposable> ();
		var scopeState = new { Operation = "TestOperation" };
		_mockInnerLogger.Setup (x => x.BeginScope (scopeState)).Returns (scope);

		// Act
		var result = _logger.BeginScope (scopeState);

		// Assert
		Assert.AreEqual (scope, result);
		_mockInnerLogger.Verify (x => x.BeginScope (scopeState), Times.Once);
	}
}

[TestClass]
public class ModernSensitiveDataFilterTests
{
	Mock<IOptions<LoggingConfiguration>> _mockOptions = null!;
	LoggingConfiguration _config = null!;
	ModernSensitiveDataFilter _filter = null!;

	[TestInitialize]
	public void Setup ()
	{
		_mockOptions = new Mock<IOptions<LoggingConfiguration>> ();
		_config = new LoggingConfiguration {
			EnableSensitiveDataFiltering = true
		};
		_mockOptions.Setup (x => x.Value).Returns (_config);
		_filter = new ModernSensitiveDataFilter (_mockOptions.Object);
	}

	[TestMethod]
	public void FilterSensitiveData_WhenFilteringDisabled_ReturnsOriginalEntry ()
	{
		// Arrange
		_config.EnableSensitiveDataFiltering = false;
		var entry = new StructuredLogEntry { Message = "test@example.com password=secret123" };

		// Act
		var result = _filter.FilterSensitiveData (entry);

		// Assert
		Assert.AreEqual (entry, result);
		Assert.AreEqual ("test@example.com password=secret123", result.Message);
	}

	[TestMethod]
	public void FilterSensitiveData_FiltersEmailAddresses ()
	{
		// Arrange
		var entry = new StructuredLogEntry {
			Message = "User test@example.com logged in"
		};

		// Act
		var result = _filter.FilterSensitiveData (entry);

		// Assert
		Assert.IsTrue (result.Message.Contains ("[REDACTED]"));
		Assert.IsFalse (result.Message.Contains ("test@example.com"));
	}

	[TestMethod]
	public void FilterSensitiveData_FiltersCreditCardNumbers ()
	{
		// Arrange
		var entry = new StructuredLogEntry {
			Message = "Payment with card 4532-1234-5678-9012 processed"
		};

		// Act
		var result = _filter.FilterSensitiveData (entry);

		// Assert
		Assert.IsTrue (result.Message.Contains ("[REDACTED]"));
		Assert.IsFalse (result.Message.Contains ("4532-1234-5678-9012"));
	}

	[TestMethod]
	public void FilterSensitiveData_FiltersPasswords ()
	{
		// Arrange
		var entry = new StructuredLogEntry {
			Message = "Login failed for password=secret123"
		};

		// Act
		var result = _filter.FilterSensitiveData (entry);

		// Assert
		Assert.IsTrue (result.Message.Contains ("[REDACTED]"));
		Assert.IsFalse (result.Message.Contains ("secret123"));
	}

	[TestMethod]
	public void FilterSensitiveData_FiltersBearerTokens ()
	{
		// Arrange
		var entry = new StructuredLogEntry {
			Message = "Request with Bearer abc123xyz789 failed"
		};

		// Act
		var result = _filter.FilterSensitiveData (entry);

		// Assert
		Assert.IsTrue (result.Message.Contains ("[REDACTED]"));
		Assert.IsFalse (result.Message.Contains ("abc123xyz789"));
	}

	[TestMethod]
	public void FilterSensitiveData_FiltersPropertiesWithSensitiveKeys ()
	{
		// Arrange
		var entry = new StructuredLogEntry {
			Properties = new Dictionary<string, object?> {
				["username"] = "john.doe",
				["password"] = "secret123",
				["api_key"] = "abc123xyz",
				["normal_property"] = "safe_value"
			}
		};

		// Act
		var result = _filter.FilterSensitiveData (entry);

		// Assert
		Assert.IsTrue (result.Properties.ContainsKey ("password_[REDACTED]"));
		Assert.IsTrue (result.Properties.ContainsKey ("api_key_[REDACTED]"));
		Assert.IsTrue (result.Properties.ContainsKey ("username"));
		Assert.IsTrue (result.Properties.ContainsKey ("normal_property"));
		Assert.IsFalse (result.Properties.ContainsKey ("password"));
		Assert.IsFalse (result.Properties.ContainsKey ("api_key"));
	}

	[TestMethod]
	public void FilterSensitiveData_FiltersExceptionData ()
	{
		// Arrange
		var entry = new StructuredLogEntry {
			Exception = new ExceptionInfo {
				Type = "System.Exception",
				Message = "Error with token abc123xyz",
				Data = new Dictionary<string, string?> {
					["ConnectionString"] = "Server=test;Password=secret",
					["UserId"] = "12345"
				}
			}
		};

		// Act
		var result = _filter.FilterSensitiveData (entry);

		// Assert
		Assert.IsNotNull (result.Exception);
		Assert.IsTrue (result.Exception.Message.Contains ("[REDACTED]"));
		Assert.IsFalse (result.Exception.Message.Contains ("abc123xyz"));
		Assert.IsNotNull (result.Exception.Data);
		Assert.IsTrue (result.Exception.Data.ContainsKey ("ConnectionString"));
		Assert.IsTrue (result.Exception.Data["ConnectionString"]!.Contains ("[REDACTED]"));
	}

	[TestMethod]
	public void FilterSensitiveData_HandlesCascadingInnerExceptions ()
	{
		// Arrange
		var entry = new StructuredLogEntry {
			Exception = new ExceptionInfo {
				Type = "System.Exception",
				Message = "Outer exception with token abc123",
				InnerExceptions =
				[
					new()
					{
						Type = "System.ArgumentException",
						Message = "Inner exception with email test@domain.com"
					}
				]
			}
		};

		// Act
		var result = _filter.FilterSensitiveData (entry);

		// Assert
		Assert.IsNotNull (result.Exception);
		Assert.IsTrue (result.Exception.Message.Contains ("[REDACTED]"));
		Assert.IsNotNull (result.Exception.InnerExceptions);
		Assert.AreEqual (1, result.Exception.InnerExceptions.Count);
		Assert.IsTrue (result.Exception.InnerExceptions[0].Message.Contains ("[REDACTED]"));
		Assert.IsFalse (result.Exception.InnerExceptions[0].Message.Contains ("test@domain.com"));
	}
}

[TestClass]
public class PerformanceLoggerTests
{
	Mock<ILogger> _mockLogger = null!;
	PerformanceLogger _performanceLogger = null!;

	[TestInitialize]
	public void Setup ()
	{
		_mockLogger = new Mock<ILogger> ();
		_mockLogger.Setup (x => x.IsEnabled (It.IsAny<LogLevel> ())).Returns (true);
	}

	[TestMethod]
	public void Constructor_StartsStopwatchAndLogsStart ()
	{
		// Act
		_performanceLogger = new PerformanceLogger (_mockLogger.Object, "TestOperation");       // Assert
		_mockLogger.Verify (x => x.Log (
			LogLevel.Debug,
			It.IsAny<EventId> (),
			It.Is<It.IsAnyType> ((v, t) => (v.ToString () ?? "").Contains ("TestOperation") && (v.ToString () ?? "").Contains ("started")),
			It.IsAny<Exception> (),
			It.IsAny<Func<It.IsAnyType, Exception?, string>> ()), Times.Once);
	}

	[TestMethod]
	public void AddProperty_AllowsAddingPropertiesAfterConstruction ()
	{
		// Arrange
		_performanceLogger = new PerformanceLogger (_mockLogger.Object, "TestOperation");

		// Act
		_performanceLogger.AddProperty ("UserId", "12345");
		_performanceLogger.AddProperty ("RequestId", Guid.NewGuid ());

		// Assert - Properties should be added without exceptions
		Assert.IsNotNull (_performanceLogger);
	}

	[TestMethod]
	public void LogMilestone_LogsIntermediateProgress ()
	{
		// Arrange
		_performanceLogger = new PerformanceLogger (_mockLogger.Object, "TestOperation");

		// Act
		_performanceLogger.LogMilestone ("DataLoaded", new Dictionary<string, object?> {
			["RecordCount"] = 100
		});

		// Assert
		_mockLogger.Verify (x => x.Log (
			LogLevel.Debug,
			It.IsAny<EventId> (),
			It.Is<It.IsAnyType> ((v, t) => (v.ToString () ?? "").Contains ("milestone") && (v.ToString () ?? "").Contains ("DataLoaded")),
			It.IsAny<Exception> (),
			It.IsAny<Func<It.IsAnyType, Exception?, string>> ()), Times.Once);
	}

	[TestMethod]
	public void Dispose_LogsCompletionWithDuration ()
	{
		// Arrange
		_performanceLogger = new PerformanceLogger (_mockLogger.Object, "TestOperation");

		// Add some delay to ensure measurable duration
		Thread.Sleep (10);

		// Act
		_performanceLogger.Dispose ();

		// Assert
		_mockLogger.Verify (x => x.Log (
			LogLevel.Information,
			It.IsAny<EventId> (),
			It.Is<It.IsAnyType> ((v, t) => v.ToString ()!.Contains ("completed")),
			It.IsAny<Exception> (),
			It.IsAny<Func<It.IsAnyType, Exception?, string>> ()), Times.Once);
	}

	[TestMethod]
	public void Fail_WithException_LogsErrorWithDuration ()
	{
		// Arrange
		_performanceLogger = new PerformanceLogger (_mockLogger.Object, "TestOperation");
		var exception = new InvalidOperationException ("Test failure");

		// Act
		_performanceLogger.Fail (exception, "Operation failed due to invalid state");

		// Assert
		_mockLogger.Verify (x => x.Log (
			LogLevel.Error,
			It.IsAny<EventId> (),
			It.Is<It.IsAnyType> ((v, t) => v.ToString ()!.Contains ("failed")),
			exception,
			It.IsAny<Func<It.IsAnyType, Exception?, string>> ()), Times.Once);
	}

	[TestMethod]
	public void Fail_WithoutException_LogsWarning ()
	{
		// Arrange
		_performanceLogger = new PerformanceLogger (_mockLogger.Object, "TestOperation");
		// Act
		_performanceLogger.Fail (reason: "Timeout occurred");

		// Assert
		_mockLogger.Verify (x => x.Log (
			LogLevel.Warning,
			It.IsAny<EventId> (),
			It.Is<It.IsAnyType> ((v, t) => (v.ToString () ?? "").Contains ("failed") && (v.ToString () ?? "").Contains ("Timeout occurred")),
			null,
			It.IsAny<Func<It.IsAnyType, Exception?, string>> ()), Times.Once);
	}

	[TestCleanup]
	public void Cleanup ()
	{
		_performanceLogger?.Dispose ();
	}
}

[TestClass]
public class ModernJsonOptionsTests
{
	[TestMethod]
	public void Default_UsesSnakeCaseNaming ()
	{
		// Arrange
		var testObject = new { PropertyName = "value", AnotherProperty = 123 };

		// Act
		var json = JsonSerializer.Serialize (testObject, ModernJsonOptions.Default);

		// Assert
		Assert.IsTrue (json.Contains ("property_name"));
		Assert.IsTrue (json.Contains ("another_property"));
		Assert.IsFalse (json.Contains ("PropertyName"));
		Assert.IsFalse (json.Contains ("AnotherProperty"));
	}

	[TestMethod]
	public void Default_IgnoresNullValues ()
	{
		// Arrange
		var testObject = new { Value = "test", NullValue = (string?)null };

		// Act
		var json = JsonSerializer.Serialize (testObject, ModernJsonOptions.Default);

		// Assert
		Assert.IsTrue (json.Contains ("value"));
		Assert.IsFalse (json.Contains ("null_value"));
	}

	[TestMethod]
	public void Default_IsNotIndented ()
	{
		// Arrange
		var testObject = new { Property1 = "value1", Property2 = "value2" };

		// Act
		var json = JsonSerializer.Serialize (testObject, ModernJsonOptions.Default);

		// Assert
		Assert.IsFalse (json.Contains ('\n'));
		Assert.IsFalse (json.Contains ("  "));
	}
}

[TestClass]
public class StructuredLoggingExtensionsTests
{
	[TestMethod]
	public void AddModernStructuredLogging_RegistersRequiredServices ()
	{
		// Arrange
		var services = new ServiceCollection ();
		var configuration = new ConfigurationBuilder ()
			.AddInMemoryCollection (new Dictionary<string, string?> {
				["Logging:EnableStructuredLogging"] = "true",
				["Logging:EnableSensitiveDataFiltering"] = "true"
			})
			.Build ();

		// Act
		services.AddModernStructuredLogging (configuration);

		// Assert
		var serviceProvider = services.BuildServiceProvider ();
		var filter = serviceProvider.GetService<ISensitiveDataFilter> ();

		Assert.IsNotNull (filter);
		Assert.IsInstanceOfType<ModernSensitiveDataFilter> (filter);
	}

	[TestMethod]
	public void LogPerformance_ReturnsPerformanceLogger ()
	{
		// Arrange
		var mockLogger = new Mock<ILogger> ();

		// Act
		using var performanceLogger = mockLogger.Object.LogPerformance ("TestOperation");

		// Assert
		Assert.IsNotNull (performanceLogger);
		Assert.IsInstanceOfType<PerformanceLogger> (performanceLogger);
	}

	[TestMethod]
	public void BeginOperationScope_ReturnsDisposableScope ()
	{
		// Arrange
		var mockLogger = new Mock<ILogger> ();
		var mockScope = Mock.Of<IDisposable> ();
		mockLogger.Setup (x => x.BeginScope (It.IsAny<Dictionary<string, object?>> ()))
			.Returns (mockScope);

		// Act
		var scope = mockLogger.Object.BeginOperationScope ("TestOperation",
			new Dictionary<string, object?> { ["Property"] = "Value" });

		// Assert
		Assert.AreEqual (mockScope, scope);
		mockLogger.Verify (x => x.BeginScope (It.IsAny<Dictionary<string, object?>> ()), Times.Once);
	}
}

[TestClass]
public class StructuredDataRecordsTests
{
	[TestMethod]
	public void StructuredLogEntry_SupportsWithExpressions ()
	{
		// Arrange
		var original = new StructuredLogEntry {
			Message = "Original message",
			Level = "Information",
			Properties = new Dictionary<string, object?> { ["Key"] = "Value" }
		};

		// Act
		var modified = original with { Message = "Modified message" };

		// Assert
		Assert.AreEqual ("Original message", original.Message);
		Assert.AreEqual ("Modified message", modified.Message);
		Assert.AreEqual (original.Level, modified.Level);
		Assert.AreEqual (original.Properties, modified.Properties);
	}

	[TestMethod]
	public void ExceptionInfo_SupportsNestedInnerExceptions ()
	{
		// Arrange
		var innerException = new ExceptionInfo {
			Type = "System.ArgumentException",
			Message = "Inner exception message"
		};

		var exception = new ExceptionInfo {
			Type = "System.InvalidOperationException",
			Message = "Outer exception message",
			InnerExceptions = [innerException]
		};

		// Assert
		Assert.AreEqual ("System.InvalidOperationException", exception.Type);
		Assert.IsNotNull (exception.InnerExceptions);
		Assert.AreEqual (1, exception.InnerExceptions.Count);
		Assert.AreEqual ("System.ArgumentException", exception.InnerExceptions[0].Type);
	}

	[TestMethod]
	public void ExceptionInfo_SupportsWithExpression ()
	{
		// Arrange
		var original = new ExceptionInfo {
			Type = "System.Exception",
			Message = "Original message",
			StackTrace = "Original stack trace"
		};

		// Act
		var modified = original with { Message = "Modified message" };

		// Assert
		Assert.AreEqual ("Original message", original.Message);
		Assert.AreEqual ("Modified message", modified.Message);
		Assert.AreEqual (original.Type, modified.Type);
		Assert.AreEqual (original.StackTrace, modified.StackTrace);
	}
}

[TestClass]
public class SensitiveDataPatternsTests
{
	[TestMethod]
	public void EmailPattern_MatchesVariousEmailFormats ()
	{
		// Arrange
		var emailPattern = new Regex (@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		var testEmails = new[]
		{
			"user@example.com",
			"test.email+tag@domain.co.uk",
			"user123@test-domain.org",
			"name_with_underscores@example.net"
		};

		// Act & Assert
		foreach (var email in testEmails) {
			Assert.IsTrue (emailPattern.IsMatch (email), $"Pattern should match email: {email}");
		}
	}

	[TestMethod]
	public void CreditCardPattern_MatchesVariousFormats ()
	{
		// Arrange
		var ccPattern = new Regex (@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);

		var testCards = new[]
		{
			"4532123456789012",
			"4532-1234-5678-9012",
			"4532 1234 5678 9012"
		};

		// Act & Assert
		foreach (var card in testCards) {
			Assert.IsTrue (ccPattern.IsMatch (card), $"Pattern should match credit card: {card}");
		}
	}

	[TestMethod]
	public void PasswordPattern_MatchesVariousFormats ()
	{
		// Arrange
		var passwordPattern = new Regex (@"(password|pwd|pass|secret|key)[\""\s]*[:=][\""\s]*[^\s\"""",;]+",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		var testPasswords = new[]
		{
			"password=secret123",
			"pwd:mypassword",
			"secret = \"topsecret\"",
			"key:abc123xyz"
		};

		// Act & Assert
		foreach (var password in testPasswords) {
			Assert.IsTrue (passwordPattern.IsMatch (password), $"Pattern should match password: {password}");
		}
	}
}