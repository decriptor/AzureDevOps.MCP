using System.Collections.Frozen;
using System.Net;
using System.Text.Json;

using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Services.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace AzureDevOps.MCP.Tests.Services.Infrastructure;

[TestClass]
public class ModernRateLimitingMiddlewareTests
{
	Mock<RequestDelegate> _mockNext = null!;
	Mock<ILogger<ModernRateLimitingMiddleware>> _mockLogger = null!;
	Mock<IRateLimitStore> _mockStore = null!;
	Mock<IOptions<RateLimitingConfiguration>> _mockOptions = null!;
	RateLimitingConfiguration _config = null!;
	ModernRateLimitingMiddleware _middleware = null!;

	[TestInitialize]
	public void Setup ()
	{
		_mockNext = new Mock<RequestDelegate> ();
		_mockLogger = new Mock<ILogger<ModernRateLimitingMiddleware>> ();
		_mockStore = new Mock<IRateLimitStore> ();
		_mockOptions = new Mock<IOptions<RateLimitingConfiguration>> ();

		_config = new RateLimitingConfiguration {
			EnableRateLimiting = true,
			RequestsPerMinute = 60,
			RequestsPerHour = 1000,
			RequestsPerDay = 10000,
			ClientIdentificationStrategy = "ip"
		};

		_mockOptions.Setup (x => x.Value).Returns (_config);
		_middleware = new ModernRateLimitingMiddleware (_mockNext.Object, _mockLogger.Object, _mockOptions.Object, _mockStore.Object);
	}

