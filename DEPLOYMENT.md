# Deployment Guide

This guide covers deploying Azure DevOps MCP to various environments using GitHub's infrastructure and container registry.

## Table of Contents
- [Quick Start](#quick-start)
- [GitHub Container Registry](#github-container-registry)
- [Environment Configuration](#environment-configuration)
- [Deployment Options](#deployment-options)
- [Monitoring and Health](#monitoring-and-health)
- [Security Considerations](#security-considerations)
- [Troubleshooting](#troubleshooting)

## Quick Start

### Using GitHub Container Registry

The easiest way to deploy is using our pre-built container images from GitHub Container Registry:

```bash
# Pull the latest image
docker pull ghcr.io/decriptor/azuredevops-mcp:latest

# Run with environment variables
docker run -d \
  --name azuredevops-mcp \
  -p 8080:8080 \  -e AZDO_ORGANIZATIONURL="https://dev.azure.com/your-org" \
  -e AZDO_PERSONALACCESSTOKEN="your-pat-token" \
  ghcr.io/decriptor/azuredevops-mcp:latest
```

### Using Docker Compose

1. Clone the repository:
```bash
git clone https://github.com/decriptor/AzureDevOps.MCP.git
cd azuredevops-mcp
```

2. Configure environment:
```bash
cp .env.example .env
# Edit .env with your configuration
```

3. Deploy:
```bash
# Production deployment
docker-compose up -d

# Development deployment
docker-compose -f docker-compose.dev.yml up -d
```

## GitHub Container Registry

### Authentication

To pull images from GitHub Container Registry:

```bash
# Using GitHub token
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Using GitHub CLI
gh auth login
echo $GITHUB_TOKEN | docker login ghcr.io -u $(gh api user --jq .login) --password-stdin
```

### Available Tags

- `latest` - Latest stable release from main branch
- `develop` - Latest development build from develop branch
- `vX.Y.Z` - Specific version releases
- `main-SHA` - Specific commit from main branch
- `develop-SHA` - Specific commit from develop branch

### Using in Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: azuredevops-mcp
spec:
  replicas: 2
  selector:
    matchLabels:
      app: azuredevops-mcp
  template:
    metadata:
      labels:
        app: azuredevops-mcp
    spec:
      imagePullSecrets:
      - name: ghcr-secret
      containers:
      - name: azuredevops-mcp
        image: ghcr.io/decriptor/azuredevops-mcp:latest
        ports:
        - containerPort: 8080
        env:
        - name: AZDO_ORGANIZATIONURL
          valueFrom:
            secretKeyRef:
              name: azuredevops-secret
              key: organization-url
        - name: AZDO_PERSONALACCESSTOKEN
          valueFrom:
            secretKeyRef:
              name: azuredevops-secret
              key: personal-access-token
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
```

## Environment Configuration

### Required Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `AZDO_ORGANIZATIONURL` | Azure DevOps organization URL | `https://dev.azure.com/myorg` |
| `AZDO_PERSONALACCESSTOKEN` | Azure DevOps PAT | `pat_xxxxxxxxxxxxxx` |

### Optional Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment name |
| `REDIS_CONNECTION_STRING` | - | Redis connection for distributed cache |
| `AZURE_KEY_VAULT_URL` | - | Azure Key Vault for secrets |
| `SENTRY_DSN` | - | Sentry error tracking |
| `LOG_LEVEL` | `Information` | Logging level |
| `RATE_LIMIT_REQUESTS_PER_MINUTE` | `60` | Rate limiting |

### Configuration Files

#### Production Configuration
```json
{
  "AzureDevOps": {
    "OrganizationUrl": "",
    "PersonalAccessToken": "",
    "ConnectionTimeoutSeconds": 30,
    "MaxRetryAttempts": 3
  },
  "Security": {
    "EnableKeyVault": true,
    "EnableApiKeyAuth": true,
    "EnableIpWhitelist": true
  },
  "Performance": {
    "EnableCircuitBreaker": true,
    "SlowOperationThresholdMs": 1000
  },
  "HealthChecks": {
    "EnableHealthChecks": true,
    "TimeoutSeconds": 30
  }
}
```

#### Development Configuration
```json
{
  "AzureDevOps": {
    "OrganizationUrl": "",
    "PersonalAccessToken": ""
  },
  "Environment": {
    "Name": "Development",
    "EnableDevelopmentFeatures": true,
    "EnableDebugEndpoints": true
  }
}
```

## Deployment Options

### 1. Docker Standalone

Simple single-container deployment:

```bash
# Create network
docker network create azuredevops-mcp-network

# Run application
docker run -d \
  --name azuredevops-mcp \
  --network azuredevops-mcp-network \
  -p 8080:8080 \
  --env-file .env \  --restart unless-stopped \
  ghcr.io/decriptor/azuredevops-mcp:latest
```

### 2. Docker Compose

Full stack with Redis and monitoring:

```bash
# Production
docker-compose up -d

# With monitoring
docker-compose --profile monitoring up -d

# Development
docker-compose -f docker-compose.dev.yml up -d
```

### 3. Kubernetes

#### Using Helm Chart (if available)
```bash
helm repo add azuredevops-mcp https://decriptor.github.io/AzureDevOps.MCP
helm install azuredevops-mcp azuredevops-mcp/azuredevops-mcp \
  --set azureDevOps.organizationUrl="https://dev.azure.com/myorg" \
  --set azureDevOps.personalAccessToken="your-token"
```

#### Using kubectl
```bash
# Create namespace
kubectl create namespace azuredevops-mcp

# Create secrets
kubectl create secret generic azuredevops-secret \
  --from-literal=organization-url="https://dev.azure.com/myorg" \
  --from-literal=personal-access-token="your-token" \
  -n azuredevops-mcp

# Apply manifests
kubectl apply -f k8s/ -n azuredevops-mcp
```

### 4. Azure Container Instances

```bash
az container create \
  --resource-group myResourceGroup \
  --name azuredevops-mcp \
  --image ghcr.io/decriptor/azuredevops-mcp:latest \
  --ports 8080 \
  --dns-name-label azuredevops-mcp \
  --environment-variables \
    AZDO_ORGANIZATIONURL="https://dev.azure.com/myorg" \
  --secure-environment-variables \
    AZDO_PERSONALACCESSTOKEN="your-token"
```

### 5. GitHub Codespaces

For development in GitHub Codespaces:

```json
// .devcontainer/devcontainer.json
{
  "name": "Azure DevOps MCP",
  "build": {
    "dockerfile": "../Dockerfile",
    "target": "development"
  },
  "forwardPorts": [5000, 5001],
  "mounts": [
    "source=azuredevops-mcp-data,target=/app/data,type=volume"
  ],
  "features": {
    "ghcr.io/devcontainers/features/docker-in-docker:2": {}
  },
  "postCreateCommand": "dotnet restore",
  "remoteEnv": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

## Monitoring and Health

### Health Checks

The application exposes health endpoints:

- `/health` - Overall health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

#### Response Format
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "application": {
      "status": "Healthy",
      "description": "Application is running in Production environment",
      "data": {
        "environment": "Production",
        "version": "1.0.0"
      }
    },
    "azuredevops": {
      "status": "Healthy",
      "description": "Azure DevOps is accessible and responsive"
    }
  }
}
```

### Metrics

Prometheus metrics available at `/metrics`:

- `azuredevops_mcp_requests_total` - Total requests
- `azuredevops_mcp_request_duration_seconds` - Request duration
- `azuredevops_mcp_cache_hits_total` - Cache hits
- `azuredevops_mcp_cache_misses_total` - Cache misses
- `azuredevops_mcp_errors_total` - Error count

### Grafana Dashboards

Import the provided Grafana dashboards from `monitoring/grafana/dashboards/`:

- **Overview Dashboard** - Application metrics and health
- **Performance Dashboard** - Detailed performance metrics
- **Security Dashboard** - Security and audit metrics

## Security Considerations

### Container Security

1. **Non-root user**: Application runs as non-root user (UID 1000)
2. **Minimal base image**: Uses Alpine Linux for smaller attack surface
3. **Security scanning**: Automated vulnerability scanning with Trivy
4. **Read-only filesystem**: Consider using read-only root filesystem

### Network Security

1. **Non-privileged ports**: Uses port 8080 (non-privileged)
2. **TLS termination**: Use reverse proxy for TLS termination
3. **Network policies**: Implement Kubernetes network policies

### Secrets Management

#### Using GitHub Secrets in CI/CD
```yaml
env:
  AZDO_ORGANIZATIONURL: ${{ secrets.AZDO_ORGANIZATIONURL }}
  AZDO_PERSONALACCESSTOKEN: ${{ secrets.AZDO_PERSONALACCESSTOKEN }}
```

#### Using Azure Key Vault
```json
{
  "Security": {
    "EnableKeyVault": true,
    "KeyVaultUrl": "https://your-keyvault.vault.azure.net/",
    "ManagedIdentityClientId": "your-managed-identity-id"
  }
}
```

#### Using Kubernetes Secrets
```bash
kubectl create secret generic azuredevops-secret \
  --from-literal=organization-url="https://dev.azure.com/myorg" \
  --from-literal=personal-access-token="your-token"
```

### Access Control

1. **API Keys**: Enable API key authentication for additional security
2. **IP Whitelisting**: Restrict access to known IP ranges
3. **Rate Limiting**: Prevent abuse with configurable rate limits

## Troubleshooting

### Common Issues

#### 1. Authentication Failures
```bash
# Check PAT permissions
curl -u :YOUR_PAT https://dev.azure.com/yourorg/_apis/projects?api-version=6.0

# Check container logs
docker logs azuredevops-mcp
```

#### 2. Network Connectivity
```bash
# Test Azure DevOps connectivity from container
docker exec azuredevops-mcp curl -f https://dev.azure.com/yourorg/_apis/projects

# Check DNS resolution
docker exec azuredevops-mcp nslookup dev.azure.com
```

#### 3. Performance Issues
```bash
# Check memory usage
docker stats azuredevops-mcp

# Check application metrics
curl http://localhost:8080/metrics

# Enable debug logging
docker run -e LOG_LEVEL=Debug ghcr.io/decriptor/azuredevops-mcp:latest
```

#### 4. Health Check Failures
```bash
# Manual health check
curl -f http://localhost:8080/health

# Detailed health status
curl http://localhost:8080/health | jq .
```

### Debug Mode

Run with debug configuration:

```bash
docker run -it \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e LOG_LEVEL=Debug \  -e ENABLE_DEBUG_ENDPOINTS=true \
  ghcr.io/decriptor/azuredevops-mcp:latest
```

### Log Analysis

#### Structured Logging
```bash
# Filter by operation
docker logs azuredevops-mcp | jq 'select(.Operation == "GetProjects")'

# Filter by errors
docker logs azuredevops-mcp | jq 'select(.Level == "Error")'

# Performance analysis
docker logs azuredevops-mcp | jq 'select(.Duration > 1000)'
```

### Resource Monitoring

```bash
# Container resource usage
docker stats azuredevops-mcp

# Application memory usage
curl http://localhost:8080/health/memory

# Cache statistics
curl http://localhost:8080/health/cache
```

## Support

For deployment issues:

1. Check the [troubleshooting section](#troubleshooting)
2. Review application logs
3. Check health endpoints
4. Create an issue on GitHub with:
   - Deployment method
   - Configuration (sanitized)
   - Error logs
   - Environment details

## Updates

To update to a newer version:

```bash
# Docker
docker pull ghcr.io/decriptor/azuredevops-mcp:latest
docker-compose up -d

# Kubernetes
kubectl set image deployment/azuredevops-mcp \
  azuredevops-mcp=ghcr.io/decriptor/azuredevops-mcp:latest

# Verify deployment
kubectl rollout status deployment/azuredevops-mcp
```