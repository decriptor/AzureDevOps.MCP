---
layout: page
title: API Reference
permalink: /api-reference/
---

# API Reference

Complete reference for all available MCP tools in the Azure DevOps MCP Server.

<div class="bg-gradient-to-br from-azure-blue/5 to-azure-dark/5 rounded-xl p-8 mb-12 border border-azure-blue/20">
  <h2 class="text-2xl font-bold text-gray-900 mb-6">📚 Quick Navigation</h2>
  <div class="grid md:grid-cols-2 gap-6">
    <div>
      <h3 class="font-semibold text-gray-900 mb-3">Core Operations</h3>
      <ul class="space-y-2 text-azure-blue">
        <li><a href="#list_projects" class="hover:text-azure-dark transition-colors">list_projects</a></li>
        <li><a href="#list_repositories" class="hover:text-azure-dark transition-colors">list_repositories</a></li>
        <li><a href="#get_file_content" class="hover:text-azure-dark transition-colors">get_file_content</a></li>
        <li><a href="#list_work_items" class="hover:text-azure-dark transition-colors">list_work_items</a></li>
      </ul>
    </div>
    <div>
      <h3 class="font-semibold text-gray-900 mb-3">Advanced Features</h3>
      <ul class="space-y-2 text-azure-blue">
        <li><a href="#safe-write-tools" class="hover:text-azure-dark transition-colors">Safe Write Operations</a></li>
        <li><a href="#batch-tools" class="hover:text-azure-dark transition-colors">Batch Operations</a></li>
        <li><a href="#performance-tools" class="hover:text-azure-dark transition-colors">Performance Monitoring</a></li>
        <li><a href="#error-handling" class="hover:text-azure-dark transition-colors">Error Handling</a></li>
      </ul>
    </div>
  </div>
</div>

## Tool Categories

<div class="grid md:grid-cols-2 lg:grid-cols-3 gap-4 my-8">
  <div class="bg-gradient-to-br from-green-50 to-green-100 border border-green-200 rounded-lg p-4">
    <h3 class="text-lg font-semibold text-green-800 mb-2">📖 Core Tools</h3>
    <p class="text-sm text-green-700 mb-3">Read-only operations that are always available</p>
    <a href="#core-azure-devops-tools" class="text-green-600 hover:text-green-800 font-medium text-sm">View Tools →</a>
  </div>

  <div class="bg-gradient-to-br from-amber-50 to-amber-100 border border-amber-200 rounded-lg p-4">
    <h3 class="text-lg font-semibold text-amber-800 mb-2">✏️ Safe Write Tools</h3>
    <p class="text-sm text-amber-700 mb-3">Opt-in write operations with safety measures</p>
    <a href="#safe-write-tools" class="text-amber-600 hover:text-amber-800 font-medium text-sm">View Tools →</a>
  </div>

  <div class="bg-gradient-to-br from-blue-50 to-blue-100 border border-blue-200 rounded-lg p-4">
    <h3 class="text-lg font-semibold text-blue-800 mb-2">⚡ Batch Tools</h3>
    <p class="text-sm text-blue-700 mb-3">High-performance bulk operations</p>
    <a href="#batch-tools" class="text-blue-600 hover:text-blue-800 font-medium text-sm">View Tools →</a>
  </div>

  <div class="bg-gradient-to-br from-purple-50 to-purple-100 border border-purple-200 rounded-lg p-4">
    <h3 class="text-lg font-semibold text-purple-800 mb-2">🧪 Test Plan Tools</h3>
    <p class="text-sm text-purple-700 mb-3">Test management operations</p>
    <a href="#test-plan-tools" class="text-purple-600 hover:text-purple-800 font-medium text-sm">View Tools →</a>
  </div>

  <div class="bg-gradient-to-br from-red-50 to-red-100 border border-red-200 rounded-lg p-4">
    <h3 class="text-lg font-semibold text-red-800 mb-2">📊 Performance Tools</h3>
    <p class="text-sm text-red-700 mb-3">System monitoring and management</p>
    <a href="#performance-tools" class="text-red-600 hover:text-red-800 font-medium text-sm">View Tools →</a>
  </div>
</div>

---

## Core Azure DevOps Tools

<div class="bg-gradient-to-r from-green-50 to-emerald-50 border-l-4 border-green-500 p-6 mb-8 rounded-r-lg shadow-sm">
  <div class="flex items-start">
    <div class="flex-shrink-0">
      <div class="bg-green-500 rounded-full p-2">
        <span class="text-xl text-white">📖</span>
      </div>
    </div>
    <div class="ml-4">
      <h3 class="text-lg font-semibold text-green-800 mb-2">Read-Only Operations</h3>
      <p class="text-green-700 leading-relaxed">These operations are always available and safe to use without any side effects. They provide comprehensive access to your Azure DevOps data with built-in caching for optimal performance.</p>
    </div>
  </div>
