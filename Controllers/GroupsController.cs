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
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
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
                var result = await _groupService.GetGroupsAsync(customerId, startIndex, count, filter);
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
        public async Task<ActionResult<ScimGroup>> GetGroup(string id)
        {
            try
            {
                string customerId = GetCustomerId();
                var group = await _groupService.GetGroupAsync(id, customerId);
                if (group == null)
                {
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "Group not found" 
                    });
                }

                return Ok(group);
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
        public async Task<ActionResult<ScimGroup>> CreateGroup([FromBody] ScimGroup group)
        {
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
                    return BadRequest(new ScimError
                    {
                        Status = 400,
                        Detail = "Missing or invalid 'schemas' property."
                    });
                }
                if (string.IsNullOrEmpty(group.DisplayName))
                {
                    return BadRequest(new ScimError
                    {
                        Status = 400,
                        Detail = "DisplayName is required"
                    });
                }
        
                string customerId = GetCustomerId();
                var createdGroup = await _groupService.CreateGroupAsync(group, customerId);
                return CreatedAtAction(nameof(GetGroup), new { id = createdGroup.Id }, createdGroup);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new ScimError
                {
                    Status = 409,
                    Detail = ex.Message
                });
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
        public async Task<ActionResult<ScimGroup>> UpdateGroup(string id, [FromBody] ScimGroup group)
        {
            try
            {
                // Validate that the ID in the URL matches the ID in the group object
                if (!string.IsNullOrEmpty(group.Id) && group.Id != id)
                {
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
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "Group not found" 
                    });
                }

                return Ok(updatedGroup);
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
        public async Task<ActionResult<ScimGroup>> PatchGroup(string id, [FromBody] ScimPatchRequest patchRequest)
        {
            try
            {
                string customerId = GetCustomerId();
                var patchedGroup = await _groupService.PatchGroupAsync(id, patchRequest, customerId);
                if (patchedGroup == null)
                {
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "Group not found" 
                    });
                }

                return Ok(patchedGroup);
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
        public async Task<ActionResult> DeleteGroup(string id)
        {
            try
            {
                string customerId = GetCustomerId();
                var deleted = await _groupService.DeleteGroupAsync(id, customerId);
                if (!deleted)
                {
                    return NotFound(new ScimError 
                    { 
                        Status = 404, 
                        Detail = "Group not found" 
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
