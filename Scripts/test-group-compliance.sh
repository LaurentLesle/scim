#!/bin/bash

# SCIM Group Compliance Test
echo "🧪 SCIM Group Compliance Test..."

# Configuration
BASE_URL="http://localhost:5000"
TOKEN=""

# Function to get authentication token
get_token() {
    echo "1️⃣ Getting authentication token..."
    TOKEN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/token" \
        -H "Content-Type: application/json" \
        -d '{"clientId": "scim_client", "clientSecret": "scim_secret", "tenantId": "tenant1"}')
    
    TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')
    
    if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
        echo "❌ Failed to get authentication token"
        echo "Response: $TOKEN_RESPONSE"
        return 1
    fi
    
    echo "✅ Got authentication token"
    return 0
}

# Function to test Group creation
test_group_creation() {
    echo ""
    echo "2️⃣ Testing Group creation..."
    
    CREATE_RESPONSE=$(curl -s -X POST "$BASE_URL/Groups" \
        -H "Content-Type: application/scim+json" \
        -H "Authorization: Bearer $TOKEN" \
        -d '{
            "displayName": "Compliance Test Group",
            "externalId": "compliance-test-001",
            "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
        }')
    
    GROUP_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id')
    
    if [ "$GROUP_ID" == "null" ] || [ -z "$GROUP_ID" ]; then
        echo "❌ Failed to create group"
        echo "Response: $CREATE_RESPONSE"
        return 1
    fi
    
    echo "✅ Group created successfully: $GROUP_ID"
    echo "$GROUP_ID"
    return 0
}

# Function to test duplicate Group creation
test_duplicate_group() {
    echo ""
    echo "3️⃣ Testing duplicate Group creation (should return 409)..."
    
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/Groups" \
        -H "Content-Type: application/scim+json" \
        -H "Authorization: Bearer $TOKEN" \
        -d '{
            "displayName": "Duplicate Group",
            "externalId": "compliance-test-001",
            "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
        }')
    
    if [ "$HTTP_STATUS" == "409" ]; then
        echo "✅ Correctly returned 409 Conflict for duplicate externalId"
        return 0
    else
        echo "❌ Expected 409 Conflict, got $HTTP_STATUS"
        return 1
    fi
}

# Function to test problematic PATCH operation
test_patch_with_type_filter() {
    local group_id=$1
    echo ""
    echo "4️⃣ Testing PATCH with unsupported member type filter..."
    
    PATCH_RESPONSE=$(curl -s -X PATCH "$BASE_URL/Groups/$group_id" \
        -H "Content-Type: application/scim+json" \
        -H "Authorization: Bearer $TOKEN" \
        -d '{
            "Operations": [
                {
                    "op": "remove",
                    "path": "members[type eq \"untyped\"].value"
                }
            ],
            "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"]
        }')
    
    # Check HTTP status
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X PATCH "$BASE_URL/Groups/$group_id" \
        -H "Content-Type: application/scim+json" \
        -H "Authorization: Bearer $TOKEN" \
        -d '{
            "Operations": [
                {
                    "op": "remove", 
                    "path": "members[type eq \"untyped\"].value"
                }
            ],
            "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"]
        }')
    
    echo "PATCH Response: $PATCH_RESPONSE"
    echo "HTTP Status: $HTTP_STATUS"
    
    if [ "$HTTP_STATUS" == "400" ]; then
        echo "✅ Correctly returned 400 Bad Request for unsupported PATCH operation"
        return 0
    else
        echo "❌ Expected 400 Bad Request, got $HTTP_STATUS"
        return 1
    fi
}

# Function to test Group creation with members
test_group_with_members() {
    echo ""
    echo "5️⃣ Testing Group creation with members..."
    
    CREATE_RESPONSE=$(curl -s -X POST "$BASE_URL/Groups" \
        -H "Content-Type: application/scim+json" \
        -H "Authorization: Bearer $TOKEN" \
        -d '{
            "displayName": "Group with Members",
            "externalId": "group-with-members-001",
            "members": [
                {
                    "value": "user-123",
                    "display": "Test User 123"
                },
                {
                    "value": "user-456", 
                    "display": "Test User 456"
                }
            ],
            "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"]
        }')
    
    GROUP_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id')
    
    if [ "$GROUP_ID" == "null" ] || [ -z "$GROUP_ID" ]; then
        echo "❌ Failed to create group with members"
        echo "Response: $CREATE_RESPONSE"
        return 1
    fi
    
    echo "✅ Group with members created successfully: $GROUP_ID"
    return 0
}

# Main test execution
main() {
    echo "🚀 Starting SCIM Group Compliance Tests..."
    
    # Test 1: Authentication
    if ! get_token; then
        exit 1
    fi
    
    # Test 2: Basic Group creation
    if GROUP_ID=$(test_group_creation); then
        echo "Group ID: $GROUP_ID"
    else
        exit 1
    fi
    
    # Test 3: Duplicate Group creation
    if ! test_duplicate_group; then
        exit 1
    fi
    
    # Test 4: PATCH with unsupported operation
    if ! test_patch_with_type_filter "$GROUP_ID"; then
        exit 1
    fi
    
    # Test 5: Group creation with members
    if ! test_group_with_members; then
        exit 1
    fi
    
    echo ""
    echo "🎉 All SCIM Group compliance tests passed!"
}

# Run the tests
main