</div>

### `list_projects`

<div class="bg-white border border-gray-200 rounded-lg p-6 mb-6">
  <div class="flex items-center justify-between mb-4">
    <h4 class="text-lg font-semibold text-gray-900">List Projects</h4>
    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
      Read-Only
    </span>
  </div>

  <p class="text-gray-700 mb-4">Lists all accessible projects in the Azure DevOps organization.</p>

  <div class="grid md:grid-cols-2 gap-6">
    <div>
      <h5 class="font-medium text-gray-900 mb-2">Parameters</h5>
      <p class="text-sm text-gray-600">None</p>
    </div>

    <div>
      <h5 class="font-medium text-gray-900 mb-2">Returns</h5>
      <div class="text-sm text-gray-600 space-y-1">
        <div><code class="bg-gray-100 px-1 rounded">id</code> - Project unique identifier</div>
        <div><code class="bg-gray-100 px-1 rounded">name</code> - Project name</div>
        <div><code class="bg-gray-100 px-1 rounded">description</code> - Project description</div>
        <div><code class="bg-gray-100 px-1 rounded">state</code> - Project state (active, inactive, etc.)</div>
        <div><code class="bg-gray-100 px-1 rounded">url</code> - Project URL</div>
      </div>
    </div>
  </div>

  <div class="mt-4">
    <h5 class="font-medium text-gray-900 mb-2">Example</h5>
    <div class="bg-gray-50 border border-gray-200 rounded p-3">
      <pre class="text-sm"><code>{
  "tool": "list_projects"
}</code></pre>
    </div>
  </div>
</div>

### `list_repositories`

Lists all repositories in a specific project.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project

**Returns**: Array of repository objects with:
- `id` - Repository unique identifier
- `name` - Repository name
- `url` - Repository URL
- `defaultBranch` - Default branch name
- `size` - Repository size in bytes

**Example**:
```json
{
  "tool": "list_repositories",
  "parameters": {
    "projectName": "MyProject"
  }
}
```

### `list_repository_items`

Lists files and folders in a repository path.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `repositoryId` (required) - Repository ID or name
- `path` (optional) - Path within repository (default: "/")
- `branch` (optional) - Branch name (default: default branch)

**Returns**: Array of item objects with:
- `objectId` - Git object ID
- `gitObjectType` - Type (blob, tree, commit)
- `path` - Full path to the item
- `isFolder` - Boolean indicating if item is a folder
- `size` - File size in bytes (for files)

**Example**:
```json
{
  "tool": "list_repository_items",
  "parameters": {
    "projectName": "MyProject",
    "repositoryId": "my-repo",
    "path": "/src",
    "branch": "main"
  }
}
```

### `get_file_content`

Gets the content of a specific file from a repository.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `repositoryId` (required) - Repository ID or name
- `path` (required) - Path to the file
- `branch` (optional) - Branch name (default: default branch)

**Returns**: File content object with:
- `content` - File contents (text or base64 for binary)
- `encoding` - Content encoding
- `path` - File path
- `size` - File size in bytes

**Example**:
```json
{
  "tool": "get_file_content",
  "parameters": {
    "projectName": "MyProject",
    "repositoryId": "my-repo",
    "path": "/README.md",
    "branch": "main"
  }
}
```

### `list_work_items`

Lists work items in a specific project with optional filtering.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `workItemType` (optional) - Filter by work item type (Bug, Task, etc.)
- `assignedTo` (optional) - Filter by assignee
- `state` (optional) - Filter by state (Active, Closed, etc.)
- `top` (optional) - Maximum number of items to return (default: 50)

**Returns**: Array of work item objects with:
- `id` - Work item ID
- `title` - Work item title
- `workItemType` - Type (Bug, Task, User Story, etc.)
- `state` - Current state
- `assignedTo` - Assigned user
- `createdDate` - Creation date
- `tags` - Array of tags

**Example**:
```json
{
  "tool": "list_work_items",
  "parameters": {
    "projectName": "MyProject",
    "workItemType": "Bug",
    "state": "Active",
    "top": 20
  }
}
```

### `get_work_item`

Gets detailed information about a specific work item.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `workItemId` (required) - Work item ID

**Returns**: Detailed work item object with:
- `id` - Work item ID
- `title` - Work item title
- `description` - Full description/details
- `workItemType` - Type
- `state` - Current state
- `assignedTo` - Assigned user
- `priority` - Priority level
- `severity` - Severity (for bugs)
- `tags` - Array of tags
- `relations` - Related work items
- `comments` - Work item comments
- `history` - Change history

