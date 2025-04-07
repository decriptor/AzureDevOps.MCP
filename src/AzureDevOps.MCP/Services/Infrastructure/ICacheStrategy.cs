using AzureDevOps.MCP.Common;

namespace AzureDevOps.MCP.Services.Infrastructure;

public interface ICacheStrategy
{
	TimeSpan GetExpiration (string cacheKey, object? value = null);
	string GenerateKey (params object[] keyParts);
	bool ShouldCache (object? value);
}

public class DefaultCacheStrategy : ICacheStrategy
{
	public TimeSpan GetExpiration (string cacheKey, object? value = null)
	{
		return cacheKey switch {
			var key when key.StartsWith ("projects") => Constants.Cache.ProjectsCacheExpiration,
			var key when key.StartsWith ("repos_") => Constants.Cache.RepositoriesCacheExpiration,
			var key when key.StartsWith ("items_") => Constants.Cache.FileContentCacheExpiration,
			var key when key.StartsWith ("file_") => Constants.Cache.FileContentCacheExpiration,
			var key when key.StartsWith ("workitems_") => Constants.Cache.WorkItemsCacheExpiration,
			var key when key.StartsWith ("workitem_") => Constants.Cache.WorkItemsCacheExpiration,
			var key when key.StartsWith ("builds_") => Constants.Cache.BuildsCacheExpiration,
			var key when key.StartsWith ("testruns_") => Constants.Cache.TestRunsCacheExpiration,
			var key when key.StartsWith ("wikis_") => Constants.Cache.WikiCacheExpiration,
			var key when key.StartsWith ("wikipage_") => Constants.Cache.WikiCacheExpiration,
			_ => TimeSpan.FromMinutes (2) // Default fallback
		};
	}

	public string GenerateKey (params object[] keyParts)
	{
		if (keyParts == null || keyParts.Length == 0) {
			throw new ArgumentException ("Key parts cannot be null or empty", nameof (keyParts));
		}

		return string.Join ("_", keyParts.Select (part =>
			part?.ToString ()?.Replace ('/', '_').Replace ('\\', '_').Replace (':', '_') ?? "null"));
	}

	public bool ShouldCache (object? value)
	{
		return value switch {
			null => false,
			string str when string.IsNullOrEmpty (str) => false,
			System.Collections.IEnumerable enumerable when !enumerable.Cast<object> ().Any () => false,
			_ => true
		};
	}
}