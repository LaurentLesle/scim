# Multi-Tenant Architecture - SCIM Service Provider

## Overview

This document describes the multi-tenant architecture implemented in the SCIM Service Provider. The multi-tenant approach allows the application to serve multiple customers (tenants) within a single instance while maintaining strict data isolation between tenants.

## Architecture Design

### Key Components

1. **Customer Model**
   - Represents a tenant in the system
   - Stores tenant identification and configuration
   - Links to users and groups belonging to the tenant

2. **Tenant Context Middleware**
   - Extracts tenant information from requests
   - Validates tenant existence and authorization
   - Makes tenant context available to controllers

3. **Data Isolation Layer**
   - Services filter data based on tenant context
   - Database queries include tenant ID in filters
   - Enforces tenant boundaries at the data access level

4. **API Layer**
   - Controllers access tenant context from the middleware
   - Applies tenant context to service calls
   - Maintains SCIM 2.0 protocol compliance

## Implementation Details

### Customer Entity

The `Customer` entity is the core of the multi-tenant architecture:

```csharp
public class Customer
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<ScimUser> Users { get; set; } = new List<ScimUser>();
    public virtual ICollection<ScimGroup> Groups { get; set; } = new List<ScimGroup>();
}
```

### Data Relationships

Each SCIM resource (User/Group) is associated with exactly one customer:

```csharp
public class ScimUser
{
    // Existing SCIM properties
    
    [Required]
    public string CustomerId { get; set; } = string.Empty;
    
    // Navigation property to Customer
    public virtual Customer? Customer { get; set; }
}

public class ScimGroup
{
    // Existing SCIM properties
    
    [Required]
    public string CustomerId { get; set; } = string.Empty;
    
    // Navigation property to Customer
    public virtual Customer? Customer { get; set; }
}
```

### Service Layer

Services include tenant context in all operations:

```csharp
public interface IUserService
{
    Task<ScimUser?> GetUserAsync(string id, string customerId);
    Task<ScimUser?> GetUserByUsernameAsync(string username, string customerId);
    Task<ScimListResponse<ScimUser>> GetUsersAsync(string customerId, int startIndex = 1, int count = 10, string? filter = null);
    Task<ScimUser> CreateUserAsync(ScimUser user, string customerId);
    Task<ScimUser?> UpdateUserAsync(string id, ScimUser user, string customerId);
    Task<ScimUser?> PatchUserAsync(string id, ScimPatchRequest patchRequest, string customerId);
    Task<bool> DeleteUserAsync(string id, string customerId);
}
```

Implementation example:

```csharp
public async Task<ScimUser?> GetUserAsync(string id, string customerId)
{
    return await _context.Users
        .Where(u => u.Id == id && u.CustomerId == customerId)
        .FirstOrDefaultAsync();
}
```

### Tenant Context Middleware

Extracts tenant information from requests:

```csharp
public class CustomerContextMiddleware
{
    private readonly RequestDelegate _next;

    public CustomerContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICustomerService customerService)
    {
        if (context.Request.Path.Value?.StartsWith("/scim/v2/") == true)
        {
            // Extract tenant ID from auth token or header
            string? tenantId = null;
            
            // Option 1: From custom header
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader))
            {
                tenantId = tenantIdHeader.ToString();
            }
            
            // Option 2: From claim in JWT
            if (string.IsNullOrEmpty(tenantId) && context.User?.Identity?.IsAuthenticated == true)
            {
                tenantId = context.User.FindFirstValue("tenant_id");
            }
            
            if (string.IsNullOrEmpty(tenantId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Missing tenant identifier" });
                return;
            }

            // Verify tenant exists
            var customer = await customerService.GetCustomerByTenantIdAsync(tenantId);
            if (customer == null || !customer.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive tenant" });
                return;
            }

            // Store customer ID in items collection for use in controllers
            context.Items["CustomerId"] = customer.Id;
        }

        await _next(context);
    }
}
```

### Controller Implementation

Controllers retrieve tenant context:

```csharp
[ApiController]
[Route("scim/v2/[controller]")]
[Authorize]
[ScimResult]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    // Helper method to get the customer ID from context
    private string GetCustomerId()
    {
        if (HttpContext.Items.TryGetValue("CustomerId", out var customerId) && customerId != null)
        {
            return customerId.ToString() ?? throw new InvalidOperationException("Customer ID is null");
        }
        throw new InvalidOperationException("Customer context not available");
    }
    
    // API methods then use GetCustomerId() for tenant context
    [HttpGet("{id}")]
    public async Task<ActionResult<ScimUser>> GetUser(string id)
    {
        var user = await _userService.GetUserAsync(id, GetCustomerId());
        
        if (user == null)
        {
            return NotFound(new ScimError
            {
                Status = 404,
                Detail = $"User with ID {id} not found"
            });
        }

        return Ok(user);
    }
    
    // Other controller methods follow the same pattern
}
```

## Authentication and Tenant Identification

The application supports multiple methods for tenant identification:

1. **HTTP Header**
   - The `X-Tenant-ID` header can be used to explicitly specify the tenant
   - Useful for service-to-service communication

2. **JWT Token Claims**
   - Claims in the JWT token can identify the tenant
   - The `tenant_id` claim is extracted from the authenticated user's token
   - Secure option for web applications using OAuth/OIDC

## Testing Approach

### Unit Tests

Unit tests include tenant context in all operations:

1. **Mock Services**
   - Mock services are updated to include tenant context parameters
   - Results are filtered based on tenant ID

2. **Controller Tests**
   - Set up tenant context in HttpContext.Items
   - Verify tenant filtering happens correctly

### Integration Tests

Integration tests include tenant headers in requests:

1. **Multi-Tenant Test Suite**
   - Tests tenant isolation
   - Verifies cross-tenant access is prevented
   - Ensures tenant-specific data retrieval

## Security Considerations

1. **Data Isolation**
   - Tenant ID filtering on all database queries
   - No direct database access across tenant boundaries

2. **Authorization**
   - Validate tenant ID before processing requests
   - Check tenant is active and authorized

3. **Error Handling**
   - Return appropriate error responses for missing/invalid tenant IDs
   - Avoid exposing internal tenant identifiers in error messages

## Deployment Considerations

1. **Database Scaling**
   - Indexes on tenant ID fields for performance
   - Consider sharding for very large multi-tenant deployments

2. **Monitoring**
   - Log tenant context with each operation
   - Monitor performance across tenants

3. **Tenant Provisioning**
   - Admin API for tenant management
   - Onboarding and offboarding processes

## Conclusion

The multi-tenant architecture provides a secure, scalable solution for serving multiple customers with a single SCIM Service Provider instance. The design prioritizes data isolation while maintaining SCIM protocol compliance.
