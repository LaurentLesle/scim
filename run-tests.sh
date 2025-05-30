#!/bin/bash

# SCIM Service Provider Test Runner
# This script runs all unit and integration tests

echo "🧪 SCIM Service Provider Test Suite"
echo "==================================="
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed or not in PATH"
    exit 1
fi

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ Failed to restore packages"
    exit 1
fi

echo "✅ Packages restored successfully"
echo ""

# Build the project
echo "🔨 Building project..."
dotnet build --no-restore
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"
echo ""

# Run tests
echo "🏃‍♂️ Running tests..."
echo ""

# Run with detailed output and collect coverage
dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory TestResults/

TEST_RESULT=$?

echo ""
if [ $TEST_RESULT -eq 0 ]; then
    echo "✅ All tests passed!"
    echo ""
    echo "📊 Test Summary:"
    echo "- Unit Tests: Service layer and controller tests"
    echo "- Integration Tests: Full API workflow tests"
    echo "- Mock Tests: Tests with fake data providers"
    echo ""
    echo "📈 Coverage reports generated in TestResults/"
else
    echo "❌ Some tests failed"
    echo ""
    echo "💡 Tips for debugging:"
    echo "- Check test output above for specific failures"
    echo "- Run individual test files: dotnet test --filter 'FullyQualifiedName~UserServiceTests'"
    echo "- Run specific test: dotnet test --filter 'Name~GetUser_WithValidId_ReturnsOkWithUser'"
fi

exit $TEST_RESULT
