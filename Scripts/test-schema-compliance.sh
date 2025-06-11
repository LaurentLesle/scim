#!/bin/bash

echo "🧪 Testing SCIM User Schema Compliance..."

# Start the SCIM service if not running
if ! curl -s http://localhost:5000/Schemas >/dev/null 2>&1; then
    echo "🚀 Starting SCIM service..."
    cd /workspaces/scim
    nohup dotnet run --urls http://localhost:5000 > scim.log 2>&1 &
    sleep 8
fi

echo ""
echo "1️⃣ Testing User Schema Endpoint..."
SCHEMA_RESPONSE=$(curl -s http://localhost:5000/Schemas/urn:ietf:params:scim:schemas:core:2.0:User)

echo "Schema Response (first 100 lines):"
echo "$SCHEMA_RESPONSE" | jq . | head -100

echo ""
echo "2️⃣ Checking if 'primary' attribute is present in emails schema..."
PRIMARY_IN_EMAILS=$(echo "$SCHEMA_RESPONSE" | jq '.attributes[] | select(.name == "emails") | .subAttributes[] | select(.name == "primary")')

if [ -n "$PRIMARY_IN_EMAILS" ]; then
    echo "✅ Primary attribute found in emails schema"
    echo "$PRIMARY_IN_EMAILS"
else
    echo "❌ Primary attribute missing in emails schema"
fi

echo ""
echo "3️⃣ Checking if 'display' attribute is present in emails schema..."
DISPLAY_IN_EMAILS=$(echo "$SCHEMA_RESPONSE" | jq '.attributes[] | select(.name == "emails") | .subAttributes[] | select(.name == "display")')

if [ -n "$DISPLAY_IN_EMAILS" ]; then
    echo "✅ Display attribute found in emails schema"
    echo "$DISPLAY_IN_EMAILS"
else
    echo "❌ Display attribute missing in emails schema"
fi

echo ""
echo "4️⃣ Testing User Creation with Enhanced Attributes..."
TOKEN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId": "scim_client", "clientSecret": "scim_secret", "tenantId": "tenant1"}')

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token' 2>/dev/null)

if [ "$TOKEN" != "null" ] && [ -n "$TOKEN" ]; then
    echo "✅ Got authentication token"
    
    # Create a user with enhanced attributes including primary flags
    CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5000/Users" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/scim+json" \
      -d '{
        "userName": "testuser.schema@example.com",
        "displayName": "Test User Schema",
        "emails": [
          {
            "value": "testuser.schema@work.com",
            "type": "work",
            "primary": true,
            "display": "Work Email"
          },
          {
            "value": "testuser.schema@home.com", 
            "type": "home",
            "primary": false,
            "display": "Home Email"
          }
        ],
        "phoneNumbers": [
          {
            "value": "+1-555-123-4567",
            "type": "work",
            "primary": true,
            "display": "Work Phone"
          }
        ],
        "ims": [
          {
            "value": "testuser.slack",
            "type": "slack", 
            "primary": true,
            "display": "Slack Handle"
          }
        ],
        "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"]
      }')
      
    echo "User Creation Response:"
    echo "$CREATE_RESPONSE" | jq .
    
    # Check if primary and display attributes are preserved
    PRIMARY_EMAIL=$(echo "$CREATE_RESPONSE" | jq '.emails[] | select(.primary == true)')
    if [ -n "$PRIMARY_EMAIL" ]; then
        echo "✅ Primary email attribute preserved in response"
    else
        echo "❌ Primary email attribute not preserved"
    fi
    
    DISPLAY_EMAIL=$(echo "$CREATE_RESPONSE" | jq '.emails[] | select(.display == "Work Email")')
    if [ -n "$DISPLAY_EMAIL" ]; then
        echo "✅ Display email attribute preserved in response"
    else
        echo "❌ Display email attribute not preserved"
    fi
    
else
    echo "❌ Failed to get authentication token"
fi

echo ""
echo "🏁 Schema compliance test completed"
