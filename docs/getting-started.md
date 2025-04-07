---
layout: page
title: Getting Started
permalink: /getting-started/
---

# Getting Started with Azure DevOps MCP Server

This guide will help you set up and configure the Azure DevOps MCP Server for your development workflow.

## Prerequisites

<div class="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-xl p-8 mb-12 border border-blue-200">
  <h3 class="text-2xl font-bold text-blue-900 mb-6 flex items-center">
    <span class="text-3xl mr-3">✅</span>
    Before You Begin
  </h3>
  
  <div class="grid md:grid-cols-3 gap-6">
    <div class="bg-white rounded-lg p-6 shadow-sm border border-blue-100">
      <div class="text-2xl mb-3">🏢</div>
      <h4 class="font-semibold text-blue-900 mb-2">Azure DevOps Account</h4>
      <p class="text-blue-700 text-sm">Access to an Azure DevOps organization with appropriate project permissions.</p>
    </div>
    
    <div class="bg-white rounded-lg p-6 shadow-sm border border-blue-100">
      <div class="text-2xl mb-3">🔑</div>
      <h4 class="font-semibold text-blue-900 mb-2">Personal Access Token</h4>
      <p class="text-blue-700 text-sm">We'll guide you through creating a secure token with the right permissions.</p>
    </div>
    
    <div class="bg-white rounded-lg p-6 shadow-sm border border-blue-100">
      <div class="text-2xl mb-3">🛠️</div>
      <h4 class="font-semibold text-blue-900 mb-2">Runtime Environment</h4>
      <div class="space-y-2 text-sm">
        <div class="flex items-center text-blue-700">
          <span class="w-2 h-2 bg-blue-500 rounded-full mr-2"></span>
          <a href="https://www.docker.com/products/docker-desktop/" class="hover:text-blue-900 font-medium">Docker</a> <span class="text-green-600 font-medium ml-1">(Recommended)</span>
        </div>
        <div class="flex items-center text-blue-700">
          <span class="w-2 h-2 bg-blue-500 rounded-full mr-2"></span>
          <a href="https://dotnet.microsoft.com/download/dotnet/9.0" class="hover:text-blue-900 font-medium">.NET 9 SDK</a> <span class="text-gray-500 text-xs ml-1">(For local dev)</span>
        </div>
      </div>
    </div>
  </div>
</div>

## Step 1: Generate Azure DevOps Personal Access Token {#generating-pat}

<div class="bg-blue-50 border-l-4 border-azure-blue p-6 mb-6">
  <div class="flex items-start">
    <div class="flex-shrink-0">
      <span class="text-2xl">🔑</span>
    </div>
    <div class="ml-3">
      <p class="text-sm text-blue-700 font-medium">Security Best Practice</p>
      <p class="text-sm text-blue-600 mt-1">Store your Personal Access Token securely and never commit it to version control. Consider using environment variables or secure vaults.</p>
    </div>
  </div>
</div>

1. **Sign in** to your Azure DevOps organization at `https://dev.azure.com/{your-organization}`

2. **Access Token Settings**:
   - Click your profile icon (top right)
   - Select **Personal access tokens**

3. **Create New Token**:
   - Click **+ New Token**
   - Name: `MCP Server Access` (or your preferred name)
   - Organization: Select your target organization
   - Expiration: Set according to your security policy

4. **Configure Permissions**:

   <div class="grid md:grid-cols-2 gap-6 my-6">
     <div class="bg-green-50 border border-green-200 rounded-lg p-4">
       <h4 class="text-lg font-semibold text-green-800 mb-3">Read-Only Access (Recommended)</h4>
       <ul class="space-y-1 text-sm text-green-700">
         <li>✅ Code: Read</li>
         <li>✅ Work Items: Read</li>
         <li>✅ Project and Team: Read</li>
         <li>✅ Test Management: Read</li>
       </ul>
     </div>

     <div class="bg-amber-50 border border-amber-200 rounded-lg p-4">
       <h4 class="text-lg font-semibold text-amber-800 mb-3">Write Operations (Optional)</h4>
       <ul class="space-y-1 text-sm text-amber-700">
         <li>✅ Code: Read & Write (for PR comments)</li>
         <li>✅ Work Items: Read & Write (for tag updates)</li>
         <li>✅ Pull Request: Read & Write (for creating drafts)</li>
       </ul>
     </div>
   </div>

