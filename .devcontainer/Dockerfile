FROM mcr.microsoft.com/devcontainers/dotnet:9.0

# Install additional tools
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        git \
        curl \
        wget \
        unzip \
        && apt-get autoremove -y && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Install PowerShell
RUN wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y powershell \
    && rm packages-microsoft-prod.deb

# Install global .NET tools
RUN dotnet tool install -g dotnet-reportgenerator-globaltool \
    && dotnet tool install -g dotnet-outdated-tool

# Add tools to PATH
ENV PATH="$PATH:/home/vscode/.dotnet/tools"

USER vscode