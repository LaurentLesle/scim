# SCIM Service Provider

This is a .NET Core 8.0 implementation of a SCIM (System for Cross-domain Identity Management) 2.0 service provider that can be integrated with Azure AD for user provisioning.

## Architecture

See the [Architecture Diagram](docs/architecture_diagram.md) for a visual representation of how this service integrates with Microsoft Entra ID (Azure AD).

## Features

- **SCIM 2.0 Compliant**: Implements the SCIM 2.0 specification
- **User Management**: Create, read, update, delete, and patch users
- **Group Management**: Create, read, update, delete, and patch groups
- **Filtering Support**: Basic filtering for users and groups
- **JWT Authentication**: Bearer token authentication for API endpoints
- **Service Discovery**: Exposes service provider configuration, resource types, and schemas
- **Entity Framework**: Uses Entity Framework Core with in-memory database (configurable for SQL Server)

## API Endpoints

### Authentication
- `POST /api/auth/token` - Generate JWT token for authentication

### Users
- `GET /scim/v2/Users` - List users with pagination and filtering
- `GET /scim/v2/Users/{id}` - Get user by ID
- `POST /scim/v2/Users` - Create new user
- `PUT /scim/v2/Users/{id}` - Update user
- `PATCH /scim/v2/Users/{id}` - Patch user
- `DELETE /scim/v2/Users/{id}` - Delete user

### Groups
- `GET /scim/v2/Groups` - List groups with pagination and filtering
- `GET /scim/v2/Groups/{id}` - Get group by ID
- `POST /scim/v2/Groups` - Create new group
- `PUT /scim/v2/Groups/{id}` - Update group
- `PATCH /scim/v2/Groups/{id}` - Patch group
- `DELETE /scim/v2/Groups/{id}` - Delete group

### Service Discovery
- `GET /scim/v2/ServiceProviderConfig` - Service provider configuration
- `GET /scim/v2/ResourceTypes` - Available resource types
- `GET /Schemas` - SCIM schemas

> **Note:** Service discovery endpoints do not require tenant headers (`X-Tenant-ID`) as they return global service information. Only resource endpoints (`/scim/v2/Users`, `/scim/v2/Groups`) require tenant context.

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Running the Application

1. Restore dependencies:
```bash
dotnet restore
```

2. Run the application:
```bash
dotnet run
```

3. The API will be available at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP)

4. Access Swagger UI at `https://localhost:5001/swagger`

### Authentication

To authenticate with the API:

1. Get a token:
```bash
curl -X POST https://localhost:5001/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "scim_client",
    "clientSecret": "scim_secret",
    "grantType": "client_credentials"
  }'
```

2. Use the returned token in subsequent requests:
```bash
curl -X GET https://localhost:5001/scim/v2/Users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Example Requests

### Create a User
```bash
curl -X POST https://localhost:5001/scim/v2/Users \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "john.doe@example.com",
    "name": {
      "givenName": "John",
      "familyName": "Doe"
    },
    "emails": [
      {
        "value": "john.doe@example.com",
        "primary": true
      }
    ],
    "active": true
  }'
```

### Patch a User (Disable)
```bash
curl -X PATCH https://localhost:5001/scim/v2/Users/{userId} \
  -H "Authorization: Bearer YOUR_TOKEN" \
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
  }'
```

### Filter Users
```bash
curl -X GET "https://localhost:5001/scim/v2/Users?filter=userName%20eq%20%22john.doe@example.com%22" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Azure AD Integration

To integrate with Azure AD:

1. **Configure Azure AD Enterprise Application**:
   - Create a new Enterprise Application in Azure AD
   - Enable automatic provisioning
   - Set the Tenant URL to: `https://your-domain.com/scim/v2`
   - Set the Secret Token to your JWT token

2. **Configure Attribute Mappings**:
   - Map Azure AD attributes to SCIM attributes
   - Common mappings:
     - `userPrincipalName` → `userName`
     - `displayName` → `displayName`
     - `givenName` → `name.givenName`
     - `surname` → `name.familyName`
     - `mail` → `emails[type eq "work"].value`

3. **Test the Connection**:
   - Use Azure AD's test connection feature
   - Verify users can be created, updated, and disabled

## Configuration

### Database
By default, the application uses an in-memory database. To use SQL Server:

1. Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ScimServiceProvider;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

2. Update `Program.cs` to use SQL Server:
```csharp
builder.Services.AddDbContext<ScimDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

3. Run migrations:
```bash
dotnet ef database update
```

### JWT Configuration
Update the JWT settings in `appsettings.json`:
```json
{
  "Jwt": {
    "Key": "YourSecretKeyHere",
    "Issuer": "https://your-domain.com",
    "Audience": "https://your-domain.com"
  }
}
```

## Production Considerations

1. **Security**:
   - Use proper authentication (OAuth 2.0, client certificates)
   - Implement rate limiting
   - Use HTTPS only
   - Validate all input data

2. **Performance**:
   - Implement caching
   - Use a production database (SQL Server, PostgreSQL)
   - Add logging and monitoring

3. **Scalability**:
   - Consider using Redis for distributed caching
   - Implement proper error handling and retry logic
   - Add health checks

## SCIM 2.0 Compliance

This implementation follows the SCIM 2.0 specification (RFC 7643, RFC 7644) and includes:

- Core schema support for Users and Groups
- Standard HTTP methods (GET, POST, PUT, PATCH, DELETE)
- Filtering with basic operators
- Pagination support
- Error handling with proper SCIM error responses
- Service provider configuration endpoints

## License

This project is licensed under the MIT License.