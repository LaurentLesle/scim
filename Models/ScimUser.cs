using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScimServiceProvider.Models
{
    public class ScimUser
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("schemas")]
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:schemas:core:2.0:User" };
        
        [System.Text.Json.Serialization.JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;
        
        // Customer relationship - not serialized in SCIM response
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string CustomerId { get; set; } = string.Empty;
        
        [ForeignKey("CustomerId")]
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual Customer? Customer { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public Name? Name { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("nickName")]
        public string? NickName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("profileUrl")]
        public string? ProfileUrl { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("userType")]
        public string? UserType { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("preferredLanguage")]
        public string? PreferredLanguage { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("locale")]
        public string? Locale { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("active")]
        public bool Active { get; set; } = true;
        
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        public string? Password { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("emails")]
        public List<Email> Emails { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("phoneNumbers")]
        public List<PhoneNumber> PhoneNumbers { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("addresses")]
        public List<Address> Addresses { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("groups")]
        public List<GroupMembership> Groups { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("roles")]
        public List<Role> Roles { get; set; } = new();
        
        // Enterprise extension
        [System.Text.Json.Serialization.JsonPropertyName("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User")]
        public EnterpriseUser? EnterpriseUser { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("meta")]
        public ScimMeta Meta { get; set; } = new();
        
        // These should be part of meta, not root level - hide from serialization
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public DateTime Created { get; set; } = DateTime.UtcNow;
        
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class Name
    {
        [System.Text.Json.Serialization.JsonPropertyName("formatted")]
        public string? Formatted { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("familyName")]
        public string? FamilyName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("givenName")]
        public string? GivenName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("honorificPrefix")]
        public string? HonorificPrefix { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("honorificSuffix")]
        public string? HonorificSuffix { get; set; }
    }

    public class Email
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "work";
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;
    }

    public class PhoneNumber
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "work";
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;
    }

    public class Address
    {
        [System.Text.Json.Serialization.JsonPropertyName("formatted")]
        public string? Formatted { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("streetAddress")]
        public string? StreetAddress { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("locality")]
        public string? Locality { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("region")]
        public string? Region { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("country")]
        public string? Country { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "work";
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;
    }

    public class GroupMembership
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "direct";
    }

    public class Role
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public string? Primary { get; set; }
    }

    public class EnterpriseUser
    {
        [System.Text.Json.Serialization.JsonPropertyName("employeeNumber")]
        public string? EmployeeNumber { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("department")]
        public string? Department { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("costCenter")]
        public string? CostCenter { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("organization")]
        public string? Organization { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("division")]
        public string? Division { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("manager")]
        public string? Manager { get; set; }
    }

    public class ScimMeta
    {
        [System.Text.Json.Serialization.JsonPropertyName("resourceType")]
        public string ResourceType { get; set; } = "User";
        
        [System.Text.Json.Serialization.JsonPropertyName("created")]
        public DateTime Created { get; set; } = DateTime.UtcNow;
        
        [System.Text.Json.Serialization.JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        
        [System.Text.Json.Serialization.JsonPropertyName("location")]
        public string? Location { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}