5. **Save Token**: Copy and securely store your token (you won't see it again!)

## Step 2: Choose Your Setup Method

<div class="grid lg:grid-cols-3 gap-6 my-8">
  <div class="bg-gradient-to-br from-blue-50 to-blue-100 border border-blue-200 rounded-lg p-6">
    <div class="flex items-center mb-4">
      <span class="text-3xl mr-3">🐳</span>
      <h3 class="text-xl font-bold text-blue-900">Docker</h3>
    </div>
    <p class="text-blue-700 mb-4">Perfect for production use and easy setup.</p>
    <div class="bg-blue-900 text-blue-100 px-3 py-1 rounded text-sm font-medium inline-block">Recommended</div>
  </div>

  <div class="bg-gradient-to-br from-purple-50 to-purple-100 border border-purple-200 rounded-lg p-6">
    <div class="flex items-center mb-4">
      <span class="text-3xl mr-3">🛠️</span>
      <h3 class="text-xl font-bold text-purple-900">Dev Containers</h3>
    </div>
    <p class="text-purple-700 mb-4">For development and contributing with VS Code.</p>
    <div class="bg-purple-900 text-purple-100 px-3 py-1 rounded text-sm font-medium inline-block">Development</div>
  </div>

  <div class="bg-gradient-to-br from-green-50 to-green-100 border border-green-200 rounded-lg p-6">
    <div class="flex items-center mb-4">
      <span class="text-3xl mr-3">⚡</span>
      <h3 class="text-xl font-bold text-green-900">Local .NET</h3>
    </div>
    <p class="text-green-700 mb-4">Direct .NET development and debugging.</p>
    <div class="bg-green-900 text-green-100 px-3 py-1 rounded text-sm font-medium inline-block">Advanced</div>
  </div>
</div>

### Option A: Docker (Recommended)

Perfect for production use and easy setup:

```bash
# Pull the latest image
docker pull ghcr.io/decriptor/azuredevops-mcp:latest

# Run with your configuration
docker run -it \
  -e AzureDevOps__OrganizationUrl="https://dev.azure.com/your-organization" \
  -e AzureDevOps__PersonalAccessToken="your-pat-goes-here" \
  ghcr.io/decriptor/azuredevops-mcp:latest
```

### Option B: Development Containers (VS Code)

For development and contributing:

1. Install [VS Code](https://code.visualstudio.com/) and [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
2. Clone the repository:
   ```bash
   git clone https://github.com/decriptor/AzureDevOps.MCP.git
   cd AzureDevOps.MCP
   ```
3. Open in VS Code and click "Reopen in Container"
4. Everything is pre-configured with .NET 9, tools, and extensions!

### Option C: Local Development

For local .NET development:

```bash
# Clone the repository
git clone https://github.com/decriptor/AzureDevOps.MCP.git
cd AzureDevOps.MCP

# Set environment variables
export AzureDevOps__OrganizationUrl="https://dev.azure.com/your-organization"
export AzureDevOps__PersonalAccessToken="your-pat-goes-here"

# Build and run
dotnet restore
dotnet build
dotnet run --project src/AzureDevOps.MCP
```

---

## Step 3: Integrate with MCP Clients

<div class="bg-gradient-to-br from-purple-50 to-indigo-50 rounded-xl p-8 mb-12 border border-purple-200">
  <h3 class="text-2xl font-bold text-purple-900 mb-6 flex items-center">
    <span class="text-3xl mr-3">🔗</span>
    Connect with Your Favorite Tools
  </h3>
  <p class="text-purple-700 text-lg max-w-3xl">Integrate the Azure DevOps MCP server with popular AI tools and development environments for seamless workflow integration.</p>
</div>

### Claude Desktop Integration

Add to your Claude Desktop configuration file:

**Location**:
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

**Configuration**:
```json
{
  "mcpServers": {
    "azure-devops": {
      "command": "docker",
      "args": [
        "run", "-i",
        "-e", "AzureDevOps__OrganizationUrl=https://dev.azure.com/your-organization",
        "-e", "AzureDevOps__PersonalAccessToken=your-pat-goes-here",
        "ghcr.io/decriptor/azuredevops-mcp:latest"
      ]
    }
  }
}
```

### VS Code Extension Integration

For VS Code MCP extensions, use the stdio transport:

```json
{
  "mcp.servers": {
    "azure-devops": {
      "command": "docker",
      "args": ["run", "-i", "ghcr.io/decriptor/azuredevops-mcp:latest"],
      "env": {
        "AzureDevOps__OrganizationUrl": "https://dev.azure.com/your-organization",
        "AzureDevOps__PersonalAccessToken": "your-pat-goes-here"
      }
    }
  }
}
```

---

## Step 4: Verify Installation

<div class="bg-gradient-to-br from-green-50 to-emerald-50 rounded-xl p-8 mb-12 border border-green-200">
  <h3 class="text-2xl font-bold text-green-900 mb-6 flex items-center">
    <span class="text-3xl mr-3">✅</span>
    Test Your Integration
  </h3>
  <p class="text-green-700 text-lg max-w-3xl">Verify that everything is working correctly by testing the connection and trying out some basic commands.</p>
</div>

Once configured, test the connection:

1. **In Claude Desktop**: Ask "List my Azure DevOps projects"
2. **Expected Response**: You should see your accessible projects
3. **Try More Commands**:
   - "Show repositories in [project-name]"
   - "Get work items for [project-name]"
   - "Show recent builds for [project-name]"

---

## Configuration Options

<div class="bg-gradient-to-br from-amber-50 to-orange-50 rounded-xl p-8 mb-12 border border-amber-200">
  <h3 class="text-2xl font-bold text-amber-900 mb-6 flex items-center">
    <span class="text-3xl mr-3">⚙️</span>
    Advanced Configuration
  </h3>
  <p class="text-amber-700 text-lg max-w-3xl">Customize the MCP server behavior with comprehensive configuration options for production deployments and specialized use cases.</p>
</div>

### Environment Variables

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `AzureDevOps__OrganizationUrl` | ✅ | Your Azure DevOps organization URL | `https://dev.azure.com/myorg` |
| `AzureDevOps__PersonalAccessToken` | ✅ | Your Personal Access Token | `pat123...` |
| `AzureDevOps__EnabledWriteOperations` | ❌ | Array of enabled write operations | `["PullRequestComments"]` |
| `AzureDevOps__RequireConfirmation` | ❌ | Require confirmation for writes | `true` |
| `AzureDevOps__EnableAuditLogging` | ❌ | Enable audit logging | `true` |

### Production Configuration

For production deployments, use `appsettings.Production.json`:

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
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
    "EnableCircuitBreaker": true
  },
  "Security": {
    "EnableKeyVault": true,
    "KeyVaultUrl": "https://your-keyvault.vault.azure.net/"
  }
}
```

## Enabling Write Operations

By default, all write operations are disabled for safety. To enable specific operations:

```json
{
  "AzureDevOps": {
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

### Available Write Operations

- **`PullRequestComments`**: Add comments to pull requests
- **`CreateDraftPullRequest`**: Create draft PRs (not published until ready)
- **`UpdateWorkItemTags`**: Add or remove tags from work items
- **`WorkItemComments`**: Add comments to work items

### Write Operation Safety

1. **Opt-In Required**: Must explicitly enable each operation type
2. **Confirmation Required**: Operations require `confirm: true` parameter
3. **Audit Logging**: All write operations are logged with full context
4. **Preview Mode**: See what will change before confirmation

## Troubleshooting

### Common Issues

**"Authentication failed"**
- Verify your PAT token has correct permissions
- Check if token has expired
- Ensure organization URL is correct

**"No projects found"**
- Verify PAT has "Project and Team: Read" permission
- Check if you have access to any projects in the organization

**"Docker: permission denied"**
- On Linux/macOS: Add user to docker group or use `sudo`
- On Windows: Ensure Docker Desktop is running

**"MCP server not responding"**
- Check Docker container logs: `docker logs [container-id]`
- Verify environment variables are set correctly
- Ensure no firewall blocking stdio communication

### Getting Help

- 📚 [API Reference]({{ '/api-reference/' | relative_url }}) - Complete tool documentation
- 💡 [Examples]({{ '/examples/' | relative_url }}) - Common usage patterns
- 🐛 [Report Issues](https://github.com/decriptor/AzureDevOps.MCP/issues) - Bug reports and feature requests
- 💬 [Discussions](https://github.com/decriptor/AzureDevOps.MCP/discussions) - Community support

## Next Steps

Now that you have the server running:

1. **Explore the [API Reference]({{ '/api-reference/' | relative_url }})** - Learn about all available tools
2. **Check out [Examples]({{ '/examples/' | relative_url }})** - See common workflows and patterns
3. **Configure Write Operations** - Enable safe write capabilities for your team
4. **Set up Monitoring** - Configure performance monitoring and alerts
5. **Contribute** - Help improve the project on [GitHub](https://github.com/decriptor/AzureDevOps.MCP)