# Azure DevOps MCP Server

A Model Context Protocol (MCP) server that provides comprehensive integration with Azure DevOps. This server enables AI assistants like Claude Desktop and development tools like VS Code to seamlessly interact with Azure DevOps through the MCP protocol using stdio transport.

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details on how to get started.

- 🐛 [Report bugs](https://github.com/decriptor/AzureDevOps.MCP/issues/new?template=bug_report.md)
- 💡 [Request features](https://github.com/decriptor/AzureDevOps.MCP/issues/new?template=feature_request.md)
- 📖 [Improve documentation](https://github.com/decriptor/AzureDevOps.MCP/issues)

## Features

- **Projects**: Browse Azure DevOps projects and team information
- **Repositories**: View repositories, files, commits, branches, and pull requests
- **Work Items**: Query and manage work items with optional write operations
- **Performance Monitoring**: Built-in performance tracking and optimization
- **Production Ready**: Comprehensive caching, error handling, and security features
- **Safe Write Operations**: Opt-in write capabilities with audit logging

## Available MCP Tools

The server exposes Azure DevOps functionality through specialized tool categories:

### Core Azure DevOps Tools (`AzureDevOpsTools`)
**Read-Only Operations (Always Available):**
- `list_projects` - Lists all projects in the Azure DevOps organization
- `list_repositories` - Lists repositories in a specific project
- `list_repository_items` - Lists files and folders in a repository path
- `get_file_content` - Gets the content of a specific file from a repository
- `list_work_items` - Lists work items in a specific project
- `get_work_item` - Gets detailed information about a specific work item

### Safe Write Tools (`SafeWriteTools`)
**Write Operations (Require Explicit Opt-In):**
- `add_pull_request_comment` - Adds a comment to a pull request (requires `PullRequestComments`)
- `create_draft_pull_request` - Creates a draft pull request (requires `CreateDraftPullRequest`)
- `update_work_item_tags` - Adds or removes tags from work items (requires `UpdateWorkItemTags`)

### Batch Operations (`BatchTools`)
**High-Performance Bulk Operations:**
- `batch_get_work_items` - Retrieves multiple work items efficiently in parallel
- `batch_get_repositories` - Gets multiple repository details in parallel

### Test Plan Management (`TestPlanTools`)
**Test Management Operations:**
- `get_test_plans` - Gets test plans for a specific Azure DevOps project
- `get_test_plan` - Gets detailed information about a specific test plan
- `get_test_suites` - Gets test suites for a specific test plan
- `get_test_runs` - Gets test runs for a specific Azure DevOps project
- `get_test_run` - Gets detailed information about a specific test run
- `get_test_results` - Gets test results for a specific test run

### Performance & Monitoring (`PerformanceTools`)
**System Management:**
- `get_performance_metrics` - View operation timings, API call statistics, and cache performance
- `get_cache_statistics` - Detailed cache performance and hit rate statistics
- `clear_cache` - Clear all cached data to force fresh API calls

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for local development)
- [Docker](https://www.docker.com/products/docker-desktop/) (for running the container)
- Azure DevOps account with appropriate permissions

## 🚀 Quick Development Setup

### Option 1: Development Containers (Recommended)
Get started in seconds with a fully configured development environment:

**VS Code:**
1. Install [VS Code](https://code.visualstudio.com/) and the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
2. Clone this repository and open in VS Code
3. Click "Reopen in Container" when prompted
4. Start coding immediately with .NET 9, tools, and extensions pre-configured!

*Having issues? See [.devcontainer/README.md](.devcontainer/README.md) for troubleshooting*

**GitHub Codespaces:**
1. Click the "Code" button on this repository
2. Select "Codespaces" → "Create codespace on main"
3. Develop directly in your browser with zero setup

### Option 2: Local Development
For traditional local development:
```bash
# Manual setup (any platform)
dotnet restore && dotnet build && dotnet test
```

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
   - **Test Management**: Read (for test plan operations)
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

The server supports comprehensive configuration through environment variables or `appsettings.json`:

### Required Configuration

| Setting | Description | Required |
|---------|-------------|----------|
| `AzureDevOps__OrganizationUrl` | Your Azure DevOps organization URL (e.g., `https://dev.azure.com/myorg`) | Yes |
| `AzureDevOps__PersonalAccessToken` | Your Azure DevOps Personal Access Token | Yes |

### Optional Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `AzureDevOps__EnabledWriteOperations` | Array of enabled write operations | `[]` |
| `AzureDevOps__RequireConfirmation` | Require explicit confirmation for write operations | `true` |
| `AzureDevOps__EnableAuditLogging` | Enable audit logging for all operations | `true` |

### Advanced Configuration

The server supports extensive production configuration options:

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
    "PersonalAccessToken": "your-pat-goes-here",
    "EnabledWriteOperations": ["PullRequestComments"],
    "RequireConfirmation": true,
    "EnableAuditLogging": true,
    "Monitoring": {
      "EnablePerformanceTracking": true,
      "EnableErrorTracking": true,
      "Sentry": {
        "Dsn": "your-sentry-dsn"
      }
    }
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
    "EnableKeyVault": false,
    "KeyVaultUrl": "https://your-keyvault.vault.azure.net/"
  },
  "RateLimiting": {
    "EnableRateLimiting": true,
    "RequestsPerMinute": 60,
    "RequestsPerHour": 1000
  }
}
```

### Available Write Operations

Enable specific write operations by adding them to `EnabledWriteOperations`:

- `PullRequestComments` - Add comments to pull requests
- `WorkItemComments` - Add comments to work items
- `CreateDraftPullRequest` - Create draft pull requests (not published until ready)
- `UpdateWorkItemTags` - Add or remove tags from work items

### Write Operation Safety Features

1. **Opt-In by Default**: All write operations are disabled unless explicitly enabled
2. **Confirmation Required**: Write operations require a `confirm: true` parameter (can be disabled)
3. **Audit Logging**: All write operations are logged with timestamp, operation type, and result
4. **Preview Mode**: Operations show what will be changed before confirmation

## Architecture

This project implements a modern, production-ready architecture with .NET 9:

### Core Architecture Layers

- **MCP Tools Layer** (`Tools/`): Five specialized tool categories implementing MCP protocol
  - `AzureDevOpsTools` - Core read operations
  - `SafeWriteTools` - Opt-in write operations with safety measures
  - `BatchTools` - High-performance bulk operations
  - `TestPlanTools` - Test plan and test management operations
  - `PerformanceTools` - System monitoring and management

- **Services Layer** (`Services/`):
  - **Core Services** (`Core/`): Domain-specific business logic (Projects, Repositories, WorkItems, Builds, Tests, Search)
  - **Infrastructure Services** (`Infrastructure/`): Cross-cutting concerns (Caching, Performance, Health, Connection Management)
  - **Legacy Service**: `AzureDevOpsService` for backward compatibility

- **Configuration Layer** (`Configuration/`): Modular configuration classes
  - `ProductionConfiguration` - Complete production settings
  - `CachingConfiguration` - Cache management
  - `PerformanceConfiguration` - Performance tuning
  - `SecurityConfiguration` - Security settings
  - `RateLimitingConfiguration` - API rate limiting

- **Security & Infrastructure**:
  - **Authorization** (`Authorization/`): Permission-based access control
  - **Security** (`Security/`): Secret management with Azure Key Vault support
  - **Error Handling** (`ErrorHandling/`): Resilient execution patterns

### Production Features

- **Performance Optimization**: Built-in caching, connection pooling, and performance monitoring
- **Reliability**: Circuit breaker patterns, retry policies, and health checks
- **Security**: Comprehensive secret management, authorization, and audit logging
- **Monitoring**: Sentry integration, structured logging, and performance metrics
- **Scalability**: Rate limiting, memory management, and efficient batch operations

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

### Technology Stack

- **.NET 9**: Latest framework with modern C# features
- **ModelContextProtocol**: MCP SDK for protocol implementation
- **Azure DevOps Client Libraries**: Official Microsoft libraries for Azure DevOps integration
- **BenchmarkDotNet**: Performance benchmarking and optimization
- **Centralized Package Management**: All package versions managed in Directory.Packages.props

### Testing

The project includes comprehensive testing infrastructure:

```bash
# Run all tests (153 passing, 7 skipped, 160 total)
dotnet test

# Run tests with coverage
./scripts/test-coverage.ps1

# Run performance benchmarks
cd tests/AzureDevOps.MCP.Tests
dotnet run --configuration Release -- cache           # Cache performance
dotnet run --configuration Release -- performance     # Service performance
dotnet run --configuration Release -- config          # Configuration benchmarks
dotnet run --configuration Release -- frozen          # .NET 9 collections performance
```

### Development Scripts

- `./scripts/dev-setup.ps1` - Quick setup for new developers
- `./scripts/test-coverage.ps1` - Run tests with coverage analysis
- `./scripts/test-coverage.ps1 -OpenReport` - Generate and open coverage report

## Example Workflow: Test Plan → Clone → Tests → PR

The Azure DevOps MCP server enables powerful workflows for test-driven development:

### Complete End-to-End Workflow

1. **Get Test Plan Requirements**
   ```bash
   # Use MCP client to get test plan details
   get_test_plans projectName="MyProject"
   get_test_plan projectName="MyProject" planId=123
   get_test_suites projectName="MyProject" planId=123
   ```

2. **Get Repository Information**
   ```bash
   # Get repository details and clone URL
   list_repositories projectName="MyProject"
   list_repository_items projectName="MyProject" repositoryId="repo-guid" path="/"
   ```

3. **Local Development** (You handle these steps)
   ```bash
   # Clone repository using information from MCP server
   git clone https://dev.azure.com/org/project/_git/repository
   cd repository

   # Create feature branch
   git checkout -b feature/implement-test-plan-123

   # Generate unit tests and UI tests based on test plan requirements
   # (Use test plan details to understand what needs to be tested)

   # Commit your changes
   git add .
   git commit -m "Implement tests for test plan #123"
   git push origin feature/implement-test-plan-123
   ```

4. **Create Pull Request**
   ```bash
   # Use MCP server to create draft PR
   create_draft_pull_request projectName="MyProject" repositoryId="repo-guid" \
     sourceBranch="feature/implement-test-plan-123" \
     targetBranch="main" \
     title="Implement tests for test plan #123" \
     description="Added unit tests and UI tests based on test plan requirements" \
     confirm=true
   ```

This workflow demonstrates how the MCP server bridges the gap between Azure DevOps test management and local development, enabling seamless test-driven development processes.

## Security Notes

- The PAT token should be kept secure and have minimal required permissions
- By default, the server only performs read operations on Azure DevOps
- Write operations must be explicitly enabled and are limited to safe operations
- All write operations are audited and logged
- Use environment variables for sensitive configuration
- Audit logs are stored locally in the `audit/` directory