**Example**:
```json
{
  "tool": "get_work_item",
  "parameters": {
    "projectName": "MyProject",
    "workItemId": 1234
  }
}
```

---

<div class="my-16 flex items-center">
  <div class="flex-grow border-t border-gray-300"></div>
  <div class="mx-4 text-gray-500 font-medium">WRITE OPERATIONS</div>
  <div class="flex-grow border-t border-gray-300"></div>
</div>

## Safe Write Tools

**Write operations that require explicit opt-in configuration**

### `add_pull_request_comment`

Adds a comment to a pull request.

**Requirements**:
- `PullRequestComments` must be enabled in configuration
- PAT token must have Code: Write permission

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `repositoryId` (required) - Repository ID or name
- `pullRequestId` (required) - Pull request ID
- `comment` (required) - Comment text
- `confirm` (required) - Must be `true` to confirm the operation

**Returns**: Comment object with:
- `id` - Comment ID
- `content` - Comment content
- `author` - Comment author
- `publishedDate` - Publication date

**Example**:
```json
{
  "tool": "add_pull_request_comment",
  "parameters": {
    "projectName": "MyProject",
    "repositoryId": "my-repo",
    "pullRequestId": 123,
    "comment": "LGTM! Great work on this feature.",
    "confirm": true
  }
}
```

### `create_draft_pull_request`

Creates a draft pull request (not published until ready).

**Requirements**:
- `CreateDraftPullRequest` must be enabled in configuration
- PAT token must have Code: Write permission

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `repositoryId` (required) - Repository ID or name
- `sourceBranch` (required) - Source branch name
- `targetBranch` (required) - Target branch name
- `title` (required) - Pull request title
- `description` (optional) - Pull request description
- `confirm` (required) - Must be `true` to confirm the operation

**Returns**: Pull request object with:
- `pullRequestId` - PR ID
- `title` - PR title
- `status` - PR status (draft)
- `url` - PR URL

**Example**:
```json
{
  "tool": "create_draft_pull_request",
  "parameters": {
    "projectName": "MyProject",
    "repositoryId": "my-repo",
    "sourceBranch": "feature/new-component",
    "targetBranch": "main",
    "title": "Add new authentication component",
    "description": "This PR adds JWT authentication support",
    "confirm": true
  }
}
```

### `update_work_item_tags`

Adds or removes tags from work items.

**Requirements**:
- `UpdateWorkItemTags` must be enabled in configuration
- PAT token must have Work Items: Write permission

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `workItemId` (required) - Work item ID
- `tagsToAdd` (optional) - Array of tags to add
- `tagsToRemove` (optional) - Array of tags to remove
- `confirm` (required) - Must be `true` to confirm the operation

**Returns**: Updated work item with new tag list

**Example**:
```json
{
  "tool": "update_work_item_tags",
  "parameters": {
    "projectName": "MyProject",
    "workItemId": 1234,
    "tagsToAdd": ["security", "urgent"],
    "tagsToRemove": ["low-priority"],
    "confirm": true
  }
}
```

---

<div class="my-16 flex items-center">
  <div class="flex-grow border-t border-gray-300"></div>
  <div class="mx-4 text-gray-500 font-medium">BULK OPERATIONS</div>
  <div class="flex-grow border-t border-gray-300"></div>
</div>

## Batch Tools

**High-performance operations for bulk data retrieval**

### `batch_get_work_items`

Retrieves multiple work items efficiently in parallel.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `workItemIds` (required) - Array of work item IDs (max 200)

**Returns**: Array of work item objects with same structure as `get_work_item`

**Example**:
```json
{
  "tool": "batch_get_work_items",
  "parameters": {
    "projectName": "MyProject",
    "workItemIds": [1234, 1235, 1236, 1237]
  }
}
```

### `batch_get_repositories`

Gets multiple repository details in parallel.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `repositoryIds` (required) - Array of repository IDs or names (max 50)

**Returns**: Array of detailed repository objects

**Example**:
```json
{
  "tool": "batch_get_repositories",
  "parameters": {
    "projectName": "MyProject",
    "repositoryIds": ["repo1", "repo2", "repo3"]
  }
}
```

---

## Test Plan Tools

**Test management operations for comprehensive test oversight**

### `get_test_plans`

Gets test plans for a specific Azure DevOps project.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project

**Returns**: Array of test plan objects with:
- `id` - Test plan ID
- `name` - Test plan name
- `state` - Plan state (Active, Inactive)
- `startDate` - Plan start date
- `endDate` - Plan end date

