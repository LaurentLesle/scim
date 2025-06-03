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
                
                // Extract tenant id from JWT token (primary method for SCIM compliance)
                string? tenantId = null;
                string? customerId = null;
                
                // Primary method: Extract tenant_id from JWT token (SCIM compliance)
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var jwtTenantId = context.User.FindFirstValue("tenant_id");
                    if (!string.IsNullOrEmpty(jwtTenantId))
                    {
                        tenantId = jwtTenantId;
                        _logger.LogInformation("🏢 Found tenant ID in JWT token: {TenantId}", tenantId);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No tenant_id claim found in JWT token");
                    }
                }
                
                // Fallback methods (for backward compatibility, but not required for SCIM compliance)
                // Option 1: From Customer-Id header (backward compatibility only)
                if (string.IsNullOrEmpty(tenantId) && context.Request.Headers.TryGetValue("Customer-Id", out var customerIdHeader))
                {
                    customerId = customerIdHeader.ToString();
                    _logger.LogInformation("🏢 Found customer ID in header (fallback): {CustomerId}", customerId);
                }
                
                // Option 2: From X-Tenant-ID header (backward compatibility only)
                if (string.IsNullOrEmpty(tenantId) && context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader))
                {
                    tenantId = tenantIdHeader.ToString();
                    _logger.LogInformation("🏢 Found tenant ID in header (fallback): {TenantId}", tenantId);
                }
                
                // Process customer context from tenant ID or direct customer ID
                if (!string.IsNullOrEmpty(tenantId))
                {
                    // Derive customer from tenant ID (preferred SCIM-compliant method)
                    var customer = await customerService.GetCustomerByTenantIdAsync(tenantId);
                    if (customer == null || !customer.IsActive)
                    {
                        _logger.LogWarning("❌ Invalid or inactive tenant: {TenantId}", tenantId);
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive tenant" });
                        return;
                    }

                    context.Items["CustomerId"] = customer.Id;
                    _logger.LogInformation("✅ Customer context set: {CustomerId} for tenant: {TenantId}", customer.Id, tenantId);
                }
                else if (!string.IsNullOrEmpty(customerId))
                {
                    // Fallback: Direct customer ID from header (backward compatibility)
                    var directCustomer = await customerService.GetCustomerAsync(customerId);
                    if (directCustomer == null || !directCustomer.IsActive)
                    {
                        _logger.LogWarning("❌ Invalid or inactive customer: {CustomerId}", customerId);
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive customer" });
                        return;
                    }
                    
                    context.Items["CustomerId"] = directCustomer.Id;
                    _logger.LogInformation("✅ Customer context set directly (fallback): {CustomerId}", directCustomer.Id);
                }
                else
                {
                    _logger.LogWarning("❌ Missing tenant identifier in JWT token for path: {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Missing tenant identifier in authentication token" });
                    return;
                }
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
