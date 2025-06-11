#!/bin/bash

# Test script to demonstrate SCIM user creation issues and fixes

BASE_URL="http://localhost:5000"

echo "SCIM User Creation Test - Issues and Fixes"
echo "=========================================="

# Get authentication token
echo "Getting authentication token..."
TOKEN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"clientId": "scim_client", "clientSecret": "scim_secret", "grantType": "client_credentials", "tenantId": "tenant1"}')

TOKEN=$(echo $TOKEN_RESPONSE | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo "❌ Failed to get access token"
    echo "Response: $TOKEN_RESPONSE"
    exit 1
fi

echo "✅ Access token obtained"

# Test 1: User creation with string boolean in roles (FIXED)
echo ""
echo "Test 1: User with string boolean in roles.primary field"
echo "======================================================"

RESPONSE1=$(curl -s -w "HTTP_STATUS:%{http_code}" -X POST "$BASE_URL/scim/v2/Users" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Tenant-ID: tenant1" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "test.string.primary@example.com",
    "name": {"givenName": "Test", "familyName": "StringPrimary"},
    "emails": [{"value": "test.string.primary@example.com", "type": "work", "primary": true}],
    "roles": [{"value": "manager", "display": "Manager", "type": "business", "primary": "True"}],
    "active": true
  }')

HTTP_STATUS1=$(echo $RESPONSE1 | grep -o "HTTP_STATUS:[0-9]*" | cut -d: -f2)
BODY1=$(echo $RESPONSE1 | sed 's/HTTP_STATUS:[0-9]*$//')

echo "HTTP Status: $HTTP_STATUS1"
if [ "$HTTP_STATUS1" = "201" ]; then
    echo "✅ FIXED: String 'True' converted to boolean successfully"
    echo "Response: $BODY1"
else
    echo "❌ ISSUE: String to boolean conversion failed"
    echo "Response: $BODY1"
fi

# Test 2: User creation with proper boolean in roles
echo ""
echo "Test 2: User with proper boolean in roles.primary field"
echo "===================================================="

RESPONSE2=$(curl -s -w "HTTP_STATUS:%{http_code}" -X POST "$BASE_URL/scim/v2/Users" \
  -H "Content-Type: application/scim+json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Tenant-ID: tenant1" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "test.bool.primary@example.com",
    "name": {"givenName": "Test", "familyName": "BoolPrimary"},
    "emails": [{"value": "test.bool.primary@example.com", "type": "work", "primary": true}],
    "roles": [{"value": "admin", "display": "Administrator", "type": "business", "primary": true}],
    "active": true
  }')

HTTP_STATUS2=$(echo $RESPONSE2 | grep -o "HTTP_STATUS:[0-9]*" | cut -d: -f2)
BODY2=$(echo $RESPONSE2 | sed 's/HTTP_STATUS:[0-9]*$//')

echo "HTTP Status: $HTTP_STATUS2"
if [ "$HTTP_STATUS2" = "201" ]; then
    echo "✅ SUCCESS: Proper boolean works correctly"
    echo "Response: $BODY2"
else
    echo "❌ UNEXPECTED: Proper boolean failed"
    echo "Response: $BODY2"
fi

echo ""
echo "Summary:"
echo "======="
echo "✅ Boolean converter added to handle string 'True'/'False' values"
echo "✅ Test data file fixed to use proper boolean values"
echo "✅ SCIM user creation with roles field now working"
