using Microsoft.AspNetCore.Mvc;
using ScimServiceProvider.Formatters;

namespace ScimServiceProvider.Controllers
{
    [ApiController]
    [Route("scim/v2")]
    [Route("")] // Support both /scim/v2/Schemas and /Schemas
    [ScimResult]
    public class SchemasController : ControllerBase
    {
        private readonly ILogger<SchemasController> _logger;

        public SchemasController(ILogger<SchemasController> logger)
        {
            _logger = logger;
        }

        [HttpGet("Schemas")]
        public ActionResult GetSchemas()
        {
            _logger.LogInformation("📄 Schemas endpoint requested - returning array of schemas");
            
            var schemas = new[]
            {
                GetUserSchema(),
                GetGroupSchema()
            };

            _logger.LogInformation("✅ Returning {SchemaCount} schemas in response", schemas.Length);
            return Ok(schemas);
        }

        [HttpGet("Schemas/{schemaUri}")]
        public ActionResult GetSchema(string schemaUri)
        {
            _logger.LogInformation("🔍 Schema requested for URI: {SchemaUri}", schemaUri);
            
            switch (schemaUri)
            {
                case "urn:ietf:params:scim:schemas:core:2.0:User":
                    _logger.LogInformation("✅ Returning User schema");
                    return Ok(GetUserSchema());
                case "urn:ietf:params:scim:schemas:core:2.0:Group":
                    _logger.LogInformation("✅ Returning Group schema");
                    return Ok(GetGroupSchema());
                default:
                    _logger.LogWarning("❌ Schema not found for URI: {SchemaUri}", schemaUri);
                    return NotFound(new { error = "Schema not found" });
            }
        }

