using AzureDevOps.MCP.Configuration;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Extension methods for configuring rate limiting services.
/// </summary>
public static class RateLimitingServiceExtensions
{
	public static IServiceCollection AddModernRateLimiting (this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<RateLimitingConfiguration> (configuration.GetSection ("RateLimiting"));
		services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore> ();
		services.AddTransient<ModernRateLimitingMiddleware> ();

		return services;
	}

	public static IApplicationBuilder UseModernRateLimiting (this IApplicationBuilder app)
	{
		return app.UseMiddleware<ModernRateLimitingMiddleware> ();
	}
}