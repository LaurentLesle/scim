namespace ScimServiceProvider.Models
{
    public class ScimListResponse<T>
    {
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:api:messages:2.0:ListResponse" };
        public int TotalResults { get; set; }
        public int StartIndex { get; set; } = 1;
        public int ItemsPerPage { get; set; }
        public List<T> Resources { get; set; } = new();
    }

    public class ScimError
    {
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:api:messages:2.0:Error" };
        public string? Detail { get; set; }
        public int Status { get; set; }
        public string? ScimType { get; set; }
    }

    public class PatchOperation
    {
        public string Op { get; set; } = string.Empty; // "add", "remove", "replace"
        public string? Path { get; set; }
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
