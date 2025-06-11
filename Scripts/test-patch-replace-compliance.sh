#!/bin/bash

# Test PATCH Replace operations to verify SCIM compliance
# This replicates the specific test case that was mentioned

echo "=== SCIM PATCH Replace Test ==="

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

# Create a user with initial values (like the compliance test)
echo "Creating user with initial values..."
USER_RESPONSE=$(curl -s -X POST "http://localhost:5000/Users" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "active": true,
    "addresses": [
      {
        "type": "work",
        "formatted": "SMECGTNERCYB",
        "streetAddress": "48051 Wyman Crescent",
        "locality": "KZGMXRWUERYN",
        "region": "OPGOERCMMDMM",
        "postalCode": "sf9 2bi",
        "primary": true,
        "country": "Czech Republic"
      }
    ],
    "displayName": "XHLNMLCHCDKY",
    "emails": [
      {
        "type": "work",
        "value": "buster.pfannerstill@lockmanlesch.us",
        "primary": true
      }
    ],
    "locale": "EEQKCKVTLCGE",
    "name": {
      "givenName": "Marianna",
      "familyName": "Clarissa",
      "formatted": "Augusta",
      "middleName": "D'\''angelo",
      "honorificPrefix": "Felix",
      "honorificSuffix": "Verona"
    },
    "nickName": "DKYYDYXIFWAM",
    "phoneNumbers": [
      {
        "type": "work",
        "value": "18-569-6230",
        "primary": true
      },
      {
        "type": "mobile",
        "value": "18-569-6230"
      },
      {
        "type": "fax",
        "value": "18-569-6230"
      }
    ],
    "preferredLanguage": "sr-Latn-RS",
    "profileUrl": "PSEDIKWEYFMO",
    "roles": [
      {
        "primary": true,
        "display": "LMYZCPJGHNTQ",
        "value": "DVILABHTDNGE",
        "type": "DNLVKNPAJLNP"
      }
    ],
    "schemas": [
      "urn:ietf:params:scim:schemas:core:2.0:User",
      "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
    ],
    "timezone": "Africa/Libreville",
    "title": "MPNHXUBNKMMT",
    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
      "employeeNumber": "UVFGABTNSFTG",
      "department": "LGAWCIDHVMLI",
      "costCenter": "DOEHSQTDYUNN",
      "organization": "SPQQVVHWJIRG",
      "division": "AYOQREXHSPPI"
    },
    "userName": "alfreda_kulas@littlegutkowski.co.uk",
    "userType": "CQYGEYKHFCTJ"
  }')

USER_ID=$(echo $USER_RESPONSE | jq -r '.id')
echo "✓ Created user with ID: $USER_ID"

echo "Initial user state:"
echo $USER_RESPONSE | jq '{
  "emails": .emails,
  "name": .name,
  "addresses": .addresses,
  "phoneNumbers": .phoneNumbers,
  "roles": .roles,
  "enterprise": ."urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
}'

