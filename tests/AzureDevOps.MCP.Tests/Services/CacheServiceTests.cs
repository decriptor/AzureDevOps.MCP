using AzureDevOps.MCP.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureDevOps.MCP.Tests.Services;

[TestClass]
public class CacheServiceTests
{
    private CacheService _cacheService = null!;
    private IMemoryCache _memoryCache = null!;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        
        var provider = services.BuildServiceProvider();
        _memoryCache = provider.GetRequiredService<IMemoryCache>();
        var logger = provider.GetRequiredService<ILogger<CacheService>>();
        
        _cacheService = new CacheService(_memoryCache, logger);
    }

    [TestMethod]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _cacheService.GetAsync<string>("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task SetAsync_AndGetAsync_ReturnsValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [TestMethod]
    public async Task GetOrSetAsync_WithNonExistentKey_CallsFactoryAndCaches()
    {
        // Arrange
        const string key = "test-key";
        const string value = "factory-value";
        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(value);
        });

        var cachedResult = await _cacheService.GetAsync<string>(key);

        // Assert
        factoryCalled.Should().BeTrue();
        result.Should().Be(value);
        cachedResult.Should().Be(value);
    }

    [TestMethod]
    public async Task GetOrSetAsync_WithExistingKey_DoesNotCallFactory()
    {
        // Arrange
        const string key = "test-key";
        const string cachedValue = "cached-value";
        const string factoryValue = "factory-value";
        var factoryCalled = false;

        await _cacheService.SetAsync(key, cachedValue);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(factoryValue);
        });

        // Assert
        factoryCalled.Should().BeFalse();
        result.Should().Be(cachedValue);
    }

    [TestMethod]
    public async Task RemoveAsync_RemovesItemFromCache()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        await _cacheService.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void Clear_RemovesAllItemsFromCache()
    {
        // Arrange
        const string key1 = "test-key-1";
        const string key2 = "test-key-2";
        const string value1 = "test-value-1";
        const string value2 = "test-value-2";

        _cacheService.SetAsync(key1, value1).Wait();
        _cacheService.SetAsync(key2, value2).Wait();

        // Act
        _cacheService.Clear();

        // Assert
        var result1 = _cacheService.GetAsync<string>(key1).Result;
        var result2 = _cacheService.GetAsync<string>(key2).Result;

        result1.Should().BeNull();
        result2.Should().BeNull();
    }
}