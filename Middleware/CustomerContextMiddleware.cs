using ScimServiceProvider.Services;
using System.Security.Claims;

namespace ScimServiceProvider.Middleware
{
    public class CustomerContextMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomerContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICustomerService customerService)
        {
            // Only handle SCIM endpoints
            if (context.Request.Path.Value?.StartsWith("/scim/v2/") == true)
            {
                // Extract tenant id from auth token or header
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

    // Extension method for middleware registration
    public static class CustomerContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomerContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomerContextMiddleware>();
        }
    }
}
