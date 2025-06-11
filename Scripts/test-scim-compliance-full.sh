#!/bin/bash

# Test the exact SCIM compliance test case that was failing
# This replicates the failing test: "The value of emails[type eq "work"].primary is Missing from the fetched Resource"

echo "=== SCIM Compliance Test: Multi-valued Attributes PATCH ==="

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

# Create a user exactly like in the compliance test
echo "Creating test user..."
USER_RESPONSE=$(curl -s -X POST "http://localhost:5000/Users" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": [
      "urn:ietf:params:scim:schemas:core:2.0:User"
    ],
    "userName": "tristin@heathcote.us"
  }')

USER_ID=$(echo $USER_RESPONSE | jq -r '.id')
echo "✓ Created user with ID: $USER_ID"

# Execute the exact PATCH operations from the failing test
echo "Executing PATCH operations..."
PATCH_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "add",
        "path": "emails[type eq \"work\"].value",
        "value": "alessandra@jakubowski.us"
      },
      {
        "op": "add",
        "path": "emails[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].formatted",
        "value": "JPFMEGHDQWTY"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].streetAddress",
        "value": "929 Price Haven"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].locality",
        "value": "FEYIAUFYZHCT"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].region",
        "value": "JDLRAOVYLPKU"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].postalCode",
        "value": "si21 1mq"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].country",
        "value": "Ireland"
      },
      {
        "op": "add",
        "path": "phoneNumbers[type eq \"work\"].value",
        "value": "44-471-5001"
      },
      {
        "op": "add",
        "path": "phoneNumbers[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "add",
        "path": "phoneNumbers[type eq \"mobile\"].value",
        "value": "44-471-5001"
      },
      {
        "op": "add",
        "path": "phoneNumbers[type eq \"fax\"].value",
        "value": "44-471-5001"
      },
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].display",
        "value": "QRKSJCAXVGLM"
      },
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].value",
        "value": "WVZKBEMYRRHB"
      },
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].type",
        "value": "QJTRWNHGKOAZ"
      }
    ],
    "schemas": [
      "urn:ietf:params:scim:api:messages:2.0:PatchOp"
    ]
  }')

echo "Response:"
echo $PATCH_RESPONSE | jq '.'

# Check specific attributes that were failing in the compliance test
echo ""
echo "=== COMPLIANCE CHECKS ==="

# Check emails[type eq "work"].primary
EMAIL_PRIMARY=$(echo $PATCH_RESPONSE | jq -r '.emails[]? | select(.type == "work") | .primary // false')
if [ "$EMAIL_PRIMARY" == "true" ]; then
  echo "✅ emails[type eq \"work\"].primary: PASS (value: $EMAIL_PRIMARY)"
else
  echo "❌ emails[type eq \"work\"].primary: FAIL (value: $EMAIL_PRIMARY)"
fi

# Check addresses[type eq "work"].primary
ADDRESS_PRIMARY=$(echo $PATCH_RESPONSE | jq -r '.addresses[]? | select(.type == "work") | .primary // false')
if [ "$ADDRESS_PRIMARY" == "true" ]; then
  echo "✅ addresses[type eq \"work\"].primary: PASS (value: $ADDRESS_PRIMARY)"
else
  echo "❌ addresses[type eq \"work\"].primary: FAIL (value: $ADDRESS_PRIMARY)"
fi

# Check phoneNumbers[type eq "work"].primary
PHONE_PRIMARY=$(echo $PATCH_RESPONSE | jq -r '.phoneNumbers[]? | select(.type == "work") | .primary // false')
if [ "$PHONE_PRIMARY" == "true" ]; then
  echo "✅ phoneNumbers[type eq \"work\"].primary: PASS (value: $PHONE_PRIMARY)"
else
  echo "❌ phoneNumbers[type eq \"work\"].primary: FAIL (value: $PHONE_PRIMARY)"
fi

# Check roles[primary eq "True"] (note the filter uses primary, and we set primary=True on creation)
ROLE_COUNT=$(echo $PATCH_RESPONSE | jq -r '.roles[]? | select(.primary == true) | .value' | wc -l)
if [ "$ROLE_COUNT" -gt 0 ]; then
  echo "✅ roles[primary eq \"True\"]: PASS (found $ROLE_COUNT role(s) with primary=true)"
else
  echo "❌ roles[primary eq \"True\"]: FAIL (no roles with primary=true found)"
fi

# Cleanup
echo ""
echo "Cleaning up..."
curl -s -X DELETE "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" > /dev/null

echo "Done."
