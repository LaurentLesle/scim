#!/bin/bash

echo "🧪 Testing Group Creation with Invalid Member Attributes..."

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
echo "2️⃣ Testing Group creation with invalid member attribute 'type'..."
CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5000/Groups" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "displayName": "Test Group with Invalid Member",
    "externalId": "test-group-invalid-001",
    "members": [
      {
        "value": "user-123",
        "type": "untyped",
        "display": "Test User"
      }
    ],
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
  }')

echo "Create Group Response:"
echo "$CREATE_RESPONSE" | jq .

# Check for error
ERROR=$(echo "$CREATE_RESPONSE" | jq -r '.detail // empty' 2>/dev/null)
STATUS=$(echo "$CREATE_RESPONSE" | jq -r '.status // empty' 2>/dev/null)

echo ""
echo "3️⃣ Analyzing response..."
if [ -n "$ERROR" ] && [ "$STATUS" = "400" ]; then
    echo "✅ CORRECTLY REJECTED: $ERROR"
else
    echo "❌ SHOULD HAVE BEEN REJECTED but got status: $STATUS"
    echo "   Response: $CREATE_RESPONSE"
fi

echo ""
echo "4️⃣ Testing Group creation with valid member attributes only..."
CREATE_RESPONSE_VALID=$(curl -s -X POST "http://localhost:5000/Groups" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "displayName": "Test Group with Valid Member",
    "externalId": "test-group-valid-001",
    "members": [
      {
        "value": "user-456",
        "display": "Valid Test User"
      }
    ],
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
  }')

echo "Create Valid Group Response:"
echo "$CREATE_RESPONSE_VALID" | jq .

# Check for success
ERROR_VALID=$(echo "$CREATE_RESPONSE_VALID" | jq -r '.detail // empty' 2>/dev/null)
STATUS_VALID=$(echo "$CREATE_RESPONSE_VALID" | jq -r '.status // empty' 2>/dev/null)

if [ -z "$ERROR_VALID" ] && [ "$STATUS_VALID" != "400" ]; then
    echo "✅ CORRECTLY ALLOWED: Valid group creation succeeded"
else
    echo "❌ SHOULD HAVE WORKED but got error: $ERROR_VALID"
fi

echo ""
echo "📋 Group Member Schema Compliance Summary:"
echo "- Invalid member attributes rejected: $([ -n "$ERROR" ] && echo "✅ YES" || echo "❌ NO")"
echo "- Valid member attributes accepted: $([ -z "$ERROR_VALID" ] && echo "✅ YES" || echo "❌ NO")"

if [ -n "$ERROR" ] && [ -z "$ERROR_VALID" ]; then
    echo ""
    echo "🎉 CONCLUSION: Group member schema validation is working correctly"
else
    echo ""
    echo "⚠️ CONCLUSION: Group member schema validation needs improvement"
fi

echo ""
echo "🏁 Group member schema compliance test completed"
