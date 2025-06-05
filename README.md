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

- `list_projects` - Lists all projects in the Azure DevOps organization
- `list_repositories` - Lists all repositories in a specific project
- `list_repository_items` - Lists files and folders in a repository path
- `get_file_content` - Gets the content of a specific file from a repository
- `list_work_items` - Lists work items in a specific project
- `get_work_item` - Gets detailed information about a specific work item

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for development)
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

| Setting | Description | Required |
|---------|-------------|----------|
| `AzureDevOps__OrganizationUrl` | Your Azure DevOps organization URL (e.g., <https://dev.azure.com/myorg>) | Yes |
| `AzureDevOps__PersonalAccessToken` | Your Azure DevOps Personal Access Token | Yes |

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

- **.NET 8**: Target framework
- **ModelContextProtocol 0.2.0-preview.3**: Latest MCP SDK
- **Azure DevOps Client Libraries**: Official Microsoft libraries for Azure DevOps integration
- **Centralized Package Management**: All package versions managed in Directory.Packages.props

## Security Notes

- The PAT token should be kept secure and have minimal required permissions
- The server only performs read operations on Azure DevOps
- All API calls are logged for debugging purposes
- Use environment variables for sensitive configuration
