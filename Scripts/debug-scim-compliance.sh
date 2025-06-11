#!/bin/bash

# Debug SCIM User Creation and PATCH operations
# This will help us understand why the compliance test is still failing

echo "=== SCIM Compliance Debug Test ==="

# Get auth token
TOKEN_RESPONSE=$(curl -s -X POST "http://localhost:5000/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "scim_client",
    "clientSecret": "scim_secret",
    "grantType": "client_credentials"
  }')
ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')

if [ -z "$ACCESS_TOKEN" ] || [ "$ACCESS_TOKEN" == "null" ]; then
  echo "Failed to get access token"
  exit 1
fi

echo "✓ Got access token: ${ACCESS_TOKEN:0:20}..."

# Test 1: Create user exactly as compliance test might
echo ""
echo "=== Test 1: Create User ==="
USER_RESPONSE=$(curl -s -X POST "http://localhost:5000/Users" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": [
      "urn:ietf:params:scim:schemas:core:2.0:User"
    ],
    "userName": "debug@test.com"
  }')

USER_ID=$(echo $USER_RESPONSE | jq -r '.id')
echo "Created user ID: $USER_ID"
echo "Initial user response:"
echo $USER_RESPONSE | jq '.'

# Test 2: PATCH with individual operations
echo ""
echo "=== Test 2: Individual PATCH Operations ==="

echo "Operation 1: Add email value..."
PATCH1_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "emails[type eq \"work\"].value",
        "value": "work@example.com"
      }
    ]
  }')
echo "After adding email value:"
echo $PATCH1_RESPONSE | jq '.emails'

echo "Operation 2: Add email primary..."
PATCH2_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "emails[type eq \"work\"].primary",
        "value": true
      }
    ]
  }')
echo "After adding email primary:"
echo $PATCH2_RESPONSE | jq '.emails'

echo "Operation 3: Add role display (using primary filter)..."
PATCH3_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].display",
        "value": "Test Role Display"
      }
    ]
  }')
echo "After adding role display:"
echo $PATCH3_RESPONSE | jq '.roles'

echo "Operation 4: Add role value (using primary filter)..."
PATCH4_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].value",
        "value": "Test Role Value"
      }
    ]
  }')
echo "After adding role value:"
echo $PATCH4_RESPONSE | jq '.roles'

echo "Operation 5: Add role type (using primary filter)..."
PATCH5_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].type",
        "value": "Test Role Type"
      }
    ]
  }')
echo "After adding role type:"
echo $PATCH5_RESPONSE | jq '.roles'

# Test 3: Check current state
echo ""
echo "=== Test 3: Current User State ==="
CURRENT_USER=$(curl -s -X GET "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN")
echo "Current user:"
echo $CURRENT_USER | jq '.'

# Test 4: Check specific properties
echo ""
echo "=== Test 4: Property Checks ==="
EMAIL_PRIMARY=$(echo $CURRENT_USER | jq -r '.emails[]? | select(.type == "work") | .primary // "missing"')
echo "emails[type eq \"work\"].primary: $EMAIL_PRIMARY"

ROLE_WITH_PRIMARY=$(echo $CURRENT_USER | jq -r '.roles[]? | select(.primary == true)')
echo "roles[primary eq \"True\"]:"
echo $ROLE_WITH_PRIMARY | jq '.'

# Cleanup
echo ""
echo "Cleaning up..."
curl -s -X DELETE "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" > /dev/null

echo "Done."
