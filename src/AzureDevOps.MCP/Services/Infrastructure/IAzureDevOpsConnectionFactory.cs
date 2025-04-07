using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.MCP.Services.Infrastructure;

public interface IAzureDevOpsConnectionFactory : IDisposable, IAsyncDisposable
{
	Task<VssConnection> GetConnectionAsync (CancellationToken cancellationToken = default);
	Task<T> GetClientAsync<T> (CancellationToken cancellationToken = default) where T : VssHttpClientBase;
	Task<bool> TestConnectionAsync (CancellationToken cancellationToken = default);
	void InvalidateConnection ();
}

public interface IConnectionPoolManager : IDisposable, IAsyncDisposable
{
	Task<VssConnection> AcquireConnectionAsync (CancellationToken cancellationToken = default);
	Task ReleaseConnectionAsync (VssConnection connection);
	Task<T> ExecuteWithConnectionAsync<T> (Func<VssConnection, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
	Task ExecuteWithConnectionAsync (Func<VssConnection, CancellationToken, Task> operation, CancellationToken cancellationToken = default);
	Task<bool> TestAllConnectionsAsync (CancellationToken cancellationToken = default);
}