	[TestMethod]
	public async Task InvokeAsync_WhenRateLimitingDisabled_CallsNext ()
	{
		// Arrange
		_config.EnableRateLimiting = false;
		var context = CreateHttpContext ();

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		_mockNext.Verify (x => x (context), Times.Once);
		_mockStore.Verify (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()), Times.Never);
	}

	[TestMethod]
	public async Task InvokeAsync_WhenPathIsExempt_CallsNext ()
	{
		// Arrange
		var context = CreateHttpContext ("/health");

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		_mockNext.Verify (x => x (context), Times.Once);
		_mockStore.Verify (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()), Times.Never);
	}

	[TestMethod]
	public async Task InvokeAsync_WhenRateLimitNotExceeded_CallsNext ()
	{
		// Arrange
		var context = CreateHttpContext ("/api/test");
		var storeResult = new RateLimitStoreResult (10, true, DateTimeOffset.UtcNow.AddMinutes (1));

		_mockStore.Setup (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()))
			.ReturnsAsync (storeResult);

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		_mockNext.Verify (x => x (context), Times.Once);
		Assert.AreEqual (200, context.Response.StatusCode);
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Limit"));
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Remaining"));
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Reset"));
	}

	[TestMethod]
	public async Task InvokeAsync_WhenRateLimitExceeded_Returns429 ()
	{
		// Arrange
		var context = CreateHttpContext ("/api/test");
		var storeResult = new RateLimitStoreResult (61, false, DateTimeOffset.UtcNow.AddMinutes (1));

		_mockStore.Setup (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()))
			.ReturnsAsync (storeResult);

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		_mockNext.Verify (x => x (context), Times.Never);
		Assert.AreEqual ((int)HttpStatusCode.TooManyRequests, context.Response.StatusCode);
		Assert.AreEqual ("application/json", context.Response.ContentType);
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Limit"));
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Remaining"));
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-RetryAfter"));
	}

	[TestMethod]
	public async Task InvokeAsync_WithApiKeyStrategy_UsesApiKeyForIdentification ()
	{
		// Arrange
		_config.ClientIdentificationStrategy = "api_key";
		var context = CreateHttpContext ("/api/test");
		context.Request.Headers["X-API-Key"] = "test-api-key-123";

		var storeResult = new RateLimitStoreResult (10, true, DateTimeOffset.UtcNow.AddMinutes (1));
		string? capturedKey = null;

		_mockStore.Setup (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()))
			.Callback<string, int, TimeSpan, CancellationToken> ((key, _, _, _) => capturedKey = key)
			.ReturnsAsync (storeResult);

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		Assert.IsNotNull (capturedKey);
		Assert.IsTrue (capturedKey.Contains ("rate_limit:"));
		// Should use hashed API key, not IP
		_mockStore.Verify (x => x.CheckAndIncrementAsync (
			It.Is<string> (k => k.StartsWith ("rate_limit:")),
			It.IsAny<int> (),
			It.IsAny<TimeSpan> (),
			It.IsAny<CancellationToken> ()), Times.AtLeastOnce);
	}

	[TestMethod]
	public async Task InvokeAsync_WithUserStrategy_UsesUserIdForIdentification ()
	{
		// Arrange
		_config.ClientIdentificationStrategy = "user";
		var context = CreateHttpContext ("/api/test");

		// Mock authenticated user
		var identity = new System.Security.Principal.GenericIdentity ("testuser@example.com");
		var principal = new System.Security.Principal.GenericPrincipal (identity, Array.Empty<string> ());
		context.User = principal;

		var storeResult = new RateLimitStoreResult (10, true, DateTimeOffset.UtcNow.AddMinutes (1));

		_mockStore.Setup (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()))
			.ReturnsAsync (storeResult);

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		_mockStore.Verify (x => x.CheckAndIncrementAsync (
			It.Is<string> (k => k.StartsWith ("rate_limit:")),
			It.IsAny<int> (),
			It.IsAny<TimeSpan> (),
			It.IsAny<CancellationToken> ()), Times.AtLeastOnce);
	}

	[TestMethod]
	public async Task InvokeAsync_ChecksAllRateLimitRules ()
	{
		// Arrange
		var context = CreateHttpContext ("/api/test");
		var storeResult = new RateLimitStoreResult (10, true, DateTimeOffset.UtcNow.AddMinutes (1));

		_mockStore.Setup (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()))
			.ReturnsAsync (storeResult);

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		// Should check per_minute, per_hour, and per_day rules
		_mockStore.Verify (x => x.CheckAndIncrementAsync (
			It.Is<string> (k => k.Contains ("per_minute")),
			60,
			TimeSpan.FromMinutes (1),
			It.IsAny<CancellationToken> ()), Times.Once);

		_mockStore.Verify (x => x.CheckAndIncrementAsync (
			It.Is<string> (k => k.Contains ("per_hour")),
			1000,
			TimeSpan.FromHours (1),
			It.IsAny<CancellationToken> ()), Times.Once);

		_mockStore.Verify (x => x.CheckAndIncrementAsync (
			It.Is<string> (k => k.Contains ("per_day")),
			10000,
			TimeSpan.FromDays (1),
			It.IsAny<CancellationToken> ()), Times.Once);
	}

	[TestMethod]
	public async Task InvokeAsync_AddsCorrectRateLimitHeaders ()
	{
		// Arrange
		var context = CreateHttpContext ("/api/test");
		var resetTime = DateTimeOffset.UtcNow.AddMinutes (1);
		var storeResult = new RateLimitStoreResult (10, true, resetTime);

		_mockStore.Setup (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()))
			.ReturnsAsync (storeResult);

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Limit"));
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Remaining"));
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Reset"));
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-RetryAfter"));
		Assert.IsTrue (context.Response.Headers.ContainsKey ("X-RateLimit-Policy"));

		// Check header values
		Assert.AreEqual ("60", context.Response.Headers["X-RateLimit-Limit"]);
		Assert.AreEqual ("50", context.Response.Headers["X-RateLimit-Remaining"]); // 60 - 10 = 50
		Assert.AreEqual (resetTime.ToUnixTimeSeconds ().ToString (), context.Response.Headers["X-RateLimit-Reset"]);
	}

	[TestMethod]
	public async Task InvokeAsync_WithRateLimitExceeded_WritesJsonErrorResponse ()
	{
		// Arrange
		var context = CreateHttpContext ("/api/test");
		var storeResult = new RateLimitStoreResult (61, false, DateTimeOffset.UtcNow.AddMinutes (1));

		_mockStore.Setup (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()))
			.ReturnsAsync (storeResult);

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		Assert.AreEqual ((int)HttpStatusCode.TooManyRequests, context.Response.StatusCode);
		Assert.AreEqual ("application/json", context.Response.ContentType);

		// Check response body
		context.Response.Body.Seek (0, SeekOrigin.Begin);
		var responseText = await new StreamReader (context.Response.Body).ReadToEndAsync ();
		Assert.IsFalse (string.IsNullOrEmpty (responseText));

		var errorResponse = JsonSerializer.Deserialize<JsonElement> (responseText);
		Assert.IsTrue (errorResponse.TryGetProperty ("error", out var errorProp));
		Assert.AreEqual ("rate_limit_exceeded", errorProp.GetString ());
		Assert.IsTrue (errorResponse.TryGetProperty ("details", out var detailsProp));
		Assert.IsTrue (detailsProp.TryGetProperty ("limit", out _));
		Assert.IsTrue (detailsProp.TryGetProperty ("remaining", out _));
	}

	[TestMethod]
	public async Task InvokeAsync_WithCombinedStrategy_UsesCombinedIdentification ()
	{
		// Arrange
		_config.ClientIdentificationStrategy = "combined";
		var context = CreateHttpContext ("/api/test");
		context.Request.Headers["X-API-Key"] = "test-api-key";

		var identity = new System.Security.Principal.GenericIdentity ("testuser");
		var principal = new System.Security.Principal.GenericPrincipal (identity, Array.Empty<string> ());
		context.User = principal;

		var storeResult = new RateLimitStoreResult (10, true, DateTimeOffset.UtcNow.AddMinutes (1));

		_mockStore.Setup (x => x.CheckAndIncrementAsync (It.IsAny<string> (), It.IsAny<int> (), It.IsAny<TimeSpan> (), It.IsAny<CancellationToken> ()))
			.ReturnsAsync (storeResult);

		// Act
		await _middleware.InvokeAsync (context);

		// Assert
		_mockStore.Verify (x => x.CheckAndIncrementAsync (
			It.Is<string> (k => k.StartsWith ("rate_limit:")),
			It.IsAny<int> (),
			It.IsAny<TimeSpan> (),
			It.IsAny<CancellationToken> ()), Times.AtLeastOnce);
	}

	static DefaultHttpContext CreateHttpContext (string path = "/api/test")
	{
		var context = new DefaultHttpContext ();
		context.Request.Path = path;
		context.Request.Method = "GET";
		context.Response.Body = new MemoryStream ();
		context.Connection.RemoteIpAddress = IPAddress.Parse ("192.168.1.100");

		return context;
	}
}

