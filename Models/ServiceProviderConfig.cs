namespace ScimServiceProvider.Models
{
    public class ServiceProviderConfig
    {
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig" };
        public string DocumentationUri { get; set; } = "https://example.com/scim/help";
        public PatchConfig Patch { get; set; } = new();
        public BulkConfig Bulk { get; set; } = new();
        public FilterConfig Filter { get; set; } = new();
        public ChangePasswordConfig ChangePassword { get; set; } = new();
        public SortConfig Sort { get; set; } = new();
        public EtagConfig Etag { get; set; } = new();
        public List<AuthScheme> AuthenticationSchemes { get; set; } = new() { new AuthScheme() };
        public MetaConfig Meta { get; set; } = new();
    }
    public class PatchConfig { public bool Supported { get; set; } = true; }
    public class BulkConfig { public bool Supported { get; set; } = false; public int MaxOperations { get; set; } = 0; public int MaxPayloadSize { get; set; } = 0; }
    public class FilterConfig { public bool Supported { get; set; } = true; public int MaxResults { get; set; } = 200; }
    public class ChangePasswordConfig { public bool Supported { get; set; } = false; }
    public class SortConfig { public bool Supported { get; set; } = false; }
    public class EtagConfig { public bool Supported { get; set; } = false; }
    public class AuthScheme {
        public string Name { get; set; } = "OAuth Bearer Token";
        public string Description { get; set; } = "Authentication scheme using the OAuth Bearer Token Standard";
        public string SpecUri { get; set; } = "http://www.rfc-editor.org/info/rfc6750";
        public string DocumentationUri { get; set; } = "https://example.com/help/oauth.html";
        public string Type { get; set; } = "oauthbearertoken";
        public bool Primary { get; set; } = true;
    }
    public class MetaConfig {
        public string Location { get; set; } = "https://example.com/v2/ServiceProviderConfig";
        public string ResourceType { get; set; } = "ServiceProviderConfig";
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1";
    }
}
