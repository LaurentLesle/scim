using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScimServiceProvider.Formatters;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;

namespace ScimServiceProvider.Controllers
{
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

        [HttpGet]
        public async Task<ActionResult<ScimListResponse<ScimUser>>> GetUsers(
            [FromQuery] int startIndex = 1,
            [FromQuery] int count = 10,
            [FromQuery] string? filter = null)
        {
            // Validate parameters according to SCIM spec
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

            try
            {
                string customerId = GetCustomerId();
                var result = await _userService.GetUsersAsync(customerId, startIndex, count, filter);
                return Ok(result);
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

        [HttpGet("{id}")]
        public async Task<ActionResult<ScimUser>> GetUser(string id)
        {
            try
            {
                string customerId = GetCustomerId();
                var user = await _userService.GetUserAsync(id, customerId);
                if (user == null)
                {
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "User not found" 
                    });
                }

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
                    return BadRequest(new ScimError
                    {
                        Status = 400,
                        Detail = "Missing or invalid 'schemas' property."
                    });
                }
                if (string.IsNullOrEmpty(user.UserName))
                {
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
                    return Conflict(new ScimError
                    {
                        Status = 409,
                        Detail = "User already exists"
                    });
                }

                var createdUser = await _userService.CreateUserAsync(user, customerId);
                return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
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

        [HttpPut("{id}")]
        public async Task<ActionResult<ScimUser>> UpdateUser(string id, [FromBody] ScimUser user)
        {
            try
            {
                // Validate that the ID in the URL matches the ID in the user object
                if (!string.IsNullOrEmpty(user.Id) && user.Id != id)
                {
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
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "User not found" 
                    });
                }

                return Ok(updatedUser);
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

        [HttpPatch("{id}")]
        public async Task<ActionResult<ScimUser>> PatchUser(string id, [FromBody] ScimPatchRequest patchRequest)
        {
            try
            {
                string customerId = GetCustomerId();
                
                var patchedUser = await _userService.PatchUserAsync(id, patchRequest, customerId);
                if (patchedUser == null)
                {
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "User not found" 
                    });
                }

                return Ok(patchedUser);
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

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            try
            {
                string customerId = GetCustomerId();
                
                var deleted = await _userService.DeleteUserAsync(id, customerId);
                if (!deleted)
                {
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "User not found" 
                    });
                }
                return NoContent();
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
    }
}
