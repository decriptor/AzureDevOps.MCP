# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an Azure DevOps MCP (Model Context Protocol) server built with .NET 9. It provides comprehensive integration with Azure DevOps through the MCP protocol, enabling tools like Claude Desktop and VS Code to interact with Azure DevOps resources with production-grade reliability and performance.

## Architecture

The application follows a modern clean architecture with production-ready features:

### MCP Tools Layer (`src/AzureDevOps.MCP/Tools/`)
- **`AzureDevOpsTools.cs`** - Core read operations (projects, repositories, files, work items)
- **`SafeWriteTools.cs`** - Opt-in write operations with safety measures and audit logging
- **`BatchTools.cs`** - High-performance bulk operations for efficient data retrieval
- **`PerformanceTools.cs`** - System monitoring, metrics collection, and cache management

### Services Layer (`src/AzureDevOps.MCP/Services/`)
- **Core Services** (`Services/Core/`): Domain-specific business logic
  - `ProjectService.cs` - Project and team management
  - `RepositoryService.cs` - Git repository operations
  - `WorkItemService.cs` - Work item queries and management
  - `BuildService.cs` - Build and pipeline operations
  - `TestService.cs` - Test management and results
  - `SearchService.cs` - Code search capabilities
- **Infrastructure Services** (`Services/Infrastructure/`): Cross-cutting concerns
  - Connection management, caching, performance monitoring, health checks
  - Rate limiting, circuit breakers, and resilient execution patterns
- **Legacy Service**: `AzureDevOpsService.cs` - Backward compatibility wrapper

### Configuration & Infrastructure
- **Configuration** (`Configuration/`): Modular configuration classes for production deployment
- **Security** (`Security/`): Secret management with Azure Key Vault integration
- **Authorization** (`Authorization/`): Permission-based access control
- **Error Handling** (`ErrorHandling/`): Resilient error handling patterns

### Application Entry Point
- **Program.cs**: Application startup with dependency injection and MCP server configuration

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
# Run all tests (149 test methods across multiple categories)
dotnet test

# Run tests with coverage analysis
./scripts/test-coverage.ps1

# Run tests with coverage and open detailed report
./scripts/test-coverage.ps1 -OpenReport

# Run performance benchmarks
cd tests/AzureDevOps.MCP.Tests
dotnet run --configuration Release -- cache           # Cache performance benchmarks
dotnet run --configuration Release -- performance     # Service performance benchmarks
dotnet run --configuration Release -- config          # Configuration benchmarks
dotnet run --configuration Release -- frozen          # .NET 9 collections benchmarks
dotnet run --configuration Release -- secrets         # Secret management benchmarks
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

### Required Configuration
The application requires Azure DevOps configuration through environment variables or `appsettings.json`:
- `AzureDevOps__OrganizationUrl` - Azure DevOps organization URL
- `AzureDevOps__PersonalAccessToken` - Personal Access Token with appropriate permissions

### Production Configuration
The application supports extensive production configuration options:
- **Caching**: Memory cache settings, cache duration configuration
- **Performance**: Operation thresholds, circuit breaker settings, monitoring options
- **Security**: Azure Key Vault integration, secret management, authorization policies
- **Rate Limiting**: API rate limiting configuration for Azure DevOps calls
- **Monitoring**: Sentry integration, structured logging, performance metrics

### Write Operations Configuration
Safe write operations can be enabled by configuring:
- `AzureDevOps__EnabledWriteOperations` - Array of enabled write operation types
- `AzureDevOps__RequireConfirmation` - Require explicit confirmation for write operations
- `AzureDevOps__EnableAuditLogging` - Enable comprehensive audit logging

## Available MCP Tools

The server exposes Azure DevOps functionality through four specialized tool categories:

### Core Azure DevOps Tools (`AzureDevOpsTools`)
- **Projects**: List all accessible projects and project details
- **Repositories**: Git operations including file browsing, content retrieval
- **Work Items**: Query and retrieve work item details

### Safe Write Tools (`SafeWriteTools`)  
- **Pull Request Operations**: Add comments to pull requests, create draft PRs
- **Work Item Management**: Update work item tags and metadata

### Batch Operations (`BatchTools`)
- **High-Performance Bulk Operations**: Efficiently retrieve multiple work items or repositories in parallel

### Performance & Monitoring (`PerformanceTools`)
- **System Metrics**: Performance monitoring, cache statistics, system management

## Key Dependencies

- **.NET 9**: Target framework
- **ModelContextProtocol**: MCP server implementation
- **Microsoft.TeamFoundationServer.Client**: Core Azure DevOps API client
- **Microsoft.VisualStudio.Services.Client**: Visual Studio Services web API client
- **Microsoft.TeamFoundation.Build.WebApi**: Build and pipeline APIs
- **Microsoft.TeamFoundation.Test.WebApi**: Test management APIs
- **Microsoft.VisualStudio.Services.Feed.WebApi**: Artifact and package feed APIs
- **Microsoft.TeamFoundation.DistributedTask.WebApi**: Agent pool and task APIs