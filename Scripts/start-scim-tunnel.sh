#!/bin/bash

echo "🚀 Starting SCIM Service with Dev Tunnel"
echo "======================================="

# Set fixed port 5000 for SCIM service
SCIM_PORT=5000

# Kill any existing processes on port 5000
echo "Checking for existing processes on port $SCIM_PORT..."
EXISTING_PID=$(lsof -ti:$SCIM_PORT 2>/dev/null)
if [ ! -z "$EXISTING_PID" ]; then
  echo "Killing existing process on port $SCIM_PORT (PID: $EXISTING_PID)"
  kill -9 $EXISTING_PID 2>/dev/null || true
  sleep 2
fi

echo "Starting SCIM service on port $SCIM_PORT"
# Start service in background with log redirection
nohup dotnet run --urls="http://0.0.0.0:$SCIM_PORT" > scim.log 2>&1 &
SCIM_PID=$!

echo "SCIM service started in background (PID: $SCIM_PID)"
echo "To view real-time logs: tail -f scim.log"

# Wait for service to start
sleep 5

# Test that service is running locally FIRST
LOCAL_TEST=$(curl -s "http://localhost:$SCIM_PORT/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"clientId": "scim_client","clientSecret": "scim_secret","grantType": "client_credentials"}')

if echo "$LOCAL_TEST" | jq -e '.access_token' > /dev/null 2>&1; then
  echo "✅ SCIM service working locally - ready to make public"
  echo ""
  echo "💡 To view real-time logs while working:"
  echo "   tail -f scim.log"
  echo "   # Or in another terminal: tail -f /workspaces/scim/scim.log"
else
  echo "❌ Fix SCIM service issues before making port public!"
  echo "Check logs with: tail -f scim.log"
  exit 1
fi

TUNNEL_ID=$(devtunnel create --allow-anonymous | grep "Tunnel ID" | awk '{print $4}')
echo "Created tunnel: $TUNNEL_ID"

# Add port forwarding for SCIM service (fixed port 5000)
devtunnel port create $TUNNEL_ID -p $SCIM_PORT --protocol http

# Start hosting (run in background)
devtunnel host $TUNNEL_ID &
echo "Tunnel is now hosting in background"

sleep 5

# Get the tunnel URL and store in environment variable (remove trailing slash)
TUNNEL_URL=$(devtunnel show $TUNNEL_ID --json | jq -r '.tunnel.ports[0].portUri | rtrimstr("/")')
export TUNNEL_URL
export SCIM_PORT
echo "Tunnel URL: $TUNNEL_URL"
echo "SCIM Port: $SCIM_PORT"

# Test the tunnel immediately
echo "Testing tunnel connectivity..."
TUNNEL_TEST=$(curl -s "${TUNNEL_URL}/api/auth/token" \
-H "Content-Type: application/json" \
-d '{"clientId": "scim_client","clientSecret": "scim_secret","grantType": "client_credentials"}')

if echo "$TUNNEL_TEST" | jq -e '.access_token' > /dev/null 2>&1; then
 echo "✅ Tunnel working - SCIM API accessible publicly!"
else
 echo "❌ Tunnel test failed. Check if devtunnel host is running."
 echo "Response: $TUNNEL_TEST"
fi

# Test the tunnel
curl -s "${TUNNEL_URL}/api/auth/token" \
 -H "Content-Type: application/json" \
 -d '{"clientId": "scim_client","clientSecret": "scim_secret","grantType": "client_credentials"}' | jq '.'
 echo $TUNNEL_URL

echo ""
echo "📊 Viewing Real-Time Logs While Service Runs in Background"
echo "========================================================="
echo ""
echo "Since the SCIM service runs in background mode (required for dev tunnels),"
echo "here are several ways to monitor logs in real-time:"
echo ""
echo "### Method 1: Tail the Log File (Recommended)"
echo "# In the same terminal or a new terminal window"
echo "tail -f scim.log"
echo ""
echo "### Method 2: VS Code Terminal Split"
echo "# Split your terminal in VS Code (Ctrl+Shift+5)"
echo "# Left side: This script output"
echo "# Right side: tail -f scim.log"
echo ""
echo "### Method 3: Watch for Specific Events"
echo "# Watch for authentication requests only"
echo "tail -f scim.log | grep \"Auth\""
echo ""
echo "# Watch for errors and warnings"
echo "tail -f scim.log | grep -E \"(warn:|error:|❌)\""
echo ""
echo "# Watch for successful operations"
echo "tail -f scim.log | grep -E \"(info:|✅|🎫)\""
echo ""
echo "🎉 Setup complete! Your SCIM service is now publicly accessible."
echo "   Public URL: $TUNNEL_URL"
echo "   Local URL: http://localhost:$SCIM_PORT"
echo "   Logs: tail -f scim.log"
