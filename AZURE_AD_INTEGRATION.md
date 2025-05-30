# Azure AD Integration Guide

This guide explains how to integrate the SCIM Service Provider with Azure AD for automatic user provisioning.

## Prerequisites

1. **Azure AD Premium License** - Required for automatic provisioning
2. **SCIM Service Provider Running** - Your SCIM API must be accessible via HTTPS
3. **Admin Access to Azure AD** - You need permissions to create and configure Enterprise Applications

## Step 1: Deploy Your SCIM Service Provider

### Option A: Local Development with Visual Studio Dev Tunnels
For testing purposes, you can use Visual Studio Dev Tunnels to expose your local development server:

```bash
# Install Dev Tunnels CLI (if not already installed)
# Download from https://aka.ms/TunnelsCliDownload
# Or install via dotnet tool:
dotnet tool install -g Microsoft.DevTunnels.Cli

# Authenticate with your Microsoft account
devtunnel user login

# Start your SCIM service
dotnet run

# In another terminal, create and start a tunnel
devtunnel create --allow-anonymous
devtunnel port create -p 5000
devtunnel host

# Note the HTTPS URL provided (e.g., https://abc123-5000.devtunnels.ms)
```

**Alternative: Using VS Code Dev Tunnels Extension**
1. Install the "Dev Tunnels" extension in VS Code
2. Open Command Palette (Ctrl+Shift+P)
3. Run "Dev Tunnels: Create Tunnel"
4. Select "Public" access
5. Choose port 5000
6. Copy the provided HTTPS URL

### Option B: Deploy to Azure App Service
For production deployment:

