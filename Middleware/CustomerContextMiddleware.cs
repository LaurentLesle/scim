using ScimServiceProvider.Services;
using System.Security.Claims;

namespace ScimServiceProvider.Middleware
{
    public class CustomerContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomerContextMiddleware> _logger;

        public CustomerContextMiddleware(RequestDelegate next, ILogger<CustomerContextMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICustomerService customerService)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();
            
            // Skip tenant validation for tenant-agnostic endpoints
            var tenantAgnosticPaths = new[]
            {
                "/schemas",  // Root-level schemas endpoint
                "/scim/v2/schemas",
                "/scim/v2/resourcetypes", 
                "/scim/v2/serviceproviderconfig",
                "/api/auth" // Auth endpoints
            };
            
            if (tenantAgnosticPaths.Any(agnosticPath => path?.StartsWith(agnosticPath) == true))
            {
                // Skip tenant validation for these endpoints
                await _next(context);
                return;
            }
            
            // Handle SCIM endpoints that require tenant validation (both /scim/v2/ and root paths)
            if (context.Request.Path.Value?.StartsWith("/scim/v2/") == true || 
                path?.StartsWith("/users") == true ||
                path?.StartsWith("/groups") == true)
            {
                _logger.LogInformation("🏢 Processing tenant validation for path: {Path}", context.Request.Path);
                
                // Extract tenant id from auth token or header
                string? tenantId = null;
                
                // Option 1: From custom header
                if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader))
                {
                    tenantId = tenantIdHeader.ToString();
                    _logger.LogInformation("🏢 Found tenant ID in header: {TenantId}", tenantId);
                }
                
                // Option 2: From claim in JWT
                if (string.IsNullOrEmpty(tenantId) && context.User?.Identity?.IsAuthenticated == true)
                {
                    tenantId = context.User.FindFirstValue("tenant_id");
                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        _logger.LogInformation("🏢 Found tenant ID in JWT token: {TenantId}", tenantId);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No tenant_id claim found in JWT token");
                    }
                }
                
                if (string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogWarning("❌ Missing tenant identifier for path: {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Missing tenant identifier" });
                    return;
                }

                // Verify tenant exists
                var customer = await customerService.GetCustomerByTenantIdAsync(tenantId);
                if (customer == null || !customer.IsActive)
                {
                    _logger.LogWarning("❌ Invalid or inactive tenant: {TenantId}", tenantId);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive tenant" });
                    return;
                }

                // Store customer ID in items collection for use in controllers
                context.Items["CustomerId"] = customer.Id;
                _logger.LogInformation("✅ Customer context set: {CustomerId} for tenant: {TenantId}", customer.Id, tenantId);
            }

            await _next(context);
        }
    }

    // Extension method for middleware registration
    public static class CustomerContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomerContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomerContextMiddleware>();
        }
    }
}
