using System.Text.RegularExpressions;

namespace AzureDevOps.MCP.Common;

public static class ValidationHelper
{
	static readonly Regex ProjectNameRegex = new (@"^[a-zA-Z0-9]([a-zA-Z0-9\-\._\s])*[a-zA-Z0-9]$", RegexOptions.Compiled);
	static readonly Regex BranchNameRegex = new (@"^[^/:?*\[\]\\]+$", RegexOptions.Compiled);
	static readonly Regex GuidRegex = new (@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);

	public static ValidationResult ValidateProjectName (string? projectName)
	{
		if (string.IsNullOrWhiteSpace (projectName)) {
			return ValidationResult.Invalid ("Project name cannot be null or empty");
		}

		if (projectName.Length > Constants.Validation.MaxProjectNameLength) {
			return ValidationResult.Invalid ($"Project name cannot exceed {Constants.Validation.MaxProjectNameLength} characters");
		}

		if (!ProjectNameRegex.IsMatch (projectName)) {
			return ValidationResult.Invalid ("Project name contains invalid characters");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidateRepositoryId (string? repositoryId)
	{
		if (string.IsNullOrWhiteSpace (repositoryId)) {
			return ValidationResult.Invalid ("Repository ID cannot be null or empty");
		}

		if (repositoryId.Length > Constants.Validation.MaxRepositoryIdLength) {
			return ValidationResult.Invalid ($"Repository ID cannot exceed {Constants.Validation.MaxRepositoryIdLength} characters");
		}

		// Allow both GUID format and repository names
		if (!GuidRegex.IsMatch (repositoryId) && !ProjectNameRegex.IsMatch (repositoryId)) {
			return ValidationResult.Invalid ("Repository ID format is invalid");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidateFilePath (string? filePath)
	{
		if (string.IsNullOrWhiteSpace (filePath)) {
			return ValidationResult.Invalid ("File path cannot be null or empty");
		}

		if (filePath.Length > Constants.Validation.MaxFilePathLength) {
			return ValidationResult.Invalid ($"File path cannot exceed {Constants.Validation.MaxFilePathLength} characters");
		}

		foreach (var forbiddenChar in Constants.Validation.ForbiddenPathChars) {
			if (filePath.Contains (forbiddenChar)) {
				return ValidationResult.Invalid ($"File path contains forbidden sequence: {forbiddenChar}");
			}
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidateBranchName (string? branchName)
	{
		if (string.IsNullOrWhiteSpace (branchName)) {
			return ValidationResult.Invalid ("Branch name cannot be null or empty");
		}

		if (branchName.Length > Constants.Validation.MaxBranchNameLength) {
			return ValidationResult.Invalid ($"Branch name cannot exceed {Constants.Validation.MaxBranchNameLength} characters");
		}

		if (!BranchNameRegex.IsMatch (branchName)) {
			return ValidationResult.Invalid ("Branch name contains invalid characters");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidateWorkItemId (int workItemId)
	{
		if (workItemId <= 0) {
			return ValidationResult.Invalid ("Work item ID must be positive");
		}

		if (workItemId > int.MaxValue - 1000) // Reserve some space
{
			return ValidationResult.Invalid ("Work item ID is too large");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidateLimit (int limit, int maxLimit)
	{
		if (limit <= 0) {
			return ValidationResult.Invalid ("Limit must be positive");
		}

		if (limit > maxLimit) {
			return ValidationResult.Invalid ($"Limit cannot exceed {maxLimit}");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidateCommentContent (string? content)
	{
		if (string.IsNullOrWhiteSpace (content)) {
			return ValidationResult.Invalid ("Comment content cannot be null or empty");
		}

		if (content.Length > Constants.Validation.MaxCommentLength) {
			return ValidationResult.Invalid ($"Comment content cannot exceed {Constants.Validation.MaxCommentLength} characters");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidatePullRequestTitle (string? title)
	{
		if (string.IsNullOrWhiteSpace (title)) {
			return ValidationResult.Invalid ("Pull request title cannot be null or empty");
		}

		if (title.Length > Constants.Validation.MaxPullRequestTitleLength) {
			return ValidationResult.Invalid ($"Pull request title cannot exceed {Constants.Validation.MaxPullRequestTitleLength} characters");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidatePullRequestDescription (string? description)
	{
		if (string.IsNullOrWhiteSpace (description)) {
			return ValidationResult.Invalid ("Pull request description cannot be null or empty");
		}

		if (description.Length > Constants.Validation.MaxPullRequestDescriptionLength) {
			return ValidationResult.Invalid ($"Pull request description cannot exceed {Constants.Validation.MaxPullRequestDescriptionLength} characters");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidateSearchQuery (string? query)
	{
		if (string.IsNullOrWhiteSpace (query)) {
			return ValidationResult.Invalid ("Search query cannot be null or empty");
		}

		if (query.Length > Constants.Validation.MaxSearchQueryLength) {
			return ValidationResult.Invalid ($"Search query cannot exceed {Constants.Validation.MaxSearchQueryLength} characters");
		}

		return ValidationResult.Valid ();
	}

	public static ValidationResult ValidateTags (string[] tags)
	{
		if (tags == null) {
			return ValidationResult.Invalid ("Tags array cannot be null");
		}

		foreach (var tag in tags) {
			if (string.IsNullOrWhiteSpace (tag)) {
				return ValidationResult.Invalid ("Tag cannot be null or empty");
			}

			if (tag.Length > Constants.Validation.MaxTagLength) {
				return ValidationResult.Invalid ($"Tag '{tag}' exceeds maximum length of {Constants.Validation.MaxTagLength} characters");
			}

			if (tag.Contains (';') || tag.Contains (',')) {
				return ValidationResult.Invalid ($"Tag '{tag}' contains invalid characters");
			}
		}

		return ValidationResult.Valid ();
	}

	public static void ThrowIfInvalid (ValidationResult result)
	{
		if (!result.IsValid) {
			throw new ArgumentException (result.ErrorMessage);
		}
	}
}

public class ValidationResult
{
	public bool IsValid { get; private set; }
	public string? ErrorMessage { get; private set; }

	ValidationResult (bool isValid, string? errorMessage = null)
	{
		IsValid = isValid;
		ErrorMessage = errorMessage;
	}

	public static ValidationResult Valid () => new (true);
	public static ValidationResult Invalid (string errorMessage) => new (false, errorMessage);
}