1. Create an Azure App Service
2. Deploy your SCIM application
3. Ensure HTTPS is enabled
4. Note the application URL (e.g., https://your-scim-app.azurewebsites.net)

## Step 2: Create Azure AD Enterprise Application

1. **Sign in to Azure Portal**
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to Azure Active Directory

2. **Create Enterprise Application**
   - Go to "Enterprise applications"
   - Click "New application"
   - Click "Create your own application"
   - Name: "Your SCIM Service Provider"
   - Select "Integrate any other application you don't find in the gallery (Non-gallery)"
   - Click "Create"

## Step 3: Configure Single Sign-On (Optional)

1. In your Enterprise Application, go to "Single sign-on"
2. Select "SAML" or configure as needed
3. This step is optional for SCIM provisioning

## Step 4: Configure Automatic Provisioning

1. **Enable Provisioning**
   - In your Enterprise Application, go to "Provisioning"
   - Click "Get started"
   - Set Provisioning Mode to "Automatic"

2. **Configure Admin Credentials**
   - **Tenant URL**: `https://your-domain.com/scim/v2` (replace with your actual URL)
   - **Secret Token**: Generate a JWT token using your auth endpoint:
     ```bash
     curl -X POST https://your-domain.com/api/auth/token \
       -H "Content-Type: application/json" \
       -d '{
         "clientId": "scim_client",
         "clientSecret": "scim_secret",
         "grantType": "client_credentials"
       }'
     ```
   - Copy the `access_token` value and paste it as the Secret Token

3. **Test Connection**
   - Click "Test Connection"
   - Ensure it returns "The supplied credentials are authorized to enable provisioning"

4. **Save Configuration**
   - Click "Save"

## Step 5: Configure Attribute Mappings

1. **Go to Mappings**
   - In the Provisioning page, expand "Mappings"
   - Click "Provision Azure Active Directory Users"

2. **Configure User Attribute Mappings**
   
   | Azure AD Attribute | SCIM Attribute | Expression | Required |
   |-------------------|----------------|------------|----------|
   | userPrincipalName | userName | [userPrincipalName] | Yes |
   | displayName | displayName | [displayName] | No |
   | givenName | name.givenName | [givenName] | No |
   | surname | name.familyName | [surname] | No |
   | mail | emails[type eq "work"].value | [mail] | No |
   | accountEnabled | active | [accountEnabled] | Yes |

3. **Configure Group Mappings (Optional)**
   - Click "Provision Azure Active Directory Groups"
   - Map `displayName` to `displayName`
   - Map `members` to `members`

4. **Save Mappings**
   - Click "Save" on each mapping page

## Step 6: Configure Provisioning Scope

1. **Set Scope**
   - In the Provisioning page, go to "Settings"
   - **Scope**: Choose between:
     - "Sync only assigned users and groups" (Recommended)
     - "Sync all users and groups"

2. **Notification Email**
   - Add your email for provisioning notifications

3. **Save Settings**

## Step 7: Assign Users and Groups

1. **Assign Users**
   - Go to "Users and groups"
   - Click "Add user/group"
   - Select users/groups to provision to your SCIM service
   - Click "Assign"

## Step 8: Start Provisioning

1. **Enable Provisioning**
   - Go back to "Provisioning"
   - Set "Provisioning Status" to "On"
   - Click "Save"

2. **Monitor Initial Sync**
   - The initial synchronization will start
   - Monitor progress in the "Provisioning logs"

## Step 9: Monitor and Troubleshoot

### View Provisioning Logs
1. Go to "Provisioning logs" in your Enterprise Application
2. Monitor successful and failed provisioning attempts
3. Check for errors and warnings

### Common Issues and Solutions

#### 1. Authentication Failures
- **Issue**: "Unauthorized" errors
- **Solution**: Ensure JWT token is valid and hasn't expired
- **Action**: Generate a new token and update the Secret Token

#### 2. Attribute Mapping Errors
- **Issue**: Users created with missing attributes
- **Solution**: Check attribute mappings and ensure required fields are mapped
- **Action**: Update mappings and restart provisioning

#### 3. User Already Exists
- **Issue**: "User already exists" errors
- **Solution**: Implement proper conflict resolution in your SCIM service
- **Action**: Check userName uniqueness and handle duplicates

#### 4. SSL Certificate Issues
- **Issue**: SSL certificate validation failures
- **Solution**: Ensure your SCIM service has a valid SSL certificate
- **Action**: Use a proper SSL certificate or configure Azure AD to ignore SSL issues (not recommended for production)

### Testing Provisioning

1. **Create Test User**
   - Create a test user in Azure AD
   - Assign them to your Enterprise Application
   - Check if they appear in your SCIM service

2. **Update User**
   - Update the test user's attributes in Azure AD
   - Verify changes are synchronized to your SCIM service

3. **Disable User**
   - Disable the test user in Azure AD
   - Confirm the user is deactivated in your SCIM service

## Step 10: Production Considerations

### Security
1. **Use Strong Authentication**
   - Implement OAuth 2.0 with proper client credentials
   - Use client certificates for enhanced security
   - Rotate tokens regularly

2. **HTTPS Only**
   - Always use HTTPS for your SCIM endpoints
   - Use valid SSL certificates

3. **Rate Limiting**
   - Implement rate limiting to prevent abuse
   - Configure appropriate throttling

### Monitoring
1. **Logging**
   - Log all SCIM operations
   - Monitor for unusual patterns
   - Set up alerts for failures

2. **Health Checks**
   - Implement health check endpoints
   - Monitor service availability

### Backup and Recovery
1. **Data Backup**
   - Regular backups of user data
   - Test recovery procedures

2. **Disaster Recovery**
   - Plan for service outages
   - Have rollback procedures

## Troubleshooting Commands

### Test SCIM Endpoints
```bash
# Test service provider configuration
curl -H "Authorization: Bearer YOUR_TOKEN" \
  https://your-domain.com/scim/v2/ServiceProviderConfig

# Test user creation
curl -X POST https://your-domain.com/scim/v2/Users \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "test@example.com",
    "name": {"givenName": "Test", "familyName": "User"},
    "emails": [{"value": "test@example.com", "primary": true}],
    "active": true
  }'

# Test user filtering
curl -H "Authorization: Bearer YOUR_TOKEN" \
  "https://your-domain.com/scim/v2/Users?filter=userName%20eq%20%22test@example.com%22"
```

### Check Azure AD Provisioning Status
1. Go to Azure AD > Enterprise Applications > Your App > Provisioning
2. Check "Provisioning logs" for detailed information
3. Look at "Provisioning performance" for statistics

## Support and Documentation

- [SCIM 2.0 Specification](https://tools.ietf.org/html/rfc7644)
- [Azure AD SCIM Documentation](https://docs.microsoft.com/en-us/azure/active-directory/app-provisioning/use-scim-to-provision-users-and-groups)
- [Troubleshooting Guide](https://docs.microsoft.com/en-us/azure/active-directory/app-provisioning/application-provisioning-quarantine-status)

## Next Steps

1. **Test thoroughly** with a small group of users
2. **Monitor provisioning logs** for issues
3. **Gradually roll out** to more users
4. **Set up monitoring and alerting**
5. **Document your configuration** for future reference

Remember to always test in a development environment before deploying to production!
