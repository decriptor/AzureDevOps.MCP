# Multi-stage Dockerfile optimized for GitHub Container Registry
# Build arguments for configuration
ARG BUILD_CONFIGURATION=Release
ARG VERSION=latest
ARG DOTNET_VERSION=9.0

# Runtime stage - minimal image for production
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS base
WORKDIR /app

# Create non-root user for security
RUN addgroup -g 1000 -S appgroup && \
    adduser -u 1000 -S appuser -G appgroup

# Install required packages and security updates
RUN apk update && \
    apk add --no-cache \
        ca-certificates \
        tzdata \
        curl \
        && rm -rf /var/cache/apk/*

# Set timezone
ENV TZ=UTC

# Configure ASP.NET Core
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Expose port (non-root port)
EXPOSE 8080

# Build stage - full SDK for building
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

# Install build dependencies
RUN apk add --no-cache git

# Copy project files for dependency restoration
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Build.targets", "."]
COPY ["nuget.config", "."]
COPY ["AzureDevOps.MCP.slnx", "."]
COPY ["src/AzureDevOps.MCP/AzureDevOps.MCP.csproj", "src/AzureDevOps.MCP/"]
COPY ["tests/AzureDevOps.MCP.Tests/AzureDevOps.MCP.Tests.csproj", "tests/AzureDevOps.MCP.Tests/"]

# Restore dependencies (cached layer)
RUN dotnet restore "AzureDevOps.MCP.slnx" \
    --runtime linux-musl-x64 \
    --verbosity minimal

# Copy source code
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]

# Build the application
WORKDIR "/src/src/AzureDevOps.MCP"
RUN dotnet build "AzureDevOps.MCP.csproj" \
    --configuration ${BUILD_CONFIGURATION:-Release} \
    --runtime linux-musl-x64 \
    --no-restore \
    --verbosity minimal \
    -p:Version=${VERSION:-1.0.0} \
    -p:AssemblyVersion=${VERSION:-1.0.0} \
    -p:FileVersion=${VERSION:-1.0.0} \
    -p:InformationalVersion=${VERSION:-1.0.0}

# Test stage - run tests in parallel with build
FROM build AS test
WORKDIR /src
RUN dotnet test "AzureDevOps.MCP.slnx" \
    --configuration ${BUILD_CONFIGURATION:-Release} \
    --no-build \
    --verbosity minimal \
    --logger "trx;LogFileName=test_results.trx" \
    --collect:"XPlat Code Coverage" \
    --results-directory /tmp/test-results

# Publish stage - create optimized published output
FROM build AS publish
ARG BUILD_CONFIGURATION
ARG VERSION

WORKDIR "/src/src/AzureDevOps.MCP"
RUN dotnet publish "AzureDevOps.MCP.csproj" \
    --configuration ${BUILD_CONFIGURATION:-Release} \
    --runtime linux-musl-x64 \
    --self-contained false \
    --no-restore \
    --output /app/publish \
    --verbosity minimal \
    -p:PublishTrimmed=false \
    -p:PublishSingleFile=false \
    -p:UseAppHost=false \
    -p:Version=${VERSION:-1.0.0}

# Remove unnecessary files from publish output
RUN find /app/publish -name "*.pdb" -delete && \
    find /app/publish -name "*.Development.json" -delete

# Final production stage
FROM base AS final
ARG VERSION
WORKDIR /app

# Set version labels
LABEL org.opencontainers.image.title="Azure DevOps MCP"
LABEL org.opencontainers.image.description="Model Context Protocol server for Azure DevOps integration"
LABEL org.opencontainers.image.version="${VERSION}"
LABEL org.opencontainers.image.vendor="Azure DevOps MCP Project"
LABEL org.opencontainers.image.source="https://github.com/decriptor/AzureDevOps.MCP"
LABEL org.opencontainers.image.documentation="https://github.com/decriptor/AzureDevOps.MCP/blob/main/README.md"
LABEL org.opencontainers.image.licenses="MIT"

# Copy published application
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Create directories with proper permissions
RUN mkdir -p /app/logs /app/data && \
    chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "AzureDevOps.MCP.dll"]

# Development stage for local development
FROM build AS development
WORKDIR /src
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
EXPOSE 5000
EXPOSE 5001
CMD ["dotnet", "watch", "run", "--project", "src/AzureDevOps.MCP/AzureDevOps.MCP.csproj", "--urls", "http://+:5000;https://+:5001"]
