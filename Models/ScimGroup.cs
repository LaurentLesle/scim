using System.ComponentModel.DataAnnotations;

namespace ScimServiceProvider.Models
{
    public class ScimGroup
    {
        public string? Id { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("schemas")]
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" };
        public string? ExternalId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
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
