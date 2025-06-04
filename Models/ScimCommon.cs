namespace ScimServiceProvider.Models
{
    public class ScimListResponse<T>
    {
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:api:messages:2.0:ListResponse" };
        public int TotalResults { get; set; }
        public int StartIndex { get; set; } = 1;
        public int ItemsPerPage { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("Resources")]
        [Newtonsoft.Json.JsonProperty("Resources")]
        public List<T> Resources { get; set; } = new();
    }

    public class ScimError
    {
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:api:messages:2.0:Error" };
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; set; }
        public int Status { get; set; }
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? ScimType { get; set; }
    }

    public class PatchOperation
    {
        public string Op { get; set; } = string.Empty; // "add", "remove", "replace"
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Path { get; set; }
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object? Value { get; set; }
    }

    // Alias for PatchOperation to match test expectations
    public class ScimPatchOperation : PatchOperation
    {
    }

    public class ScimPatchRequest
    {
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:api:messages:2.0:PatchOp" };
        public List<ScimPatchOperation> Operations { get; set; } = new();
    }

    // Aliases for types defined in ScimUser.cs to match test expectations
    public class ScimName : Name
    {
    }

    // Aliases for types defined in ScimGroup.cs to match test expectations  
    public class ScimGroupMember : GroupMember
    {
    }
}
