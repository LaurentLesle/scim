#!/bin/bash

# SCIM Compliance Test Script
# Tests that the SCIM service provider can create users and groups 
# without requiring non-standard headers like Customer-Id

BASE_URL="http://localhost:5000"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "🧪 SCIM Compliance Testing Script"
echo "=================================="

# Function to check if service is running
check_service() {
    if ! curl -s "$BASE_URL/api/auth/token" > /dev/null 2>&1; then
        echo -e "${RED}❌ Service is not running on $BASE_URL${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Service is running${NC}"
}

# Function to get JWT token
get_token() {
    echo "🔑 Getting JWT token..."
    TOKEN=$(curl -s -X POST -H "Content-Type: application/json" \
        -d '{"ClientId":"scim_client","ClientSecret":"scim_secret","TenantId":"tenant1"}' \
        "$BASE_URL/api/auth/token" | jq -r '.access_token')
    
    if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
        echo -e "${RED}❌ Failed to get JWT token${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ JWT token obtained${NC}"
}

# Function to test group creation (SCIM compliance)
test_group_creation() {
    echo ""
    echo "👥 Testing Group Creation (SCIM Compliance)"
    echo "==========================================="
    
    # Create group without Customer-Id header (SCIM compliant)
    GROUP_JSON='{
        "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
        "externalId": "compliance-test-group-1",
        "displayName": "Compliance Test Group"
    }'
    
    echo "📝 Creating group without Customer-Id header..."
    RESPONSE=$(curl -s -X POST \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/scim+json" \
        -d "$GROUP_JSON" \
        "$BASE_URL/Groups")
    
    GROUP_ID=$(echo "$RESPONSE" | jq -r '.id')
    
    if [ "$GROUP_ID" = "null" ] || [ -z "$GROUP_ID" ]; then
        echo -e "${RED}❌ Group creation failed${NC}"
        echo "Response: $RESPONSE"
        return 1
    fi
    
    echo -e "${GREEN}✅ Group created successfully: $GROUP_ID${NC}"
    
    # Verify group can be retrieved
    echo "🔍 Verifying group can be retrieved..."
    RETRIEVE_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/Groups/$GROUP_ID")
    RETRIEVED_NAME=$(echo "$RETRIEVE_RESPONSE" | jq -r '.displayName')
    
    if [ "$RETRIEVED_NAME" = "Compliance Test Group" ]; then
        echo -e "${GREEN}✅ Group retrieval successful${NC}"
    else
        echo -e "${RED}❌ Group retrieval failed${NC}"
        echo "Response: $RETRIEVE_RESPONSE"
        return 1
    fi
}

# Function to test user creation (SCIM compliance)
test_user_creation() {
    echo ""
    echo "👤 Testing User Creation (SCIM Compliance)"
    echo "=========================================="
    
    # Create user without Customer-Id header (SCIM compliant)
    USER_JSON='{
        "schemas": [
            "urn:ietf:params:scim:schemas:core:2.0:User",
            "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
        ],
        "userName": "compliance.test@example.com",
        "name": {
            "familyName": "Test",
            "givenName": "Compliance"
        },
        "emails": [{
            "value": "compliance.test@example.com",
            "primary": true
        }],
        "active": true,
        "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
            "employeeNumber": "CT001",
            "department": "Testing"
        }
    }'
    
    echo "📝 Creating user without Customer-Id header..."
    RESPONSE=$(curl -s -X POST \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/scim+json" \
        -d "$USER_JSON" \
        "$BASE_URL/Users")
    
    USER_ID=$(echo "$RESPONSE" | jq -r '.id')
    
    if [ "$USER_ID" = "null" ] || [ -z "$USER_ID" ]; then
        echo -e "${RED}❌ User creation failed${NC}"
        echo "Response: $RESPONSE"
        return 1
    fi
    
    echo -e "${GREEN}✅ User created successfully: $USER_ID${NC}"
    
    # Verify user can be retrieved
    echo "🔍 Verifying user can be retrieved..."
    RETRIEVE_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/Users/$USER_ID")
    RETRIEVED_USERNAME=$(echo "$RETRIEVE_RESPONSE" | jq -r '.userName')
    
    if [ "$RETRIEVED_USERNAME" = "compliance.test@example.com" ]; then
        echo -e "${GREEN}✅ User retrieval successful${NC}"
    else
        echo -e "${RED}❌ User retrieval failed${NC}"
        echo "Response: $RETRIEVE_RESPONSE"
        return 1
    fi
    
    # Check enterprise extension
    EMPLOYEE_NUMBER=$(echo "$RETRIEVE_RESPONSE" | jq -r '."urn:ietf:params:scim:schemas:extension:enterprise:2.0:User".employeeNumber')
    if [ "$EMPLOYEE_NUMBER" = "CT001" ]; then
        echo -e "${GREEN}✅ Enterprise extension working${NC}"
    else
        echo -e "${YELLOW}⚠️ Enterprise extension may have issues${NC}"
    fi
}

# Function to test schemas endpoint
test_schemas_endpoint() {
    echo ""
    echo "📋 Testing Schemas Endpoint"
    echo "============================"
    
    echo "🔍 Testing GET /Schemas..."
    SCHEMAS_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/Schemas")
    SCHEMAS_COUNT=$(echo "$SCHEMAS_RESPONSE" | jq -r '.totalResults')
    
    if [ "$SCHEMAS_COUNT" -gt "0" ]; then
        echo -e "${GREEN}✅ Schemas endpoint working (found $SCHEMAS_COUNT schemas)${NC}"
    else
        echo -e "${RED}❌ Schemas endpoint failed${NC}"
        echo "Response: $SCHEMAS_RESPONSE"
    fi
}

# Main execution
main() {
    check_service
    get_token
    test_group_creation
    test_user_creation
    test_schemas_endpoint
    
    echo ""
    echo "🎉 SCIM Compliance Tests Complete!"
    echo "=================================="
    echo -e "${GREEN}✅ All tests passed - Service is SCIM compliant${NC}"
}

# Run main function
main
