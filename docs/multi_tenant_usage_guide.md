# Multi-Tenant SCIM Service Provider - Configuration and Usage Guide

This guide provides instructions for configuring and using the multi-tenant features of the SCIM Service Provider.

## Configuration

### 1. Database Setup

The SCIM Service Provider uses Entity Framework Core with a multi-tenant data model. Configure your database connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=ScimDb;User Id=your-user;Password=your-password;TrustServerCertificate=True"
  }
}
```

For development purposes, you can use the in-memory database provider, which is configured by default.

### 2. Tenant Authentication Configuration

Configure tenant authentication in `appsettings.json`. You can choose between JWT tokens and/or HTTP headers for tenant identification:

```json
{
  "TenantConfiguration": {
    "UseJwtClaims": true,
    "JwtClaimName": "tenant_id",
    "UseHeader": true,
    "HeaderName": "X-Tenant-ID",
    "RequireTenant": true
  }
}
```

Options:
- `UseJwtClaims`: When true, tenant ID will be extracted from JWT claims
- `JwtClaimName`: Name of the claim containing tenant ID
- `UseHeader`: When true, tenant ID can be provided via HTTP header
- `HeaderName`: Name of the HTTP header for tenant ID
- `RequireTenant`: When true, requests without tenant context will be rejected

### 3. JWT Authentication

If using JWT authentication with tenant claims, configure JWT validation in `appsettings.json`:

```json
{
  "JwtSettings": {
    "Issuer": "https://your-identity-provider/",
    "Audience": "scim-api",
    "SecurityKey": "your-signing-key-for-development-only"
  }
}
```

For production environments, use proper key management and avoid storing secrets in configuration files.

## Customer Management

### Creating Customers (Tenants)

Customers can be created using the Customer API:

```http
POST /api/customers
Content-Type: application/json

{
  "name": "Contoso Ltd",
  "tenantId": "contoso"
}
```

Response:

```json
{
  "id": "c8b7da4e-127c-4d89-9b9a-7467e12ef1ea",
  "name": "Contoso Ltd",
  "tenantId": "contoso",
  "isActive": true,
  "createdAt": "2025-05-30T10:15:22Z"
}
```

Store the customer ID as it will be needed for administrative operations.

### Listing Customers

View all customers:

```http
GET /api/customers
Authorization: Bearer {admin-token}
```

### Customer Details

View a specific customer:

```http
GET /api/customers/{customerId}
Authorization: Bearer {admin-token}
```

### Activating/Deactivating Customers

Deactivate a customer to temporarily suspend access:

```http
PATCH /api/customers/{customerId}
Content-Type: application/json
Authorization: Bearer {admin-token}

{
  "isActive": false
}
```

## Using the Multi-Tenant SCIM API

### 1. Specifying Tenant Context

When making requests to the SCIM API, specify the tenant context using either:

**Option 1: HTTP Header**

```http
GET /scim/v2/Users
X-Tenant-ID: contoso
Authorization: Bearer {token}
```

**Option 2: JWT Token with Tenant Claim**

Ensure your JWT token includes the configured tenant claim (e.g., `tenant_id`).

### 2. Creating Users

Create users within a tenant context:

```http
POST /scim/v2/Users
X-Tenant-ID: contoso
Content-Type: application/json
Authorization: Bearer {token}

{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
  "userName": "johndoe@contoso.com",
  "name": {
    "givenName": "John",
    "familyName": "Doe"
  },
  "emails": [
    {
      "value": "johndoe@contoso.com",
      "primary": true
    }
  ],
  "active": true
}
```

### 3. Creating Groups

Create groups within a tenant context:

```http
POST /scim/v2/Groups
X-Tenant-ID: contoso
Content-Type: application/json
Authorization: Bearer {token}

{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "displayName": "Engineering",
  "members": [
    {
      "value": "{user-id}",
      "display": "John Doe"
    }
  ]
}
```

### 4. Accessing Users and Groups

When accessing users or groups, tenant context is required and data will be filtered by tenant:

```http
GET /scim/v2/Users
X-Tenant-ID: contoso
Authorization: Bearer {token}
```

This will only return users belonging to the "contoso" tenant.

## Data Isolation

The multi-tenant architecture ensures complete data isolation between tenants:

1. Each tenant can only access its own users and groups
2. Tenant context must be valid and active for API access
3. Cross-tenant operations are prevented at the service layer

## Error Handling

Common tenant-related errors:

| Status Code | Description | Solution |
|-------------|-------------|----------|
| 400 | Missing tenant identifier | Provide tenant ID via header or JWT token |
| 403 | Invalid or inactive tenant | Verify tenant ID exists and is active |
| 404 | Resource not found | Ensure resource exists within specified tenant |

## Monitoring and Troubleshooting

Logs include tenant context for troubleshooting:

```
info: ScimServiceProvider[0] - Request received for tenant: contoso
info: ScimServiceProvider[0] - User created in tenant contoso: johndoe@contoso.com
error: ScimServiceProvider[0] - Tenant not found: fabrikam
```

## Security Considerations

1. Use HTTPS in all environments
2. Implement proper JWT validation
3. Use strong tenant IDs that are not easily guessable
4. Consider rate limiting per tenant
5. Audit all tenant management operations

By following these guidelines, you can securely manage and use the multi-tenant SCIM Service Provider.
