FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/AzureDevOps.MCP/AzureDevOps.MCP.csproj", "src/AzureDevOps.MCP/"]
COPY ["Directory.Packages.props", "."]
RUN dotnet restore "src/AzureDevOps.MCP/AzureDevOps.MCP.csproj"
COPY . .
WORKDIR "/src/src/AzureDevOps.MCP"
RUN dotnet build "AzureDevOps.MCP.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureDevOps.MCP.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureDevOps.MCP.dll"]
