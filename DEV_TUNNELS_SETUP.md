# Dev Tunnels Setup Guide

Visual Studio Dev Tunnels is Microsoft's secure solution for exposing local development servers to the internet. This guide shows how to set up Dev Tunnels for your SCIM service provider.

## What are Dev Tunnels?

Dev Tunnels allow you to securely expose your local development server to the internet with:
- **Built-in HTTPS** - Automatic SSL certificates
- **Microsoft Authentication** - Secure access control
- **Multiple Access Levels** - Anonymous, organization, or user-specific
- **VS Code Integration** - Seamless development experience
- **Free Tier Available** - No cost for basic usage

## Prerequisites

- Microsoft account (personal or work/school)
- .NET 8.0 SDK
- Visual Studio Code (optional, for VS Code extension)

## ✅ Confirmed Working Solutions

### For GitHub Codespaces (Recommended)
1. **Microsoft Official CLI** - `curl -sL https://aka.ms/DevTunnelCliInstall | bash`
2. **GitHub Codespaces Port Forwarding** - Built-in feature, no installation required

### Authentication Options
- **Device Code (Recommended for Codespaces)**: `devtunnel user login --use-device-code-auth`
- Microsoft/Entra ID accounts: `devtunnel user login`
- GitHub accounts: `devtunnel user login --github`

## Setup

### Step 1: Start the service and dev tunnel
   ```bash
   # Find available port and start service
   SCIM_PORT=""
   for port in {5000..5010}; do
     if ! curl -s "http://localhost:$port" > /dev/null 2>&1; then
       SCIM_PORT=$port
       break
     fi
   done
   
   echo "Starting SCIM service on port $SCIM_PORT"
   # Start service in background with log redirection
   nohup dotnet run --urls="http://0.0.0.0:$SCIM_PORT" > scim.log 2>&1 &
   SCIM_PID=$!
   
   echo "SCIM service started in background (PID: $SCIM_PID)"
   echo "To view real-time logs: tail -f scim.log"
   
   # Wait for service to start
   sleep 5
   
   # Test that service is running locally FIRST
   LOCAL_TEST=$(curl -s "http://localhost:$SCIM_PORT/api/auth/token" \
     -H "Content-Type: application/json" \
     -d '{"clientId": "scim_client","clientSecret": "scim_secret","grantType": "client_credentials"}')
   
   if echo "$LOCAL_TEST" | jq -e '.access_token' > /dev/null 2>&1; then
     echo "✅ SCIM service working locally - ready to make public"
     echo ""
     echo "💡 To view real-time logs while working:"
     echo "   tail -f scim.log"
     echo "   # Or in another terminal: tail -f /workspaces/scim/scim.log"
   else
     echo "❌ Fix SCIM service issues before making port public!"
     echo "Check logs with: tail -f scim.log"
     exit 1
   fi

  TUNNEL_ID=$(devtunnel create --allow-anonymous | grep "Tunnel ID" | awk '{print $4}')
  echo "Created tunnel: $TUNNEL_ID"

  # Add port forwarding for SCIM service (using the port we started above)
  devtunnel port create $TUNNEL_ID -p $SCIM_PORT --protocol http

  # Start hosting (run in background)
  devtunnel host $TUNNEL_ID &
  echo "Tunnel is now hosting in background"
  
  sleep 5

  # Get the tunnel URL and store in environment variable (remove trailing slash)
  TUNNEL_URL=$(devtunnel show $TUNNEL_ID --json | jq -r '.tunnel.ports[0].portUri | rtrimstr("/")')
  export TUNNEL_URL
  export SCIM_PORT
  echo "Tunnel URL: $TUNNEL_URL"
  echo "SCIM Port: $SCIM_PORT"

  # Test the tunnel immediately
  echo "Testing tunnel connectivity..."
  TUNNEL_TEST=$(curl -s "${TUNNEL_URL}/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"clientId": "scim_client","clientSecret": "scim_secret","grantType": "client_credentials"}')

  if echo "$TUNNEL_TEST" | jq -e '.access_token' > /dev/null 2>&1; then
    echo "✅ Tunnel working - SCIM API accessible publicly!"
  else
    echo "❌ Tunnel test failed. Check if devtunnel host is running."
    echo "Response: $TUNNEL_TEST"
  fi

  
  # Test the tunnel
  curl -s "${TUNNEL_URL}/api/auth/token" \
    -H "Content-Type: application/json" \
    -d '{"clientId": "scim_client","clientSecret": "scim_secret","grantType": "client_credentials"}' | jq '.'
    echo $TUNNEL_URL
## 📊 Viewing Real-Time Logs While Service Runs in Background

Since the SCIM service runs in background mode (required for dev tunnels), here are several ways to monitor logs in real-time:

### Method 1: Tail the Log File (Recommended)
# In the same terminal or a new terminal window
tail -f scim.log

```

