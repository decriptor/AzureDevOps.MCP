---
layout: page
title: Examples & Tutorials
permalink: /examples/
---

# Examples & Tutorials

Learn how to use the Azure DevOps MCP Server with practical examples and common workflows.

## Basic Usage Examples

<div class="grid lg:grid-cols-2 gap-8 my-8">
  <div class="bg-gradient-to-br from-blue-50 to-blue-100 border border-blue-200 rounded-lg p-6">
    <div class="flex items-center mb-4">
      <span class="text-2xl mr-3">🔍</span>
      <h3 class="text-xl font-bold text-blue-900">Exploring Your Organization</h3>
    </div>
    <p class="text-blue-700 mb-4">Start by getting familiar with your Azure DevOps organization and discover available projects and repositories.</p>
    <div class="bg-blue-900 text-blue-100 px-3 py-1 rounded text-sm font-medium inline-block">Foundation</div>
  </div>

  <div class="bg-gradient-to-br from-green-50 to-green-100 border border-green-200 rounded-lg p-6">
    <div class="flex items-center mb-4">
      <span class="text-2xl mr-3">📋</span>
      <h3 class="text-xl font-bold text-green-900">Working with Work Items</h3>
    </div>
    <p class="text-green-700 mb-4">Query and analyze work items, bugs, and user stories across your projects with powerful filtering capabilities.</p>
    <div class="bg-green-900 text-green-100 px-3 py-1 rounded text-sm font-medium inline-block">Core Workflow</div>
  </div>
</div>

### Exploring Your Organization

<div class="bg-white border border-gray-200 rounded-lg p-6 mb-8">
  <div class="flex items-center mb-4">
    <span class="text-xl mr-2">🔍</span>
    <h4 class="text-lg font-semibold text-gray-900">Discovery Workflow</h4>
  </div>

  <p class="text-gray-700 mb-4">Start by getting familiar with your Azure DevOps organization:</p>

  <div class="space-y-4">
    <div class="border-l-4 border-blue-500 bg-blue-50 p-4">
      <h5 class="font-medium text-blue-900 mb-2">Step 1: List All Projects</h5>
      <div class="bg-gray-900 text-gray-100 p-3 rounded text-sm font-mono">
        <code>list_projects</code>
      </div>
    </div>

    <div class="border-l-4 border-blue-500 bg-blue-50 p-4">
      <h5 class="font-medium text-blue-900 mb-2">Step 2: Explore Project Repositories</h5>
      <div class="bg-gray-900 text-gray-100 p-3 rounded text-sm font-mono">
        <code>list_repositories projectName="MyProject"</code>
      </div>
    </div>

    <div class="border-l-4 border-blue-500 bg-blue-50 p-4">
      <h5 class="font-medium text-blue-900 mb-2">Step 3: Browse Repository Contents</h5>
      <div class="bg-gray-900 text-gray-100 p-3 rounded text-sm font-mono">
        <code>list_repository_items projectName="MyProject" repositoryId="my-repo" path="/"</code>
      </div>
    </div>
  </div>
</div>

### Working with Work Items

<div class="bg-white border border-gray-200 rounded-lg p-6 mb-8">
  <div class="flex items-center mb-4">
    <span class="text-xl mr-2">📋</span>
    <h4 class="text-lg font-semibold text-gray-900">Work Item Management</h4>
  </div>

  <p class="text-gray-700 mb-4">Query and analyze work items across your projects:</p>

  <div class="grid md:grid-cols-2 gap-6">
    <div class="space-y-4">
      <div class="border-l-4 border-green-500 bg-green-50 p-4">
        <h5 class="font-medium text-green-900 mb-2">Query Active Bugs</h5>
        <div class="bg-gray-900 text-gray-100 p-3 rounded text-sm font-mono">
          <code>list_work_items projectName="MyProject" workItemType="Bug" state="Active"</code>
        </div>
      </div>

      <div class="border-l-4 border-green-500 bg-green-50 p-4">
        <h5 class="font-medium text-green-900 mb-2">Get Work Item Details</h5>
        <div class="bg-gray-900 text-gray-100 p-3 rounded text-sm font-mono">
          <code>get_work_item projectName="MyProject" workItemId=1234</code>
        </div>
      </div>
    </div>

    <div>
      <div class="border-l-4 border-purple-500 bg-purple-50 p-4">
        <h5 class="font-medium text-purple-900 mb-2">Batch Operations</h5>
        <div class="bg-gray-900 text-gray-100 p-3 rounded text-sm font-mono">
          <code>batch_get_work_items projectName="MyProject" workItemIds=[1234, 1235, 1236]</code>
        </div>
        <p class="text-sm text-purple-700 mt-2">💡 Use batch operations for better performance when retrieving multiple items.</p>
      </div>
    </div>
  </div>
</div>
```

### Code Repository Analysis

Analyze your codebase and review processes:

```
# Get file contents
get_file_content projectName="MyProject" repositoryId="my-repo" path="/README.md"

# Browse source code structure
list_repository_items projectName="MyProject" repositoryId="my-repo" path="/src"

