namespace AzureDevOps.MCP.Validation;

public class ValidationException : ArgumentException
{
	public ValidationException(string message) : base(message) { }
	public ValidationException(string message, Exception innerException) : base(message, innerException) { }
	public ValidationException(string message, string paramName) : base(message, paramName) { }
}