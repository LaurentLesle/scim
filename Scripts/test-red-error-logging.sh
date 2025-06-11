#!/bin/bash

echo "🧪 Testing Internal Server Error Red Logging with Test Endpoint..."

# Kill any existing SCIM processes
pkill -f "dotnet run"
sleep 2

# Start the SCIM service
echo "🚀 Starting SCIM service..."
cd /workspaces/scim
nohup dotnet run --urls http://localhost:5000 > scim.log 2>&1 &
SCIM_PID=$!
echo "Service started (PID: $SCIM_PID)"

# Wait for service to start
sleep 8

# Get a fresh token
echo "🔐 Getting authentication token..."
TOKEN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId": "scim_client", "clientSecret": "scim_secret", "tenantId": "tenant1"}')

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token' 2>/dev/null)
if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Failed to get token"
    echo "Full response: $TOKEN_RESPONSE"
    exit 1
fi

echo "✅ Got token: ${TOKEN:0:50}..."

# Test the endpoint that will definitely trigger an internal server error
echo ""
echo "1️⃣ Testing endpoint that triggers internal server error..."
ERROR_RESPONSE=$(curl -s -X GET "http://localhost:5000/Users/test-error" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json")

echo "Test Error Response:"
echo "$ERROR_RESPONSE" | jq .

echo ""
echo "🔍 Checking logs for red error messages..."
echo "Last 10 lines of scim.log:"
tail -10 scim.log

echo ""
echo "🎨 If you see red text with 🚨 INTERNAL SERVER ERROR above, the logging is working correctly!"
echo "🏁 Test completed"