# Check multiple repositories at once
batch_get_repositories projectName="MyProject" repositoryIds=["repo1", "repo2", "repo3"]
```

---

## Advanced Workflows

<div class="text-center mb-12">
  <p class="text-xl text-gray-600 max-w-3xl mx-auto">Discover advanced patterns and workflows for complex Azure DevOps operations, from comprehensive testing to security audits.</p>
</div>

### Test Plan Analysis Workflow

<div class="bg-white border border-gray-200 rounded-lg p-6 mb-8">
  <div class="flex items-center mb-4">
    <span class="text-xl mr-2">🧪</span>
    <h4 class="text-lg font-semibold text-gray-900">Comprehensive Test Management</h4>
  </div>

  <p class="text-gray-700 mb-6">Systematic approach to test plan analysis and quality assurance:</p>

```
# 1. Get all test plans for a project
get_test_plans projectName="MyProject"

# 2. Dive into a specific test plan
get_test_plan projectName="MyProject" planId=123

# 3. Analyze test suites and cases
get_test_suites projectName="MyProject" planId=123

# 4. Review recent test runs
get_test_runs projectName="MyProject" planId=123

# 5. Get detailed results for a test run
get_test_run projectName="MyProject" runId=456
get_test_results projectName="MyProject" runId=456
```

### Code Review and PR Management

Streamline your pull request workflow:

```
# 1. Review repository structure
list_repositories projectName="MyProject"

# 2. Check specific files before creating PR
get_file_content projectName="MyProject" repositoryId="my-repo" path="/src/main.cs"

# 3. Create a draft pull request (requires write permissions)
create_draft_pull_request
  projectName="MyProject"
  repositoryId="my-repo"
  sourceBranch="feature/new-feature"
  targetBranch="main"
  title="Add new authentication feature"
  description="This PR implements JWT-based authentication"
  confirm=true

# 4. Add review comments (requires write permissions)
add_pull_request_comment
  projectName="MyProject"
  repositoryId="my-repo"
  pullRequestId=123
  comment="Great implementation! Just one suggestion on error handling."
  confirm=true
```
</div>

---

### Work Item Management

<div class="bg-white border border-gray-200 rounded-lg p-6 mb-8">
  <div class="flex items-center mb-4">
    <span class="text-xl mr-2">📋</span>
    <h4 class="text-lg font-semibold text-gray-900">Comprehensive Work Item Operations</h4>
  </div>

  <p class="text-gray-700 mb-6">Organize and track work items effectively with advanced querying and management capabilities:</p>

```
# 1. Query work items by different criteria
list_work_items projectName="MyProject" workItemType="User Story" state="Active"
list_work_items projectName="MyProject" assignedTo="john.doe@company.com"

# 2. Get detailed work item information
get_work_item projectName="MyProject" workItemId=1234

# 3. Update work item tags (requires write permissions)
update_work_item_tags
  projectName="MyProject"
  workItemId=1234
  tagsToAdd=["priority-1", "security"]
  tagsToRemove=["needs-triage"]
  confirm=true

# 4. Batch process multiple work items
batch_get_work_items projectName="MyProject" workItemIds=[1234, 1235, 1236, 1237]
```

## Common Use Cases

### Project Health Dashboard

Create a comprehensive project overview:

```bash
# Get project overview
list_projects

# For each project, get:
# - Repository count and details
list_repositories projectName="ProjectA"

# - Active work item count by type
list_work_items projectName="ProjectA" workItemType="Bug" state="Active"
list_work_items projectName="ProjectA" workItemType="Task" state="Active"
list_work_items projectName="ProjectA" workItemType="User Story" state="Active"

# - Test coverage and results
get_test_plans projectName="ProjectA"
get_test_runs projectName="ProjectA"

# - Performance metrics
get_performance_metrics
get_cache_statistics
```

### Release Planning

Plan and track releases across multiple repositories:

```bash
# 1. Identify all repositories in the project
list_repositories projectName="MyProject"

# 2. Check current state of each repository
batch_get_repositories projectName="MyProject" repositoryIds=["api", "frontend", "mobile"]

# 3. Review release-related work items
list_work_items projectName="MyProject" workItemType="User Story" state="Resolved"
list_work_items projectName="MyProject" workItemType="Bug" state="Closed"

# 4. Analyze test readiness
get_test_plans projectName="MyProject"
get_test_runs projectName="MyProject"

# 5. Review recent changes in key files
get_file_content projectName="MyProject" repositoryId="api" path="/CHANGELOG.md"
get_file_content projectName="MyProject" repositoryId="frontend" path="/package.json"
```

### Security and Compliance Audit

Audit your projects for security and compliance:

```bash
# 1. Get all projects and repositories
list_projects
list_repositories projectName="MyProject"

# 2. Check security-related files
get_file_content projectName="MyProject" repositoryId="my-repo" path="/SECURITY.md"
get_file_content projectName="MyProject" repositoryId="my-repo" path="/.github/dependabot.yml"

# 3. Review security-tagged work items
list_work_items projectName="MyProject" workItemType="Bug"
# (then filter results by security tags)

# 4. Check test coverage for security features
get_test_plans projectName="MyProject"
get_test_suites projectName="MyProject" planId=123

