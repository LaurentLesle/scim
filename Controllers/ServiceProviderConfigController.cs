using Microsoft.AspNetCore.Mvc;
using ScimServiceProvider.Formatters;

namespace ScimServiceProvider.Controllers
{
    [ApiController]
    [Route("scim/v2")]
    [ScimResult]
    public class ServiceProviderConfigController : ControllerBase
    {
        [HttpGet("ServiceProviderConfig")]
        public ActionResult GetServiceProviderConfig()
        {
            var config = new ScimServiceProvider.Models.ServiceProviderConfig();
            var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            return new ContentResult {
                Content = json,
                ContentType = "application/scim+json",
                StatusCode = 200
            };
        }

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

        [HttpGet("ResourceTypes/{id}")]
        public ActionResult GetResourceType(string id)
        {
            switch (id.ToLower())
            {
                case "user":
                    return Ok(new
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
                    });
                case "group":
                    return Ok(new
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
                    });
                default:
                    return NotFound(new { error = "ResourceType not found" });
            }
        }
    }
}