[TestClass]
public class InMemoryRateLimitStoreTests
{
	Mock<ILogger<InMemoryRateLimitStore>> _mockLogger = null!;
	InMemoryRateLimitStore _store = null!;

	[TestInitialize]
	public void Setup ()
	{
		_mockLogger = new Mock<ILogger<InMemoryRateLimitStore>> ();
		_store = new InMemoryRateLimitStore (_mockLogger.Object);
	}

	[TestCleanup]
	public void Cleanup ()
	{
		_store?.Dispose ();
	}

	[TestMethod]
	public async Task CheckAndIncrementAsync_WithNewKey_AllowsRequestAndIncrementsCount ()
	{
		// Arrange
		const string key = "test-key";
		const int limit = 10;
		var window = TimeSpan.FromMinutes (1);

		// Act
		var result = await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);

		// Assert
		Assert.IsTrue (result.IsAllowed);
		Assert.AreEqual (1, result.RequestCount);
		Assert.IsTrue (result.ResetTime > DateTimeOffset.UtcNow);
	}

	[TestMethod]
	public async Task CheckAndIncrementAsync_WithExistingKey_IncrementsCount ()
	{
		// Arrange
		const string key = "test-key";
		const int limit = 10;
		var window = TimeSpan.FromMinutes (1);

		// Act
		var result1 = await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);
		var result2 = await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);

		// Assert
		Assert.IsTrue (result1.IsAllowed);
		Assert.AreEqual (1, result1.RequestCount);
		Assert.IsTrue (result2.IsAllowed);
		Assert.AreEqual (2, result2.RequestCount);
		Assert.AreEqual (result1.ResetTime, result2.ResetTime);
	}

	[TestMethod]
	public async Task CheckAndIncrementAsync_WhenLimitExceeded_DisallowsRequest ()
	{
		// Arrange
		const string key = "test-key";
		const int limit = 2;
		var window = TimeSpan.FromMinutes (1);

		// Act
		await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);
		await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);
		var result = await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);

		// Assert
		Assert.IsFalse (result.IsAllowed);
		Assert.AreEqual (3, result.RequestCount);
	}

	[TestMethod]
	public async Task CheckAndIncrementAsync_WithExpiredWindow_ResetsCount ()
	{
		// Arrange
		const string key = "test-key";
		const int limit = 10;
		var shortWindow = TimeSpan.FromMilliseconds (10); // Very short window

		// Act
		var result1 = await _store.CheckAndIncrementAsync (key, limit, shortWindow, CancellationToken.None);

		// Wait for window to expire
		await Task.Delay (20);

		var result2 = await _store.CheckAndIncrementAsync (key, limit, shortWindow, CancellationToken.None);

		// Assert
		Assert.IsTrue (result1.IsAllowed);
		Assert.AreEqual (1, result1.RequestCount);
		Assert.IsTrue (result2.IsAllowed);
		Assert.AreEqual (1, result2.RequestCount); // Reset to 1
		Assert.IsTrue (result2.ResetTime > result1.ResetTime);
	}

	[TestMethod]
	public async Task ResetAsync_RemovesKeyFromStore ()
	{
		// Arrange
		const string key = "test-key";
		const int limit = 10;
		var window = TimeSpan.FromMinutes (1);

		// Add entry
		await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);

		// Act
		await _store.ResetAsync (key, CancellationToken.None);

		// Assert
		var count = await _store.GetCurrentCountAsync (key, CancellationToken.None);
		Assert.AreEqual (0, count);
	}

	[TestMethod]
	public async Task GetCurrentCountAsync_WithExistingKey_ReturnsCount ()
	{
		// Arrange
		const string key = "test-key";
		const int limit = 10;
		var window = TimeSpan.FromMinutes (1);

		// Add entries
		await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);
		await _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None);

		// Act
		var count = await _store.GetCurrentCountAsync (key, CancellationToken.None);

		// Assert
		Assert.AreEqual (2, count);
	}

	[TestMethod]
	public async Task GetCurrentCountAsync_WithNonExistentKey_ReturnsZero ()
	{
		// Act
		var count = await _store.GetCurrentCountAsync ("non-existent-key", CancellationToken.None);

		// Assert
		Assert.AreEqual (0, count);
	}

	[TestMethod]
	public async Task GetCurrentCountAsync_WithExpiredKey_ReturnsZero ()
	{
		// Arrange
		const string key = "test-key";
		const int limit = 10;
		var shortWindow = TimeSpan.FromMilliseconds (10);

		// Add entry
		await _store.CheckAndIncrementAsync (key, limit, shortWindow, CancellationToken.None);

		// Wait for expiration
		await Task.Delay (20);

		// Act
		var count = await _store.GetCurrentCountAsync (key, CancellationToken.None);

		// Assert
		Assert.AreEqual (0, count);
	}

	[TestMethod]
	public async Task Store_HandlesHighConcurrency ()
	{
		// Arrange
		const string key = "test-key";
		const int limit = 1000;
		const int concurrentRequests = 100;
		var window = TimeSpan.FromMinutes (1);

		// Act
		var tasks = Enumerable.Range (0, concurrentRequests)
			.Select (_ => _store.CheckAndIncrementAsync (key, limit, window, CancellationToken.None))
			.ToArray ();

		var results = await Task.WhenAll (tasks);

		// Assert
		Assert.AreEqual (concurrentRequests, results.Length);
		Assert.IsTrue (results.All (r => r.IsAllowed)); // All should be allowed since limit is high

		// Final count should match concurrent requests
		var finalCount = await _store.GetCurrentCountAsync (key, CancellationToken.None);
		Assert.AreEqual (concurrentRequests, finalCount);
	}
}

