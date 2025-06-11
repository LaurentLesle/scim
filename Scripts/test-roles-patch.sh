#!/bin/bash

# Test script to reproduce the roles PATCH issue
BASE_URL="http://localhost:5000"

echo "🔐 Getting authentication token..."
TOKEN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "scim_client",
    "clientSecret": "scim_secret",
    "grantType": "client_credentials",
    "tenantId": "test-tenant-id"
  }')

TOKEN=$(echo $TOKEN_RESPONSE | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "❌ Failed to get token"
  exit 1
fi

echo "✅ Token obtained"

echo "➕ Creating initial user..."
USER_RESPONSE=$(curl -s -X POST "$BASE_URL/scim/v2/Users" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Tenant-ID: test-tenant-id" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "rickey_torp@rohan.co.uk"
  }')

USER_ID=$(echo $USER_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)

if [ -z "$USER_ID" ]; then
  echo "❌ Failed to create user"
  echo "Response: $USER_RESPONSE"
  exit 1
fi

echo "✅ User created with ID: $USER_ID"

echo "🔧 Applying PATCH operations (same as in the user's request)..."
PATCH_RESPONSE=$(curl -s -X PATCH "$BASE_URL/scim/v2/Users/$USER_ID" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Tenant-ID: test-tenant-id" \
  -d '{
    "Operations": [
      {
        "op": "add",
        "path": "emails[type eq \"work\"].value",
        "value": "roberta_kuvalis@hayesmertz.com"
      },
      {
        "op": "add",
        "path": "emails[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].formatted",
        "value": "QQHYDJGFDRFX"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].streetAddress",
        "value": "7841 Goodwin Loaf"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].locality",
        "value": "UGWEYHVKKITS"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].region",
        "value": "JQQETQNHZCDZ"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].postalCode",
        "value": "ru86 0et"
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "add",
        "path": "addresses[type eq \"work\"].country",
        "value": "Uzbekistan"
      },
      {
        "op": "add",
        "path": "phoneNumbers[type eq \"work\"].value",
        "value": "22-377-3428"
      },
      {
        "op": "add",
        "path": "phoneNumbers[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "add",
        "path": "phoneNumbers[type eq \"mobile\"].value",
        "value": "22-377-3428"
      },
      {
        "op": "add",
        "path": "phoneNumbers[type eq \"fax\"].value",
        "value": "22-377-3428"
      },
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].display",
        "value": "MSSEWRRJREQY"
      },
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].value",
        "value": "CYDBGQIIERUP"
      },
      {
        "op": "add",
        "path": "roles[primary eq \"True\"].type",
        "value": "QRPQKXXKNVRS"
      },
      {
        "op": "add",
        "value": {
          "active": true,
          "displayName": "CDHQYEYIKBGB",
          "title": "LOATBSFQQPBF",
          "preferredLanguage": "nb-SJ",
          "name.givenName": "Adell",
          "name.familyName": "Lesley",
          "name.formatted": "Henderson",
          "name.middleName": "Guadalupe",
          "name.honorificPrefix": "Reinhold",
          "name.honorificSuffix": "Mathilde",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber": "DVJTEKOQVLLR",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department": "TEUFEQIRBMVQ",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter": "XAOYALZZMQDE",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization": "LARDLXQMBEZT",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division": "USYTEGEBXVTJ",
          "userType": "XXEMRJMTGJYS",
          "nickName": "EZUFFGHXIFHD",
          "locale": "ECRNKKFCCWRQ",
          "timezone": "Africa/Djibouti",
          "profileUrl": "UTISNQJTBOMN"
        }
      }
    ],
    "schemas": [
      "urn:ietf:params:scim:api:messages:2.0:PatchOp"
    ]
  }')

echo "📊 PATCH response:"
echo $PATCH_RESPONSE | jq '.'

echo ""
echo "🔍 Checking roles array:"
echo $PATCH_RESPONSE | jq '.roles'

echo ""
echo "🔍 Checking emails array:"
echo $PATCH_RESPONSE | jq '.emails'

echo ""
echo "🔍 Checking addresses array:"
echo $PATCH_RESPONSE | jq '.addresses'

echo ""
echo "🔍 Checking phoneNumbers array:"
echo $PATCH_RESPONSE | jq '.phoneNumbers'
