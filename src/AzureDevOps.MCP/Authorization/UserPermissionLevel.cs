namespace AzureDevOps.MCP.Authorization;

/// <summary>
/// Defines user permission levels for Azure DevOps operations.
/// </summary>
public enum UserPermissionLevel
{
	Reader = 1,
	Contributor = 2,
	Administrator = 3
}