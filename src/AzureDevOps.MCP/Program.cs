using System.Security.Claims;
using System.Text.Json;

using AzureDevOps.MCP;
using AzureDevOps.MCP.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder (args);

// Configure JSON serialization for .NET 9 performance
builder.Services.ConfigureHttpJsonOptions (options => {
	options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	options.SerializerOptions.WriteIndented = false;
	options.SerializerOptions.PropertyNameCaseInsensitive = true;
	options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
	options.SerializerOptions.TypeInfoResolverChain.Insert (0, AzureDevOpsJsonContext.Default);
});

// Configure Sentry for error tracking and performance monitoring
builder.WebHost.UseSentry ((context, options) => {
	options.Dsn = context.Configuration["Monitoring:Sentry:Dsn"] ?? "";
	options.Environment = builder.Environment.EnvironmentName;
	options.Release = typeof (Program).Assembly.GetName ().Version?.ToString ();
	options.TracesSampleRate = builder.Environment.IsDevelopment () ? 1.0 : 0.1;
	options.ProfilesSampleRate = builder.Environment.IsDevelopment () ? 1.0 : 0.1;
	options.AttachStacktrace = true;
	options.SendDefaultPii = false;
	options.MaxBreadcrumbs = 100;
	options.Debug = builder.Environment.IsDevelopment ();

	// .NET 9: Enhanced performance monitoring
	options.TracesSampleRate = 1.0;
	options.DiagnosticLevel = SentryLevel.Info;
});

// Configure OpenTelemetry for distributed tracing
builder.Services.AddOpenTelemetry ()
	.WithTracing (tracing => {
		tracing
			.AddAspNetCoreInstrumentation (options => {
				options.RecordException = true;
				options.EnrichWithHttpRequest = (activity, request) => {
					activity.SetTag ("http.user_agent", request.Headers.UserAgent.ToString ());
					activity.SetTag ("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString ());
				};
				options.EnrichWithHttpResponse = (activity, response) => {
					activity.SetTag ("http.response.status_code", response.StatusCode);
				};
			})
			.AddHttpClientInstrumentation (options => {
				options.RecordException = true;
				options.EnrichWithHttpRequestMessage = (activity, request) => {
					activity.SetTag ("http.url", request.RequestUri?.ToString ());
				};
				options.EnrichWithHttpResponseMessage = (activity, response) => {
					activity.SetTag ("http.response.status_code", (int)response.StatusCode);
				};
			})
			.SetResourceBuilder (ResourceBuilder.CreateDefault ()
				.AddService ("azuredevops-mcp", typeof (Program).Assembly.GetName ().Version?.ToString ())
				.AddAttributes (
				[
					new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
					new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)
				]))
			.AddConsoleExporter ()
			.AddSource ("AzureDevOps.MCP.*");
	})
	.WithMetrics (metrics => {
		metrics
			.AddAspNetCoreInstrumentation ()
			.AddHttpClientInstrumentation ()
			// Runtime instrumentation may not be available in all environments
			// .AddProcessInstrumentation () // Temporarily disabled
			.AddConsoleExporter ()
			.AddMeter ("AzureDevOps.MCP.*");
	});

