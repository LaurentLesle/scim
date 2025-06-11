#!/bin/bash

echo "🧪 Testing Email Display PATCH Operation..."

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
echo "2️⃣ Creating a test user with email..."
CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5000/Users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "userName": "testuser.emaildisplay@example.com",
    "displayName": "Test User Email Display",
    "emails": [
      {
        "value": "testuser.work@company.com",
        "type": "work",
        "primary": true,
        "display": "Work Email"
      }
    ],
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"]
  }')

USER_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id' 2>/dev/null)

if [ "$USER_ID" = "null" ] || [ -z "$USER_ID" ]; then
    echo "❌ Failed to create user"
    echo "Response: $CREATE_RESPONSE"
    exit 1
fi

echo "✅ Created user with ID: $USER_ID"

echo ""
echo "3️⃣ Testing PATCH operation to update email display..."
PATCH_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "replace",
        "path": "emails[type eq \"work\"].display",
        "value": "Updated Work Email Display"
      }
    ],
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"]
  }')

echo "PATCH Response:"
echo "$PATCH_RESPONSE" | jq .

echo ""
echo "4️⃣ Verifying the patch was applied..."
GET_RESPONSE=$(curl -s -X GET "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json")

UPDATED_DISPLAY=$(echo "$GET_RESPONSE" | jq -r '.emails[] | select(.type == "work") | .display' 2>/dev/null)

if [ "$UPDATED_DISPLAY" = "Updated Work Email Display" ]; then
    echo "✅ Email display successfully updated to: $UPDATED_DISPLAY"
else
    echo "❌ Email display was not updated correctly. Current value: $UPDATED_DISPLAY"
fi

echo ""
echo "🏁 Test completed"