# Apply the exact PATCH operations from the failing test
echo ""
echo "Applying PATCH replace operations..."
PATCH_RESPONSE=$(curl -s -X PATCH "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "replace",
        "path": "emails[type eq \"work\"].value",
        "value": "elbert@murphy.us"
      },
      {
        "op": "replace",
        "path": "emails[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "replace",
        "path": "addresses[type eq \"work\"].formatted",
        "value": "VFPRNMVLQPFG"
      },
      {
        "op": "replace",
        "path": "addresses[type eq \"work\"].streetAddress",
        "value": "116 Towne Summit"
      },
      {
        "op": "replace",
        "path": "addresses[type eq \"work\"].locality",
        "value": "UQSMOIYPGKQI"
      },
      {
        "op": "replace",
        "path": "addresses[type eq \"work\"].region",
        "value": "RGSFCPWGLJRP"
      },
      {
        "op": "replace",
        "path": "addresses[type eq \"work\"].postalCode",
        "value": "wp2 3oc"
      },
      {
        "op": "replace",
        "path": "addresses[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "replace",
        "path": "addresses[type eq \"work\"].country",
        "value": "Namibia"
      },
      {
        "op": "replace",
        "path": "phoneNumbers[type eq \"work\"].value",
        "value": "33-749-9990"
      },
      {
        "op": "replace",
        "path": "phoneNumbers[type eq \"work\"].primary",
        "value": true
      },
      {
        "op": "replace",
        "path": "phoneNumbers[type eq \"mobile\"].value",
        "value": "33-749-9990"
      },
      {
        "op": "replace",
        "path": "phoneNumbers[type eq \"fax\"].value",
        "value": "33-749-9990"
      },
      {
        "op": "replace",
        "path": "roles[primary eq \"True\"].display",
        "value": "WMIIFOEBBTKE"
      },
      {
        "op": "replace",
        "path": "roles[primary eq \"True\"].value",
        "value": "JVQBTZOTYYVB"
      },
      {
        "op": "replace",
        "path": "roles[primary eq \"True\"].type",
        "value": "BHOZLXUVBKAG"
      },
      {
        "op": "replace",
        "value": {
          "active": true,
          "displayName": "QWUNAMCBRHTB",
          "title": "GWJZNRYZYFWD",
          "preferredLanguage": "dsb-DE",
          "name.givenName": "Janessa",
          "name.familyName": "Eloy",
          "name.formatted": "Petra",
          "name.middleName": "Amanda",
          "name.honorificPrefix": "Cheyenne",
          "name.honorificSuffix": "Loren",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber": "PQFWQUWGUFQK",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department": "ERQODAWELTIZ",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter": "LTQKRXTPYGIA",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization": "TLHYWCPUMAVI",
          "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division": "XOHRCGTBDZFH",
          "userType": "KFZAEDZCBHLI",
          "nickName": "XELTCHWSZKDN",
          "locale": "KDEDZZBGRAOB",
          "timezone": "America/Belize",
          "profileUrl": "EEASFOPRMBDT"
        }
      }
    ],
    "schemas": [
      "urn:ietf:params:scim:api:messages:2.0:PatchOp"
    ]
  }')

echo "✓ PATCH operations completed"

echo ""
echo "=== VALIDATION CHECKS ==="

# Check each field that was mentioned in the error
echo "Checking specific field values..."

# Email value
EMAIL_VALUE=$(echo $PATCH_RESPONSE | jq -r '.emails[]? | select(.type == "work") | .value')
if [ "$EMAIL_VALUE" == "elbert@murphy.us" ]; then
  echo "✅ emails[type eq \"work\"].value: PASS ($EMAIL_VALUE)"
else
  echo "❌ emails[type eq \"work\"].value: FAIL (got: $EMAIL_VALUE, expected: elbert@murphy.us)"
fi

# Name fields
GIVEN_NAME=$(echo $PATCH_RESPONSE | jq -r '.name.givenName')
FAMILY_NAME=$(echo $PATCH_RESPONSE | jq -r '.name.familyName')
FORMATTED_NAME=$(echo $PATCH_RESPONSE | jq -r '.name.formatted')
MIDDLE_NAME=$(echo $PATCH_RESPONSE | jq -r '.name.middleName')
HONORIFIC_PREFIX=$(echo $PATCH_RESPONSE | jq -r '.name.honorificPrefix')
HONORIFIC_SUFFIX=$(echo $PATCH_RESPONSE | jq -r '.name.honorificSuffix')

