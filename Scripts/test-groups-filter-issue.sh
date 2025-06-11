#!/bin/bash

echo "🧪 Testing GET Groups with filter=Id..."

# Kill any existing SCIM processes on port 8080
pkill -f "dotnet run --urls http://localhost:5000"
sleep 2

# Start the SCIM service on port 8080
echo "🚀 Starting SCIM service on port 8080..."
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

echo "Token response: $TOKEN_RESPONSE"

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token' 2>/dev/null)
if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Failed to get token"
    echo "Full response: $TOKEN_RESPONSE"
    exit 1
fi

echo "✅ Got token: ${TOKEN:0:50}..."

# Test 1: Create a group first
echo ""
echo "1️⃣ Creating a test group..."
CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5000/Groups" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "displayName": "Test Group for Filtering",
    "externalId": "test-group-001",
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
  }')

echo "Create Response:"
echo "$CREATE_RESPONSE" | jq .

GROUP_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id' 2>/dev/null)
echo "Created Group ID: $GROUP_ID"

# Test 2: Now test GET Groups with filter
echo ""
echo "2️⃣ Testing GET Groups with filter=Id..."
FILTER_RESPONSE=$(curl -s -X GET "http://localhost:5000/Groups?filter=Id%20eq%20%22$GROUP_ID%22" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json")

echo "Filter Response:"
echo "$FILTER_RESPONSE" | jq .

# Test 3: Test with a non-existent ID
echo ""
echo "3️⃣ Testing GET Groups with filter=Id (non-existent ID)..."
FILTER_RESPONSE2=$(curl -s -X GET "http://localhost:5000/Groups?filter=Id%20eq%20%22non-existent-id%22" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json")

echo "Filter Response (non-existent):"
echo "$FILTER_RESPONSE2" | jq .

# Test 4: Test problematic filter that might trigger the error
echo ""
echo "4️⃣ Testing GET Groups with problematic filter..."
FILTER_RESPONSE3=$(curl -s -X GET "http://localhost:5000/Groups?filter=members%5Btype%20eq%20%22untyped%22%5D.value%20eq%20%22some-value%22" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json")

echo "Problematic Filter Response:"
echo "$FILTER_RESPONSE3" | jq .

echo ""
echo "🏁 Test completed"
