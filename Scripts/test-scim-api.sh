#!/bin/bash

# SCIM Service Provider Test Script
# This script demonstrates how to interact with the SCIM API

# Configuration - Update these values based on your setup
BASE_URL="http://localhost:5000"  # Change to your Dev Tunnel URL if using tunnels
CLIENT_ID="scim_client"
CLIENT_SECRET="scim_secret"

echo "SCIM Service Provider Test Script"
echo "================================="
echo "Testing against: $BASE_URL"
echo ""

# Check if we should use a tunnel URL
read -p "Are you using a Dev Tunnel? (y/n): " use_tunnel
if [[ $use_tunnel == "y" || $use_tunnel == "Y" ]]; then
    read -p "Enter your Dev Tunnel URL (e.g., https://abc123-5000.devtunnels.ms): " tunnel_url
    if [[ ! -z "$tunnel_url" ]]; then
        BASE_URL="$tunnel_url"
        echo "Updated base URL to: $BASE_URL"
    fi
fi

echo ""

# Step 1: Get access token
echo "1. Getting access token..."
TOKEN_RESPONSE=$(curl -s -k -X POST "$BASE_URL/api/auth/token" \
  -H "Content-Type: application/json" \
  -d "{
    \"clientId\": \"$CLIENT_ID\",
    \"clientSecret\": \"$CLIENT_SECRET\",
    \"grantType\": \"client_credentials\"
  }")

TOKEN=$(echo $TOKEN_RESPONSE | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo "Failed to get access token"
    echo "Response: $TOKEN_RESPONSE"
    exit 1
fi

echo "✓ Access token obtained"

# Step 2: Test service provider configuration
echo ""
echo "2. Testing service provider configuration..."
curl -s -k -X GET "$BASE_URL/scim/v2/ServiceProviderConfig" \
  -H "Authorization: Bearer $TOKEN" | jq '.'

# Step 3: Create a test user
echo ""
echo "3. Creating a test user..."
USER_RESPONSE=$(curl -s -k -X POST "$BASE_URL/scim/v2/Users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "john.doe@example.com",
    "name": {
      "givenName": "John",
      "familyName": "Doe",
      "formatted": "John Doe"
    },
    "displayName": "John Doe",
    "emails": [
      {
        "value": "john.doe@example.com",
        "type": "work",
        "primary": true
      }
    ],
    "active": true
  }')

USER_ID=$(echo $USER_RESPONSE | jq -r '.id')
echo "✓ User created with ID: $USER_ID"

# Step 4: Get the created user
echo ""
echo "4. Getting the created user..."
curl -s -k -X GET "$BASE_URL/scim/v2/Users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" | jq '.'

# Step 5: Update user (disable)
echo ""
echo "5. Disabling the user..."
curl -s -k -X PATCH "$BASE_URL/scim/v2/Users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "replace",
        "path": "active",
        "value": false
      }
    ]
  }' | jq '.'

# Step 6: List users with filter
echo ""
echo "6. Listing users with filter..."
curl -s -k -X GET "$BASE_URL/scim/v2/Users?filter=userName%20eq%20%22john.doe@example.com%22" \
  -H "Authorization: Bearer $TOKEN" | jq '.'

# Step 7: Create a test group
echo ""
echo "7. Creating a test group..."
GROUP_RESPONSE=$(curl -s -k -X POST "$BASE_URL/scim/v2/Groups" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "displayName": "Test Group",
    "members": [
      {
        "value": "'$USER_ID'",
        "display": "John Doe"
      }
    ]
  }')

GROUP_ID=$(echo $GROUP_RESPONSE | jq -r '.id')
echo "✓ Group created with ID: $GROUP_ID"

# Step 8: List all users
echo ""
echo "8. Listing all users..."
curl -s -k -X GET "$BASE_URL/scim/v2/Users" \
  -H "Authorization: Bearer $TOKEN" | jq '.'

# Step 9: List all groups
echo ""
echo "9. Listing all groups..."
curl -s -k -X GET "$BASE_URL/scim/v2/Groups" \
  -H "Authorization: Bearer $TOKEN" | jq '.'

echo ""
echo "✓ Test completed successfully!"
echo ""
echo "To run this script:"
echo "1. Start the SCIM service: dotnet run"
echo "2. (Optional) Set up Dev Tunnel:"
echo "   - Install: dotnet tool install -g Microsoft.DevTunnels.Cli"
echo "   - Login: devtunnel user login"
echo "   - Create: devtunnel create scim-dev --allow-anonymous"
echo "   - Port: devtunnel port create scim-dev -p 5000"
echo "   - Host: devtunnel host scim-dev"
echo "3. Make this script executable: chmod +x test-scim-api.sh"
echo "4. Run the script: ./test-scim-api.sh"
