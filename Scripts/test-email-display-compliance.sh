#!/bin/bash

echo "🧪 Testing SCIM Compliance for Email Display Attribute..."

# Start the SCIM service if not running
if ! curl -s http://localhost:5000/Schemas >/dev/null 2>&1; then
    echo "🚀 Starting SCIM service..."
    cd /workspaces/scim
    nohup dotnet run --urls http://localhost:5000 > scim.log 2>&1 &
    sleep 8
fi

echo ""
echo "1️⃣ Verifying Schema Definition for Email Display..."
SCHEMA_RESPONSE=$(curl -s http://localhost:5000/Schemas/urn:ietf:params:scim:schemas:core:2.0:User)

DISPLAY_ATTR=$(echo "$SCHEMA_RESPONSE" | jq '.attributes[] | select(.name == "emails") | .subAttributes[] | select(.name == "display")')

if [ -n "$DISPLAY_ATTR" ]; then
    echo "✅ Display sub-attribute found in emails schema definition"
    echo "$DISPLAY_ATTR" | jq .
else
    echo "❌ Display sub-attribute missing from emails schema definition"
fi

echo ""
echo "2️⃣ Getting authentication token..."
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
echo "3️⃣ Creating user with work email..."
CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5000/Users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "userName": "compliance.test@example.com",
    "displayName": "Compliance Test User",
    "emails": [
      {
        "value": "work@company.com",
        "type": "work",
        "primary": true,
        "display": "Original Work Email"
      }
    ],
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"]
  }')

USER_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id' 2>/dev/null)
echo "✅ Created user: $USER_ID"

echo ""
echo "4️⃣ Testing PATCH to update email display (SHOULD be supported)..."

# Test 1: Replace email display
PATCH_RESPONSE1=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
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

echo "Replace display result:"
echo "$PATCH_RESPONSE1" | jq . | head -20

# Check for error
ERROR1=$(echo "$PATCH_RESPONSE1" | jq -r '.detail // empty' 2>/dev/null)
if [ -n "$ERROR1" ]; then
    echo "❌ Error occurred: $ERROR1"
else
    echo "✅ Replace operation succeeded"
fi

echo ""
echo "5️⃣ Testing PATCH to update email value (for comparison)..."

# Test 2: Replace email value 
PATCH_RESPONSE2=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "replace", 
        "path": "emails[type eq \"work\"].value",
        "value": "updated.work@company.com"
      }
    ],
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"]
  }')

echo "Replace value result:"
echo "$PATCH_RESPONSE2" | jq . | head -20

# Check for error
ERROR2=$(echo "$PATCH_RESPONSE2" | jq -r '.detail // empty' 2>/dev/null)
if [ -n "$ERROR2" ]; then
    echo "❌ Error occurred: $ERROR2"
else
    echo "✅ Replace value operation succeeded"
fi

echo ""
echo "6️⃣ Final verification - retrieving user..."
GET_RESPONSE=$(curl -s -X GET "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json")

echo "Final user emails:"
echo "$GET_RESPONSE" | jq '.emails'

echo ""
echo "📋 SCIM Compliance Summary:"
echo "- Schema defines display sub-attribute for emails: $([ -n "$DISPLAY_ATTR" ] && echo "✅ YES" || echo "❌ NO")"
echo "- PATCH emails[type eq \"work\"].display works: $([ -z "$ERROR1" ] && echo "✅ YES" || echo "❌ NO")"
echo "- PATCH emails[type eq \"work\"].value works: $([ -z "$ERROR2" ] && echo "✅ YES" || echo "❌ NO")"

if [ -z "$ERROR1" ] && [ -n "$DISPLAY_ATTR" ]; then
    echo ""
    echo "🎉 CONCLUSION: emails[type eq \"work\"].display IS supported according to SCIM 2.0 specification"
else
    echo ""
    echo "⚠️ CONCLUSION: There may be an issue with the implementation or configuration"
fi

echo ""
echo "🏁 Compliance test completed"