[ "$GIVEN_NAME" == "Janessa" ] && echo "✅ name.givenName: PASS" || echo "❌ name.givenName: FAIL (got: $GIVEN_NAME)"
[ "$FAMILY_NAME" == "Eloy" ] && echo "✅ name.familyName: PASS" || echo "❌ name.familyName: FAIL (got: $FAMILY_NAME)"
[ "$FORMATTED_NAME" == "Petra" ] && echo "✅ name.formatted: PASS" || echo "❌ name.formatted: FAIL (got: $FORMATTED_NAME)"
[ "$MIDDLE_NAME" == "Amanda" ] && echo "✅ name.middleName: PASS" || echo "❌ name.middleName: FAIL (got: $MIDDLE_NAME)"
[ "$HONORIFIC_PREFIX" == "Cheyenne" ] && echo "✅ name.honorificPrefix: PASS" || echo "❌ name.honorificPrefix: FAIL (got: $HONORIFIC_PREFIX)"
[ "$HONORIFIC_SUFFIX" == "Loren" ] && echo "✅ name.honorificSuffix: PASS" || echo "❌ name.honorificSuffix: FAIL (got: $HONORIFIC_SUFFIX)"

# Address fields
ADDR_FORMATTED=$(echo $PATCH_RESPONSE | jq -r '.addresses[]? | select(.type == "work") | .formatted')
ADDR_STREET=$(echo $PATCH_RESPONSE | jq -r '.addresses[]? | select(.type == "work") | .streetAddress')
ADDR_LOCALITY=$(echo $PATCH_RESPONSE | jq -r '.addresses[]? | select(.type == "work") | .locality')
ADDR_REGION=$(echo $PATCH_RESPONSE | jq -r '.addresses[]? | select(.type == "work") | .region')
ADDR_POSTAL=$(echo $PATCH_RESPONSE | jq -r '.addresses[]? | select(.type == "work") | .postalCode')
ADDR_COUNTRY=$(echo $PATCH_RESPONSE | jq -r '.addresses[]? | select(.type == "work") | .country')

[ "$ADDR_FORMATTED" == "VFPRNMVLQPFG" ] && echo "✅ addresses[type eq \"work\"].formatted: PASS" || echo "❌ addresses[type eq \"work\"].formatted: FAIL (got: $ADDR_FORMATTED)"
[ "$ADDR_STREET" == "116 Towne Summit" ] && echo "✅ addresses[type eq \"work\"].streetAddress: PASS" || echo "❌ addresses[type eq \"work\"].streetAddress: FAIL (got: $ADDR_STREET)"
[ "$ADDR_LOCALITY" == "UQSMOIYPGKQI" ] && echo "✅ addresses[type eq \"work\"].locality: PASS" || echo "❌ addresses[type eq \"work\"].locality: FAIL (got: $ADDR_LOCALITY)"
[ "$ADDR_REGION" == "RGSFCPWGLJRP" ] && echo "✅ addresses[type eq \"work\"].region: PASS" || echo "❌ addresses[type eq \"work\"].region: FAIL (got: $ADDR_REGION)"
[ "$ADDR_POSTAL" == "wp2 3oc" ] && echo "✅ addresses[type eq \"work\"].postalCode: PASS" || echo "❌ addresses[type eq \"work\"].postalCode: FAIL (got: $ADDR_POSTAL)"
[ "$ADDR_COUNTRY" == "Namibia" ] && echo "✅ addresses[type eq \"work\"].country: PASS" || echo "❌ addresses[type eq \"work\"].country: FAIL (got: $ADDR_COUNTRY)"

# Phone numbers
PHONE_WORK=$(echo $PATCH_RESPONSE | jq -r '.phoneNumbers[]? | select(.type == "work") | .value')
PHONE_MOBILE=$(echo $PATCH_RESPONSE | jq -r '.phoneNumbers[]? | select(.type == "mobile") | .value')
PHONE_FAX=$(echo $PATCH_RESPONSE | jq -r '.phoneNumbers[]? | select(.type == "fax") | .value')