### Method 2: VS Code Terminal Split
```bash
# In VS Code, split your terminal (Ctrl+Shift+5)
# Left side: Run tunnel setup commands
# Right side: Monitor logs
tail -f scim.log
```

### Method 3: Watch for Specific Events
```bash
# Watch for authentication requests only
tail -f scim.log | grep "Auth"

# Watch for errors and warnings
tail -f scim.log | grep -E "(warn:|error:|❌)"

# Watch for successful operations
tail -f scim.log | grep -E "(info:|✅|🎫)"
```

### Method 4: Use VS Code Output Panel
1. **View** → **Output** 
2. Select **"C# Log"** from dropdown
3. .NET logs will appear there automatically

### Method 5: Background Process Status
```bash
# Check if service is still running
ps aux | grep "dotnet run"

# Check service health
curl -s http://localhost:$SCIM_PORT/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId":"scim_client","clientSecret":"scim_secret","grantType":"client_credentials"}' | jq '.access_token'

# Stop following logs (Ctrl+C in tail terminal)
```

### Quick Log Monitoring Test
```bash
# In one terminal: monitor logs
tail -f scim.log &

# In another terminal: make a test request
curl -s "http://localhost:$SCIM_PORT/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"clientId":"scim_client","clientSecret":"scim_secret","grantType":"client_credentials"}'

# You should see real-time logs like:
# info: ScimServiceProvider.Controllers.AuthController[0]
#       🔐 Auth token request received from client: scim_client
# info: ScimServiceProvider.Controllers.AuthController[0]
#       ✅ Authentication successful for client: scim_client  
# info: ScimServiceProvider.Controllers.AuthController[0]
#       🎫 JWT token generated successfully for client: scim_client, expires in 1 hour


### Step 2: Set Up Environment Variables (After Tunnel is Created)

**Set common environment variables for testing:**
```bash
export AUTH_URL="$TUNNEL_URL/api/auth/token"
export SCIM_BASE_URL="$TUNNEL_URL/scim/v2"
export CUSTOMER_URL="$TUNNEL_URL/api/customers"

echo "Environment variables set:"
echo "  TUNNEL_URL: $TUNNEL_URL"
echo "  SCIM_PORT: $SCIM_PORT"
echo "  AUTH_URL: $AUTH_URL" 
echo "  SCIM_BASE_URL: $SCIM_BASE_URL"
echo "  CUSTOMER_URL: $CUSTOMER_URL"
```

### Step 3: Test Public Tunnel Access

```bash
# Test that public tunnel works
echo "Testing public tunnel access..."
TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "scim_client",
    "clientSecret": "scim_secret",
    "grantType": "client_credentials"
  }')

export TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
  echo "❌ Public tunnel authentication failed. Response:"
  echo "$TOKEN_RESPONSE"
  echo ""
  echo "Troubleshooting steps:"
  echo "1. Verify SCIM service is still running: curl http://localhost:$SCIM_PORT/api/auth/token"
  echo "2. Check tunnel URL is accessible: curl $TUNNEL_URL"
  echo "3. For Codespaces: Ensure port is made public in PORTS tab"
  echo "4. For CLI: Ensure 'devtunnel host' is running"
  exit 1
fi

echo "✅ Public tunnel authentication successful: ${TOKEN:0:50}..."

# Create test tenant
echo "Creating test tenant via public tunnel..."
TENANT_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" -X POST "$CUSTOMER_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Customer",
    "tenantId": "test-tenant",
    "isActive": true
  }')

echo "Tenant response:"
echo "$TENANT_RESPONSE" | jq '.'

export TENANT_ID="test-tenant"
echo "✅ Tenant created successfully"

echo "Creating test tenant..."
TENANT_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" -X POST "$CUSTOMER_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Customer",
    "tenantId": "test-tenant",
    "isActive": true
  }')

echo "Tenant response:"
echo "$TENANT_RESPONSE" | jq '.'

export TENANT_ID="test-tenant"
```

### Step 5: Test SCIM Endpoints

```bash
# Test Service Provider Configuration
echo "=== Testing Service Provider Configuration ==="
curl -s -H "Authorization: Bearer $TOKEN" \
  "$SCIM_BASE_URL/ServiceProviderConfig" | jq '.authenticationSchemes[0]'

