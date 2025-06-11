#!/bin/bash

# Test PATCH User - Add email with primary attribute
# This tests the specific failing case from the SCIM compliance test

echo "=== SCIM User PATCH Test: emails[type eq \"work\"].primary ==="

# Get auth token
TOKEN_RESPONSE=$(curl -s -X POST "http://localhost:5000/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "scim_client",
    "clientSecret": "scim_secret",
    "grantType": "client_credentials"
  }')
ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)

if [ -z "$ACCESS_TOKEN" ]; then
  echo "Failed to get access token"
  exit 1
fi

echo "✓ Got access token: ${ACCESS_TOKEN:0:20}..."

# Create a user first
echo "Creating test user..."
USER_RESPONSE=$(curl -s -X POST "http://localhost:5000/Users" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "test-patch-email@example.com"
  }')

USER_ID=$(echo $USER_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "✓ Created user with ID: $USER_ID"

# PATCH user with email primary attribute
echo "PATCH user with emails[type eq \"work\"].primary..."
PATCH_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "emails[type eq \"work\"].value",
        "value": "work@example.com"
      },
      {
        "op": "add",
        "path": "emails[type eq \"work\"].primary",
        "value": true
      }
    ]
  }')

echo "Response:"
echo $PATCH_RESPONSE | jq '.'

# Check if primary is present in emails
PRIMARY_COUNT=$(echo $PATCH_RESPONSE | jq -r '.emails[]? | select(.type == "work") | .primary // false' | grep -c "true" || echo "0")

if [ "$PRIMARY_COUNT" -gt 0 ]; then
  echo "✅ SUCCESS: emails[type eq \"work\"].primary is present and set to true"
else
  echo "❌ FAILED: emails[type eq \"work\"].primary is missing or not set to true"
  echo "Email object:"
  echo $PATCH_RESPONSE | jq '.emails[]? | select(.type == "work")'
fi

# Cleanup
echo "Cleaning up..."
curl -s -X DELETE "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" > /dev/null

echo "Done."
