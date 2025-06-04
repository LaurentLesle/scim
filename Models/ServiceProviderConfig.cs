namespace ScimServiceProvider.Models
{
    public class ServiceProviderConfig
    {
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig" };
        public string? DocumentationUri { get; set; } = "http://example.com/help/scim.html";
        public PatchConfig Patch { get; set; } = new();
        public BulkConfig Bulk { get; set; } = new();
        public FilterConfig Filter { get; set; } = new();
        public ChangePasswordConfig ChangePassword { get; set; } = new();
        public SortConfig Sort { get; set; } = new();
        public List<AuthScheme> AuthenticationSchemes { get; set; } = new() 
        { 
            new AuthScheme 
            {
                Name = "OAuth Bearer Token",
                Description = "Authentication scheme using the OAuth Bearer Token Standard",
                SpecUri = "http://www.rfc-editor.org/info/rfc6750",
                DocumentationUri = "http://example.com/help/oauth.html"
            },
            new AuthScheme
            {
                Name = "HTTP Basic",
                Description = "Authentication scheme using the HTTP Basic Standard",
                SpecUri = "http://www.rfc-editor.org/info/rfc2617",
                DocumentationUri = "http://example.com/help/httpBasic.html"
            }
        };
        public MetaConfig Meta { get; set; } = new();
    }
    
    public class PatchConfig 
    { 
        public bool Supported { get; set; } = true; 
    }
    
    public class BulkConfig 
    { 
        public bool Supported { get; set; } = false; 
        public int MaxOperations { get; set; } = 1000; 
        public int MaxPayloadSize { get; set; } = 1048576; 
    }
    
    public class FilterConfig 
    { 
        public bool Supported { get; set; } = true; 
        public int MaxResults { get; set; } = 200; 
    }
    
    public class ChangePasswordConfig 
    { 
        public bool Supported { get; set; } = true; 
    }
    
    public class SortConfig 
    { 
        public bool Supported { get; set; } = true; 
    }
    
    public class AuthScheme 
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string? SpecUri { get; set; } = "";
        public string? DocumentationUri { get; set; } = "";
    }
    
    public class MetaConfig 
    {
        public string Location { get; set; } = "https://example.com/v2/ServiceProviderConfig";
        public string ResourceType { get; set; } = "ServiceProviderConfig";
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "W/\"3694e05e9dff594\"";
    }
}
