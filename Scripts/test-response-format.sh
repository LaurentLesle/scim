#!/bin/bash

echo "🧪 Testing SCIM Response Format Compliance"
echo "=========================================="

# Get auth token
echo "🔑 Getting authentication token..."
TOKEN=$(curl -s -X POST "http://localhost:5000/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username": "scim_client", "password": "scim_password"}' | jq -r '.token')

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
  echo "❌ Failed to get auth token"
  exit 1
fi

echo "✅ Token obtained: ${TOKEN:0:20}..."

# Test User Creation
echo ""
echo "👤 Testing User Creation Response Format"
echo "========================================"

USER_RESPONSE=$(curl -s -X POST "http://localhost:5000/scim/v2/Users" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "format.test.user",
    "name": {
      "givenName": "Format",
      "familyName": "Test"
    },
    "emails": [
      {
        "value": "format.test@example.com",
        "primary": true
      }
    ],
    "active": true
  }')

echo "📋 User Response Fields:"
echo "$USER_RESPONSE" | jq 'keys'

echo ""
echo "🔍 Checking for customerId field (should be absent):"
if echo "$USER_RESPONSE" | jq -e '.customerId' > /dev/null 2>&1; then
  echo "❌ FAIL: customerId found in response!"
  echo "$USER_RESPONSE" | jq '.customerId'
else
  echo "✅ PASS: customerId not found in response (SCIM compliant)"
fi

echo ""
echo "🔍 Checking for SCIM required fields:"
echo "- schemas: $(echo "$USER_RESPONSE" | jq -e '.schemas' > /dev/null && echo "✅ Present" || echo "❌ Missing")"
echo "- id: $(echo "$USER_RESPONSE" | jq -e '.id' > /dev/null && echo "✅ Present" || echo "❌ Missing")"
echo "- userName: $(echo "$USER_RESPONSE" | jq -e '.userName' > /dev/null && echo "✅ Present" || echo "❌ Missing")"
echo "- meta: $(echo "$USER_RESPONSE" | jq -e '.meta' > /dev/null && echo "✅ Present" || echo "❌ Missing")"

echo ""
echo "📝 Full User Response:"
echo "$USER_RESPONSE" | jq '.'

# Test Group Creation
echo ""
echo "👥 Testing Group Creation Response Format"
echo "========================================"

GROUP_RESPONSE=$(curl -s -X POST "http://localhost:5000/scim/v2/Groups" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
    "displayName": "Format Test Group"
  }')

echo "📋 Group Response Fields:"
echo "$GROUP_RESPONSE" | jq 'keys'

echo ""
echo "🔍 Checking for customerId field (should be absent):"
if echo "$GROUP_RESPONSE" | jq -e '.customerId' > /dev/null 2>&1; then
  echo "❌ FAIL: customerId found in response!"
  echo "$GROUP_RESPONSE" | jq '.customerId'
else
  echo "✅ PASS: customerId not found in response (SCIM compliant)"
fi

echo ""
echo "🔍 Checking for SCIM required fields:"
echo "- schemas: $(echo "$GROUP_RESPONSE" | jq -e '.schemas' > /dev/null && echo "✅ Present" || echo "❌ Missing")"
echo "- id: $(echo "$GROUP_RESPONSE" | jq -e '.id' > /dev/null && echo "✅ Present" || echo "❌ Missing")"
echo "- displayName: $(echo "$GROUP_RESPONSE" | jq -e '.displayName' > /dev/null && echo "✅ Present" || echo "❌ Missing")"
echo "- meta: $(echo "$GROUP_RESPONSE" | jq -e '.meta' > /dev/null && echo "✅ Present" || echo "❌ Missing")"

echo ""
echo "📝 Full Group Response:"
echo "$GROUP_RESPONSE" | jq '.'

echo ""
echo "🎉 SCIM Response Format Testing Complete!"