[TestClass]
public class RateLimitingDataStructuresTests
{
	[TestMethod]
	public void RateLimitRule_WithRecord_SupportsValueEquality ()
	{
		// Arrange
		var rule1 = new RateLimitRule (TimeSpan.FromMinutes (1), 60);
		var rule2 = new RateLimitRule (TimeSpan.FromMinutes (1), 60);
		var rule3 = new RateLimitRule (TimeSpan.FromMinutes (2), 60);

		// Assert
		Assert.AreEqual (rule1, rule2); // Value equality
		Assert.AreNotEqual (rule1, rule3);
		Assert.AreEqual (rule1.GetHashCode (), rule2.GetHashCode ());
		Assert.AreNotEqual (rule1.GetHashCode (), rule3.GetHashCode ());
	}

	[TestMethod]
	public void RateLimitEntry_WithRecord_SupportsWithExpression ()
	{
		// Arrange
		var originalEntry = new RateLimitEntry (10, DateTimeOffset.UtcNow);

		// Act
		var updatedEntry = originalEntry with { RequestCount = 15 };

		// Assert
		Assert.AreEqual (10, originalEntry.RequestCount);
		Assert.AreEqual (15, updatedEntry.RequestCount);
		Assert.AreEqual (originalEntry.ResetTime, updatedEntry.ResetTime);
	}

