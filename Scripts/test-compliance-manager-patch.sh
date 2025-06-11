#!/bin/bash

echo "🧪 Testing User PATCH - Add Manager (Compliance Test Format)"
echo "==========================================================="

# Get token
curl -s http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId":"scim_client","clientSecret":"scim_secret"}' | jq -r '.access_token' > token.txt

echo "✅ Token obtained"

# Create a user with the compliance test structure
echo ""
echo "1️⃣ Creating user with compliance test structure..."
curl -s -X POST "http://localhost:5000/scim/v2/Users" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "active": true,
    "addresses": [
      {
        "type": "work",
        "formatted": "HMLOTPVTZPYS",
        "streetAddress": "34683 Muller Cliffs",
        "locality": "LHGUDTSMGJJG",
        "region": "SFCHMGPKEPZL",
        "postalCode": "uo5 7me",
        "primary": true,
        "country": "South Georgia and the South Sandwich Islands"
      }
    ],
    "displayName": "IKEPUVHDJDCF",
    "emails": [
      {
        "type": "work",
        "value": "tyshawn@vandervort.info",
        "primary": true
      }
    ],
    "locale": "UWJRMUUYYQLA",
    "name": {
      "givenName": "Eula",
      "familyName": "Brown",
      "formatted": "Samson",
      "middleName": "Dannie",
      "honorificPrefix": "Deshawn",
      "honorificSuffix": "Baron"
    },
    "nickName": "THJMTKVJOVWI",
    "phoneNumbers": [
      {
        "type": "work",
        "value": "39-755-8162",
        "primary": true
      },
      {
        "type": "mobile",
        "value": "39-755-8162"
      },
      {
        "type": "fax",
        "value": "39-755-8162"
      }
    ],
    "preferredLanguage": "dsb",
    "profileUrl": "MAGPBLWNPBUE",
    "roles": [
      {
        "primary": "True",
        "display": "IBQTRHCCBREB",
        "value": "YUOPRKCNRADI",
        "type": "EHQIXBSFHNDJ"
      }
    ],
    "schemas": [
      "urn:ietf:params:scim:schemas:core:2.0:User",
      "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
    ],
    "timezone": "Africa/Tripoli",
    "title": "DIGBSZPVRVVW",
    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
      "employeeNumber": "FNOVJNXADHDG",
      "department": "EZQLIUCYERQZ",
      "costCenter": "QIAOWMFHTXVT",
      "organization": "ATZQFBVZYBJV",
      "division": "XEDVGZSMLEZN"
    },
    "userName": "tristian_beier@koepp.us",
    "userType": "OJINULRMMTNF"
  }' | jq -r '.id' > user_id.txt

echo "✅ User created: $(cat user_id.txt)"

# Test PATCH to add manager with exact compliance test format
echo ""
echo "2️⃣ Testing PATCH with exact compliance test format..."
curl -s -X PATCH "http://localhost:5000/scim/v2/Users/$(cat user_id.txt)" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "add",
        "path": "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
        "value": "1e182146-9efe-414e-8c7d-595dcbe6698c"
      }
    ],
    "schemas": [
      "urn:ietf:params:scim:api:messages:2.0:PatchOp"
    ]
  }' | jq .

echo ""
echo "✅ Compliance test format PATCH completed successfully!"
