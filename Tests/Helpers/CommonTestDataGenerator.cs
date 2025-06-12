using Bogus;
using ScimServiceProvider.Models;

namespace ScimServiceProvider.Tests.Helpers
{
    /// <summary>
    /// Provides common test data generation utilities
    /// </summary>
    public static class CommonTestDataGenerator
    {
        private static readonly Faker Faker = new();
        
        /// <summary>
        /// Default test customer ID for testing
        /// </summary>
        public const string DefaultCustomerId = "test-customer-id";

        /// <summary>
        /// Generates a fake customer for testing
        /// </summary>
        public static Customer GenerateCustomer(string? id = null, string? tenantId = null)
        {
            var customerFaker = new Faker<Customer>()
                .RuleFor(c => c.Id, f => id ?? f.Random.Guid().ToString())
                .RuleFor(c => c.Name, f => f.Company.CompanyName())
                .RuleFor(c => c.TenantId, f => tenantId ?? f.Random.AlphaNumeric(10))
                .RuleFor(c => c.Description, f => f.Lorem.Sentence())
                .RuleFor(c => c.IsActive, true)
                .RuleFor(c => c.Created, DateTime.UtcNow)
                .RuleFor(c => c.LastModified, DateTime.UtcNow);

            return customerFaker.Generate();
        }

        /// <summary>
        /// Generates a SCIM patch request for testing
        /// </summary>
        public static ScimPatchRequest GeneratePatchRequest(string operation = "replace", string? path = null, object? value = null)
        {
            return new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new()
                    {
                        Op = operation,
                        Path = path ?? "displayName",
                        Value = value ?? Faker.Name.FullName()
                    }
                }
            };
        }

        /// <summary>
        /// Generates a JWT payload for testing authentication
        /// </summary>
        public static Dictionary<string, object> GenerateJwtPayload()
        {
            return new Dictionary<string, object>
            {
                { "iss", "https://login.microsoftonline.com/test-tenant/v2.0" },
                { "aud", "test-audience" },
                { "sub", Faker.Random.Guid().ToString() },
                { "exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() },
                { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "client_id", "test-client-id" },
                { "tenant_id", "test-tenant" }
            };
        }

        /// <summary>
        /// Creates a SCIM list response for testing
        /// </summary>
        public static ScimListResponse<T> CreateListResponse<T>(List<T> items, int startIndex = 1, int count = 10)
        {
            return new ScimListResponse<T>
            {
                TotalResults = items.Count,
                StartIndex = startIndex,
                ItemsPerPage = Math.Min(count, items.Count),
                Resources = items.Take(count).ToList(),
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:ListResponse" }
            };
        }
    }
}
