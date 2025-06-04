using Microsoft.AspNetCore.Mvc;
using ScimServiceProvider.Formatters;

namespace ScimServiceProvider.Controllers
{
    [ApiController]
    [Route("")]
    [ScimResult]
    public class ResourceTypesController : ControllerBase
    {
        [HttpGet("ResourceTypes")]
        public ActionResult GetResourceTypes()
        {
            var resourceTypes = new object[]
            {
                new
                {
                    schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" },
                    id = "User",
                    name = "User",
                    endpoint = "/Users",
                    description = "User Account",
                    schema = "urn:ietf:params:scim:schemas:core:2.0:User",
                    schemaExtensions = new[]
                    {
                        new
                        {
                            schema = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User",
                            required = true
                        }
                    },
                    meta = new
                    {
                        location = "https://example.com/v2/ResourceTypes/User",
                        resourceType = "ResourceType"
                    }
                },
                new
                {
                    schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" },
                    id = "Group",
                    name = "Group",
                    endpoint = "/Groups",
                    description = "Group",
                    schema = "urn:ietf:params:scim:schemas:core:2.0:Group",
                    meta = new
                    {
                        location = "https://example.com/v2/ResourceTypes/Group",
                        resourceType = "ResourceType"
                    }
                }
            };

            return Ok(resourceTypes);
        }
    }
}
