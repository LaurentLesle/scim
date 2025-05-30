# Dev Tunnels Setup Guide

Visual Studio Dev Tunnels is Microsoft's secure solution for exposing local development servers to the internet. This guide shows how to set up Dev Tunnels for your SCIM service provider.

## What are Dev Tunnels?

Dev Tunnels allow you to securely expose your local development server to the internet with:
- **Built-in HTTPS** - Automatic SSL certificates
- **Microsoft Authentication** - Secure access control
- **Multiple Access Levels** - Anonymous, organization, or user-specific
- **VS Code Integration** - Seamless development experience
- **Free Tier Available** - No cost for basic usage

## Prerequisites

- Microsoft account (personal or work/school)
- .NET 8.0 SDK
- Visual Studio Code (optional, for VS Code extension)

## Option 1: Using Dev Tunnels CLI

### 1. Install Dev Tunnels CLI

**Via .NET Tool (Recommended):**
```bash
dotnet tool install -g Microsoft.DevTunnels.Cli
```

**Via Direct Download:**
- Download from [https://aka.ms/TunnelsCliDownload](https://aka.ms/TunnelsCliDownload)
- Extract and add to your PATH

### 2. Authenticate
```bash
# Login with your Microsoft account
devtunnel user login

# Verify login
devtunnel user show
```

### 3. Create and Configure Tunnel

**Quick Setup (Anonymous Access):**
```bash
# Create tunnel with anonymous access
devtunnel create scim-dev --allow-anonymous

# Add port forwarding for your SCIM service
devtunnel port create scim-dev -p 5000

# Start hosting
devtunnel host scim-dev
```

**Secure Setup (Authenticated Access):**
```bash
# Create tunnel with organization access
devtunnel create scim-dev --expiration 1d

# Add port forwarding
devtunnel port create scim-dev -p 5000

# Start hosting
devtunnel host scim-dev
```

### 4. Start Your SCIM Service
```bash
# In another terminal
cd /workspaces/scim
dotnet run
```

Your SCIM service will be available at the tunnel URL (e.g., `https://abc123-5000.devtunnels.ms`)

## Option 2: Using VS Code Dev Tunnels Extension

### 1. Install Extension
1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "Dev Tunnels"
4. Install the official Microsoft Dev Tunnels extension

### 2. Create Tunnel via Command Palette
1. Open Command Palette (Ctrl+Shift+P)
2. Type "Dev Tunnels: Create Tunnel"
3. Configure tunnel settings:
   - **Name**: `scim-dev`
   - **Access**: Choose based on your needs:
     - **Public**: Anonymous access (good for Azure AD testing)
     - **Organization**: Requires Microsoft account from your org
     - **Private**: Requires your specific Microsoft account

### 3. Forward Port
1. Start your SCIM service: `dotnet run`
2. In VS Code, go to "Ports" tab (usually at bottom)
3. Click "Forward a Port"
4. Enter `5000`
5. Right-click on the port and select "Change Port Visibility"
6. Choose "Public" to make it accessible via tunnel

### 4. Get Tunnel URL
- The tunnel URL will be shown in the Ports tab
- Copy the HTTPS URL for use in Azure AD configuration

## Option 3: Using VS Code Settings (Automatic)

### 1. Configure VS Code Settings
Create or update `.vscode/settings.json`:

```json
{
    "devTunnels.access": "public",
    "devTunnels.ports": [
        {
            "portNumber": 5000,
            "label": "SCIM API",
            "protocol": "https"
        }
    ]
}
```

### 2. Auto-start with Launch Configuration
Update `.vscode/launch.json`:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch SCIM with Dev Tunnel",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/bin/Debug/net8.0/ScimServiceProvider.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development",
                "ASPNETCORE_URLS": "http://localhost:5000"
            },
            "portForwarding": {
                "5000": {
                    "label": "SCIM API",
                    "access": "public"
                }
            }
        }
    ]
}
```

## Managing Tunnels

### List Tunnels
```bash
devtunnel list
```

### Delete Tunnel
```bash
devtunnel delete scim-dev
```

### Show Tunnel Details
```bash
devtunnel show scim-dev
```

### Update Tunnel Access
```bash
devtunnel update scim-dev --allow-anonymous
```

## Security Considerations

### For Development/Testing
- **Anonymous Access**: Use for Azure AD testing and demonstrations
- **Time-limited**: Set short expiration times
- **Port-specific**: Only expose necessary ports

### For Staging/Demo
- **Organization Access**: Restrict to your Microsoft organization
- **Longer Expiration**: Set appropriate expiration times
- **Monitoring**: Monitor tunnel usage and access logs

### Best Practices
1. **Use Organization Access** when possible for better security
2. **Set Expiration Times** to limit exposure duration
3. **Monitor Access Logs** in the Dev Tunnels portal
4. **Rotate Tunnels Regularly** for long-running demos
5. **Use HTTPS Only** (automatic with Dev Tunnels)

## Troubleshooting

### Common Issues

#### 1. Authentication Problems
```bash
# Clear and re-authenticate
devtunnel user logout
devtunnel user login
```

#### 2. Port Already in Use
```bash
# Check what's using the port
netstat -tulpn | grep :5000

