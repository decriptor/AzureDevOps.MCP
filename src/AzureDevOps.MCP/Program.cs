using AzureDevOps.MCP.Services;
using Microsoft.Model.Context.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IAzureDevOpsService, AzureDevOpsService>();

// Configure MCP
builder.Services.AddMcp();

var app = builder.Build();

// Configure middleware
app.UseMcp();

app.Run();
