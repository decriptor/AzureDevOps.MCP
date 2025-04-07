FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AzureDevOps.MCP.csproj", "."]
RUN dotnet restore "./AzureDevOps.MCP.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "AzureDevOps.MCP.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureDevOps.MCP.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureDevOps.MCP.dll"]
