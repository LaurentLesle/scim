using System.ComponentModel.DataAnnotations;

namespace ScimServiceProvider.Models
{
    public class Customer
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string TenantId { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime Created { get; set; } = DateTime.UtcNow;
        
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual ICollection<ScimUser> Users { get; set; } = new List<ScimUser>();
        
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual ICollection<ScimGroup> Groups { get; set; } = new List<ScimGroup>();
    }
}
