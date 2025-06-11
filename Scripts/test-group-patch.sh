#!/bin/bash

# Test PATCH Group displayName update
echo "🧪 Testing PATCH Group displayName update..."

# First, create a group
echo "1️⃣ Creating a test group..."
CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5000/scim/v2/Groups" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer test-token" \
  -d '{
    "displayName": "ORIGINAL_NAME",
    "externalId": "test-group-patch-001",
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
  }')

echo "Create Response:"
echo "$CREATE_RESPONSE" | jq .

# Extract the group ID
GROUP_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id')
echo "Created Group ID: $GROUP_ID"

if [ "$GROUP_ID" == "null" ] || [ -z "$GROUP_ID" ]; then
    echo "❌ Failed to create group"
    exit 1
fi

# Now test the PATCH operation to update displayName
echo ""
echo "2️⃣ Patching group displayName..."
PATCH_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/scim/v2/Groups/$GROUP_ID" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer test-token" \
  -d '{
    "Operations": [
      {
        "op": "replace",
        "value": {
          "displayName": "UPDATED_NAME"
        }
      }
    ],
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"]
  }')

echo "Patch Response:"
echo "$PATCH_RESPONSE" | jq .

# Check if displayName was updated
UPDATED_NAME=$(echo "$PATCH_RESPONSE" | jq -r '.displayName')
echo ""
echo "Expected: UPDATED_NAME"
echo "Actual: $UPDATED_NAME"

if [ "$UPDATED_NAME" == "UPDATED_NAME" ]; then
    echo "✅ PATCH Group displayName test PASSED!"
else
    echo "❌ PATCH Group displayName test FAILED!"
    echo "Group displayName was not updated correctly"
    exit 1
fi

echo ""
echo "3️⃣ Clean up - deleting test group..."
curl -s -X DELETE "http://localhost:5000/scim/v2/Groups/$GROUP_ID" \
  -H "Authorization: Bearer test-token"

echo "✅ Test completed successfully!"