        private object GetUserSchema()
        {
            return new
            {
                schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Schema" },
                id = "urn:ietf:params:scim:schemas:core:2.0:User",
                name = "User",
                description = "User Account",
                attributes = new object[]
                {
                    new
                    {
                        name = "id",
                        type = "string",
                        description = "Unique identifier for the SCIM resource as defined by the Service Provider",
                        required = false,
                        caseExact = true,
                        mutability = "readOnly",
                        returned = "always",
                        uniqueness = "server",
                        multiValued = false
                    },
                    new
                    {
                        name = "externalId",
                        type = "string",
                        description = "A String that is an identifier for the resource as defined by the provisioning client",
                        required = false,
                        caseExact = true,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "userName",
                        type = "string",
                        description = "Unique identifier for the User, typically used by the user to directly authenticate",
                        required = true,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "server",
                        multiValued = false
                    },
                    new
                    {
                        name = "name",
                        type = "complex",
                        description = "The components of the user's real name",
                        required = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false,
                        subAttributes = new object[]
                        {
                            new
                            {
                                name = "formatted",
                                type = "string",
                                description = "The full name, including all middle names, titles, and suffixes as appropriate",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "familyName",
                                type = "string",
                                description = "The family name of the User, or last name",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "givenName",
                                type = "string",
                                description = "The given name of the User, or first name",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "middleName",
                                type = "string",
                                description = "The middle name(s) of the User",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "honorificPrefix",
                                type = "string",
                                description = "The honorific prefix(es) of the User, or title",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "honorificSuffix",
                                type = "string",
                                description = "The honorific suffix(es) of the User, or suffix",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            }
                        }
                    },
                    new
                    {
                        name = "displayName",
                        type = "string",
                        description = "The name of the User, suitable for display to end-users",
                        required = false,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "nickName",
                        type = "string",
                        description = "The casual way to address the user in real life",
                        required = false,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "profileUrl",
                        type = "reference",
                        description = "A fully qualified URL pointing to a page representing the User's online profile",
                        required = false,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "title",
                        type = "string",
                        description = "The user's title, such as \"Vice President\"",
                        required = false,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "userType",
                        type = "string",
                        description = "Used to identify the relationship between the organization and the user",
                        required = false,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "preferredLanguage",
                        type = "string",
                        description = "Indicates the User's preferred written or spoken language",
                        required = false,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "locale",
                        type = "string",
                        description = "Used to indicate the User's default location for purposes of localizing items",
                        required = false,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "timezone",
                        type = "string",
                        description = "The User's time zone in the 'Olson' time zone database format",
                        required = false,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "active",
                        type = "boolean",
                        description = "A Boolean value indicating the User's administrative status",
                        required = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "emails",
                        type = "complex",
                        description = "Email addresses for the user",
                        required = false,
                        multiValued = true,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        subAttributes = new object[]
                        {
                            new
                            {
                                name = "value",
                                type = "string",
                                description = "Email addresses for the user",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "display",
                                type = "string",
                                description = "A human-readable name, primarily used for display purposes",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "type",
                                type = "string",
                                description = "A label indicating the attribute's function",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                canonicalValues = new[] { "work", "home", "other" },
                                multiValued = false
                            },
                            new
                            {
                                name = "primary",
                                type = "boolean",
                                description = "A Boolean value indicating the 'primary' or preferred attribute value for this attribute",
                                required = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            }
                        }
                    },
                    new
                    {
                        name = "phoneNumbers",
                        type = "complex",
                        description = "Phone numbers for the User",
                        required = false,
                        multiValued = true,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        subAttributes = new object[]
                        {
                            new
                            {
                                name = "value",
                                type = "string",
                                description = "Phone number of the User",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "display",
                                type = "string",
                                description = "A human-readable name, primarily used for display purposes",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "type",
                                type = "string",
                                description = "A label indicating the attribute's function",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                canonicalValues = new[] { "work", "home", "mobile", "fax", "pager", "other" },
                                multiValued = false
                            },
                            new
                            {
                                name = "primary",
                                type = "boolean",
                                description = "A Boolean value indicating the 'primary' or preferred attribute value for this attribute",
                                required = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            }
                        }
                    },
                    new
                    {
                        name = "addresses",
                        type = "complex",
                        description = "A physical mailing address for this User",
                        required = false,
                        multiValued = true,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        subAttributes = new object[]
                        {
                            new
                            {
                                name = "formatted",
                                type = "string",
                                description = "The full mailing address, formatted for display or use with a mailing label",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "streetAddress",
                                type = "string",
                                description = "The full street address component",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "locality",
                                type = "string",
                                description = "The city or locality component",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "region",
                                type = "string",
                                description = "The state or region component",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "postalCode",
                                type = "string",
                                description = "The zip code or postal code component",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "country",
                                type = "string",
                                description = "The country name component",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "type",
                                type = "string",
                                description = "A label indicating the attribute's function",
                                required = false,
                                caseExact = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                canonicalValues = new[] { "work", "home", "other" },
                                multiValued = false
                            },
                            new
                            {
                                name = "primary",
                                type = "boolean",
                                description = "A Boolean value indicating the 'primary' or preferred attribute value for this attribute",
                                required = false,
                                mutability = "readWrite",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            }
                        }
                    },
                    new
                    {
                        name = "groups",
                        type = "complex",
                        description = "A list of groups to which the user belongs",
                        required = false,
                        multiValued = true,
                        mutability = "readOnly",
                        returned = "default",
                        uniqueness = "none",
                        subAttributes = new object[]
                        {
                            new
                            {
                                name = "value",
                                type = "string",
                                description = "The identifier of the User's group",
                                required = false,
                                caseExact = false,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "$ref",
                                type = "reference",
                                referenceTypes = new[] { "User", "Group" },
                                description = "The URI of the corresponding 'Group' resource to which the user belongs",
                                required = false,
                                caseExact = false,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "display",
                                type = "string",
                                description = "A human-readable name, primarily used for display purposes",
                                required = false,
                                caseExact = false,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "type",
                                type = "string",
                                description = "A label indicating the attribute's function",
                                required = false,
                                caseExact = false,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                canonicalValues = new[] { "direct", "indirect" },
                                multiValued = false
                            }
                        }
                    },
                    new
                    {
                        name = "meta",
                        type = "complex",
                        description = "A complex attribute containing resource metadata",
                        required = false,
                        mutability = "readOnly",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false,
                        subAttributes = new object[]
                        {
                            new
                            {
                                name = "resourceType",
                                type = "string",
                                description = "The name of the resource type of the resource",
                                required = false,
                                caseExact = true,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "created",
                                type = "dateTime",
                                description = "The DateTime the Resource was added to the Service Provider",
                                required = false,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "lastModified",
                                type = "dateTime",
                                description = "The most recent DateTime that the details of this resource were updated",
                                required = false,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "location",
                                type = "string",
                                description = "The URI of the resource being returned",
                                required = false,
                                caseExact = true,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "version",
                                type = "string",
                                description = "The version of the resource being returned",
                                required = false,
                                caseExact = true,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            }
                        }
                    }
                },
                meta = new
                {
                    resourceType = "Schema",
                    location = "/v2/Schemas/urn:ietf:params:scim:schemas:core:2.0:User"
                }
            };
        }

