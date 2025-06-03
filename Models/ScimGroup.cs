using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScimServiceProvider.Models
{
    public class ScimGroup
    {
        public string? Id { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("schemas")]
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" };
        public string? ExternalId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        
        // Customer relationship - set by service from authentication context
        // Note: Not [Required] in model binding as it's derived from JWT token, not request body
        public string CustomerId { get; set; } = string.Empty;
        
        [ForeignKey("CustomerId")]
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual Customer? Customer { get; set; }
        public List<GroupMember> Members { get; set; } = new();
        public ScimMeta Meta { get; set; } = new();
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class GroupMember
    {
        public string Value { get; set; } = string.Empty;
        public string? Display { get; set; }
        public string Type { get; set; } = "User";
    }
}
