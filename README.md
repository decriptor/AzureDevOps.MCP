# Azure DevOps MCP Server

This project implements a Model Context Protocol (MCP) server that provides read-only integration with Azure DevOps. It allows tools like Visual Studio Code to interact with Azure DevOps through the MCP protocol.

## Features

- Browse Azure DevOps projects
- View repositories and their contents
- Query work items
- Read-only access to Azure DevOps resources

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for development)
- [Docker](https://www.docker.com/products/docker-desktop/) (for running the container)
- Azure DevOps account with appropriate permissions

## Getting Started

### Generating an Azure DevOps Personal Access Token (PAT)

1. Sign in to your Azure DevOps organization (https://dev.azure.com/{your-organization})
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
docker build -t azuredevops-mcp .

docker run -p 3000:80 \
  -e AzureDevOps__OrganizationUrl="https://dev.azure.com/your-organization" \
  -e AzureDevOps__PersonalAccessToken="your-pat-goes-here" \
  azuredevops-mcp
```

### Connecting to the MCP Server from VS Code

1. Install the MCP Client extension for VS Code
2. Open VS Code settings (File > Preferences > Settings)
3. Search for "MCP"
4. Add a new server entry with:
   - Name: Azure DevOps
   - URL: http://localhost:3000

## Configuration

The server can be configured through environment variables or by editing the appsettings.json file:

| Setting | Description | Default |
|---------|-------------|---------|
| AzureDevOps__OrganizationUrl | Your Azure DevOps organization URL | empty |
| AzureDevOps__PersonalAccessToken | Your Azure DevOps PAT | empty |

## Building from Source

```bash
dotnet restore
dotnet build
dotnet run
```
