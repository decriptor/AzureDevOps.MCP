# Azure DevOps MCP Server

This project implements a Model Context Protocol (MCP) server that provides read-only integration with Azure DevOps. It allows tools like Visual Studio Code to interact with Azure DevOps through the MCP protocol using stdio transport.

## Features

- **Projects**: Browse Azure DevOps projects
- **Repositories**: View repositories and their contents
- **Files**: Read file contents from repositories
- **Work Items**: Query and view work items
- **Read-only access**: Safe integration with Azure DevOps resources

## Available Tools

The MCP server exposes the following tools:

### Read-Only Tools (Always Available)
- `list_projects` - Lists all projects in the Azure DevOps organization
- `list_repositories` - Lists all repositories in a specific project
- `list_repository_items` - Lists files and folders in a repository path
- `get_file_content` - Gets the content of a specific file from a repository
- `list_work_items` - Lists work items in a specific project
- `get_work_item` - Gets detailed information about a specific work item
- `get_audit_logs` - Retrieves audit logs for write operations

### Safe Write Tools (Require Explicit Opt-In)
- `add_pull_request_comment` - Adds a comment to a pull request (requires `PullRequestComments` in `EnabledWriteOperations`)
- `create_draft_pull_request` - Creates a draft pull request (requires `CreateDraftPullRequest` in `EnabledWriteOperations`)
- `update_work_item_tags` - Adds or removes tags from work items (requires `UpdateWorkItemTags` in `EnabledWriteOperations`)

### Performance & Batch Tools
- `batch_get_work_items` - Retrieves multiple work items efficiently in parallel
- `batch_get_file_contents` - Gets contents of multiple files in parallel
- `batch_list_repository_items` - Lists items from multiple repository paths in parallel
- `get_performance_metrics` - View operation timings, API call statistics, and cache performance
- `clear_cache` - Clear all cached data to force fresh API calls

### Extended Read Tools
- `search_code` - Search for code across repositories with project and repository filters
- `get_wikis` - List all wikis in a project
- `get_wiki_page` - Read wiki page content
- `get_builds` - List builds with optional filtering by build definition
- `get_test_runs` - List test runs in a project
- `get_test_results` - Get test results for a specific test run
- `download_build_artifact` - Download build artifacts as streams

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for development)
- [Docker](https://www.docker.com/products/docker-desktop/) (for running the container)
- Azure DevOps account with appropriate permissions

## Getting Started

### Generating an Azure DevOps Personal Access Token (PAT)

1. Sign in to your Azure DevOps organization (<https://dev.azure.com/{your-organization}>)
2. Click on your profile icon in the top right corner
3. Select **Personal access tokens** from the dropdown menu
4. Click **+ New Token**
5. Name your token (e.g., "MCP Server Access")
6. Select the appropriate organization where you want to use the token
7. For read-only access, select the following scopes:
   - **Code**: Read
   - **Work Items**: Read
   - **Project and Team**: Read
8. Set an expiration date for your token
9. Click **Create**
10. **Copy and securely store your token** (you won't be able to see it again!)

### Running the MCP Server with Docker

```bash
# Build the Docker image
docker build -t azuredevops-mcp .

# Run the container with environment variables
docker run -it \
  -e AzureDevOps__OrganizationUrl="https://dev.azure.com/your-organization" \
  -e AzureDevOps__PersonalAccessToken="your-pat-goes-here" \
  azuredevops-mcp
```

### Running from Source

```bash
# Set environment variables
$env:AzureDevOps__OrganizationUrl="https://dev.azure.com/your-organization"
$env:AzureDevOps__PersonalAccessToken="your-pat-goes-here"

# Build and run
dotnet restore
dotnet build
dotnet run --project src/AzureDevOps.MCP
```

### Connecting to the MCP Server

The server uses stdio transport, which means it communicates via standard input/output. This is the standard way MCP servers are integrated with tools like Claude Desktop, VS Code extensions, or other MCP clients.

For Claude Desktop, add this to your MCP configuration:

```json
{
  "mcpServers": {
    "azure-devops": {
      "command": "docker",
      "args": [
        "run", "-i",
        "-e", "AzureDevOps__OrganizationUrl=https://dev.azure.com/your-organization",
        "-e", "AzureDevOps__PersonalAccessToken=your-pat-goes-here",
        "azuredevops-mcp"
      ]
    }
  }
}
```

## Configuration

The server can be configured through environment variables or by editing the appsettings.json file:

| Setting | Description | Required | Default |
|---------|-------------|----------|---------|
| `AzureDevOps__OrganizationUrl` | Your Azure DevOps organization URL (e.g., <https://dev.azure.com/myorg>) | Yes | - |
| `AzureDevOps__PersonalAccessToken` | Your Azure DevOps Personal Access Token | Yes | - |
| `AzureDevOps__EnabledWriteOperations` | Array of enabled write operations (see Safe Write Operations below) | No | `[]` |
| `AzureDevOps__RequireConfirmation` | Require explicit confirmation for write operations | No | `true` |
| `AzureDevOps__EnableAuditLogging` | Enable audit logging for all write operations | No | `true` |

### Safe Write Operations

The server supports opt-in write operations for low-risk actions. To enable specific write operations, add them to the `EnabledWriteOperations` array:

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
    "PersonalAccessToken": "your-pat-goes-here",
    "EnabledWriteOperations": ["PullRequestComments"],
    "RequireConfirmation": true,
    "EnableAuditLogging": true
  }
}
```

Available safe write operations:
- `PullRequestComments` - Add comments to pull requests
- `CreateDraftPullRequest` - Create draft pull requests (not published until ready)
- `UpdateWorkItemTags` - Add or remove tags from work items
- Coming soon: `WorkItemComments`

### Write Operation Safety Features

1. **Opt-In by Default**: All write operations are disabled unless explicitly enabled
2. **Confirmation Required**: Write operations require a `confirm: true` parameter (can be disabled)
3. **Audit Logging**: All write operations are logged with timestamp, operation type, and result
4. **Preview Mode**: Operations show what will be changed before confirmation

## Architecture

This project follows .NET best practices:

- **Centralized Package Management**: Uses Directory.Packages.props for version management
- **Dependency Injection**: Proper service registration and injection
- **Logging**: Structured logging throughout the application
- **Error Handling**: Comprehensive error handling with meaningful messages
- **Clean Architecture**: Separation of concerns with Services and Tools layers

## Building from Source

```bash
# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project src/AzureDevOps.MCP
```

## Development

The project uses:

- **.NET 9**: Target framework
- **ModelContextProtocol 0.2.0-preview.3**: Latest MCP SDK
- **Azure DevOps Client Libraries**: Official Microsoft libraries for Azure DevOps integration
- **Centralized Package Management**: All package versions managed in Directory.Packages.props

## Security Notes

- The PAT token should be kept secure and have minimal required permissions
- By default, the server only performs read operations on Azure DevOps
- Write operations must be explicitly enabled and are limited to safe operations
- All write operations are audited and logged
- Use environment variables for sensitive configuration
- Audit logs are stored locally in the `audit/` directory
