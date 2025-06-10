using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Security;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.MCP.ErrorHandling;

public interface IErrorHandler
{
	Task<T> ExecuteWithErrorHandlingAsync<T> (
		Func<CancellationToken, Task<T>> operation,
		string operationName,
		CancellationToken cancellationToken = default);

	Task ExecuteWithErrorHandlingAsync (
		Func<CancellationToken, Task> operation,
		string operationName,
		CancellationToken cancellationToken = default);

	bool ShouldRetry (Exception exception);
	TimeSpan GetRetryDelay (int attemptNumber);
}

public class ResilientErrorHandler : IErrorHandler
{
	readonly ILogger<ResilientErrorHandler> _logger;

	// .NET 9: Use FrozenSet for O(1) lookups with zero allocations
	static readonly FrozenSet<Type> RetryableExceptionTypes = new HashSet<Type>
	{
		typeof(HttpRequestException),
		typeof(TaskCanceledException),
		typeof(SocketException),
		typeof(TimeoutException)
	}.ToFrozenSet ();

	// .NET 9: Use FrozenDictionary for fast lookup
	static readonly FrozenDictionary<HttpStatusCode, ErrorCategory> HttpStatusErrorMap =
		new Dictionary<HttpStatusCode, ErrorCategory> {
			[HttpStatusCode.BadRequest] = ErrorCategory.ClientError,
			[HttpStatusCode.Unauthorized] = ErrorCategory.AuthenticationError,
			[HttpStatusCode.Forbidden] = ErrorCategory.AuthorizationError,
			[HttpStatusCode.NotFound] = ErrorCategory.NotFound,
			[HttpStatusCode.TooManyRequests] = ErrorCategory.RateLimited,
			[HttpStatusCode.InternalServerError] = ErrorCategory.ServerError,
			[HttpStatusCode.BadGateway] = ErrorCategory.ServerError,
			[HttpStatusCode.ServiceUnavailable] = ErrorCategory.ServerError,
			[HttpStatusCode.GatewayTimeout] = ErrorCategory.ServerError
		}.ToFrozenDictionary ();

	const int MaxRetryAttempts = 3;
	static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds (500);
	static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds (30);

	public ResilientErrorHandler (ILogger<ResilientErrorHandler> logger)
	{
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
	}

	public async Task<T> ExecuteWithErrorHandlingAsync<T> (
		Func<CancellationToken, Task<T>> operation,
		string operationName,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace (operationName);
		ArgumentNullException.ThrowIfNull (operation);

		using var activity = ActivitySource.StartActivity ($"ErrorHandler.{operationName}");
		activity?.SetTag ("operation.name", operationName);

		var attempt = 0;
		Exception? lastException = null;

		while (attempt < MaxRetryAttempts) {
			attempt++;
			activity?.SetTag ("attempt.number", attempt);

			try {
				_logger.LogDebug ("Executing operation {OperationName}, attempt {Attempt}/{MaxAttempts}",
					operationName, attempt, MaxRetryAttempts);

				var result = await operation (cancellationToken);

				if (attempt > 1) {
					_logger.LogInformation ("Operation {OperationName} succeeded on attempt {Attempt}",
						operationName, attempt);
				}

				activity?.SetStatus (ActivityStatusCode.Ok);
				return result;
			} catch (Exception ex) when (ShouldCatch (ex, attempt, operationName, out var category)) {
				lastException = ex;
				activity?.SetStatus (ActivityStatusCode.Error, ex.Message);
				activity?.SetTag ("error.category", category.ToString ());
				activity?.SetTag ("error.type", ex.GetType ().Name);

				if (attempt < MaxRetryAttempts && ShouldRetry (ex)) {
					var delay = GetRetryDelay (attempt);

					_logger.LogWarning (ex,
						"Operation {OperationName} failed on attempt {Attempt}/{MaxAttempts}. " +
						"Category: {ErrorCategory}. Retrying in {DelayMs}ms",
						operationName, attempt, MaxRetryAttempts, category, delay.TotalMilliseconds);

					await Task.Delay (delay, cancellationToken);
					continue;
				}

				// Final attempt or non-retryable error
				var enhancedException = CreateEnhancedException (ex, operationName, category, attempt);

				_logger.LogError (enhancedException,
					"Operation {OperationName} failed permanently after {Attempts} attempts. Category: {ErrorCategory}",
					operationName, attempt, category);

				throw enhancedException;
			}
		}

		// This should never be reached, but satisfy compiler
		throw CreateEnhancedException (lastException!, operationName, ErrorCategory.Unknown, attempt);
	}

