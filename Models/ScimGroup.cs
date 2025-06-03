using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScimServiceProvider.Models
{
    public class ScimGroup
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("schemas")]
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" };
        
        [System.Text.Json.Serialization.JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;
        
        // Customer relationship - not serialized in SCIM response
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string CustomerId { get; set; } = string.Empty;
        
        [ForeignKey("CustomerId")]
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual Customer? Customer { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("members")]
        public List<GroupMember> Members { get; set; } = new();
        
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

    public class GroupMember
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "User";
    }
}
