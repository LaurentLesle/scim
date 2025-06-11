#!/bin/bash

echo "🧪 Enhanced Logging Test Script"
echo "==============================="

# Wait for application to be ready
sleep 3

echo ""
echo "1️⃣ Testing AUTH endpoint (DEBUG request + TRACE response)..."
curl -s http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId":"scim_client","clientSecret":"scim_secret"}' | jq -r '.access_token' > token.txt

echo "✅ Token obtained"

echo ""
echo "2️⃣ Testing GROUP creation (DEBUG request + TRACE response)..."
curl -s -X POST "http://localhost:5000/scim/v2/Groups" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "displayName": "Enhanced Logging Test Group",
    "externalId": "enhanced-logging-test-001",
    "members": [
      {
        "value": "user1",
        "display": "User One"
      }
    ]
  }' | jq -r '.id' > group_id.txt

echo "✅ Group created: $(cat group_id.txt)"

echo ""
echo "3️⃣ Testing PATCH compliance error (DEBUG request + TRACE response)..."
curl -s -X PATCH "http://localhost:5000/scim/v2/Groups/$(cat group_id.txt)" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "Operations": [
      {
        "op": "replace",
        "path": "members[type eq \"untyped\"].value",
        "value": "user123"
      }
    ]
  }' > /dev/null

echo "✅ PATCH compliance error triggered"

echo ""
echo "4️⃣ Testing GET with query parameters (query params logging)..."
curl -s "http://localhost:5000/scim/v2/Groups?filter=displayName eq \"Enhanced Logging Test Group\"&startIndex=1&count=10" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Accept: application/scim+json" > /dev/null

echo "✅ GET with query parameters completed"

echo ""
echo "🎯 Check the application logs above to see:"
echo "   📝 DEBUG: Request payloads for POST/PATCH operations"
echo "   📤 TRACE: Response bodies for all operations"
echo "   📋 INFO: Query parameters logging"
echo "   🔍 WARN: Error details with status codes"
echo "   🔑 INFO: Authentication header detection"
echo ""
echo "✨ Enhanced logging test completed!"
