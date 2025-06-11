#!/bin/bash

echo "🧪 Testing User PATCH - Add Manager"
echo "===================================="

# Get token
curl -s http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId":"scim_client","clientSecret":"scim_secret"}' | jq -r '.access_token' > token.txt

echo "✅ Token obtained"

# Create a user first
echo ""
echo "1️⃣ Creating test user..."
CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5000/scim/v2/Users" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "active": true,
    "displayName": "Test User For Manager",
    "emails": [
      {
        "type": "work",
        "value": "testuser-$(date +%s)@example.com",
        "primary": true
      }
    ],
    "name": {
      "givenName": "Test",
      "familyName": "User"
    },
    "schemas": [
      "urn:ietf:params:scim:schemas:core:2.0:User",
      "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
    ],
    "userName": "testuser-$(date +%s)@example.com",
    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
      "employeeNumber": "12345",
      "department": "Engineering"
    }
  }')

echo "$CREATE_RESPONSE" | jq -r '.id' > user_id.txt

echo "Debug - Create response:"
echo "$CREATE_RESPONSE"
echo "User ID extracted: $(cat user_id.txt)"

echo "✅ User created: $(cat user_id.txt)"

# Test PATCH to add manager
echo ""
echo "2️⃣ Testing PATCH to add manager..."
echo "Request payload:"
cat << 'EOF'
{
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
}
EOF

echo ""
echo "Response:"
curl -i -X PATCH "http://localhost:5000/scim/v2/Users/$(cat user_id.txt)" \
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
  }'

echo ""
echo ""
echo "🎯 Check if the response includes:"
echo "   - Status: 200 OK"
echo "   - manager object with 'value' and '\$ref' properties"
echo "   - Enterprise schema in response"
