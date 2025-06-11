#!/bin/bash

echo "Testing PATCH Group with unsupported member type filtering..."

# Get auth token
echo "Getting auth token..."
TOKEN=$(curl -s -X POST "http://localhost:5001/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "scim_client",
    "clientSecret": "scim_secret",
    "tenantId": "tenant1"
  }' | jq -r '.access_token // empty')

if [ -z "$TOKEN" ]; then
    echo "❌ Failed to get auth token"
    exit 1
fi

echo "✅ Got auth token"

# First, create a group
GROUP_ID=$(curl -s -X POST "http://localhost:5001/Groups" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
    "displayName": "Test Group for Error Testing",
    "externalId": "test-error-group"
  }' | jq -r '.id // empty')

if [ -z "$GROUP_ID" ]; then
    echo "❌ Failed to create test group"
    exit 1
fi

echo "✅ Created test group with ID: $GROUP_ID"

# Now test the PATCH with unsupported member type filtering
echo "Testing PATCH with unsupported member type filtering..."
PATCH_RESPONSE=$(curl -s -X PATCH "http://localhost:5001/Groups/$GROUP_ID" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "replace",
        "path": "members[type eq \"untyped\"].value",
        "value": "test-value"
      }
    ]
  }')

echo "Response:"
echo "$PATCH_RESPONSE" | jq . 2>/dev/null || echo "$PATCH_RESPONSE"

# Clean up
curl -s -X DELETE "http://localhost:5001/Groups/$GROUP_ID" \
  -H "Authorization: Bearer $TOKEN" > /dev/null

echo "✅ Test completed and group cleaned up"