// Configure authentication and authorization
builder.Services.AddAuthentication (options => {
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer (options => {
	options.RequireHttpsMetadata = !builder.Environment.IsDevelopment ();
	options.SaveToken = true;
	options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters {
		ValidateIssuerSigningKey = true,
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ClockSkew = TimeSpan.FromMinutes (5),
		// Configure these from settings
		ValidIssuer = builder.Configuration["Authentication:Issuer"],
		ValidAudience = builder.Configuration["Authentication:Audience"]
	};

	// .NET 9: Enhanced token validation events
	options.Events = new JwtBearerEvents {
		OnAuthenticationFailed = context => {
			context.HttpContext.Items["auth_failure_reason"] = context.Exception.Message;
			return Task.CompletedTask;
		},
		OnTokenValidated = context => {
			// Add custom claims or validation logic
			var identity = context.Principal?.Identity as ClaimsIdentity;
			identity?.AddClaim (new Claim ("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds ().ToString ()));
			return Task.CompletedTask;
		}
	};
});

builder.Services.AddAuthorizationBuilder ()
	.AddPolicy ("RequireAuthentication", policy => policy.RequireAuthenticatedUser ())
	.AddPolicy ("RequireAdministrator", policy => policy.RequireRole ("Administrator"))
	.AddPolicy ("RequireContributor", policy => policy.RequireRole ("Contributor", "Administrator"));

// Configure caching
builder.Services.AddMemoryCache (options => {
	options.SizeLimit = 1000; // Limit cache entries
	options.CompactionPercentage = 0.25; // Remove 25% when limit reached
});

// Add distributed caching if Redis is configured
if (!string.IsNullOrEmpty (builder.Configuration.GetConnectionString ("Redis"))) {
	builder.Services.AddStackExchangeRedisCache (options => {
		options.Configuration = builder.Configuration.GetConnectionString ("Redis");
		options.InstanceName = "AzureDevOpsMCP";
	});
}

// Configure all Azure DevOps MCP services using clean architecture
builder.Services.AddAzureDevOpsMcpServices (builder.Configuration);

// Add basic memory cache
builder.Services.AddMemoryCache ();

// Add MCP server
builder.Services.AddMcpServer ();

// Configure logging with structured logging
builder.Logging.ClearProviders ();
builder.Logging.AddConsole (options => {
	options.FormatterName = "json";
});

if (builder.Environment.IsDevelopment ()) {
	builder.Logging.AddDebug ();
	builder.Logging.SetMinimumLevel (LogLevel.Debug);
} else {
	builder.Logging.SetMinimumLevel (LogLevel.Information);
}

// Add Sentry logging
builder.Logging.AddSentry (options => {
	options.MinimumBreadcrumbLevel = LogLevel.Debug;
	options.MinimumEventLevel = LogLevel.Warning;
	options.DiagnosticLevel = SentryLevel.Debug;
	options.AttachStacktrace = true;
});

var app = builder.Build ();

// Configure the HTTP request pipeline with .NET 9 performance optimizations
app.UseRouting ();

// Add security headers middleware
app.Use (async (context, next) => {
	context.Response.Headers.Append ("X-Content-Type-Options", "nosniff");
	context.Response.Headers.Append ("X-Frame-Options", "DENY");
	context.Response.Headers.Append ("X-XSS-Protection", "1; mode=block");
	context.Response.Headers.Append ("Referrer-Policy", "strict-origin-when-cross-origin");

	if (context.Request.IsHttps) {
		context.Response.Headers.Append ("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
	}

	await next ();
});

// Add authentication and authorization
app.UseAuthentication ();
app.UseAuthorization ();

// Configure Sentry request tracing
app.UseSentryTracing ();

// Configure health checks
app.MapHealthChecks ("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
	ResponseWriter = async (context, report) => {
		context.Response.ContentType = "application/json";

		var result = JsonSerializer.Serialize (new {
			status = report.Status.ToString (),
			totalDuration = report.TotalDuration.TotalMilliseconds,
			entries = report.Entries.Select (e => new {
				name = e.Key,
				status = e.Value.Status.ToString (),
				duration = e.Value.Duration.TotalMilliseconds,
				description = e.Value.Description,
				data = e.Value.Data,
				exception = e.Value.Exception?.Message
			})
		}, AzureDevOpsJsonContext.Default.JsonElement);

		await context.Response.WriteAsync (result);
	}
});

app.MapHealthChecks ("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
	Predicate = check => check.Tags.Contains ("ready")
});

app.MapHealthChecks ("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
	Predicate = check => check.Tags.Contains ("live")
});

// Add metrics endpoint for monitoring
app.MapGet ("/metrics", () => {
	return Results.Json (new {
		timestamp = DateTimeOffset.UtcNow,
		runtime = new {
			totalMemory = GC.GetTotalMemory (false),
			gen0Collections = GC.CollectionCount (0),
			gen1Collections = GC.CollectionCount (1),
			gen2Collections = GC.CollectionCount (2),
			totalAllocatedBytes = GC.GetTotalAllocatedBytes (false)
		}
	}, AzureDevOpsJsonContext.Default.JsonElement);
})
.RequireAuthorization ("RequireAuthentication");

// Configure MCP server with stdio transport
// Note: Commenting out as the extension method may not be available
// app.WithStdioServerTransport();

if (!app.Environment.IsDevelopment ()) {
	app.UseExceptionHandler ("/error");
	app.UseHsts ();
	app.UseHttpsRedirection ();
}

// Global exception handler
app.UseExceptionHandler (errorApp => {
	errorApp.Run (async context => {
		var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature> ();
		var exception = exceptionHandlerPathFeature?.Error;

		if (exception != null) {
			SentrySdk.CaptureException (exception);
		}

		context.Response.StatusCode = 500;
		context.Response.ContentType = "application/json";

		var response = JsonSerializer.Serialize (new {
			error = "An unexpected error occurred",
			timestamp = DateTimeOffset.UtcNow,
			traceId = System.Diagnostics.Activity.Current?.TraceId.ToString ()
		}, AzureDevOpsJsonContext.Default.JsonElement);

		await context.Response.WriteAsync (response);
	});
});

// Startup validation using new architecture
try {
	using var scope = app.Services.CreateScope ();
	var secretManager = scope.ServiceProvider.GetRequiredService<AzureDevOps.MCP.Security.ISecretManager> ();

	// Validate critical secrets are available
	await secretManager.GetSecretAsync ("OrganizationUrl");
	await secretManager.GetSecretAsync ("PersonalAccessToken");

	var connectionFactory = scope.ServiceProvider.GetRequiredService<AzureDevOps.MCP.Services.Infrastructure.IAzureDevOpsConnectionFactory> ();

	// Test connection by getting a client
	var connection = await connectionFactory.GetConnectionAsync ();
	if (!connection.HasAuthenticated) {
		await connection.ConnectAsync ();
	}

	if (!connection.HasAuthenticated) {
		throw new InvalidOperationException ("Azure DevOps connection authentication failed during startup");
	}

	app.Logger.LogInformation ("Azure DevOps MCP Server startup validation completed successfully");
} catch (Exception ex) {
	app.Logger.LogCritical (ex, "Startup validation failed. Application cannot start.");
	throw;
}

app.Logger.LogInformation ("Azure DevOps MCP Server starting on {Environment} environment", app.Environment.EnvironmentName);

await app.RunAsync ();
