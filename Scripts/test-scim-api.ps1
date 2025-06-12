# SCIM Service Provider Test Script (PowerShell)
# This script demonstrates how to interact with the SCIM API

$BaseUrl = "https://localhost:5001"  # Change to your Dev Tunnel URL if using tunnels
$ClientId = "scim_client"
$ClientSecret = "scim_secret"

Write-Host "SCIM Service Provider Test Script" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host "Testing against: $BaseUrl" -ForegroundColor Cyan
Write-Host ""

# Check if we should use a tunnel URL
$useTunnel = Read-Host "Are you using a Dev Tunnel? (y/n)"
if ($useTunnel -eq "y" -or $useTunnel -eq "Y") {
    $tunnelUrl = Read-Host "Enter your Dev Tunnel URL (e.g., https://abc123-5000.devtunnels.ms)"
    if (![string]::IsNullOrWhiteSpace($tunnelUrl)) {
        $BaseUrl = $tunnelUrl
        Write-Host "Updated base URL to: $BaseUrl" -ForegroundColor Cyan
    }
}

Write-Host ""

# Ignore SSL certificate errors for local testing
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

# Step 1: Get access token
Write-Host "1. Getting access token..." -ForegroundColor Yellow

$tokenBody = @{
    clientId = $ClientId
    clientSecret = $ClientSecret
    grantType = "client_credentials"
} | ConvertTo-Json

try {
    $tokenResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/token" -Method Post -Body $tokenBody -ContentType "application/json"
    $token = $tokenResponse.access_token
    Write-Host "✓ Access token obtained" -ForegroundColor Green
} catch {
    Write-Host "Failed to get access token: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 2: Test service provider configuration
Write-Host ""
Write-Host "2. Testing service provider configuration..." -ForegroundColor Yellow
try {
    $config = Invoke-RestMethod -Uri "$BaseUrl/scim/v2/ServiceProviderConfig" -Method Get -Headers $headers
    Write-Host "✓ Service provider configuration retrieved" -ForegroundColor Green
} catch {
    Write-Host "Failed to get service provider config: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Create a test user
Write-Host ""
Write-Host "3. Creating a test user..." -ForegroundColor Yellow

$userBody = @{
    userName = "john.doe@example.com"
    name = @{
        givenName = "John"
        familyName = "Doe"
        formatted = "John Doe"
    }
    displayName = "John Doe"
    emails = @(
        @{
            value = "john.doe@example.com"
            type = "work"
            primary = $true
        }
    )
    active = $true
} | ConvertTo-Json -Depth 3

try {
    $userResponse = Invoke-RestMethod -Uri "$BaseUrl/scim/v2/Users" -Method Post -Body $userBody -Headers $headers
    $userId = $userResponse.id
    Write-Host "✓ User created with ID: $userId" -ForegroundColor Green
} catch {
    Write-Host "Failed to create user: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Get the created user
Write-Host ""
Write-Host "4. Getting the created user..." -ForegroundColor Yellow
try {
    $user = Invoke-RestMethod -Uri "$BaseUrl/scim/v2/Users/$userId" -Method Get -Headers $headers
    Write-Host "✓ User retrieved: $($user.userName)" -ForegroundColor Green
} catch {
    Write-Host "Failed to get user: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Update user (disable)
Write-Host ""
Write-Host "5. Disabling the user..." -ForegroundColor Yellow

$patchBody = @{
    schemas = @("urn:ietf:params:scim:api:messages:2.0:PatchOp")
    Operations = @(
        @{
            op = "replace"
            path = "active"
            value = $false
        }
    )
} | ConvertTo-Json -Depth 3

try {
    $patchedUser = Invoke-RestMethod -Uri "$BaseUrl/scim/v2/Users/$userId" -Method Patch -Body $patchBody -Headers $headers
    Write-Host "✓ User disabled successfully" -ForegroundColor Green
} catch {
    Write-Host "Failed to patch user: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 6: List users with filter
Write-Host ""
Write-Host "6. Listing users with filter..." -ForegroundColor Yellow
try {
    $encodedFilter = [System.Web.HttpUtility]::UrlEncode('userName eq "john.doe@example.com"')
    $users = Invoke-RestMethod -Uri "$BaseUrl/scim/v2/Users?filter=$encodedFilter" -Method Get -Headers $headers
    Write-Host "✓ Found $($users.totalResults) user(s)" -ForegroundColor Green
} catch {
    Write-Host "Failed to list users: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 7: Create a test group
Write-Host ""
Write-Host "7. Creating a test group..." -ForegroundColor Yellow

$groupBody = @{
    displayName = "Test Group"
    members = @(
        @{
            value = $userId
            display = "John Doe"
            type = "User"
        }
    )
} | ConvertTo-Json -Depth 3

try {
    $groupResponse = Invoke-RestMethod -Uri "$BaseUrl/scim/v2/Groups" -Method Post -Body $groupBody -Headers $headers
    $groupId = $groupResponse.id
    Write-Host "✓ Group created with ID: $groupId" -ForegroundColor Green
} catch {
    Write-Host "Failed to create group: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 8: List all users
Write-Host ""
Write-Host "8. Listing all users..." -ForegroundColor Yellow
try {
    $allUsers = Invoke-RestMethod -Uri "$BaseUrl/scim/v2/Users" -Method Get -Headers $headers
    Write-Host "✓ Total users: $($allUsers.totalResults)" -ForegroundColor Green
} catch {
    Write-Host "Failed to list all users: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 9: List all groups
Write-Host ""
Write-Host "9. Listing all groups..." -ForegroundColor Yellow
try {
    $allGroups = Invoke-RestMethod -Uri "$BaseUrl/scim/v2/Groups" -Method Get -Headers $headers
    Write-Host "✓ Total groups: $($allGroups.totalResults)" -ForegroundColor Green
} catch {
    Write-Host "Failed to list all groups: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "✓ Test completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To run this script:" -ForegroundColor Cyan
Write-Host "1. Start the SCIM service: dotnet run" -ForegroundColor Cyan
Write-Host "2. Run this script: .\test-scim-api.ps1" -ForegroundColor Cyan