	[TestMethod]
	public void RateLimitCheckResult_CalculatesRemainingRequests ()
	{
		// Arrange
		var result = new RateLimitCheckResult ("per_minute", 60, 25, true, DateTimeOffset.UtcNow.AddMinutes (1));

		// Act
		var remaining = result.RemainingRequests;

		// Assert
		Assert.AreEqual (35, remaining); // 60 - 25 = 35
	}

	[TestMethod]
	public void RateLimitCheckResult_WithExceededLimit_ReturnsZeroRemaining ()
	{
		// Arrange
		var result = new RateLimitCheckResult ("per_minute", 60, 75, false, DateTimeOffset.UtcNow.AddMinutes (1));

		// Act
		var remaining = result.RemainingRequests;

		// Assert
		Assert.AreEqual (0, remaining); // Math.Max(0, 60 - 75) = 0
	}

	[TestMethod]
	public void RateLimitResult_WithFrozenDictionary_IsImmutable ()
	{
		// Arrange
		var rules = new Dictionary<string, RateLimitCheckResult> {
			["per_minute"] = new ("per_minute", 60, 10, true, DateTimeOffset.UtcNow.AddMinutes (1))
		}.ToFrozenDictionary ();

		var result = new RateLimitResult (
			IsAllowed: true,
			ClientId: "test-client",
			RequestCount: 10,
			RequestLimit: 60,
			RemainingRequests: 50,
			ResetTime: DateTimeOffset.UtcNow.AddMinutes (1),
			RetryAfter: TimeSpan.FromMinutes (1),
			Rules: rules
		);

		// Assert
		Assert.IsTrue (result.Rules.ContainsKey ("per_minute"));
		Assert.AreEqual (1, result.Rules.Count);

		// Verify it's frozen (cannot be modified)
		Assert.IsInstanceOfType<FrozenDictionary<string, RateLimitCheckResult>> (result.Rules);
	}
}

[TestClass]
public class RateLimitingServiceExtensionsTests
{
	[TestMethod]
	public void AddModernRateLimiting_RegistersRequiredServices ()
	{
		// Arrange
		var services = new ServiceCollection ();
		services.AddLogging (); // Add logging services required by InMemoryRateLimitStore
		var configuration = new ConfigurationBuilder ()
			.AddInMemoryCollection (new Dictionary<string, string?> {
				["RateLimiting:EnableRateLimiting"] = "true",
				["RateLimiting:RequestsPerMinute"] = "60"
			})
			.Build ();

		// Act
		services.AddModernRateLimiting (configuration);
		// Assert
		var serviceProvider = services.BuildServiceProvider ();
		var rateLimitStore = serviceProvider.GetService<IRateLimitStore> ();

		Assert.IsNotNull (rateLimitStore);
		Assert.IsInstanceOfType<InMemoryRateLimitStore> (rateLimitStore);

		// Note: Don't test middleware resolution since it requires RequestDelegate
		// which is provided by the ASP.NET Core pipeline, not the DI container
	}
}