**Example**:
```json
{
  "tool": "get_test_plans",
  "parameters": {
    "projectName": "MyProject"
  }
}
```

### `get_test_plan`

Gets detailed information about a specific test plan.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `planId` (required) - Test plan ID

**Returns**: Detailed test plan object with suites and configuration

**Example**:
```json
{
  "tool": "get_test_plan",
  "parameters": {
    "projectName": "MyProject",
    "planId": 123
  }
}
```

### `get_test_suites`

Gets test suites for a specific test plan.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `planId` (required) - Test plan ID

**Returns**: Array of test suite objects with test cases

**Example**:
```json
{
  "tool": "get_test_suites",
  "parameters": {
    "projectName": "MyProject",
    "planId": 123
  }
}
```

### `get_test_runs`

Gets test runs for a specific Azure DevOps project.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `planId` (optional) - Filter by test plan ID

**Returns**: Array of test run objects with execution details

**Example**:
```json
{
  "tool": "get_test_runs",
  "parameters": {
    "projectName": "MyProject",
    "planId": 123
  }
}
```

### `get_test_run`

Gets detailed information about a specific test run.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `runId` (required) - Test run ID

**Returns**: Detailed test run object with all test results

**Example**:
```json
{
  "tool": "get_test_run",
  "parameters": {
    "projectName": "MyProject",
    "runId": 456
  }
}
```

### `get_test_results`

Gets test results for a specific test run.

**Parameters**:
- `projectName` (required) - Name of the Azure DevOps project
- `runId` (required) - Test run ID

**Returns**: Array of test result objects with pass/fail status

**Example**:
```json
{
  "tool": "get_test_results",
  "parameters": {
    "projectName": "MyProject",
    "runId": 456
  }
}
```

---

## Performance Tools

**System management and monitoring capabilities**

### `get_performance_metrics`

View operation timings, API call statistics, and cache performance.

**Parameters**: None

**Returns**: Performance metrics object with:
- `operationTimings` - Average response times by operation
- `apiCallCounts` - Number of API calls by endpoint
- `cacheHitRates` - Cache performance statistics
- `memoryUsage` - Current memory consumption
- `systemMetrics` - Overall system performance

**Example**:
```json
{
  "tool": "get_performance_metrics"
}
```

### `get_cache_statistics`

Detailed cache performance and hit rate statistics.

**Parameters**: None

**Returns**: Cache statistics object with:
- `hitRate` - Overall cache hit percentage
- `missRate` - Cache miss percentage
- `evictionCount` - Number of cache evictions
- `totalRequests` - Total cache requests
- `memoryUsage` - Cache memory consumption

**Example**:
```json
{
  "tool": "get_cache_statistics"
}
```

### `clear_cache`

Clear all cached data to force fresh API calls.

**Parameters**:
- `confirm` (required) - Must be `true` to confirm the operation

**Returns**: Confirmation message

**Example**:
```json
{
  "tool": "clear_cache",
  "parameters": {
    "confirm": true
  }
}
```

---

## Error Handling

All tools return standardized error responses:

```json
{
  "error": {
    "code": "AUTHENTICATION_FAILED",
    "message": "Invalid personal access token",
    "details": {
      "operation": "list_projects",
      "timestamp": "2024-01-15T10:30:00Z"
    }
  }
}
```

### Common Error Codes

- `AUTHENTICATION_FAILED` - Invalid or expired PAT token
- `AUTHORIZATION_DENIED` - Insufficient permissions
- `PROJECT_NOT_FOUND` - Project does not exist or no access
- `REPOSITORY_NOT_FOUND` - Repository does not exist
- `WORK_ITEM_NOT_FOUND` - Work item does not exist
- `OPERATION_NOT_ENABLED` - Write operation not enabled in configuration
- `CONFIRMATION_REQUIRED` - Write operation requires confirm=true
- `RATE_LIMIT_EXCEEDED` - Too many API calls
- `VALIDATION_ERROR` - Invalid parameters

## Rate Limiting

The server implements intelligent rate limiting:

- **Default**: 60 requests per minute, 1000 per hour
- **Batch Operations**: Count as single request regardless of item count
- **Cached Results**: Don't count toward rate limits
- **Circuit Breaker**: Automatic fallback when Azure DevOps is overloaded

## Caching Strategy

Intelligent caching improves performance:

- **Project/Repository Metadata**: 30 minutes
- **Work Item Details**: 15 minutes
- **File Contents**: 60 minutes
- **Test Results**: 5 minutes
- **Performance Metrics**: Real-time (no cache)

Cache can be cleared using the `clear_cache` tool when fresh data is needed.