	public async Task ExecuteWithErrorHandlingAsync (
		Func<CancellationToken, Task> operation,
		string operationName,
		CancellationToken cancellationToken = default)
	{
		await ExecuteWithErrorHandlingAsync (async ct => {
			await operation (ct);
			return true; // Dummy return value
		}, operationName, cancellationToken);
	}

	public bool ShouldRetry (Exception exception)
	{
		return exception switch {
			// .NET 9: Pattern matching with when clause
			// Commenting out VssServiceException as it's not available in current packages
			// VssServiceException vssEx when IsRetryableHttpStatus(vssEx.HttpStatusCode) => true,
			HttpRequestException => true,
			TaskCanceledException tce when !tce.CancellationToken.IsCancellationRequested => true, // Timeout
			SocketException => true,
			TimeoutException => true,
			_ => RetryableExceptionTypes.Contains (exception.GetType ())
		};
	}

	public TimeSpan GetRetryDelay (int attemptNumber)
	{
		// Exponential backoff with jitter (.NET 9: Random.Shared)
		var baseDelayMs = BaseDelay.TotalMilliseconds * Math.Pow (2, attemptNumber - 1);
		var jitterMs = Random.Shared.NextDouble () * baseDelayMs * 0.1; // 10% jitter

		var totalDelayMs = Math.Min (baseDelayMs + jitterMs, MaxDelay.TotalMilliseconds);

		return TimeSpan.FromMilliseconds (totalDelayMs);
	}

	static bool ShouldCatch (
		Exception exception,
		int attempt,
		string operationName,
		out ErrorCategory category)
	{
		category = CategorizeException (exception);

		// Never catch cancellation from user
		if (exception is OperationCanceledException oce && oce.CancellationToken.IsCancellationRequested) {
			return false;
		}

		return true;
	}

	static ErrorCategory CategorizeException (Exception exception)
	{
		return exception switch {
			VssServiceException vssEx => ErrorCategory.ServerError, // Simplified - HttpStatusCode may not be available
			UnauthorizedAccessException => ErrorCategory.AuthorizationError,
			SecurityException => ErrorCategory.AuthenticationError,
			Validation.ValidationException => ErrorCategory.ClientError,
			ArgumentException => ErrorCategory.ClientError,
			HttpRequestException httpEx when httpEx.Message.Contains ("timeout", StringComparison.OrdinalIgnoreCase) => ErrorCategory.Timeout,
			TaskCanceledException tce when !tce.CancellationToken.IsCancellationRequested => ErrorCategory.Timeout,
			TimeoutException => ErrorCategory.Timeout,
			SocketException => ErrorCategory.NetworkError,
			HttpRequestException => ErrorCategory.NetworkError,
			_ => ErrorCategory.Unknown
		};
	}

	static bool IsRetryableHttpStatus (HttpStatusCode statusCode)
	{
		return statusCode is
			HttpStatusCode.InternalServerError or
			HttpStatusCode.BadGateway or
			HttpStatusCode.ServiceUnavailable or
			HttpStatusCode.GatewayTimeout or
			HttpStatusCode.TooManyRequests;
	}

	static Exception CreateEnhancedException (
		Exception originalException,
		string operationName,
		ErrorCategory category,
		int attempts)
	{
		var message = $"Operation '{operationName}' failed after {attempts} attempts. Category: {category}";

		return category switch {
			ErrorCategory.AuthenticationError => new UnauthorizedAccessException (message, originalException),
			ErrorCategory.AuthorizationError => new UnauthorizedAccessException (message, originalException),
			ErrorCategory.ClientError => new ArgumentException (message, originalException),
			ErrorCategory.NotFound => new InvalidOperationException ($"{message}. Resource not found.", originalException),
			ErrorCategory.RateLimited => new InvalidOperationException ($"{message}. Rate limit exceeded.", originalException),
			_ => new InvalidOperationException (message, originalException)
		};
	}

	// .NET 9: Use ActivitySource for distributed tracing
	static readonly ActivitySource ActivitySource = new ("AzureDevOps.MCP.ErrorHandling");
}

public enum ErrorCategory
{
	Unknown,
	ClientError,
	AuthenticationError,
	AuthorizationError,
	NotFound,
	RateLimited,
	ServerError,
	NetworkError,
	Timeout
}

public class OperationContext
{
	public required string OperationName { get; init; }
	public Dictionary<string, object> Properties { get; init; } = [];
	public DateTime StartTime { get; init; } = DateTime.UtcNow;
	public string? UserId { get; init; }
	public string? CorrelationId { get; init; }
}