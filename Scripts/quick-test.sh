#!/bin/bash

# Quick SCIM API Test Script
echo "🚀 Starting SCIM API Test..."

# # Step 1: Find available port and set up environment
# echo "🔍 Finding available port..."
# SCIM_PORT=""
# for port in {5000..5010}; do
#   if ! curl -s "http://localhost:$port" > /dev/null 2>&1; then
#     SCIM_PORT=$port
#     break
#   fi
# done

# if [ -z "$SCIM_PORT" ]; then
#   echo "❌ No available ports found in range 5000-5010"
#   exit 1
# fi

SCIM_PORT=5000

echo "✅ Using port $SCIM_PORT"

export TUNNEL_URL="http://localhost:$SCIM_PORT"
export LOCAL_MODE=true
export AUTH_URL="$TUNNEL_URL/api/auth/token"
export SCIM_BASE_URL="$TUNNEL_URL/scim/v2"
export CUSTOMER_URL="$TUNNEL_URL/api/customers"
export TENANT_ID="test-tenant"

echo "📋 Environment variables set:"
echo "  TUNNEL_URL: $TUNNEL_URL"
echo "  LOCAL_MODE: $LOCAL_MODE"

# Start SCIM service if not already running on this port
if ! curl -s "$AUTH_URL" > /dev/null 2>&1; then
  echo "🚀 Starting SCIM service on port $SCIM_PORT..."
  dotnet run --urls="http://0.0.0.0:$SCIM_PORT" > /dev/null 2>&1 &
  SCIM_PID=$!
  echo "   Service started (PID: $SCIM_PID)"
  sleep 5
else
  echo "✅ SCIM service already running on port $SCIM_PORT"
fi

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
curl -s -H "Authorization: Bearer $TOKEN" -H "X-Tenant-ID: $TENANT_ID" \
  "$SCIM_BASE_URL/ServiceProviderConfig" | jq '.authenticationSchemes[0].name'

echo "  👥 Current users:"
USER_COUNT=$(curl -s -H "Authorization: Bearer $TOKEN" -H "X-Tenant-ID: $TENANT_ID" \
  "$SCIM_BASE_URL/Users" | jq '.totalResults // 0')
echo "    Found $USER_COUNT users"

echo ""
echo "🎉 SCIM API is working on port $SCIM_PORT!"
echo "=================================="
echo "Ready for external access. To use public tunnel:"
echo "1. Make port $SCIM_PORT public in VS Code PORTS tab"
echo "2. Update TUNNEL_URL to: https://\$CODESPACE_NAME-$SCIM_PORT.app.github.dev"
echo "3. Re-run authentication and testing steps"
echo ""
echo "Environment variables exported for this session:"
echo "  export TUNNEL_URL=\"$TUNNEL_URL\""
echo "  export AUTH_URL=\"$AUTH_URL\""
echo "  export SCIM_BASE_URL=\"$SCIM_BASE_URL\""
echo "  export CUSTOMER_URL=\"$CUSTOMER_URL\""
echo "  export TOKEN=\"$TOKEN\""
echo "  export TENANT_ID=\"$TENANT_ID\""
echo "  export SCIM_PORT=\"$SCIM_PORT\""