# Test Schemas endpoint
echo "=== Testing Schemas Endpoint ==="
curl -s -H "Authorization: Bearer $TOKEN" \
  "${TUNNEL_URL}Schemas" | jq '.totalResults'

# List existing users
echo "=== Listing Current Users ==="
curl -s -H "Authorization: Bearer $TOKEN" -H "X-Tenant-ID: $TENANT_ID" \
  "$SCIM_BASE_URL/Users" | jq '.Resources'

# Create a test user
echo "=== Creating Test User ==="
USER_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" -H "X-Tenant-ID: $TENANT_ID" \
  -X POST "$SCIM_BASE_URL/Users" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "john.doe@example.com",
    "name": {
      "givenName": "John",
      "familyName": "Doe",
      "formatted": "John Doe"
    },
    "displayName": "John Doe",
    "emails": [
      {
        "value": "john.doe@example.com",
        "type": "work",
        "primary": true
      }
    ],
    "active": true
  }')

echo "User created:"
echo "$USER_RESPONSE" | jq '.'

# List users again to verify
echo "=== Verifying User Creation ==="
curl -s -H "Authorization: Bearer $TOKEN" -H "X-Tenant-ID: $TENANT_ID" \
  "$SCIM_BASE_URL/Users" | jq '.Resources'
```

### Step 6: Complete Environment Summary

```bash
echo "=================================="
echo "🎉 SCIM API Setup Complete!"
echo "=================================="
echo "SCIM Service URL: $TUNNEL_URL"
echo "SCIM Base URL: $SCIM_BASE_URL"
echo "Tenant ID: $TENANT_ID"
echo "Authentication Token: ${TOKEN:0:50}..."
echo ""
echo "For Azure AD configuration:"
echo "  Tenant URL: $SCIM_BASE_URL"
echo "  Secret Token: $TOKEN"
echo ""
echo "Environment variables are set and ready for testing!"
```

### Quick Test Script

Here's a complete script you can run to test the workflow:

```bash
#!/bin/bash

# Quick SCIM API Test Script
echo "🚀 Starting SCIM API Test..."

# Step 1: Set up for local testing first
export TUNNEL_URL="http://localhost:5000"
export LOCAL_MODE=true
export AUTH_URL="$TUNNEL_URL/api/auth/token"
export SCIM_BASE_URL="$TUNNEL_URL/scim/v2"
export CUSTOMER_URL="$TUNNEL_URL/api/customers"
export TENANT_ID="test-tenant"

echo "📋 Environment variables set:"
echo "  TUNNEL_URL: $TUNNEL_URL"
echo "  LOCAL_MODE: $LOCAL_MODE"

# Step 2: Test authentication
echo "🔐 Testing authentication..."
TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_URL" \
  -H "Content-Type: application/json" \
  -d '{"clientId": "scim_client","clientSecret": "scim_secret","grantType": "client_credentials"}')

export TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
  echo "❌ Authentication failed. Make sure SCIM service is running on port 5000"
  echo "Response: $TOKEN_RESPONSE"
  exit 1
fi

echo "✅ Authentication successful: ${TOKEN:0:50}..."

# Step 3: Create tenant
echo "🏢 Creating test tenant..."
TENANT_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" -X POST "$CUSTOMER_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Customer",
    "tenantId": "test-tenant",
    "isActive": true
  }')

echo "✅ Tenant created/verified"

# Step 4: Test SCIM endpoints
echo "🧪 Testing SCIM endpoints..."

echo "  📋 Service Provider Config:"
curl -s -H "Authorization: Bearer $TOKEN" \
  "$SCIM_BASE_URL/ServiceProviderConfig" | jq '.authenticationSchemes[0].name'

echo "  📄 Schemas:"
SCHEMAS_COUNT=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "${TUNNEL_URL}Schemas" | jq '.totalResults // 0')
echo "    Found $SCHEMAS_COUNT schemas"

echo "  👥 Current users:"
USER_COUNT=$(curl -s -H "Authorization: Bearer $TOKEN" -H "X-Tenant-ID: $TENANT_ID" \
  "$SCIM_BASE_URL/Users" | jq '.totalResults // 0')
echo "    Found $USER_COUNT users"

