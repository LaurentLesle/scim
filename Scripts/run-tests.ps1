# SCIM Service Provider Test Runner (PowerShell)
# This script runs all unit and integration tests

Write-Host "🧪 SCIM Service Provider Test Suite" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host ""

# Check if dotnet is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: .NET SDK is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Restore packages
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
$restoreResult = dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore packages" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Packages restored successfully" -ForegroundColor Green
Write-Host ""

# Build the project
Write-Host "🔨 Building project..." -ForegroundColor Yellow
$buildResult = dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

# Run tests
Write-Host "🏃‍♂️ Running tests..." -ForegroundColor Yellow
Write-Host ""

# Run with detailed output and collect coverage
dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory TestResults/

$testResult = $LASTEXITCODE

Write-Host ""
if ($testResult -eq 0) {
    Write-Host "✅ All tests passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Test Summary:" -ForegroundColor Cyan
    Write-Host "- Unit Tests: Service layer and controller tests" -ForegroundColor White
    Write-Host "- Integration Tests: Full API workflow tests" -ForegroundColor White
    Write-Host "- Mock Tests: Tests with fake data providers" -ForegroundColor White
    Write-Host ""
    Write-Host "📈 Coverage reports generated in TestResults/" -ForegroundColor Cyan
} else {
    Write-Host "❌ Some tests failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Tips for debugging:" -ForegroundColor Yellow
    Write-Host "- Check test output above for specific failures" -ForegroundColor White
    Write-Host "- Run individual test files: dotnet test --filter 'FullyQualifiedName~UserServiceTests'" -ForegroundColor White
    Write-Host "- Run specific test: dotnet test --filter 'Name~GetUser_WithValidId_ReturnsOkWithUser'" -ForegroundColor White
}

exit $testResult
