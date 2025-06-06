using AzureDevOps.MCP.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureDevOps.MCP.Tests.Services;

[TestClass]
public class PerformanceServiceTests
{
	PerformanceService _performanceService = null!;

	[TestInitialize]
	public void Setup ()
	{
		var services = new ServiceCollection ();
		services.AddLogging ();

		var provider = services.BuildServiceProvider ();
		var logger = provider.GetRequiredService<ILogger<PerformanceService>> ();

		_performanceService = new PerformanceService (logger);
	}

	[TestMethod]
	public void TrackOperation_ReturnsDisposableTracker ()
	{
		// Act
		var tracker = _performanceService.TrackOperation ("TestOperation");

		// Assert
		tracker.Should ().NotBeNull ();
		tracker.Should ().BeAssignableTo<IDisposable> ();
	}

	[TestMethod]
	public async Task GetMetricsAsync_InitialState_ReturnsEmptyMetrics ()
	{
		// Act
		var metrics = await _performanceService.GetMetricsAsync ();

		// Assert
		metrics.Should ().NotBeNull ();
		metrics.TotalOperations.Should ().Be (0);
		metrics.TotalApiCalls.Should ().Be (0);
		metrics.Operations.Should ().BeEmpty ();
		metrics.ApiCalls.Should ().BeEmpty ();
	}

	[TestMethod]
	public async Task TrackOperation_CompletedOperation_RecordsMetrics ()
	{
		// Arrange
		const string operationName = "TestOperation";

		// Act
		using (var tracker = _performanceService.TrackOperation (operationName)) {
			await Task.Delay (10); // Simulate some work
		}

		var metrics = await _performanceService.GetMetricsAsync ();

		// Assert
		metrics.TotalOperations.Should ().Be (1);
		metrics.Operations.Should ().ContainKey (operationName);
		metrics.Operations[operationName].Count.Should ().Be (1);
		metrics.Operations[operationName].MinDurationMs.Should ().BeGreaterOrEqualTo (0);
	}

	[TestMethod]
	public void RecordApiCall_Success_UpdatesStats ()
	{
		// Arrange
		const string apiName = "TestApi";
		const long duration = 100;

		// Act
		_performanceService.RecordApiCall (apiName, duration, success: true);

		// Assert
		var metrics = _performanceService.GetMetricsAsync ().Result;
		metrics.TotalApiCalls.Should ().Be (1);
		metrics.ApiCalls.Should ().ContainKey (apiName);
		metrics.ApiCalls[apiName].SuccessCount.Should ().Be (1);
		metrics.ApiCalls[apiName].FailureCount.Should ().Be (0);
		metrics.ApiCalls[apiName].TotalDurationMs.Should ().Be (duration);
	}

	[TestMethod]
	public void RecordApiCall_Failure_UpdatesStats ()
	{
		// Arrange
		const string apiName = "TestApi";
		const long duration = 200;

		// Act
		_performanceService.RecordApiCall (apiName, duration, success: false);

		// Assert
		var metrics = _performanceService.GetMetricsAsync ().Result;
		metrics.ApiCalls[apiName].SuccessCount.Should ().Be (0);
		metrics.ApiCalls[apiName].FailureCount.Should ().Be (1);
	}
}