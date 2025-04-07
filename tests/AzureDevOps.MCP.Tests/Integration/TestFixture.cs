using AzureDevOps.MCP.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureDevOps.MCP.Tests.Integration;

/// <summary>
/// Test fixture for integration tests that sets up a complete service container
/// with real Azure DevOps configuration for testing.
/// </summary>
public class IntegrationTestFixture : IAsyncDisposable
{
	public IServiceProvider ServiceProvider { get; private set; }
	public IConfiguration Configuration { get; private set; }

	readonly IHost _host;

	public IntegrationTestFixture ()
	{
		// Build configuration from test settings
		var configurationBuilder = new ConfigurationBuilder ()
			.AddJsonFile ("appsettings.test.json", optional: true)
			.AddEnvironmentVariables ()
			.AddInMemoryCollection (GetTestConfiguration ());

		Configuration = configurationBuilder.Build ();

		// Build host with test services
		var hostBuilder = Host.CreateDefaultBuilder ()
			.ConfigureServices ((context, services) => {
				// Add Azure DevOps MCP services
				services.AddAzureDevOpsMcpServices (Configuration);

				// Add test-specific services
				services.AddSingleton<TestDataProvider> ();

				// Configure logging for tests
				services.AddLogging (builder => {
					builder.SetMinimumLevel (LogLevel.Debug);
					builder.AddConsole ();
					builder.AddDebug ();
				});
			})
			.ConfigureAppConfiguration (builder => {
				builder.Sources.Clear ();
				builder.AddConfiguration (Configuration);
			});

		_host = hostBuilder.Build ();
		ServiceProvider = _host.Services;
	}

	static Dictionary<string, string?> GetTestConfiguration ()
	{
		return new Dictionary<string, string?> {
			["AzureDevOps:OrganizationUrl"] = GetTestOrganizationUrl (),
			["AzureDevOps:PersonalAccessToken"] = GetTestPersonalAccessToken (),
			["AzureDevOps:Cache:DefaultExpirationMinutes"] = "5",
			["AzureDevOps:Cache:MaxItems"] = "100",
			["AzureDevOps:RateLimit:RequestsPerMinute"] = "60",
			["Logging:LogLevel:Default"] = "Debug",
			["Logging:LogLevel:AzureDevOps.MCP"] = "Debug"
		};
	}

	static string GetTestOrganizationUrl ()
	{
		return Environment.GetEnvironmentVariable ("TEST_AZDO_ORGANIZATION_URL")
			?? "https://dev.azure.com/test-organization";
	}

	static string GetTestPersonalAccessToken ()
	{
		return Environment.GetEnvironmentVariable ("TEST_AZDO_PAT")
			?? "test-pat-token";
	}

	public async ValueTask DisposeAsync ()
	{
		if (_host != null) {
			await _host.StopAsync ();
			_host.Dispose ();
		}
		GC.SuppressFinalize (this);
	}

	/// <summary>
	/// Determines if integration tests should run based on environment configuration.
	/// </summary>
	public bool ShouldRunIntegrationTests ()
	{
		var organizationUrl = GetTestOrganizationUrl ();
		var pat = GetTestPersonalAccessToken ();

		// Only run if real credentials are provided
		return !string.IsNullOrEmpty (organizationUrl)
			&& !organizationUrl.Contains ("test-organization")
			&& !string.IsNullOrEmpty (pat)
			&& !pat.Contains ("test-pat");
	}
}

/// <summary>
/// Provides test data for integration tests.
/// </summary>
public class TestDataProvider
{
	public string TestProjectName => GetTestProjectName ();
	public string TestRepositoryName => GetTestRepositoryName ();
	public int TestWorkItemId => GetTestWorkItemId ();

	static string GetTestProjectName ()
	{
		return Environment.GetEnvironmentVariable ("TEST_PROJECT_NAME") ?? "TestProject";
	}

	static string GetTestRepositoryName ()
	{
		return Environment.GetEnvironmentVariable ("TEST_REPOSITORY_NAME") ?? "TestRepo";
	}

	static int GetTestWorkItemId ()
	{
		return int.TryParse (Environment.GetEnvironmentVariable ("TEST_WORK_ITEM_ID"), out var id) ? id : 1;
	}
}

/// <summary>
/// Base class for integration tests that provides common setup and utilities.
/// </summary>
public abstract class IntegrationTestBase : IAsyncDisposable
{
	protected readonly IntegrationTestFixture Fixture;
	protected readonly IServiceProvider ServiceProvider;
	protected readonly TestDataProvider TestData;
	protected readonly ILogger Logger;

	protected static IntegrationTestFixture? _fixture;

	protected IntegrationTestBase ()
	{
		Fixture = _fixture ?? throw new ArgumentNullException (nameof (_fixture));
		ServiceProvider = _fixture.ServiceProvider;
		TestData = ServiceProvider.GetRequiredService<TestDataProvider> ();

		var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory> ();
		Logger = loggerFactory.CreateLogger (GetType ().Name);
	}

	/// <summary>
	/// Skip test if integration tests should not run.
	/// </summary>
	protected void SkipIfNotIntegrationTest ()
	{
		if (!Fixture.ShouldRunIntegrationTests ()) {
			Skip.If (true, "Integration tests disabled - set TEST_AZDO_ORGANIZATION_URL and TEST_AZDO_PAT environment variables to enable");
		}
	}

	/// <summary>
	/// Gets a service from the dependency injection container.
	/// </summary>
	protected T GetService<T> () where T : notnull
	{
		return ServiceProvider.GetRequiredService<T> ();
	}

	/// <summary>
	/// Gets a scoped service for the duration of a test.
	/// </summary>
	protected async Task<T> GetScopedServiceAsync<T> (Func<T, Task> testAction) where T : notnull
	{
		using var scope = ServiceProvider.CreateScope ();
		var service = scope.ServiceProvider.GetRequiredService<T> ();
		await testAction (service);
		return service;
	}

	public virtual ValueTask DisposeAsync ()
	{
		return ValueTask.CompletedTask;
	}
}

/// <summary>
/// Skip helper for conditional test execution.
/// </summary>
public static class Skip
{
	public static void If (bool condition, string reason)
	{
		if (condition) {
			throw new SkipException (reason);
		}
	}
}

/// <summary>
/// Exception thrown to skip a test.
/// </summary>
public class SkipException : Exception
{
	public SkipException (string reason) : base (reason) { }
}