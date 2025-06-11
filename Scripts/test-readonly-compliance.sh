#!/bin/bash

echo "🧪 Testing SCIM Compliance - Verifying emails[].display is READ-ONLY..."

# Start the SCIM service if not running
if ! curl -s http://localhost:5000/Schemas >/dev/null 2>&1; then
    echo "🚀 Starting SCIM service..."
    cd /workspaces/scim
    nohup dotnet run --urls http://localhost:5000 > scim.log 2>&1 &
    sleep 8
fi

echo ""
echo "1️⃣ Verifying Schema Definition shows display as READ-ONLY..."
SCHEMA_RESPONSE=$(curl -s http://localhost:5000/Schemas/urn:ietf:params:scim:schemas:core:2.0:User)

DISPLAY_ATTR=$(echo "$SCHEMA_RESPONSE" | jq '.attributes[] | select(.name == "emails") | .subAttributes[] | select(.name == "display")')

if [ -n "$DISPLAY_ATTR" ]; then
    echo "✅ Display sub-attribute found in emails schema definition"
    MUTABILITY=$(echo "$DISPLAY_ATTR" | jq -r '.mutability')
    echo "   Mutability: $MUTABILITY"
    if [ "$MUTABILITY" = "readOnly" ]; then
        echo "✅ Correctly marked as readOnly"
    else
        echo "❌ Should be marked as readOnly but is: $MUTABILITY"
    fi
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
    "userName": "readonly.test@example.com", 
    "displayName": "Read Only Test User",
    "emails": [
      {
        "value": "work@company.com",
        "type": "work",
        "primary": true
      }
    ],
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"]
  }')

USER_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id' 2>/dev/null)
echo "✅ Created user: $USER_ID"

echo ""
echo "4️⃣ Testing PATCH to update email display (SHOULD be REJECTED)..."

# Test 1: Try to replace email display - should fail
PATCH_RESPONSE1=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "replace",
        "path": "emails[type eq \"work\"].display",
        "value": "Should Not Work"
      }
    ],
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"]
  }')

echo "Replace display result:"
echo "$PATCH_RESPONSE1" | jq . | head -20

# Check for error (should be an error)
ERROR1=$(echo "$PATCH_RESPONSE1" | jq -r '.detail // empty' 2>/dev/null)
STATUS1=$(echo "$PATCH_RESPONSE1" | jq -r '.status // empty' 2>/dev/null)

if [ -n "$ERROR1" ] && [ "$STATUS1" = "400" ]; then
    echo "✅ CORRECTLY REJECTED: $ERROR1"
else
    echo "❌ SHOULD HAVE BEEN REJECTED but got status: $STATUS1"
fi

echo ""
echo "5️⃣ Testing PATCH to update phoneNumbers display (SHOULD be REJECTED)..."

# Test 2: Try to replace phone display - should also fail
PATCH_RESPONSE2=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "replace",
        "path": "phoneNumbers[type eq \"work\"].display", 
        "value": "Should Not Work Either"
      }
    ],
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"]
  }')

echo "Replace phone display result:"
echo "$PATCH_RESPONSE2" | jq . | head -20

# Check for error (should be an error)
ERROR2=$(echo "$PATCH_RESPONSE2" | jq -r '.detail // empty' 2>/dev/null)
STATUS2=$(echo "$PATCH_RESPONSE2" | jq -r '.status // empty' 2>/dev/null)

if [ -n "$ERROR2" ] && [ "$STATUS2" = "400" ]; then
    echo "✅ CORRECTLY REJECTED: $ERROR2"
else
    echo "❌ SHOULD HAVE BEEN REJECTED but got status: $STATUS2"
fi

echo ""
echo "6️⃣ Testing PATCH to update email value (SHOULD work)..."

# Test 3: Replace email value - should work
PATCH_RESPONSE3=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
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
echo "$PATCH_RESPONSE3" | jq . | head -20

# Check for success (should not be an error)
ERROR3=$(echo "$PATCH_RESPONSE3" | jq -r '.detail // empty' 2>/dev/null)
STATUS3=$(echo "$PATCH_RESPONSE3" | jq -r '.status // empty' 2>/dev/null)

if [ -z "$ERROR3" ] && [ "$STATUS3" != "400" ]; then
    echo "✅ CORRECTLY ALLOWED: email value update works"
else
    echo "❌ SHOULD HAVE WORKED but got error: $ERROR3"
fi

echo ""
echo "📋 SCIM Read-Only Compliance Summary:"
echo "- Schema marks display as readOnly: $([ "$MUTABILITY" = "readOnly" ] && echo "✅ YES" || echo "❌ NO")"
echo "- PATCH emails[].display rejected: $([ -n "$ERROR1" ] && echo "✅ YES" || echo "❌ NO")"
echo "- PATCH phoneNumbers[].display rejected: $([ -n "$ERROR2" ] && echo "✅ YES" || echo "❌ NO")"
echo "- PATCH emails[].value still works: $([ -z "$ERROR3" ] && echo "✅ YES" || echo "❌ NO")"

if [ "$MUTABILITY" = "readOnly" ] && [ -n "$ERROR1" ] && [ -n "$ERROR2" ] && [ -z "$ERROR3" ]; then
    echo ""
    echo "🎉 CONCLUSION: SCIM 2.0 read-only compliance for display attributes is CORRECTLY IMPLEMENTED"
else
    echo ""
    echo "⚠️ CONCLUSION: Some compliance issues remain"
fi

echo ""
echo "🏁 Read-only compliance test completed"
