using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ScimServiceProvider.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("token")]
        public ActionResult<object> GenerateToken([FromBody] TokenRequest request)
        {
            _logger.LogInformation("🔐 Auth token request received from client: {ClientId}", request.ClientId);
            
            // Simple authentication for demo purposes
            // In production, validate against your authentication system
            if (request.ClientId == "scim_client" && request.ClientSecret == "scim_secret")
            {
                _logger.LogInformation("✅ Authentication successful for client: {ClientId}", request.ClientId);
                
                // Use provided tenant ID or default to tenant1 for backward compatibility
                var tenantId = !string.IsNullOrEmpty(request.TenantId) ? request.TenantId : "tenant1";
                _logger.LogInformation("🏢 Using tenant ID: {TenantId} for client: {ClientId}", tenantId, request.ClientId);
                
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "");
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, request.ClientId),
                        new Claim(ClaimTypes.Role, "SCIMClient"),
                        new Claim("tenant_id", tenantId) // Add tenant ID as a claim
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation("🎫 JWT token generated successfully for client: {ClientId}, tenant: {TenantId}, expires in 1 hour", request.ClientId, tenantId);

                return Ok(new
                {
                    access_token = tokenString,
                    token_type = "Bearer",
                    expires_in = 3600
                });
            }

            _logger.LogWarning("❌ Authentication failed for client: {ClientId} - invalid credentials", request.ClientId);
            return Unauthorized(new { error = "invalid_client" });
        }
    }

    public class TokenRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string GrantType { get; set; } = "client_credentials";
        public string? TenantId { get; set; } // Optional tenant ID for multi-tenant scenarios
    }
}
