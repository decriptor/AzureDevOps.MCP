# Developer setup script
Write-Host "🚀 Setting up Azure DevOps MCP development environment..." -ForegroundColor Green

# Check for .NET 9
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET version: $dotnetVersion" -ForegroundColor Green
    
    if ($dotnetVersion -notlike "9.*") {
        Write-Host "⚠️  .NET 9 is recommended. Current version: $dotnetVersion" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ .NET is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Restore packages
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Package restore failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Build solution
Write-Host "🔨 Building solution..." -ForegroundColor Cyan
dotnet build --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Run tests
Write-Host "🧪 Running tests..." -ForegroundColor Cyan
dotnet test --no-build --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Install development tools
Write-Host "🛠️  Installing development tools..." -ForegroundColor Cyan

$tools = @(
    "dotnet-reportgenerator-globaltool",
    "dotnet-outdated-tool"
)

foreach ($tool in $tools) {
    try {
        dotnet tool install -g $tool 2>$null
        Write-Host "✅ Installed: $tool" -ForegroundColor Green
    } catch {
        Write-Host "ℹ️  Tool already installed: $tool" -ForegroundColor Blue
    }
}

# Create sample configuration if it doesn't exist
$sampleConfig = @"
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
    "PersonalAccessToken": "your-pat-here",
    "EnabledWriteOperations": [],
    "RequireConfirmation": true,
    "EnableAuditLogging": true
  }
}
"@

$configPath = "src/AzureDevOps.MCP/appsettings.Development.json"
if (!(Test-Path $configPath)) {
    Write-Host "📝 Creating sample development configuration..." -ForegroundColor Cyan
    $sampleConfig | Out-File -FilePath $configPath -Encoding UTF8
    Write-Host "✅ Created: $configPath" -ForegroundColor Green
    Write-Host "   Please update with your Azure DevOps details" -ForegroundColor Yellow
}

Write-Host "`n🎉 Development environment setup complete!" -ForegroundColor Green
Write-Host "`n📚 Next steps:" -ForegroundColor Cyan
Write-Host "   1. Update $configPath with your Azure DevOps details" -ForegroundColor White
Write-Host "   2. Run: dotnet run --project src/AzureDevOps.MCP" -ForegroundColor White
Write-Host "   3. Run tests with coverage: ./scripts/test-coverage.ps1" -ForegroundColor White