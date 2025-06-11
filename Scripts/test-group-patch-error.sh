#!/bin/bash

echo "🧪 Testing SCIM Group PATCH operation with unsupported member type..."

# Start the SCIM service if not running
if ! curl -s http://localhost:5000/Schemas >/dev/null 2>&1; then
    echo "🚀 Starting SCIM service..."
    cd /workspaces/scim
    nohup dotnet run --urls http://localhost:5000 > scim.log 2>&1 &
    sleep 8
fi

echo ""
echo "1️⃣ Getting authentication token..."
TOKEN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId": "scim_client", "clientSecret": "scim_secret", "tenantId": "tenant1"}')

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token' 2>/dev/null)

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Failed to get token"
    exit 1
fi

echo "✅ Got authentication token"

echo ""
echo "2️⃣ Creating a basic group..."
CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5000/Groups" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "displayName": "Test Group for PATCH",
    "externalId": "test-group-patch-001",
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
  }')

GROUP_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id' 2>/dev/null)
echo "✅ Created group: $GROUP_ID"

echo ""
echo "3️⃣ Testing PATCH operation with members[type eq \"untyped\"].value path..."
PATCH_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Groups/$GROUP_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "remove",
        "path": "members[type eq \"untyped\"].value"
      }
    ],
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"]
  }')

echo "PATCH Response:"
echo "$PATCH_RESPONSE" | jq .

# Check for specific error
ERROR=$(echo "$PATCH_RESPONSE" | jq -r '.detail // empty' 2>/dev/null)
STATUS=$(echo "$PATCH_RESPONSE" | jq -r '.status // empty' 2>/dev/null)

echo ""
echo "4️⃣ Analyzing the error response..."
if [ -n "$ERROR" ] && [ "$STATUS" = "400" ]; then
    echo "✅ CORRECTLY REJECTED with error: $ERROR"
    
    # Check if the error message matches the expected format
    if [[ "$ERROR" == *"members[type eq \"untyped\"].value for Group is not supported by the SCIM protocol"* ]]; then
        echo "✅ Error message matches expected format"
    else
        echo "❌ Error message format is different than expected"
    fi
else
    echo "❌ Expected error but got status: $STATUS"
fi

echo ""
echo "🏁 PATCH error reproduction test completed"
