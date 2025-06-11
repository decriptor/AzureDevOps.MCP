using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AzureDevOps.MCP.Configuration;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Modern rate limiting middleware using .NET 9 features and high-performance algorithms.
/// </summary>
public sealed class ModernRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ModernRateLimitingMiddleware> _logger;
    private readonly RateLimitingConfiguration _config;
    private readonly IRateLimitStore _store;
    
    // Use frozen dictionary for rate limit configurations
    private static readonly FrozenDictionary<string, RateLimitRule> DefaultRules = new Dictionary<string, RateLimitRule>
    {
        ["per_minute"] = new(TimeSpan.FromMinutes(1), 60),
        ["per_hour"] = new(TimeSpan.FromHours(1), 1000),
        ["per_day"] = new(TimeSpan.FromDays(1), 10000)
    }.ToFrozenDictionary();

    // Use frozen set for exempt paths
    private static readonly FrozenSet<string> ExemptPaths = new HashSet<string>
    {
        "/health",
        "/health/live",
        "/health/ready",
        "/metrics"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public ModernRateLimitingMiddleware(
        RequestDelegate next,
        ILogger<ModernRateLimitingMiddleware> logger,
        IOptions<RateLimitingConfiguration> config,
        IRateLimitStore store)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_config.EnableRateLimiting)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        
        // Skip rate limiting for exempt paths using modern collection expressions
        if (ExemptPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientId(context);
        var rateLimitResult = await CheckRateLimitAsync(clientId, context.RequestAborted);

        // Add rate limit headers using modern pattern matching
        AddRateLimitHeaders(context, rateLimitResult);

        if (!rateLimitResult.IsAllowed)
        {
            await HandleRateLimitExceeded(context, rateLimitResult);
            return;
        }

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // Use modern switch expression for client identification
        var clientId = _config.ClientIdentificationStrategy.ToLowerInvariant() switch
        {
            "ip" => GetClientIpAddress(context),
            "user" => GetUserId(context) ?? GetClientIpAddress(context),
            "api_key" => GetApiKey(context) ?? GetClientIpAddress(context),
            "combined" => GetCombinedClientId(context),
            _ => GetClientIpAddress(context)
        };

        // Hash client ID for privacy and consistent length using .NET 9 crypto APIs
        return HashClientId(clientId);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Modern pattern matching for IP extraction
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() 
                       ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                       ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
                       ?? "unknown";

        return ipAddress;
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User?.Identity?.Name;
    }

    private static string? GetApiKey(HttpContext context)
    {
        return context.Request.Headers["X-API-Key"].FirstOrDefault()
               ?? context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCombinedClientId(HttpContext context)
    {
        var components = new List<string>
        {
            GetClientIpAddress(context),
            GetUserId(context) ?? "anonymous",
            GetApiKey(context) ?? "no-key"
        };

        return string.Join(":", components);
    }

    private static string HashClientId(string clientId)
    {
        // Use modern .NET 9 crypto APIs for hashing
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(clientId));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<RateLimitResult> CheckRateLimitAsync(string clientId, CancellationToken cancellationToken)
    {
        var results = new List<RateLimitCheckResult>();

        // Check all rate limit rules concurrently using modern async patterns
        var checkTasks = DefaultRules.Select(async rule =>
        {
            var key = $"rate_limit:{clientId}:{rule.Key}";
            var limit = GetLimitForRule(rule.Key);
            var window = rule.Value.WindowSize;
            
            var result = await _store.CheckAndIncrementAsync(key, limit, window, cancellationToken);
            return new RateLimitCheckResult(rule.Key, limit, result.RequestCount, result.IsAllowed, result.ResetTime);
        });

        var checkResults = await Task.WhenAll(checkTasks);
        results.AddRange(checkResults);

        // Find the most restrictive result using modern LINQ
        var mostRestrictive = results.MinBy(r => r.RemainingRequests) ?? results.First();
        
        return new RateLimitResult(
            IsAllowed: results.All(r => r.IsAllowed),
            ClientId: clientId,
            RequestCount: mostRestrictive.RequestCount,
            RequestLimit: mostRestrictive.RequestLimit,
            RemainingRequests: mostRestrictive.RemainingRequests,
            ResetTime: mostRestrictive.ResetTime,
            RetryAfter: CalculateRetryAfter(mostRestrictive.ResetTime),
            Rules: results.ToFrozenDictionary(r => r.RuleName, r => r)
        );
    }

    private int GetLimitForRule(string ruleName) => ruleName switch
    {
        "per_minute" => _config.RequestsPerMinute,
        "per_hour" => _config.RequestsPerHour,
        "per_day" => _config.RequestsPerDay,
        _ => _config.RequestsPerMinute
    };

    private static TimeSpan CalculateRetryAfter(DateTimeOffset resetTime)
    {
        var retryAfter = resetTime - DateTimeOffset.UtcNow;
        return retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1);
    }

    private static void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        context.Response.Headers["X-RateLimit-Limit"] = result.RequestLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, result.RemainingRequests).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = result.ResetTime.ToUnixTimeSeconds().ToString();
        context.Response.Headers["X-RateLimit-RetryAfter"] = ((int)result.RetryAfter.TotalSeconds).ToString();
        context.Response.Headers["X-RateLimit-Policy"] = string.Join(",", result.Rules.Keys);
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitResult result)
    {
        _logger.LogWarning("Rate limit exceeded for client {ClientId}. Limit: {Limit}, Count: {Count}, Reset: {Reset}",
            result.ClientId, result.RequestLimit, result.RequestCount, result.ResetTime);

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        // Use modern JSON serialization with .NET 9 improvements
        var errorResponse = new
        {
            error = "rate_limit_exceeded",
            message = "Rate limit exceeded. Please try again later.",
            details = new
            {
                limit = result.RequestLimit,
                remaining = result.RemainingRequests,
                reset_time = result.ResetTime.ToString("O"),
                retry_after_seconds = (int)result.RetryAfter.TotalSeconds,
                client_id_hash = result.ClientId[..8] + "..." // Show first 8 chars for debugging
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}

