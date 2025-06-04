using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScimServiceProvider.Formatters;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;

namespace ScimServiceProvider.Controllers
{
    [ApiController]
    [Route("scim/v2/[controller]")]
    [Route("[controller]")] // Support both /scim/v2/Users and /Users
    [Authorize]
    [ScimResult]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
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

        [HttpGet]
        public async Task<ActionResult<ScimListResponse<ScimUser>>> GetUsers(
            [FromQuery] int startIndex = 1,
            [FromQuery] int count = 10,
            [FromQuery] string? filter = null,
            [FromQuery] string? attributes = null,
            [FromQuery] string? excludedAttributes = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            var customerId = GetCustomerId();
            _logger.LogInformation("👥 GET Users requested for customer: {CustomerId}, startIndex: {StartIndex}, count: {Count}, filter: {Filter}, attributes: {Attributes}, sortBy: {SortBy}", 
                customerId, startIndex, count, filter ?? "none", attributes ?? "none", sortBy ?? "none");
            
            // Validate parameters according to SCIM spec
            if (startIndex <= 0)
            {
                _logger.LogWarning("❌ Invalid startIndex: {StartIndex} - must be greater than 0", startIndex);
                return BadRequest(new ScimError 
                { 
                    Status = 400, 
                    Detail = "startIndex must be greater than 0" 
                });
            }

            if (count <= 0)
            {
                _logger.LogWarning("❌ Invalid count: {Count} - must be greater than 0", count);
                return BadRequest(new ScimError 
                { 
                    Status = 400, 
                    Detail = "count must be greater than 0" 
                });
            }

            // Validate sortOrder if provided
            if (!string.IsNullOrEmpty(sortOrder) && 
                !string.Equals(sortOrder, "ascending", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("❌ Invalid sortOrder: {SortOrder} - must be 'ascending' or 'descending'", sortOrder);
                return BadRequest(new ScimError 
                { 
                    Status = 400, 
                    Detail = "sortOrder must be 'ascending' or 'descending'" 
                });
            }

            try
            {
                var result = await _userService.GetUsersAsync(customerId, startIndex, count, filter, attributes, excludedAttributes, sortBy, sortOrder);
                _logger.LogInformation("✅ Retrieved {UserCount} users for customer: {CustomerId}", 
                    result.TotalResults, customerId);
                return Ok(result);
            }
            catch (InvalidOperationException iex)
            {
                _logger.LogWarning("❌ Customer context error: {ErrorMessage}", iex.Message);
                return BadRequest(new ScimError { Status = 400, Detail = iex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error retrieving users: {ErrorMessage}", ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScimUser>> GetUser(string id)
        {
            _logger.LogInformation("👤 GET User requested for ID: {UserId}", id);
            
            try
            {
                string customerId = GetCustomerId();
                var user = await _userService.GetUserAsync(id, customerId);
                if (user == null)
                {
                    _logger.LogWarning("❌ User not found: {UserId} for customer: {CustomerId}", id, customerId);
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "User not found" 
                    });
                }

                _logger.LogInformation("✅ User retrieved: {UserId} ({UserName}) for customer: {CustomerId}", 
                    id, user.UserName, customerId);
                return Ok(user);
            }
            catch (InvalidOperationException iex)
            {
                return BadRequest(new ScimError { Status = 400, Detail = iex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }


        [HttpPost]
        public async Task<ActionResult<ScimUser>> CreateUser([FromBody] ScimUser user)
        {
            _logger.LogInformation("➕ POST User requested - creating user with UserName: {UserName}", user?.UserName ?? "null");
            
            if (user == null)
            {
                _logger.LogWarning("❌ User creation failed - request body is null");
                return BadRequest(new ScimError
                {
                    Status = 400,
                    Detail = "Request body cannot be null"
                });
            }
            
            try
            {
                // If Id is null or empty, generate a new one (SCIM spec: server generates Id)
                if (string.IsNullOrEmpty(user.Id))
                {
                    user.Id = Guid.NewGuid().ToString();
                }
                // SCIM requires the schemas property
                if (user.Schemas == null || !user.Schemas.Contains("urn:ietf:params:scim:schemas:core:2.0:User"))
                {
                    _logger.LogWarning("❌ Invalid schemas property in create user request");
                    return BadRequest(new ScimError
                    {
                        Status = 400,
                        Detail = "Missing or invalid 'schemas' property."
                    });
                }
                if (string.IsNullOrEmpty(user.UserName))
                {
                    _logger.LogWarning("❌ UserName is required but not provided in create user request");
                    return BadRequest(new ScimError
                    {
                        Status = 400,
                        Detail = "UserName is required"
                    });
                }

                // Get customer ID from context
                string customerId = GetCustomerId();
            
                // Check if user already exists
                var existingUser = await _userService.GetUserByUsernameAsync(user.UserName, customerId);
                if (existingUser != null)
                {
                    _logger.LogWarning("❌ User creation failed - user already exists: {UserName} for customer: {CustomerId}", user.UserName, customerId);
                    return Conflict(new ScimError
                    {
                        Status = 409,
                        Detail = "User already exists"
                    });
                }

                var createdUser = await _userService.CreateUserAsync(user, customerId);
                _logger.LogInformation("✅ User created successfully: {UserId} ({UserName}) for customer: {CustomerId}", 
                    createdUser.Id, createdUser.UserName, customerId);
                return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error creating user: {ErrorMessage}", ex.Message);
                return StatusCode(500, new ScimError
                {
                    Status = 500,
                    Detail = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ScimUser>> UpdateUser(string id, [FromBody] ScimUser user)
        {
            _logger.LogInformation("✏️ PUT User requested - updating user: {UserId}", id);
            
            try
            {
                // Validate that the ID in the URL matches the ID in the user object
                if (!string.IsNullOrEmpty(user.Id) && user.Id != id)
                {
                    _logger.LogWarning("❌ ID mismatch in update user request - URL: {UrlId}, Body: {BodyId}", id, user.Id);
                    return BadRequest(new ScimError 
                    { 
                        Status = 400, 
                        Detail = "ID in URL does not match ID in request body" 
                    });
                }

                string customerId = GetCustomerId();
                
                var updatedUser = await _userService.UpdateUserAsync(id, user, customerId);
                if (updatedUser == null)
                {
                    _logger.LogWarning("❌ User update failed - user not found: {UserId} for customer: {CustomerId}", id, customerId);
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "User not found" 
                    });
                }

                _logger.LogInformation("✅ User updated successfully: {UserId} ({UserName}) for customer: {CustomerId}", 
                    updatedUser.Id, updatedUser.UserName, customerId);
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error updating user {UserId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<ScimUser>> PatchUser(string id, [FromBody] ScimPatchRequest patchRequest)
        {
            _logger.LogInformation("🔧 PATCH User requested - patching user: {UserId}", id);
            
            try
            {
                string customerId = GetCustomerId();
                
                var patchedUser = await _userService.PatchUserAsync(id, patchRequest, customerId);
                if (patchedUser == null)
                {
                    _logger.LogWarning("❌ User patch failed - user not found: {UserId} for customer: {CustomerId}", id, customerId);
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "User not found" 
                    });
                }

                _logger.LogInformation("✅ User patched successfully: {UserId} ({UserName}) for customer: {CustomerId}", 
                    patchedUser.Id, patchedUser.UserName, customerId);
                return Ok(patchedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error patching user {UserId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            _logger.LogInformation("🗑️ DELETE User requested - deleting user: {UserId}", id);
            
            try
            {
                string customerId = GetCustomerId();
                
                var deleted = await _userService.DeleteUserAsync(id, customerId);
                if (!deleted)
                {
                    _logger.LogWarning("❌ User deletion failed - user not found: {UserId} for customer: {CustomerId}", id, customerId);
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "User not found" 
                    });
                }
                
                _logger.LogInformation("✅ User deleted successfully: {UserId} for customer: {CustomerId}", id, customerId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error deleting user {UserId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }

        [HttpPost(".search")]
        public async Task<ActionResult<ScimListResponse<ScimUser>>> SearchUsers([FromBody] ScimSearchRequest searchRequest)
        {
            var customerId = GetCustomerId();
            _logger.LogInformation("🔍 POST Users/.search requested for customer: {CustomerId}, filter: {Filter}", 
                customerId, searchRequest?.Filter ?? "none");

            if (searchRequest == null)
            {
                return BadRequest(new ScimError
                {
                    Status = 400,
                    Detail = "Search request body is required"
                });
            }

            // Validate parameters
            var startIndex = searchRequest.StartIndex ?? 1;
            var count = searchRequest.Count ?? 10;

            if (startIndex <= 0)
            {
                return BadRequest(new ScimError
                {
                    Status = 400,
                    Detail = "startIndex must be greater than 0"
                });
            }

            if (count <= 0)
            {
                return BadRequest(new ScimError
                {
                    Status = 400,
                    Detail = "count must be greater than 0"
                });
            }

            // Validate sortOrder if provided
            if (!string.IsNullOrEmpty(searchRequest.SortOrder) &&
                !string.Equals(searchRequest.SortOrder, "ascending", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(searchRequest.SortOrder, "descending", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ScimError
                {
                    Status = 400,
                    Detail = "sortOrder must be 'ascending' or 'descending'"
                });
            }

            try
            {
                var result = await _userService.GetUsersAsync(customerId, startIndex, count, 
                    searchRequest.Filter, searchRequest.Attributes, searchRequest.ExcludedAttributes,
                    searchRequest.SortBy, searchRequest.SortOrder);
                _logger.LogInformation("✅ Search returned {UserCount} users for customer: {CustomerId}", 
                    result.TotalResults, customerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error in search users: {ErrorMessage}", ex.Message);
                return StatusCode(500, new ScimError
                {
                    Status = 500,
                    Detail = ex.Message
                });
            }
        }
    }
}