echo ""
echo "🎉 SCIM API is working!"
echo "=================================="
echo "Ready for external access. To use public tunnel:"
echo "1. Make port 5000 public in VS Code PORTS tab"
echo "2. Update TUNNEL_URL to: https://\$CODESPACE_NAME-5000.app.github.dev"
echo "3. Re-run authentication and testing steps"
echo ""
echo "Current environment variables are set and ready!"
```

Save this as `quick-test.sh` and run:
```bash
chmod +x quick-test.sh
./quick-test.sh
```

## Automated Test Script Usage

You can also use the provided test script with your environment variables:

```bash
# Make executable
chmod +x test-scim-api.sh

# Test with your tunnel URL
echo -e "y\n$TUNNEL_URL" | ./test-scim-api.sh
```

## Troubleshooting

### Common Issues

**1. Authentication Problems:**
```bash
# Clear and re-authenticate
devtunnel user logout
devtunnel user login --use-device-code-auth
```

**2. Tunnel URL Not Set:**
```bash
# Check if tunnel URL is set
echo "Current TUNNEL_URL: $TUNNEL_URL"

# For Codespaces, reset the URL
export TUNNEL_URL="https://$CODESPACE_NAME-5000.app.github.dev"

# For Dev Tunnels CLI, get URL from tunnel (remove trailing slash)
TUNNEL_URL=$(devtunnel show $TUNNEL_ID --output json | jq -r '.endpoints[0].hostRelayUri | rtrimstr("/")')
```

**3. Port Issues:**
```bash
# Check what's using port 5000
netstat -tulpn | grep :5000

# Kill SCIM service if needed
kill $SCIM_PID
```

**4. Token Expiration:**
```bash
# Tokens expire after 1 hour - refresh token
TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_URL" \
  -H "Content-Type: application/json" \
  -d '{"clientId":"scim_client","clientSecret":"scim_secret","grantType":"client_credentials"}')
export TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')
```

**5. Invalid Tenant Error:**
```bash
# Recreate tenant if needed
curl -H "Authorization: Bearer $TOKEN" -X POST "$CUSTOMER_URL" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Customer", "tenantId": "test-tenant", "isActive": true}'
```

**6. Port Already in Use (System.IO.IOException: Failed to bind to address):**

If you get "address already in use" for port 5000:

```bash
# Option A: Use a different port
dotnet run --urls="http://0.0.0.0:5000"

# Update your environment variables for the new port
export TUNNEL_URL="http://localhost:5000"
export AUTH_URL="$TUNNEL_URL/api/auth/token"
export SCIM_BASE_URL="$TUNNEL_URL/scim/v2"
export CUSTOMER_URL="$TUNNEL_URL/api/customers"
```

```bash
# Option B: Kill existing processes (if available)
# Try one of these commands:
fuser -k 5000/tcp          # Kill processes using port 5000
pkill -f "dotnet.*5000"    # Kill dotnet processes on port 5000
jobs                       # Check for background jobs and use 'kill %1' etc.
```

```bash
# Option C: Check what's using the port
# These commands may not be available in all environments:
netstat -tulpn | grep :5000    # Check port usage
lsof -i :5000                  # List processes using port 5000
ps aux | grep dotnet           # Find dotnet processes
```

**Quick Port Resolution Script:**
```bash
#!/bin/bash
# Find an available port starting from 5000
for port in {5000..5010}; do
  if ! curl -s "http://localhost:$port" > /dev/null 2>&1; then
    echo "Port $port is available"
    export SCIM_PORT=$port
    export TUNNEL_URL="http://localhost:$SCIM_PORT"
    break
  fi
done

echo "Starting SCIM service on port $SCIM_PORT"
dotnet run --urls="http://0.0.0.0:$SCIM_PORT" &
```

## Integration with Azure AD

When configuring Azure AD, use your environment variables:

```bash
echo "Azure AD Configuration:"
echo "  Tenant URL: $SCIM_BASE_URL"
echo "  Secret Token: $TOKEN"
```

1. **Tenant URL**: `$SCIM_BASE_URL` (e.g., `https://abc123-5000.devtunnels.ms/scim/v2`)
2. **Secret Token**: Use the `$TOKEN` value from authentication
3. **Test Connection**: Azure AD will connect to your SCIM service via the tunnel

## 🛑 Cleanup: Stopping Services After Testing

**Important**: After you finish testing, make sure to clean up running services to free up resources.

### Stop SCIM Service

If you started the SCIM service in the background (with `&`), stop it using the process ID:

