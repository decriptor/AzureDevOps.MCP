# PowerShell script to run tests with coverage
param(
    [string]$OutputPath = "TestResults",
    [switch]$OpenReport
)

Write-Host "🧪 Running tests with coverage..." -ForegroundColor Green

# Clean previous results
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory $OutputPath --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Find coverage file
$coverageFile = Get-ChildItem -Path $OutputPath -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1

if ($coverageFile) {
    Write-Host "📊 Coverage report generated: $($coverageFile.FullName)" -ForegroundColor Yellow
    
    # Install reportgenerator if not available
    if (!(Get-Command "reportgenerator" -ErrorAction SilentlyContinue)) {
        Write-Host "📦 Installing ReportGenerator..." -ForegroundColor Cyan
        dotnet tool install -g dotnet-reportgenerator-globaltool
    }
    
    # Generate HTML report
    $htmlPath = Join-Path $OutputPath "html"
    reportgenerator -reports:$coverageFile.FullName -targetdir:$htmlPath -reporttypes:"Html;TextSummary"
    
    # Show summary
    $summaryFile = Join-Path $htmlPath "Summary.txt"
    if (Test-Path $summaryFile) {
        Write-Host "`n📈 Coverage Summary:" -ForegroundColor Green
        Get-Content $summaryFile | Write-Host
    }
    
    if ($OpenReport) {
        $indexFile = Join-Path $htmlPath "index.html"
        if (Test-Path $indexFile) {
            Start-Process $indexFile
        }
    }
} else {
    Write-Host "⚠️  No coverage file found" -ForegroundColor Yellow
}

Write-Host "`n✅ Test run completed!" -ForegroundColor Green