# 5. Add security tags to relevant work items
update_work_item_tags
  projectName="MyProject"
  workItemId=1234
  tagsToAdd=["security-review", "compliance"]
  confirm=true
```

## Performance Optimization Tips

### Batch Operations

Use batch operations for better performance:

```bash
# Instead of multiple individual calls:
# get_work_item projectName="MyProject" workItemId=1234
# get_work_item projectName="MyProject" workItemId=1235
# get_work_item projectName="MyProject" workItemId=1236

# Use batch operation:
batch_get_work_items projectName="MyProject" workItemIds=[1234, 1235, 1236]

# Same for repositories:
batch_get_repositories projectName="MyProject" repositoryIds=["repo1", "repo2", "repo3"]
```

### Cache Management

Monitor and manage caching for optimal performance:

```bash
# Check cache performance
get_cache_statistics

# Get performance metrics
get_performance_metrics

# Clear cache when you need fresh data
clear_cache confirm=true
```

### Monitoring System Performance

Keep track of system performance:

```bash
# Regular performance monitoring
get_performance_metrics

# Check for:
# - Slow operations (>1000ms threshold)
# - High API call counts
# - Low cache hit rates
# - Memory usage patterns

# Monitor cache effectiveness
get_cache_statistics

# Look for:
# - Hit rate >80% (good)
# - Low eviction counts
# - Reasonable memory usage
```

## Integration Patterns

### Claude Desktop Workflows

Common patterns when using with Claude Desktop:

**Project Analysis**:
> "Can you analyze the current state of MyProject? Show me the active work items, recent test runs, and any security-related issues."

**Code Review Assistance**:
> "Help me review the pull requests in MyProject. Check the recent changes and suggest areas that need attention."

**Release Planning**:
> "What's the status of the next release for MyProject? Show me completed features, outstanding bugs, and test coverage."

### VS Code Integration

Use with VS Code MCP extensions for enhanced development workflow:

1. **Contextual Work Item Creation**: Create work items based on TODO comments
2. **Automated PR Comments**: Add comments based on code analysis
3. **Test Plan Updates**: Update test plans when adding new features
4. **Performance Monitoring**: Track API usage and performance in development

## Error Handling Examples

Handle common errors gracefully:

```bash
# Authentication issues
# Error: AUTHENTICATION_FAILED - Check your PAT token

# Permission issues
# Error: AUTHORIZATION_DENIED - Verify PAT permissions

# Write operation not enabled
# Error: OPERATION_NOT_ENABLED - Enable in configuration

# Rate limiting
# Error: RATE_LIMIT_EXCEEDED - Wait and retry, or use batch operations

# Missing confirmation
# Error: CONFIRMATION_REQUIRED - Add confirm=true parameter
```

## Configuration Examples

### Basic Read-Only Setup

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/myorg",
    "PersonalAccessToken": "your-pat-here"
  }
}
```

### Write Operations Enabled

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/myorg",
    "PersonalAccessToken": "your-pat-here",
    "EnabledWriteOperations": [
      "PullRequestComments",
      "CreateDraftPullRequest",
      "UpdateWorkItemTags"
    ],
    "RequireConfirmation": true,
    "EnableAuditLogging": true
  }
}
```

### Production Configuration

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/myorg",
    "EnabledWriteOperations": ["PullRequestComments"],
    "RequireConfirmation": true,
    "EnableAuditLogging": true
  },
  "Caching": {
    "EnableMemoryCache": true,
    "MaxMemoryCacheSizeMB": 100,
    "DefaultCacheDurationMinutes": 15
  },
  "Performance": {
    "SlowOperationThresholdMs": 1000,
    "EnableCircuitBreaker": true,
    "EnableMonitoring": true
  },
  "Security": {
    "EnableKeyVault": true,
    "KeyVaultUrl": "https://your-keyvault.vault.azure.net/"
  },
  "RateLimiting": {
    "EnableRateLimiting": true,
    "RequestsPerMinute": 60,
    "RequestsPerHour": 1000
  }
}
```

## Best Practices

### Security
- 🔒 Use minimal required PAT permissions
- 🔐 Enable audit logging for write operations
- 🛡️ Use Azure Key Vault in production
- ⚠️ Always require confirmation for write operations

### Performance
- ⚡ Use batch operations when processing multiple items
- 💾 Monitor cache hit rates and performance metrics
- 🎯 Set appropriate cache durations for your use case
- 📊 Use performance monitoring to identify bottlenecks

### Operations
- 📝 Enable audit logging for compliance
- 🔄 Implement proper error handling in your workflows
- 📈 Monitor rate limits and adjust batch sizes accordingly
- 🛠️ Use draft PRs for safer code review processes

## Need Help?

- 📚 [API Reference](api-reference.html) - Complete tool documentation
- 🚀 [Getting Started](getting-started.html) - Setup and configuration
- 🐛 [Report Issues](https://github.com/decriptor/AzureDevOps.MCP/issues) - Bug reports
- 💬 [Discussions](https://github.com/decriptor/AzureDevOps.MCP/discussions) - Community support