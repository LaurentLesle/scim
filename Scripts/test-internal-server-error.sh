#!/bin/bash

echo "🧪 Testing Internal Server Error Red Logging..."

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

# Test a request that might cause an internal server error
# Try to trigger an error by sending malformed JSON to create user
echo ""
echo "1️⃣ Testing malformed JSON that might trigger internal server error..."
ERROR_RESPONSE=$(curl -s -X POST "http://localhost:5000/Users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{"invalid": "json", "missing": "required_fields"}')

echo "Error Response:"
echo "$ERROR_RESPONSE" | jq .

# Try another potential error - invalid filter
echo ""
echo "2️⃣ Testing invalid complex filter..."
ERROR_RESPONSE2=$(curl -s -X GET "http://localhost:5000/Users?filter=invalid.complex.filter.that.might.cause.error" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json")

echo "Filter Error Response:"
echo "$ERROR_RESPONSE2" | jq .

echo ""
echo "🔍 Checking logs for red error messages..."
echo "Last 20 lines of scim.log:"
tail -20 scim.log

echo ""
echo "🏁 Test completed - Check the console output above for red error messages"
