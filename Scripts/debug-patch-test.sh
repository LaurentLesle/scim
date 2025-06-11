#!/bin/bash

# Debug PATCH operation - test if data is actually being saved to database

# Set up variables
BASE_URL="https://upgraded-guacamole-5gpx6g7jqp6h7j47-5000.app.github.dev"
USERNAME="testuser_debug"
EMAIL_ADDR="test@debug.com"

echo "🔍 Debug PATCH Test - Checking if data persists in database"
echo "=================================================="

# Get token
echo "Getting access token..."
TOKEN_RESPONSE=$(curl -s -X POST "${BASE_URL}/oauth/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=test-client&client_secret=test-secret&scope=scim")

if [ $? -ne 0 ]; then
    echo "❌ Failed to get token"
    exit 1
fi

TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')
if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Failed to extract token from response: $TOKEN_RESPONSE"
    exit 1
fi

echo "✅ Got token: ${TOKEN:0:20}..."

# Create a user first
echo ""
echo "Creating user..."
CREATE_RESPONSE=$(curl -s -X POST "${BASE_URL}/scim/v2/Users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "'$USERNAME'",
    "name": {
      "givenName": "Debug",
      "familyName": "User"
    },
    "emails": [],
    "active": true
  }')

USER_ID=$(echo $CREATE_RESPONSE | jq -r '.id')
if [ "$USER_ID" = "null" ] || [ -z "$USER_ID" ]; then
    echo "❌ Failed to create user: $CREATE_RESPONSE"
    exit 1
fi

echo "✅ Created user with ID: $USER_ID"

# Apply PATCH operation
echo ""
echo "Applying PATCH operation..."
PATCH_RESPONSE=$(curl -s -X PATCH "${BASE_URL}/scim/v2/Users/${USER_ID}" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "emails[primary eq \"true\"].value",
        "value": "'$EMAIL_ADDR'"
      }
    ]
  }')

echo "PATCH Response:"
echo "$PATCH_RESPONSE" | jq '.'

# Get the user again to see if the data persisted
echo ""
echo "Getting user after PATCH..."
GET_RESPONSE=$(curl -s -X GET "${BASE_URL}/scim/v2/Users/${USER_ID}" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json")

echo "GET Response after PATCH:"
echo "$GET_RESPONSE" | jq '.'

# Compare emails arrays
echo ""
echo "Comparing emails arrays:"
echo "PATCH response emails: $(echo $PATCH_RESPONSE | jq '.emails')"
echo "GET response emails: $(echo $GET_RESPONSE | jq '.emails')"

# Clean up
echo ""
echo "Cleaning up user..."
curl -s -X DELETE "${BASE_URL}/scim/v2/Users/${USER_ID}" \
  -H "Authorization: Bearer $TOKEN" > /dev/null

echo "✅ Test completed"
