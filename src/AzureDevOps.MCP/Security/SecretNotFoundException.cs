namespace AzureDevOps.MCP.Security;

/// <summary>
/// Exception thrown when a secret cannot be found in any configured source.
/// </summary>
public class SecretNotFoundException : Exception
{
	/// <summary>
	/// The name of the secret that could not be found.
	/// </summary>
	public string SecretName { get; }

	/// <summary>
	/// Initializes a new instance of the SecretNotFoundException.
	/// </summary>
	/// <param name="secretName">The name of the secret</param>
	/// <param name="message">The exception message</param>
	public SecretNotFoundException(string secretName, string message)
		: base(message)
	{
		SecretName = secretName;
	}

	/// <summary>
	/// Initializes a new instance of the SecretNotFoundException.
	/// </summary>
	/// <param name="secretName">The name of the secret</param>
	/// <param name="message">The exception message</param>
	/// <param name="innerException">The inner exception</param>
	public SecretNotFoundException(string secretName, string message, Exception innerException)
		: base(message, innerException)
	{
		SecretName = secretName;
	}
}