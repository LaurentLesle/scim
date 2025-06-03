using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScimServiceProvider.Formatters;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;

namespace ScimServiceProvider.Controllers
{
    [ApiController]
    [Route("scim/v2/[controller]")]
    [Route("[controller]")] // Support both /scim/v2/Groups and /Groups
    [Authorize]
    [ScimResult]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(IGroupService groupService, ILogger<GroupsController> logger)
        {
            _groupService = groupService;
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
        public async Task<ActionResult<ScimListResponse<ScimGroup>>> GetGroups(
            [FromQuery] int startIndex = 1,
            [FromQuery] int count = 10,
            [FromQuery] string? filter = null)
        {
            var customerId = GetCustomerId();
            _logger.LogInformation("👥 GET Groups requested for customer: {CustomerId}, startIndex: {StartIndex}, count: {Count}, filter: {Filter}", 
                customerId, startIndex, count, filter ?? "none");
            
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

            try
            {
                var result = await _groupService.GetGroupsAsync(customerId, startIndex, count, filter);
                _logger.LogInformation("✅ Retrieved {GroupCount} groups for customer: {CustomerId}", 
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
                _logger.LogError("❌ Error retrieving groups: {ErrorMessage}", ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScimGroup>> GetGroup(string id)
        {
            _logger.LogInformation("👥 GET Group requested for ID: {GroupId}", id);
            
            try
            {
                string customerId = GetCustomerId();
                var group = await _groupService.GetGroupAsync(id, customerId);
                if (group == null)
                {
                    _logger.LogWarning("❌ Group not found: {GroupId} for customer: {CustomerId}", id, customerId);
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "Group not found" 
                    });
                }

                _logger.LogInformation("✅ Group retrieved: {GroupId} ({DisplayName}) for customer: {CustomerId}", 
                    id, group.DisplayName, customerId);
                return Ok(group);
            }
            catch (InvalidOperationException iex)
            {
                _logger.LogWarning("❌ Customer context error: {ErrorMessage}", iex.Message);
                return BadRequest(new ScimError { Status = 400, Detail = iex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error retrieving group {GroupId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }

                [HttpPost]
        public async Task<ActionResult<ScimGroup>> CreateGroup([FromBody] ScimGroup group)
        {
            _logger.LogInformation("➕ POST Group requested - creating group with DisplayName: {DisplayName}", group?.DisplayName ?? "null");
            
            if (group == null)
            {
                _logger.LogWarning("❌ Group creation failed - request body is null");
                return BadRequest(new ScimError
                {
                    Status = 400,
                    Detail = "Request body cannot be null"
                });
            }
            
            try
            {
                // If Id is null or empty, generate a new one (SCIM spec: server generates Id)
                if (string.IsNullOrEmpty(group.Id))
                {
                    group.Id = Guid.NewGuid().ToString();
                }
                // SCIM requires the schemas property
                if (group.Schemas == null || !group.Schemas.Contains("urn:ietf:params:scim:schemas:core:2.0:Group"))
                {
                    _logger.LogWarning("❌ Invalid schemas property in create group request");
                    return BadRequest(new ScimError
                    {
                        Status = 400,
                        Detail = "Missing or invalid 'schemas' property."
                    });
                }
                if (string.IsNullOrEmpty(group.DisplayName))
                {
                    _logger.LogWarning("❌ DisplayName is required but not provided in create group request");
                    return BadRequest(new ScimError
                    {
                        Status = 400,
                        Detail = "DisplayName is required"
                    });
                }
        
                string customerId = GetCustomerId();
                var createdGroup = await _groupService.CreateGroupAsync(group, customerId);
                _logger.LogInformation("✅ Group created successfully: {GroupId} ({DisplayName}) for customer: {CustomerId}", 
                    createdGroup.Id, createdGroup.DisplayName, customerId);
                return CreatedAtAction(nameof(GetGroup), new { id = createdGroup.Id }, createdGroup);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning("❌ Group creation failed - group already exists: {DisplayName}", group.DisplayName);
                return Conflict(new ScimError
                {
                    Status = 409,
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error creating group: {ErrorMessage}", ex.Message);
                return StatusCode(500, new ScimError
                {
                    Status = 500,
                    Detail = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ScimGroup>> UpdateGroup(string id, [FromBody] ScimGroup group)
        {
            _logger.LogInformation("✏️ PUT Group requested - updating group: {GroupId}", id);
            
            try
            {
                // Validate that the ID in the URL matches the ID in the group object
                if (!string.IsNullOrEmpty(group.Id) && group.Id != id)
                {
                    _logger.LogWarning("❌ ID mismatch in update group request - URL: {UrlId}, Body: {BodyId}", id, group.Id);
                    return BadRequest(new ScimError 
                    { 
                        Status = 400, 
                        Detail = "ID in URL does not match ID in request body" 
                    });
                }

                string customerId = GetCustomerId();
                var updatedGroup = await _groupService.UpdateGroupAsync(id, group, customerId);
                if (updatedGroup == null)
                {
                    _logger.LogWarning("❌ Group update failed - group not found: {GroupId} for customer: {CustomerId}", id, customerId);
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "Group not found" 
                    });
                }

                _logger.LogInformation("✅ Group updated successfully: {GroupId} ({DisplayName}) for customer: {CustomerId}", 
                    updatedGroup.Id, updatedGroup.DisplayName, customerId);
                return Ok(updatedGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error updating group {GroupId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<ScimGroup>> PatchGroup(string id, [FromBody] ScimPatchRequest patchRequest)
        {
            _logger.LogInformation("🔧 PATCH Group requested - patching group: {GroupId}", id);
            
            try
            {
                string customerId = GetCustomerId();
                var patchedGroup = await _groupService.PatchGroupAsync(id, patchRequest, customerId);
                if (patchedGroup == null)
                {
                    _logger.LogWarning("❌ Group patch failed - group not found: {GroupId} for customer: {CustomerId}", id, customerId);
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "Group not found" 
                    });
                }

                _logger.LogInformation("✅ Group patched successfully: {GroupId} ({DisplayName}) for customer: {CustomerId}", 
                    patchedGroup.Id, patchedGroup.DisplayName, customerId);
                return Ok(patchedGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error patching group {GroupId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteGroup(string id)
        {
            _logger.LogInformation("🗑️ DELETE Group requested - deleting group: {GroupId}", id);
            
            try
            {
                string customerId = GetCustomerId();
                var deleted = await _groupService.DeleteGroupAsync(id, customerId);
                if (!deleted)
                {
                    _logger.LogWarning("❌ Group deletion failed - group not found: {GroupId} for customer: {CustomerId}", id, customerId);
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "Group not found" 
                    });
                }
                
                _logger.LogInformation("✅ Group deleted successfully: {GroupId} for customer: {CustomerId}", id, customerId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error deleting group {GroupId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new ScimError 
                { 
                    Status = 500, 
                    Detail = ex.Message 
                });
            }
        }
    }
}