        private object GetGroupSchema()
        {
            return new
            {
                schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Schema" },
                id = "urn:ietf:params:scim:schemas:core:2.0:Group",
                name = "Group",
                description = "Group",
                attributes = new object[]
                {
                    new
                    {
                        name = "id",
                        type = "string",
                        description = "Unique identifier for the SCIM resource as defined by the Service Provider",
                        required = false,
                        caseExact = true,
                        mutability = "readOnly",
                        returned = "always",
                        uniqueness = "server",
                        multiValued = false
                    },
                    new
                    {
                        name = "externalId",
                        type = "string",
                        description = "A String that is an identifier for the resource as defined by the provisioning client",
                        required = false,
                        caseExact = true,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "displayName",
                        type = "string",
                        description = "A human-readable name for the Group",
                        required = true,
                        caseExact = false,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false
                    },
                    new
                    {
                        name = "members",
                        type = "complex",
                        description = "A list of members of the Group",
                        required = false,
                        multiValued = true,
                        mutability = "readWrite",
                        returned = "default",
                        uniqueness = "none",
                        subAttributes = new object[]
                        {
                            new
                            {
                                name = "value",
                                type = "string",
                                description = "Identifier of the member of this Group",
                                required = false,
                                caseExact = false,
                                mutability = "immutable",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "$ref",
                                type = "reference",
                                referenceTypes = new[] { "User", "Group" },
                                description = "The URI corresponding to a SCIM resource that is a member of this Group",
                                required = false,
                                caseExact = false,
                                mutability = "immutable",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "type",
                                type = "string",
                                description = "A label indicating the type of resource",
                                required = false,
                                caseExact = false,
                                mutability = "immutable",
                                returned = "default",
                                uniqueness = "none",
                                canonicalValues = new[] { "User", "Group" },
                                multiValued = false
                            },
                            new
                            {
                                name = "display",
                                type = "string",
                                description = "A human-readable name, primarily used for display purposes",
                                required = false,
                                caseExact = false,
                                mutability = "immutable",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            }
                        }
                    },
                    new
                    {
                        name = "meta",
                        type = "complex",
                        description = "A complex attribute containing resource metadata",
                        required = false,
                        mutability = "readOnly",
                        returned = "default",
                        uniqueness = "none",
                        multiValued = false,
                        subAttributes = new object[]
                        {
                            new
                            {
                                name = "resourceType",
                                type = "string",
                                description = "The name of the resource type of the resource",
                                required = false,
                                caseExact = true,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "created",
                                type = "dateTime",
                                description = "The DateTime the Resource was added to the Service Provider",
                                required = false,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "lastModified",
                                type = "dateTime",
                                description = "The most recent DateTime that the details of this resource were updated",
                                required = false,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "location",
                                type = "string",
                                description = "The URI of the resource being returned",
                                required = false,
                                caseExact = true,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            },
                            new
                            {
                                name = "version",
                                type = "string",
                                description = "The version of the resource being returned",
                                required = false,
                                caseExact = true,
                                mutability = "readOnly",
                                returned = "default",
                                uniqueness = "none",
                                multiValued = false
                            }
                        }
                    }
                },
                meta = new
                {
                    resourceType = "Schema",
                    location = "/v2/Schemas/urn:ietf:params:scim:schemas:core:2.0:Group"
                }
            };
        }
    }
}
