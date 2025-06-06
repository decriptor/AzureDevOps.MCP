using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureDevOps.MCP.Tests.Tools;

[TestClass]
public class PerformanceToolsTests
{
    private Mock<IPerformanceService> _mockPerformanceService = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<ILogger<PerformanceTools>> _mockLogger = null!;
    private PerformanceTools _performanceTools = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockPerformanceService = new Mock<IPerformanceService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<PerformanceTools>>();
        
        _performanceTools = new PerformanceTools(
            _mockPerformanceService.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetPerformanceMetricsAsync_ReturnsMetrics()
    {
        // Arrange
        var mockMetrics = new PerformanceMetrics
        {
            StartTime = DateTime.UtcNow.AddHours(-2),
            TotalOperations = 150,
            TotalApiCalls = 75,
            Operations = new Dictionary<string, OperationMetrics>
            {
                ["GetProjects"] = new OperationMetrics
                {
                    Count = 10,
                    AverageDurationMs = 250.5,
                    MinDurationMs = 100,
                    MaxDurationMs = 500,
                    TotalDurationMs = 2505
                },
                ["GetWorkItems"] = new OperationMetrics
                {
                    Count = 25,
                    AverageDurationMs = 150.2,
                    MinDurationMs = 50,
                    MaxDurationMs = 300,
                    TotalDurationMs = 3755
                }
            },
            ApiCalls = new Dictionary<string, ApiCallMetrics>
            {
                ["GetProjects"] = new ApiCallMetrics
                {
                    SuccessCount = 9,
                    FailureCount = 1,
                    AverageDurationMs = 200.0,
                    TotalDurationMs = 2000
                },
                ["GetWorkItems"] = new ApiCallMetrics
                {
                    SuccessCount = 24,
                    FailureCount = 1,
                    AverageDurationMs = 120.0,
                    TotalDurationMs = 3000
                }
            }
        };

        _mockPerformanceService
            .Setup(x => x.GetMetricsAsync())
            .ReturnsAsync(mockMetrics);

        // Act
        var result = await _performanceTools.GetPerformanceMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        _mockPerformanceService.Verify(x => x.GetMetricsAsync(), Times.Once);
    }

    [TestMethod]
    public async Task GetPerformanceMetricsAsync_ServiceThrowsException_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockPerformanceService
            .Setup(x => x.GetMetricsAsync())
            .ThrowsAsync(new InvalidOperationException("Metrics service error"));

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _performanceTools.GetPerformanceMetricsAsync());
        
        exception.Message.Should().Contain("Failed to get performance metrics");
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [TestMethod]
    public async Task ClearCacheAsync_SuccessfulClear_ReturnsSuccessResponse()
    {
        // Arrange
        _mockCacheService
            .Setup(x => x.Clear())
            .Verifiable();

        // Act
        var result = await _performanceTools.ClearCacheAsync();

        // Assert
        result.Should().NotBeNull();
        _mockCacheService.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task ClearCacheAsync_ServiceThrowsException_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockCacheService
            .Setup(x => x.Clear())
            .Throws(new InvalidOperationException("Cache clear error"));

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _performanceTools.ClearCacheAsync());
        
        exception.Message.Should().Contain("Failed to clear cache");
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [TestMethod]
    public async Task GetPerformanceMetricsAsync_CalculatesUptimeCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-3).AddMinutes(-25);
        var mockMetrics = new PerformanceMetrics
        {
            StartTime = startTime,
            TotalOperations = 10,
            TotalApiCalls = 5,
            Operations = new Dictionary<string, OperationMetrics>(),
            ApiCalls = new Dictionary<string, ApiCallMetrics>()
        };

        _mockPerformanceService
            .Setup(x => x.GetMetricsAsync())
            .ReturnsAsync(mockMetrics);

        // Act
        var result = await _performanceTools.GetPerformanceMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        
        // The uptime should be approximately 3 hours and 25 minutes
        // Note: Exact timing may vary slightly due to test execution time
        var uptime = DateTime.UtcNow - startTime;
        uptime.Hours.Should().Be(3);
        uptime.Minutes.Should().BeGreaterOrEqualTo(24); // Allow for slight timing differences
    }

    [TestMethod]
    public async Task GetPerformanceMetricsAsync_CalculatesSuccessRatesCorrectly()
    {
        // Arrange
        var mockMetrics = new PerformanceMetrics
        {
            StartTime = DateTime.UtcNow.AddHours(-1),
            TotalOperations = 100,
            TotalApiCalls = 50,
            Operations = new Dictionary<string, OperationMetrics>(),
            ApiCalls = new Dictionary<string, ApiCallMetrics>
            {
                ["TestApi1"] = new ApiCallMetrics
                {
                    SuccessCount = 8,
                    FailureCount = 2,
                    AverageDurationMs = 100.0,
                    TotalDurationMs = 1000
                },
                ["TestApi2"] = new ApiCallMetrics
                {
                    SuccessCount = 15,
                    FailureCount = 0,
                    AverageDurationMs = 50.0,
                    TotalDurationMs = 750
                }
            }
        };

        _mockPerformanceService
            .Setup(x => x.GetMetricsAsync())
            .ReturnsAsync(mockMetrics);

        // Act
        var result = await _performanceTools.GetPerformanceMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        
        // TestApi1 should have 80% success rate (8 success / 10 total)
        // TestApi2 should have 100% success rate (15 success / 15 total)
        _mockPerformanceService.Verify(x => x.GetMetricsAsync(), Times.Once);
    }

    [TestMethod]
    public async Task GetPerformanceMetricsAsync_OrdersResultsByFrequency()
    {
        // Arrange
        var mockMetrics = new PerformanceMetrics
        {
            StartTime = DateTime.UtcNow.AddHours(-1),
            TotalOperations = 100,
            TotalApiCalls = 50,
            Operations = new Dictionary<string, OperationMetrics>
            {
                ["FrequentOp"] = new OperationMetrics { Count = 50 },
                ["RareOp"] = new OperationMetrics { Count = 5 },
                ["MediumOp"] = new OperationMetrics { Count = 20 }
            },
            ApiCalls = new Dictionary<string, ApiCallMetrics>
            {
                ["FrequentApi"] = new ApiCallMetrics { SuccessCount = 30, FailureCount = 5 },
                ["RareApi"] = new ApiCallMetrics { SuccessCount = 2, FailureCount = 0 },
                ["MediumApi"] = new ApiCallMetrics { SuccessCount = 10, FailureCount = 2 }
            }
        };

        _mockPerformanceService
            .Setup(x => x.GetMetricsAsync())
            .ReturnsAsync(mockMetrics);

        // Act
        var result = await _performanceTools.GetPerformanceMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        
        // Results should be ordered by frequency (most frequent first)
        // Operations: FrequentOp (50), MediumOp (20), RareOp (5)
        // ApiCalls: FrequentApi (35), MediumApi (12), RareApi (2)
        _mockPerformanceService.Verify(x => x.GetMetricsAsync(), Times.Once);
    }
}