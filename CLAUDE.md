# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an Azure DevOps MCP (Model Context Protocol) server built with .NET 9 and ASP.NET Core. It provides read-only integration with Azure DevOps through the MCP protocol, enabling tools like VS Code to interact with Azure DevOps resources.

## Architecture

The application follows a layered architecture:

- **Controllers**: MCP controllers in `src/AzureDevOps.MCP/MCP/` that expose Azure DevOps functionality as MCP tools
  - `ProjectsController.cs` - Lists Azure DevOps projects
  - `RepositoriesController.cs` - Git repository operations (files, commits, branches, tags, pull requests)
  - `WorkItemsController.cs` - Work item queries and details
  - `PipelinesController.cs` - Build and release pipeline management
  - `TestPlansController.cs` - Test plan, suite, and run operations
  - `ArtifactsController.cs` - Package feed and artifact management
- **Services**: Business logic layer in `src/AzureDevOps.MCP/Services/`
  - `AzureDevOpsService.cs` - Core service that wraps Azure DevOps REST API clients
  - `IAzureDevOpsService.cs` - Service interface defining all Azure DevOps operations
- **Program.cs**: Application entry point with MCP server configuration using `AddMcpServer()` and `WithStdioServerTransport()`

The server uses Microsoft's Azure DevOps REST API client libraries and the ModelContextProtocol NuGet package for MCP functionality.

## Common Commands

### Development
```bash
# Quick setup for new developers
./scripts/dev-setup.ps1

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project src/AzureDevOps.MCP

# Build from solution root
dotnet build AzureDevOps.MCP.slnx
```

### Testing & Quality
```bash
# Run all tests
dotnet test

# Run tests with coverage
./scripts/test-coverage.ps1

# Run tests with coverage and open report
./scripts/test-coverage.ps1 -OpenReport

# Run performance benchmarks
dotnet test --filter "FullyQualifiedName~PerformanceBenchmarks"
```

### Docker
```bash
# Build Docker image
docker build -t azuredevops-mcp .

# Run with environment variables
docker run -p 3000:80 \
  -e AzureDevOps__OrganizationUrl="https://dev.azure.com/your-organization" \
  -e AzureDevOps__PersonalAccessToken="your-pat-goes-here" \
  azuredevops-mcp
```

## Configuration

The application requires Azure DevOps configuration through environment variables or `appsettings.json`:
- `AzureDevOps__OrganizationUrl` - Azure DevOps organization URL
- `AzureDevOps__PersonalAccessToken` - Personal Access Token with Read permissions for Code, Work Items, and Project/Team

## Available MCP Tools

The server exposes comprehensive Azure DevOps functionality through these MCP tool categories:

- **Projects**: List all accessible projects
- **Repositories**: Git operations including file browsing, commits, branches, tags, and pull requests  
- **Work Items**: Query and retrieve work item details
- **Pipelines**: Build definitions, builds, agent pools, release definitions, and releases
- **Test Plans**: Test plans, suites, and test runs
- **Artifacts**: Package feeds and packages

## Key Dependencies

- **.NET 9**: Target framework
- **ModelContextProtocol**: MCP server implementation
- **Microsoft.TeamFoundationServer.Client**: Core Azure DevOps API client
- **Microsoft.VisualStudio.Services.Client**: Visual Studio Services web API client
- **Microsoft.TeamFoundation.Build.WebApi**: Build and pipeline APIs
- **Microsoft.TeamFoundation.Test.WebApi**: Test management APIs
- **Microsoft.VisualStudio.Services.Feed.WebApi**: Artifact and package feed APIs
- **Microsoft.TeamFoundation.DistributedTask.WebApi**: Agent pool and task APIs