# Use a different port
devtunnel port create scim-dev -p 5001
```

#### 3. Tunnel Not Accessible
- Check tunnel access settings
- Verify your SCIM service is running on the correct port
- Ensure firewall isn't blocking the connection

#### 4. VS Code Extension Issues
- Restart VS Code
- Check output panel for Dev Tunnels logs
- Try creating tunnel via CLI instead

### Testing Your Tunnel

Once your tunnel is running, test it:

```bash
# Replace with your actual tunnel URL
TUNNEL_URL="https://abc123-5000.devtunnels.ms"

# Test service provider config
curl "$TUNNEL_URL/scim/v2/ServiceProviderConfig"

# Test authentication (should fail without token)
curl "$TUNNEL_URL/scim/v2/Users"

# Get token and test
TOKEN=$(curl -s -X POST "$TUNNEL_URL/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"clientId":"scim_client","clientSecret":"scim_secret","grantType":"client_credentials"}' \
  | jq -r '.access_token')

curl -H "Authorization: Bearer $TOKEN" "$TUNNEL_URL/scim/v2/Users"
```

## Integration with Azure AD

When configuring Azure AD:

1. **Tenant URL**: Use your Dev Tunnel URL + `/scim/v2`
   - Example: `https://abc123-5000.devtunnels.ms/scim/v2`

2. **Secret Token**: Generate JWT token using your tunnel URL:
   ```bash
   curl -X POST https://abc123-5000.devtunnels.ms/api/auth/token \
     -H "Content-Type: application/json" \
     -d '{"clientId":"scim_client","clientSecret":"scim_secret","grantType":"client_credentials"}'
   ```

3. **Test Connection**: Azure AD will connect to your local SCIM service via the tunnel

## Costs and Limits

### Free Tier Includes:
- 3 active tunnels
- Anonymous and authenticated access
- HTTPS support
- Basic monitoring

### Paid Plans:
- More active tunnels
- Advanced security features
- Enhanced monitoring and logging
- SLA guarantees

## Alternative: Production Deployment

For production or long-term staging, consider deploying to:
- **Azure App Service**
- **Azure Container Instances**
- **Azure Kubernetes Service**
- **Any cloud provider with HTTPS support**

Dev Tunnels are perfect for development and testing but shouldn't be used for production workloads.

## Resources

- [Dev Tunnels Documentation](https://docs.microsoft.com/en-us/azure/developer/dev-tunnels/)
- [VS Code Dev Tunnels Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.vscode-dev-tunnels)
- [Dev Tunnels CLI Download](https://aka.ms/TunnelsCliDownload)
- [Dev Tunnels Portal](https://portal.azure.com/#view/Microsoft_Azure_DevTunnels/TunnelsDashboardBlade)
