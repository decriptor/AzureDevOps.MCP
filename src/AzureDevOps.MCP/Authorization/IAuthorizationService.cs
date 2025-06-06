using System.Collections.Frozen;
using System.Security.Claims;

namespace AzureDevOps.MCP.Authorization;

public interface IAuthorizationService
{
    Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        string resource,
        string operation,
        CancellationToken cancellationToken = default);

    Task<AuthorizationResult> AuthorizeProjectAccessAsync(
        ClaimsPrincipal user,
        string projectName,
        ProjectPermission permission,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(
        ClaimsPrincipal user,
        string permission,
        CancellationToken cancellationToken = default);
}

public class BasicAuthorizationService : IAuthorizationService
{
    private readonly ILogger<BasicAuthorizationService> _logger;
    
    // .NET 9: Use FrozenSet for O(1) lookups with zero allocation
    private static readonly FrozenSet<string> ReadOperations = new HashSet<string>
    {
        "GetProjects",
        "GetRepositories", 
        "GetWorkItems",
        "GetBuilds",
        "GetTestPlans",
        "GetFiles",
        "GetCommits",
        "GetBranches",
        "GetTags",
        "GetPullRequests",
        "SearchCode"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> WriteOperations = new HashSet<string>
    {
        "CreateWorkItem",
        "UpdateWorkItem",
        "CreatePullRequest",
        "UpdatePullRequest",
        "CreateBranch",
        "DeleteBranch",
        "TriggerBuild",
        "CreateTestRun"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> AdminOperations = new HashSet<string>
    {
        "DeleteProject",
        "ManagePermissions",
        "ManageUsers",
        "ConfigureProject"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public BasicAuthorizationService(ILogger<BasicAuthorizationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        string resource,
        string operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        using var activity = System.Diagnostics.Activity.Current?.Source
            .StartActivity($"Authorization.{operation}");
        
        activity?.SetTag("resource", resource);
        activity?.SetTag("operation", operation);
        activity?.SetTag("user.id", GetUserId(user));

        try
        {
            // Check if user is authenticated
            if (!user.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("Authorization failed: User not authenticated for operation {Operation} on {Resource}",
                    operation, resource);
                return AuthorizationResult.Forbidden("User not authenticated");
            }

            // Get user permissions
            var userPermissions = GetUserPermissions(user);
            var requiredPermission = GetRequiredPermission(operation);

            // Check permission level
            if (!HasRequiredPermission(userPermissions, requiredPermission))
            {
                _logger.LogWarning("Authorization failed: User {UserId} lacks {RequiredPermission} permission for operation {Operation} on {Resource}",
                    GetUserId(user), requiredPermission, operation, resource);
                
                return AuthorizationResult.Forbidden($"Insufficient permissions. Required: {requiredPermission}");
            }

            // Additional resource-specific checks
            var resourceCheck = await CheckResourceAccessAsync(user, resource, operation, cancellationToken);
            if (!resourceCheck.IsAuthorized)
            {
                return resourceCheck;
            }

            _logger.LogDebug("Authorization succeeded: User {UserId} authorized for operation {Operation} on {Resource}",
                GetUserId(user), operation, resource);

            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            return AuthorizationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authorization error for operation {Operation} on {Resource}", operation, resource);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            return AuthorizationResult.Error("Authorization check failed", ex);
        }
    }

    public async Task<AuthorizationResult> AuthorizeProjectAccessAsync(
        ClaimsPrincipal user,
        string projectName,
        ProjectPermission permission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        // Validate project name
        var projectValidation = Validation.ValidationHelper.ValidateProjectName(projectName);
        if (!projectValidation.IsValid)
        {
            return AuthorizationResult.Forbidden($"Invalid project name: {projectValidation.ErrorMessage}");
        }

        // Check if user has access to specific project
        var projectClaim = user.FindFirst($"project:{projectName.ToLowerInvariant()}");
        if (projectClaim == null)
        {
            // Check for wildcard project access
            var wildcardClaim = user.FindFirst("project:*");
            if (wildcardClaim == null)
            {
                _logger.LogWarning("User {UserId} denied access to project {ProjectName} - no project claim",
                    GetUserId(user), projectName);
                return AuthorizationResult.Forbidden($"Access denied to project '{projectName}'");
            }
        }

        // Check permission level
        var userRole = GetUserRole(user, projectName);
        if (!HasProjectPermission(userRole, permission))
        {
            _logger.LogWarning("User {UserId} denied {Permission} access to project {ProjectName} - insufficient role {Role}",
                GetUserId(user), permission, projectName, userRole);
            return AuthorizationResult.Forbidden($"Insufficient permissions for project '{projectName}'. Required: {permission}");
        }

        return AuthorizationResult.Success();
    }

    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal user,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var result = await AuthorizeAsync(user, "system", permission, cancellationToken);
        return result.IsAuthorized;
    }

    private static UserPermissionLevel GetUserPermissions(ClaimsPrincipal user)
    {
        // Check for admin role
        if (user.IsInRole("Administrator") || user.HasClaim("role", "admin"))
        {
            return UserPermissionLevel.Administrator;
        }

        // Check for contributor role
        if (user.IsInRole("Contributor") || user.HasClaim("role", "contributor"))
        {
            return UserPermissionLevel.Contributor;
        }

        // Default to reader
        return UserPermissionLevel.Reader;
    }

    private static UserPermissionLevel GetRequiredPermission(string operation)
    {
        return operation switch
        {
            _ when AdminOperations.Contains(operation) => UserPermissionLevel.Administrator,
            _ when WriteOperations.Contains(operation) => UserPermissionLevel.Contributor,
            _ when ReadOperations.Contains(operation) => UserPermissionLevel.Reader,
            _ => UserPermissionLevel.Administrator // Default to most restrictive
        };
    }

    private static bool HasRequiredPermission(UserPermissionLevel userLevel, UserPermissionLevel requiredLevel)
    {
        return userLevel >= requiredLevel;
    }

    private async Task<AuthorizationResult> CheckResourceAccessAsync(
        ClaimsPrincipal user,
        string resource,
        string operation,
        CancellationToken cancellationToken)
    {
        // Resource-specific authorization logic
        return resource.ToLowerInvariant() switch
        {
            var r when r.StartsWith("project:") => await CheckProjectResourceAsync(user, r, operation, cancellationToken),
            var r when r.StartsWith("repository:") => await CheckRepositoryResourceAsync(user, r, operation, cancellationToken),
            _ => AuthorizationResult.Success() // Allow by default for other resources
        };
    }

    private async Task<AuthorizationResult> CheckProjectResourceAsync(
        ClaimsPrincipal user,
        string resource,
        string operation,
        CancellationToken cancellationToken)
    {
        // Extract project name from resource (format: "project:projectName")
        var projectName = resource.Substring("project:".Length);
        var permission = GetProjectPermissionFromOperation(operation);
        
        return await AuthorizeProjectAccessAsync(user, projectName, permission, cancellationToken);
    }

    private async Task<AuthorizationResult> CheckRepositoryResourceAsync(
        ClaimsPrincipal user,
        string resource,
        string operation,
        CancellationToken cancellationToken)
    {
        // Repository access inherits from project access
        // Format: "repository:projectName/repoName"
        var parts = resource.Substring("repository:".Length).Split('/', 2);
        if (parts.Length < 2)
        {
            return AuthorizationResult.Forbidden("Invalid repository resource format");
        }

        var projectName = parts[0];
        var permission = GetProjectPermissionFromOperation(operation);
        
        return await AuthorizeProjectAccessAsync(user, projectName, permission, cancellationToken);
    }

    private static ProjectPermission GetProjectPermissionFromOperation(string operation)
    {
        return operation switch
        {
            _ when WriteOperations.Contains(operation) => ProjectPermission.Contribute,
            _ when AdminOperations.Contains(operation) => ProjectPermission.Administer,
            _ => ProjectPermission.Read
        };
    }

    private static ProjectRole GetUserRole(ClaimsPrincipal user, string projectName)
    {
        var roleClaim = user.FindFirst($"project:{projectName.ToLowerInvariant()}:role") 
                       ?? user.FindFirst("project:*:role")
                       ?? user.FindFirst("role");

        return roleClaim?.Value?.ToLowerInvariant() switch
        {
            "admin" or "administrator" => ProjectRole.Administrator,
            "contributor" or "developer" => ProjectRole.Contributor,
            "reader" or "stakeholder" => ProjectRole.Reader,
            _ => ProjectRole.Reader
        };
    }

    private static bool HasProjectPermission(ProjectRole role, ProjectPermission permission)
    {
        return (role, permission) switch
        {
            (ProjectRole.Administrator, _) => true,
            (ProjectRole.Contributor, ProjectPermission.Read or ProjectPermission.Contribute) => true,
            (ProjectRole.Reader, ProjectPermission.Read) => true,
            _ => false
        };
    }

    private static string? GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? user.FindFirst("sub")?.Value 
               ?? user.FindFirst(ClaimTypes.Name)?.Value;
    }
}

// .NET 9: Use enum for better performance and type safety
public enum UserPermissionLevel
{
    Reader = 1,
    Contributor = 2,
    Administrator = 3
}

public enum ProjectPermission
{
    Read,
    Contribute,
    Administer
}

public enum ProjectRole
{
    Reader = 1,
    Contributor = 2,
    Administrator = 3
}

public readonly record struct AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    private AuthorizationResult(bool isAuthorized, string? errorMessage = null, Exception? exception = null)
    {
        IsAuthorized = isAuthorized;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static AuthorizationResult Success() => new(true);
    
    public static AuthorizationResult Forbidden(string message) => new(false, message);
    
    public static AuthorizationResult Error(string message, Exception? exception = null) => 
        new(false, message, exception);

    public void ThrowIfNotAuthorized()
    {
        if (!IsAuthorized)
        {
            throw Exception ?? new UnauthorizedAccessException(ErrorMessage ?? "Access denied");
        }
    }
}