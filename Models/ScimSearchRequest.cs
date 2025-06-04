namespace ScimServiceProvider.Models
{
    public class ScimSearchRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("schemas")]
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:api:messages:2.0:SearchRequest" };

        [System.Text.Json.Serialization.JsonPropertyName("attributes")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Attributes { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("excludedAttributes")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? ExcludedAttributes { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("filter")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Filter { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("sortBy")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? SortBy { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("sortOrder")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? SortOrder { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("startIndex")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int? StartIndex { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("count")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int? Count { get; set; }
    }
}