[ "$PHONE_WORK" == "33-749-9990" ] && echo "✅ phoneNumbers[type eq \"work\"].value: PASS" || echo "❌ phoneNumbers[type eq \"work\"].value: FAIL (got: $PHONE_WORK)"
[ "$PHONE_MOBILE" == "33-749-9990" ] && echo "✅ phoneNumbers[type eq \"mobile\"].value: PASS" || echo "❌ phoneNumbers[type eq \"mobile\"].value: FAIL (got: $PHONE_MOBILE)"
[ "$PHONE_FAX" == "33-749-9990" ] && echo "✅ phoneNumbers[type eq \"fax\"].value: PASS" || echo "❌ phoneNumbers[type eq \"fax\"].value: FAIL (got: $PHONE_FAX)"

# Enterprise extension
EMP_NUMBER=$(echo $PATCH_RESPONSE | jq -r '."urn:ietf:params:scim:schemas:extension:enterprise:2.0:User".employeeNumber')
DEPARTMENT=$(echo $PATCH_RESPONSE | jq -r '."urn:ietf:params:scim:schemas:extension:enterprise:2.0:User".department')
COST_CENTER=$(echo $PATCH_RESPONSE | jq -r '."urn:ietf:params:scim:schemas:extension:enterprise:2.0:User".costCenter')
ORGANIZATION=$(echo $PATCH_RESPONSE | jq -r '."urn:ietf:params:scim:schemas:extension:enterprise:2.0:User".organization')
DIVISION=$(echo $PATCH_RESPONSE | jq -r '."urn:ietf:params:scim:schemas:extension:enterprise:2.0:User".division')

[ "$EMP_NUMBER" == "PQFWQUWGUFQK" ] && echo "✅ enterprise.employeeNumber: PASS" || echo "❌ enterprise.employeeNumber: FAIL (got: $EMP_NUMBER)"
[ "$DEPARTMENT" == "ERQODAWELTIZ" ] && echo "✅ enterprise.department: PASS" || echo "❌ enterprise.department: FAIL (got: $DEPARTMENT)"
[ "$COST_CENTER" == "LTQKRXTPYGIA" ] && echo "✅ enterprise.costCenter: PASS" || echo "❌ enterprise.costCenter: FAIL (got: $COST_CENTER)"
[ "$ORGANIZATION" == "TLHYWCPUMAVI" ] && echo "✅ enterprise.organization: PASS" || echo "❌ enterprise.organization: FAIL (got: $ORGANIZATION)"
[ "$DIVISION" == "XOHRCGTBDZFH" ] && echo "✅ enterprise.division: PASS" || echo "❌ enterprise.division: FAIL (got: $DIVISION)"

# Roles
ROLE_DISPLAY=$(echo $PATCH_RESPONSE | jq -r '.roles[]? | select(.primary == true) | .display')
ROLE_VALUE=$(echo $PATCH_RESPONSE | jq -r '.roles[]? | select(.primary == true) | .value')
ROLE_TYPE=$(echo $PATCH_RESPONSE | jq -r '.roles[]? | select(.primary == true) | .type')

[ "$ROLE_DISPLAY" == "WMIIFOEBBTKE" ] && echo "✅ roles[primary eq \"True\"].display: PASS" || echo "❌ roles[primary eq \"True\"].display: FAIL (got: $ROLE_DISPLAY)"
[ "$ROLE_VALUE" == "JVQBTZOTYYVB" ] && echo "✅ roles[primary eq \"True\"].value: PASS" || echo "❌ roles[primary eq \"True\"].value: FAIL (got: $ROLE_VALUE)"
[ "$ROLE_TYPE" == "BHOZLXUVBKAG" ] && echo "✅ roles[primary eq \"True\"].type: PASS" || echo "❌ roles[primary eq \"True\"].type: FAIL (got: $ROLE_TYPE)"

echo ""
echo "Final user state:"
echo $PATCH_RESPONSE | jq '.'

# Cleanup
echo ""
echo "Cleaning up..."
curl -s -X DELETE "http://localhost:5000/Users/$USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" > /dev/null

echo "Done."
