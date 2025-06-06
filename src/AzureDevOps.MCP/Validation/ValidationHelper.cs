using System.Text.RegularExpressions;

namespace AzureDevOps.MCP.Validation;

public static class ValidationHelper
{
    private static readonly Regex ProjectNameRegex = new(@"^[a-zA-Z0-9]([a-zA-Z0-9\-\._\s])*[a-zA-Z0-9]$", 
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    
    private static readonly Regex BranchNameRegex = new(@"^[^/:?*\[\]\\]+$", 
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    
    private static readonly Regex FilePathRegex = new(@"^[^<>:""|?*]+$", 
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] ForbiddenPathSequences = { "..", "//", "\\\\", "<", ">", "|", "?", "*", ":" };
    
    public const int MaxProjectNameLength = 64;
    public const int MaxFilePathLength = 260;
    public const int MaxBranchNameLength = 250;
    public const int MaxWorkItemIdValue = int.MaxValue;
    public const int MinWorkItemIdValue = 1;

    public static ValidationResult ValidateProjectName(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return ValidationResult.Invalid("Project name cannot be null or empty");

        if (projectName.Length > MaxProjectNameLength)
            return ValidationResult.Invalid($"Project name cannot exceed {MaxProjectNameLength} characters");

        if (projectName.Length < 2)
            return ValidationResult.Invalid("Project name must be at least 2 characters long");

        if (!ProjectNameRegex.IsMatch(projectName))
            return ValidationResult.Invalid("Project name contains invalid characters. Only alphanumeric, hyphens, underscores, periods, and spaces are allowed");

        if (projectName.StartsWith(" ") || projectName.EndsWith(" "))
            return ValidationResult.Invalid("Project name cannot start or end with spaces");

        return ValidationResult.Valid();
    }

    public static ValidationResult ValidateRepositoryId(string? repositoryId)
    {
        if (string.IsNullOrWhiteSpace(repositoryId))
            return ValidationResult.Invalid("Repository ID cannot be null or empty");

        // Repository ID can be either a GUID or a repository name
        if (Guid.TryParse(repositoryId, out _))
            return ValidationResult.Valid();

        // Validate as repository name (similar to project name rules)
        return ValidateProjectName(repositoryId);
    }

    public static ValidationResult ValidateBranchName(string? branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            return ValidationResult.Invalid("Branch name cannot be null or empty");

        if (branchName.Length > MaxBranchNameLength)
            return ValidationResult.Invalid($"Branch name cannot exceed {MaxBranchNameLength} characters");

        // Remove refs/heads/ prefix if present
        var cleanBranchName = branchName.StartsWith("refs/heads/") 
            ? branchName.Substring("refs/heads/".Length) 
            : branchName;

        if (!BranchNameRegex.IsMatch(cleanBranchName))
            return ValidationResult.Invalid("Branch name contains invalid characters");

        if (cleanBranchName.Contains(".."))
            return ValidationResult.Invalid("Branch name cannot contain consecutive dots");

        return ValidationResult.Valid();
    }

    public static ValidationResult ValidateFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return ValidationResult.Invalid("File path cannot be null or empty");

        if (filePath.Length > MaxFilePathLength)
            return ValidationResult.Invalid($"File path cannot exceed {MaxFilePathLength} characters");

        // Check for forbidden sequences
        foreach (var forbiddenSequence in ForbiddenPathSequences)
        {
            if (filePath.Contains(forbiddenSequence))
                return ValidationResult.Invalid($"File path contains forbidden sequence: {forbiddenSequence}");
        }

        // Check for path traversal attempts
        var normalizedPath = Path.GetFullPath(filePath);
        if (!normalizedPath.StartsWith(filePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Invalid("File path contains path traversal sequences");

        if (!FilePathRegex.IsMatch(filePath))
            return ValidationResult.Invalid("File path contains invalid characters");

        return ValidationResult.Valid();
    }

    public static ValidationResult ValidateWorkItemId(int workItemId)
    {
        if (workItemId < MinWorkItemIdValue || workItemId > MaxWorkItemIdValue)
            return ValidationResult.Invalid($"Work item ID must be between {MinWorkItemIdValue} and {MaxWorkItemIdValue}");

        return ValidationResult.Valid();
    }

    public static ValidationResult ValidateLimit(int limit, int maxLimit = 1000)
    {
        if (limit < 1)
            return ValidationResult.Invalid("Limit must be at least 1");

        if (limit > maxLimit)
            return ValidationResult.Invalid($"Limit cannot exceed {maxLimit}");

        return ValidationResult.Valid();
    }

    public static ValidationResult ValidateWiql(string? wiql)
    {
        if (string.IsNullOrWhiteSpace(wiql))
            return ValidationResult.Invalid("WIQL query cannot be null or empty");

        // Basic WIQL validation - prevent obvious injection attempts
        var dangerousKeywords = new[] { "DROP", "DELETE", "INSERT", "UPDATE", "EXEC", "EXECUTE", "SP_", "XP_" };
        var upperWiql = wiql.ToUpperInvariant();

        foreach (var keyword in dangerousKeywords)
        {
            if (upperWiql.Contains(keyword))
                return ValidationResult.Invalid($"WIQL query contains potentially dangerous keyword: {keyword}");
        }

        // Ensure it starts with SELECT
        if (!upperWiql.TrimStart().StartsWith("SELECT"))
            return ValidationResult.Invalid("WIQL query must start with SELECT");

        return ValidationResult.Valid();
    }

    public static ValidationResult ValidateOrganizationUrl(string? organizationUrl)
    {
        if (string.IsNullOrWhiteSpace(organizationUrl))
            return ValidationResult.Invalid("Organization URL cannot be null or empty");

        if (!Uri.TryCreate(organizationUrl, UriKind.Absolute, out var uri))
            return ValidationResult.Invalid("Organization URL must be a valid absolute URL");

        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Invalid("Organization URL must use HTTPS");

        if (!uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Invalid("Organization URL must be a valid Azure DevOps URL");

        return ValidationResult.Valid();
    }

    public static ValidationResult ValidatePersonalAccessToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ValidationResult.Invalid("Personal Access Token cannot be null or empty");

        if (token.Length < 10)
            return ValidationResult.Invalid("Personal Access Token appears to be too short");

        if (token.Length > 2048)
            return ValidationResult.Invalid("Personal Access Token appears to be too long");

        // Basic format check - Azure DevOps PATs are typically base64-ish
        if (!Regex.IsMatch(token, @"^[a-zA-Z0-9+/=]+$"))
            return ValidationResult.Invalid("Personal Access Token contains invalid characters");

        return ValidationResult.Valid();
    }
}

public class ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    private ValidationResult() { }

    public static ValidationResult Valid() => new() { IsValid = true };

    public static ValidationResult Invalid(string errorMessage) => new() 
    { 
        IsValid = false, 
        ErrorMessage = errorMessage ?? "Validation failed"
    };

    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new ValidationException(ErrorMessage ?? "Validation failed");
    }
}

public class ValidationException : ArgumentException
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    public ValidationException(string message, string paramName) : base(message, paramName) { }
}