#!/bin/bash

# Simple test script to verify SCIM PATCH functionality for multi-valued attributes
BASE_URL="http://localhost:5000"

echo "🚀 Testing SCIM PATCH functionality for multi-valued attributes..."

# First, create a minimal user
echo "📝 Creating a minimal user..."
CREATE_RESPONSE=$(curl -s -X POST "$BASE_URL/scim/v2/Users" \
  -H "Content-Type: application/scim+json" \
  -H "X-Customer-Id: test-customer" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "testuser@example.com",
    "active": true,
    "name": {
      "givenName": "Test",
      "familyName": "User"
    }
  }')

echo "Create response: $CREATE_RESPONSE"

# Extract user ID
USER_ID=$(echo $CREATE_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)

if [ -z "$USER_ID" ]; then
  echo "❌ Failed to create user"
  exit 1
fi

echo "✅ Created user with ID: $USER_ID"

# Test 1: Add roles to the user
echo "📝 Adding roles via PATCH..."
PATCH1_RESPONSE=$(curl -s -X PATCH "$BASE_URL/scim/v2/Users/$USER_ID" \
  -H "Content-Type: application/scim+json" \
  -H "X-Customer-Id: test-customer" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "roles",
        "value": [
          {
            "value": "admin",
            "display": "Administrator",
            "type": "system"
          },
          {
            "value": "user",
            "display": "Regular User", 
            "type": "application"
          }
        ]
      }
    ]
  }')

echo "Patch 1 response: $PATCH1_RESPONSE"

# Test 2: Add more roles with a filter
echo "📝 Adding additional role with filter via PATCH..."
PATCH2_RESPONSE=$(curl -s -X PATCH "$BASE_URL/scim/v2/Users/$USER_ID" \
  -H "Content-Type: application/scim+json" \
  -H "X-Customer-Id: test-customer" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "roles[type eq \"application\"]",
        "value": {
          "primary": "true"
        }
      }
    ]
  }')

echo "Patch 2 response: $PATCH2_RESPONSE"

# Test 3: Add emails
echo "📝 Adding emails via PATCH..."
PATCH3_RESPONSE=$(curl -s -X PATCH "$BASE_URL/scim/v2/Users/$USER_ID" \
  -H "Content-Type: application/scim+json" \
  -H "X-Customer-Id: test-customer" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "add",
        "path": "emails",
        "value": [
          {
            "value": "work@example.com",
            "type": "work",
            "primary": true
          },
          {
            "value": "personal@example.com",
            "type": "home"
          }
        ]
      }
    ]
  }')

echo "Patch 3 response: $PATCH3_RESPONSE"

# Final: Get the user to verify all changes persisted
echo "📝 Getting final user state..."
FINAL_RESPONSE=$(curl -s -X GET "$BASE_URL/scim/v2/Users/$USER_ID" \
  -H "X-Customer-Id: test-customer")

echo "🔍 Final user state:"
echo $FINAL_RESPONSE | python3 -m json.tool 2>/dev/null || echo $FINAL_RESPONSE

# Check if roles and emails are present
ROLES_COUNT=$(echo $FINAL_RESPONSE | grep -o '"roles":\[[^]]*\]' | grep -o '"value":' | wc -l)
EMAILS_COUNT=$(echo $FINAL_RESPONSE | grep -o '"emails":\[[^]]*\]' | grep -o '"value":' | wc -l)

echo ""
echo "📊 Summary:"
echo "  Roles found: $ROLES_COUNT"
echo "  Emails found: $EMAILS_COUNT"

if [ "$ROLES_COUNT" -ge 2 ] && [ "$EMAILS_COUNT" -ge 2 ]; then
  echo "✅ PATCH test successful - multi-valued attributes persisted correctly!"
else
  echo "❌ PATCH test failed - multi-valued attributes not persisted correctly"
  exit 1
fi