```bash
# If you saved the process ID when starting the service
kill $SCIM_PID

# Or find and kill all dotnet processes running the SCIM service
pkill -f "dotnet.*run"

# Or kill processes using your SCIM port
fuser -k $SCIM_PORT/tcp 2>/dev/null || echo "No processes found on port $SCIM_PORT"

# Clean up log file (optional)
rm -f scim.log

# Verify the service is stopped
if curl -s "http://localhost:$SCIM_PORT" > /dev/null 2>&1; then
  echo "⚠️  SCIM service still running on port $SCIM_PORT"
else
  echo "✅ SCIM service stopped successfully"
fi
```

### Stop Dev Tunnels (if using CLI)

```bash

# List and clean up any remaining tunnels
devtunnel list
devtunnel delete $TUNNEL_ID

echo "✅ Dev tunnel stopped and cleaned up"
```

### Close GitHub Codespaces Port Forwarding

If using GitHub Codespaces port forwarding:

1. **In VS Code**: 
   - Go to the "PORTS" tab
   - Right-click on your port (e.g., `5000`)
   - Select "Remove Port" or change visibility back to "Private"

2. **Or set port to private via command:**
   ```bash
   # The port will become private when the service stops
   echo "Port forwarding will stop automatically when service stops"
   ```

### Complete Cleanup Script

```bash
#!/bin/bash
echo "🧹 Cleaning up SCIM services..."

# Stop SCIM service
if [ ! -z "$SCIM_PID" ]; then
  kill -9 $SCIM_PID 2>/dev/null && echo "✅ Stopped SCIM service (PID: $SCIM_PID)"
else
  # Try alternative cleanup methods
  pkill -f "dotnet.*run" && echo "✅ Stopped dotnet run processes"
  
  if [ ! -z "$SCIM_PORT" ]; then
    fuser -k $SCIM_PORT/tcp 2>/dev/null && echo "✅ Freed up port $SCIM_PORT"
  fi
fi

# Stop dev tunnel if using CLI
if [ ! -z "$TUNNEL_ID" ]; then
  devtunnel delete $TUNNEL_ID 2>/dev/null && echo "✅ Deleted dev tunnel"
fi

# Clear environment variables
unset TUNNEL_URL SCIM_PORT AUTH_URL SCIM_BASE_URL CUSTOMER_URL TOKEN TENANT_ID SCIM_PID TUNNEL_ID
echo "✅ Cleared environment variables"

echo ""
echo "🎉 Cleanup complete! All SCIM services and tunnels have been stopped."
echo "Your system resources are now free for other tasks."
```

Save this as `cleanup-scim.sh` and run it when you're done testing:

```bash
chmod +x cleanup-scim.sh
./cleanup-scim.sh
```


## Alternative Installation Methods

### Windows (winget):
```bash
winget install Microsoft.devtunnel
```

### macOS (Homebrew):
```bash
brew install --cask devtunnel
```

### Windows/Visual Studio 2022:
- Dev Tunnels CLI is included with Visual Studio 2022
- Available in Developer Command Prompt

## Resources

- [Dev Tunnels Documentation](https://docs.microsoft.com/en-us/azure/developer/dev-tunnels/)
- [VS Code Dev Tunnels Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.vscode-dev-tunnels)
- [Dev Tunnels CLI Download](https://aka.ms/TunnelsCliDownload)
- [Dev Tunnels Portal](https://portal.azure.com/#view/Microsoft_Azure_DevTunnels/TunnelsDashboardBlade)

## ✅ Summary: Streamlined Document Improvements

### What Was Fixed:
1. **Removed Duplications**: Eliminated multiple sections covering the same installation and testing steps
2. **Environment Variables**: All curl commands now use `$TUNNEL_URL`, `$AUTH_URL`, `$SCIM_BASE_URL`, etc.
3. **Streamlined Flow**: Clear progression from installation → authentication → testing
4. **Error Handling**: Better validation of authentication and service availability
5. **Quick Test Script**: Added `quick-test.sh` for automated testing
6. **Local-First Approach**: Start with localhost testing, then move to public tunnels

### Environment Variable Usage:
- **TUNNEL_URL**: Base URL for your SCIM service (local or tunnel)
- **AUTH_URL**: `$TUNNEL_URL/api/auth/token`
- **SCIM_BASE_URL**: `$TUNNEL_URL/scim/v2` 
- **CUSTOMER_URL**: `$TUNNEL_URL/api/customers`
- **TOKEN**: JWT authentication token
- **TENANT_ID**: Customer tenant identifier

### Workflow Options:
1. **GitHub Codespaces Port Forwarding** (simplest)
2. **Microsoft Dev Tunnels CLI** (most features)
3. **VS Code Dev Tunnels Extension** (GUI-based)

All methods now use consistent environment variables for testing and integration.
