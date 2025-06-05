using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IAzureDevOpsService, AzureDevOpsService>();
builder.Services.AddSingleton<AzureDevOpsTools>();

// Configure MCP Server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.Run();
