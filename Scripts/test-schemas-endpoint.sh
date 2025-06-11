#!/bin/bash

# Test script for SCIM Schemas endpoint

echo "Testing SCIM /Schemas endpoint..."

# Start the service in the background
cd /workspaces/scim
dotnet run --no-launch-profile --urls "http://localhost:5000" > /dev/null 2>&1 &
SERVICE_PID=$!

# Wait for service to start
sleep 10

echo "Service started with PID: $SERVICE_PID"

echo "Testing /Schemas endpoint..."
curl -s -X GET "http://localhost:5000/Schemas" \
     -H "Content-Type: application/scim+json" \
     | jq '.' || echo "Failed to reach endpoint"

echo ""
echo "Testing specific User schema: /Schemas/urn:ietf:params:scim:schemas:core:2.0:User"
curl -s -X GET "http://localhost:5000/Schemas/urn:ietf:params:scim:schemas:core:2.0:User" \
     -H "Content-Type: application/scim+json" \
     | jq '.name, .description' || echo "Failed to reach endpoint"

echo ""
echo "Testing specific Group schema: /Schemas/urn:ietf:params:scim:schemas:core:2.0:Group"
curl -s -X GET "http://localhost:5000/Schemas/urn:ietf:params:scim:schemas:core:2.0:Group" \
     -H "Content-Type: application/scim+json" \
     | jq '.name, .description' || echo "Failed to reach endpoint"

echo ""
echo "Testing non-existent schema (should return 404):"
curl -s -w "HTTP Status: %{http_code}\n" -X GET "http://localhost:5000/Schemas/non-existent-schema" \
     -H "Content-Type: application/scim+json" \
     | head -5

# Kill the service
kill $SERVICE_PID
echo "Service stopped."
