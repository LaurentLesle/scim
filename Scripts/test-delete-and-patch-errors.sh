#!/bin/bash

echo "🧪 Testing DELETE Group operation and related PATCH scenarios..."

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

# Test 1: DELETE non-existent group (should return 404)
echo ""
echo "🗑️ Test 1: DELETE non-existent group..."
DELETE_RESPONSE=$(curl -s -X DELETE "http://localhost:5001/Groups/362b158b-b6ea-4d73-9342-bf49d84bebbf" \
  -H "Authorization: Bearer $TOKEN")

echo "DELETE non-existent group response:"
echo "$DELETE_RESPONSE" | jq . 2>/dev/null || echo "$DELETE_RESPONSE"

# Test 2: Create a group, then DELETE it (should return 204)
echo ""
echo "🗑️ Test 2: Create and DELETE group..."
GROUP_ID=$(curl -s -X POST "http://localhost:5001/Groups" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
    "displayName": "Test Group for Delete",
    "externalId": "test-delete-group"
  }' | jq -r '.id // empty')

if [ -z "$GROUP_ID" ]; then
    echo "❌ Failed to create test group for deletion"
    exit 1
fi

echo "✅ Created test group with ID: $GROUP_ID"

DELETE_RESPONSE2=$(curl -s -w "HTTP_STATUS:%{http_code}" -X DELETE "http://localhost:5001/Groups/$GROUP_ID" \
  -H "Authorization: Bearer $TOKEN")

echo "DELETE existing group response:"
echo "$DELETE_RESPONSE2"

# Test 3: Try various PATCH operations with unsupported paths
echo ""
echo "🔧 Test 3: PATCH with various unsupported member paths..."

# Create another group for PATCH testing
GROUP_ID2=$(curl -s -X POST "http://localhost:5001/Groups" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
    "displayName": "Test Group for PATCH",
    "externalId": "test-patch-group"
  }' | jq -r '.id // empty')

if [ -z "$GROUP_ID2" ]; then
    echo "❌ Failed to create test group for PATCH"
    exit 1
fi

echo "✅ Created test group for PATCH with ID: $GROUP_ID2"

# Test different unsupported member operations
echo ""
echo "🔧 Test 3a: PATCH replace with type filtering..."
PATCH_RESPONSE1=$(curl -s -X PATCH "http://localhost:5001/Groups/$GROUP_ID2" \
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

echo "PATCH replace with type filtering response:"
echo "$PATCH_RESPONSE1" | jq . 2>/dev/null || echo "$PATCH_RESPONSE1"

echo ""
echo "🔧 Test 3b: PATCH add with type filtering..."
PATCH_RESPONSE2=$(curl -s -X PATCH "http://localhost:5001/Groups/$GROUP_ID2" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "members[type eq \"user\"].value",
        "value": "test-user-id"
      }
    ]
  }')

echo "PATCH add with type filtering response:"
echo "$PATCH_RESPONSE2" | jq . 2>/dev/null || echo "$PATCH_RESPONSE2"

echo ""
echo "🔧 Test 3c: PATCH remove with type filtering..."
PATCH_RESPONSE3=$(curl -s -X PATCH "http://localhost:5001/Groups/$GROUP_ID2" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "remove",
        "path": "members[type eq \"untyped\"].value"
      }
    ]
  }')

echo "PATCH remove with type filtering response:"
echo "$PATCH_RESPONSE3" | jq . 2>/dev/null || echo "$PATCH_RESPONSE3"

# Clean up
curl -s -X DELETE "http://localhost:5001/Groups/$GROUP_ID2" \
  -H "Authorization: Bearer $TOKEN" > /dev/null

echo ""
echo "✅ All tests completed and groups cleaned up"
