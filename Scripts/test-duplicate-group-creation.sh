#!/bin/bash

echo "🧪 Testing Duplicate Group Creation with Invalid Member Type..."

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
echo "2️⃣ Creating first group with members containing 'type' attribute..."
CREATE_RESPONSE1=$(curl -s -X POST "http://localhost:5000/Groups" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "displayName": "Duplicate Group Test",
    "externalId": "duplicate-group-001",
    "members": [
      {
        "value": "user-123",
        "type": "untyped",
        "display": "Test User"
      }
    ],
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
  }')

echo "First Group Creation Response:"
echo "$CREATE_RESPONSE1" | jq .

GROUP_ID1=$(echo "$CREATE_RESPONSE1" | jq -r '.id // empty' 2>/dev/null)
ERROR1=$(echo "$CREATE_RESPONSE1" | jq -r '.detail // empty' 2>/dev/null)

echo ""
echo "3️⃣ Creating duplicate group with same externalId..."
CREATE_RESPONSE2=$(curl -s -X POST "http://localhost:5000/Groups" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "displayName": "Duplicate Group Test",
    "externalId": "duplicate-group-001",
    "members": [
      {
        "value": "user-456",
        "type": "untyped",
        "display": "Another Test User"
      }
    ],
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
  }')

echo "Duplicate Group Creation Response:"
echo "$CREATE_RESPONSE2" | jq .

ERROR2=$(echo "$CREATE_RESPONSE2" | jq -r '.detail // empty' 2>/dev/null)
STATUS2=$(echo "$CREATE_RESPONSE2" | jq -r '.status // empty' 2>/dev/null)

echo ""
echo "4️⃣ Analyzing responses..."
echo "First group creation:"
if [ -n "$GROUP_ID1" ] && [ -z "$ERROR1" ]; then
    echo "✅ First group created successfully (ID: $GROUP_ID1)"
    echo "   Note: 'type' attribute was ignored during deserialization"
else
    echo "❌ First group creation failed: $ERROR1"
fi

echo ""
echo "Duplicate group creation:"
if [ -n "$ERROR2" ] && [ "$STATUS2" = "409" ]; then
    echo "✅ Duplicate correctly rejected: $ERROR2"
else
    echo "❌ Duplicate should have been rejected but got status: $STATUS2"
fi

echo ""
echo "🏁 Duplicate group creation test completed"
