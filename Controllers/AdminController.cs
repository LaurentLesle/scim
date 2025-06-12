using Microsoft.AspNetCore.Mvc;
using ScimServiceProvider.Services;
using ScimServiceProvider.Models;
using System.Text;

namespace ScimServiceProvider.Controllers
{
    public class AdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IGroupService _groupService;
        private readonly ICustomerService _customerService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUserService userService, 
            IGroupService groupService, 
            ICustomerService customerService,
            ILogger<AdminController> logger)
        {
            _userService = userService;
            _groupService = groupService;
            _customerService = customerService;
            _logger = logger;
        }

        /// <summary>
        /// Redirect to admin login page
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            if (IsAdminAuthenticated())
            {
                return Redirect("/admin.html");
            }
            return Redirect("/admin-login.html");
        }

        /// <summary>
        /// Admin authentication endpoint
        /// </summary>
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] AdminLoginRequest request)
        {
            try
            {
                if (request.Username == "scim_admin" && request.Password == "admin123")
                {
                    var adminToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"admin:{DateTime.UtcNow.Ticks}"));
                    
                    Response.Cookies.Append("AdminSession", adminToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddHours(8)
                    });
                    
                    return Ok(new { 
                        success = true, 
                        token = adminToken,
                        message = "Authentication successful" 
                    });
                }
                
                return Unauthorized(new { error = "Invalid username or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin authentication");
                return StatusCode(500, new { error = "Authentication failed" });
            }
        }

        /// <summary>
        /// Admin logout endpoint
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AdminSession");
            return Ok(new { success = true, message = "Logged out successfully" });
        }

        /// <summary>
        /// Get all customers (secured endpoint)
        /// </summary>
        [HttpGet("api/customers")]
        public async Task<IActionResult> GetCustomers()
        {
            if (!IsAdminAuthenticated())
            {
                return Unauthorized(new { error = "Authentication required" });
            }

            try
            {
                var customers = await _customerService.GetAllCustomersAsync();
                return Ok(customers.Select(c => new 
                {
                    Id = c.Id,
                    Name = c.Name,
                    TenantId = c.TenantId,
                    IsActive = c.IsActive,
                    Created = c.Created,
                    TokenGenerationInfo = new
                    {
                        ClientId = "scim_client",
                        ClientSecret = "scim_secret",
                        GrantType = "client_credentials",
                        TenantId = c.TenantId,
                        TokenEndpoint = "/api/auth/token",
                        ExampleCurl = $"curl -X POST \"http://localhost:5000/api/auth/token\" -H \"Content-Type: application/json\" -d '{{\"clientId\": \"scim_client\",\"clientSecret\": \"scim_secret\",\"grantType\": \"client_credentials\",\"tenantId\": \"{c.TenantId}\"}}'",
                        SCIMEndpoints = new
                        {
                            Users = "http://localhost:5000/scim/v2/Users",
                            Groups = "http://localhost:5000/scim/v2/Groups"
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return StatusCode(500, new { error = "Error retrieving customers" });
            }
        }

        /// <summary>
        /// Check if user is authenticated as admin
        /// </summary>
        private bool IsAdminAuthenticated()
        {
            if (Request.Cookies.TryGetValue("AdminSession", out var sessionToken))
            {
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(sessionToken));
                    return decoded.StartsWith("admin:");
                }
                catch
                {
                    return false;
                }
            }
            
            if (Request.Headers.TryGetValue("X-Admin-Token", out var headerToken) && headerToken.FirstOrDefault() != null)
            {
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(headerToken.FirstOrDefault()!));
                    return decoded.StartsWith("admin:");
                }
                catch
                {
                    return false;
                }
            }
            
            return false;
        }
    }
}
