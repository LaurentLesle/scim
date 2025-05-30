using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScimServiceProvider.Models
{
    public class ScimUser
    {
        public string? Id { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("schemas")]
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:schemas:core:2.0:User" };
        public string? ExternalId { get; set; }
        public string UserName { get; set; } = string.Empty;
        
        // Customer relationship
        public string CustomerId { get; set; } = string.Empty;
        
        [ForeignKey("CustomerId")]
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual Customer? Customer { get; set; }
        public Name? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? NickName { get; set; }
        public string? ProfileUrl { get; set; }
        public string? Title { get; set; }
        public string? UserType { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? Locale { get; set; }
        public string? Timezone { get; set; }
        public bool Active { get; set; } = true;
        public string? Password { get; set; }
        public List<Email> Emails { get; set; } = new();
        public List<PhoneNumber> PhoneNumbers { get; set; } = new();
        public List<Address> Addresses { get; set; } = new();
        public List<GroupMembership> Groups { get; set; } = new();
        public ScimMeta Meta { get; set; } = new();
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class Name
    {
        public string? Formatted { get; set; }
        public string? FamilyName { get; set; }
        public string? GivenName { get; set; }
        public string? MiddleName { get; set; }
        public string? HonorificPrefix { get; set; }
        public string? HonorificSuffix { get; set; }
    }

    public class Email
    {
        public string Value { get; set; } = string.Empty;
        public string? Display { get; set; }
        public string Type { get; set; } = "work";
        public bool Primary { get; set; } = false;
    }

    public class PhoneNumber
    {
        public string Value { get; set; } = string.Empty;
        public string? Display { get; set; }
        public string Type { get; set; } = "work";
        public bool Primary { get; set; } = false;
    }

    public class Address
    {
        public string? Formatted { get; set; }
        public string? StreetAddress { get; set; }
        public string? Locality { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string Type { get; set; } = "work";
        public bool Primary { get; set; } = false;
    }

    public class GroupMembership
    {
        public string Value { get; set; } = string.Empty;
        public string? Display { get; set; }
        public string Type { get; set; } = "direct";
    }

    public class ScimMeta
    {
        public string ResourceType { get; set; } = "User";
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string? Location { get; set; }
        public string? Version { get; set; }
    }
}
