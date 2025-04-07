# Development Container

This directory contains development container configurations for a consistent development experience.

## Quick Start

### Option 1: VS Code Dev Containers (Recommended)
1. Install [VS Code](https://code.visualstudio.com/) and the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
2. Clone this repository
3. Open in VS Code
4. Click "Reopen in Container" when prompted (or use Command Palette: "Dev Containers: Reopen in Container")

### Option 2: GitHub Codespaces
1. Go to the repository on GitHub
2. Click the green "Code" button
3. Select "Codespaces" tab
4. Click "Create codespace on main"

## Configuration Files

### `devcontainer.json` (Default)
- **Use Case**: Standard development with full tooling
- **Features**: 
  - .NET 9 SDK
  - Docker-in-Docker support
  - GitHub CLI
  - PowerShell
  - Pre-configured VS Code extensions
  - Development tools (ReportGenerator, dotnet-outdated)

### `devcontainer-simple.json` (Alternative)
- **Use Case**: Multi-container setup with Redis
- **Features**:
  - Application container with .NET 9
  - Redis container for caching tests
  - Docker Compose orchestration
  - Minimal VS Code configuration

## What's Included

### Pre-installed Tools
- ✅ .NET 9 SDK
- ✅ Docker CLI
- ✅ GitHub CLI

### VS Code Extensions
- ✅ C# Dev Kit
- ✅ EditorConfig support
- ✅ Docker extension
- ✅ GitHub Actions extension
- ✅ Spell checker

### Port Forwarding
- **8080**: MCP Server (main application)
- **5000**: HTTP development server
- **5001**: HTTPS development server
- **6379**: Redis (when using docker-compose setup)

## Environment Variables

The container sets these environment variables:
- `DOTNET_CLI_TELEMETRY_OPTOUT=1` - Disable .NET telemetry
- `DOTNET_NOLOGO=1` - Hide .NET startup messages
- `ASPNETCORE_ENVIRONMENT=Development` - Set development mode

## Volume Mounts

- **NuGet Cache**: Persistent volume for faster package restoration
- **Source Code**: Workspace mounted for live editing

## Usage

### Running the Application
```bash
# Build and run
dotnet run --project src/AzureDevOps.MCP

# Run with specific configuration
dotnet run --project src/AzureDevOps.MCP --configuration Debug
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# Test coverage is handled by CI/CD
```

### Docker Commands
```bash
# Build Docker image
docker build -t azuredevops-mcp .

# Run with docker-compose
docker-compose -f docker-compose.dev.yml up
```

## Configuration

### Azure DevOps Setup
1. Copy `.env.example` to `.env.development`
2. Update with your Azure DevOps details:
   ```bash
   AZDO_ORGANIZATIONURL=https://dev.azure.com/your-organization
   AZDO_PERSONALACCESSTOKEN=your-pat-token
   ```

### Using with GitHub Codespaces
The configuration is optimized for GitHub Codespaces with:
- Automatic port forwarding
- Pre-built development environment
- Integrated GitHub CLI for repository operations

## Troubleshooting

### Container Won't Start
1. **Ensure Docker is running**
   - Windows: Check Docker Desktop is running
   - macOS: Check Docker Desktop is running
   - Linux: Check `docker` service is active

2. **Check Docker resources**
   - Allocate at least 4GB RAM to Docker
   - Ensure sufficient disk space (2GB+ free)

3. **Try alternative configurations**
   - Use `devcontainer-minimal.json` for basic setup
   - Use `devcontainer-simple.json` for docker-compose setup

4. **Rebuild container**
   - Command Palette: "Dev Containers: Rebuild Container"
   - Or: "Dev Containers: Rebuild and Reopen in Container"

### Common Issues

**Error: "Command failed"**
- Try the minimal configuration first
- Check Docker Desktop is not updating
- Restart Docker Desktop and VS Code

**Permission Issues**
- Ensure your user has Docker permissions
- On Linux: Add user to `docker` group

**Slow Performance**
- Increase Docker Desktop memory (8GB+ recommended)
- Close other Docker containers
- Use SSD storage for Docker

**Extension Issues**
- Start with minimal extensions first
- Install additional extensions after container starts
- Some extensions require container restart

### Build Issues Resolved

The previous build issues with package version conflicts have been resolved:
- ✅ Fixed Microsoft.CodeAnalysis package version conflicts
- ✅ Corrected Sentry performance service implementation
- ✅ Updated cache statistics method calls
- ✅ Simplified devcontainer setup process

### Getting Help
If devcontainers don't work for you:
- Manual setup: `dotnet restore && dotnet build && dotnet test`
- Create an issue with your Docker/VS Code versions
- All known dependency conflicts have been resolved

## Customization

### Adding Extensions
Edit `.devcontainer/devcontainer.json`:
```json
"extensions": [
  "ms-dotnettools.csharp",
  "your-extension-id"
]
```

### Adding Tools
Edit `.devcontainer/Dockerfile` or `onCreateCommand` in `devcontainer.json`

### Environment Variables
Add to `remoteEnv` in `devcontainer.json`:
```json
"remoteEnv": {
  "YOUR_VARIABLE": "value"
}
```