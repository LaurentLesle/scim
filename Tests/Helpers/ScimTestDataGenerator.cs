using Bogus;
using ScimServiceProvider.Models;

namespace ScimServiceProvider.Tests.Helpers
{
    /// <summary>
    /// Provides fake test data generation for SCIM entities using Bogus library
    /// </summary>
    public static class ScimTestDataGenerator
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
        /// Generates a fake SCIM user with realistic data
        /// </summary>
        public static ScimUser GenerateUser(string? id = null, string? userName = null, bool active = true, string? customerId = null, string? externalId = null)
        {
            var userFaker = new Faker<ScimUser>()
                .RuleFor(u => u.Id, f => id ?? f.Random.Guid().ToString())
                .RuleFor(u => u.UserName, f => userName ?? f.Internet.Email())
                .RuleFor(u => u.DisplayName, f => f.Name.FullName())
                .RuleFor(u => u.Active, active)
                .RuleFor(u => u.ExternalId, f => externalId ?? f.Random.AlphaNumeric(10))
                .RuleFor(u => u.CustomerId, f => customerId ?? DefaultCustomerId)
                .RuleFor(u => u.Name, (f, u) => new Name
                {
                    GivenName = f.Name.FirstName(),
                    FamilyName = f.Name.LastName(),
                    MiddleName = f.Random.Bool(0.3f) ? f.Name.FirstName() : null,
                    Formatted = f.Name.FullName(),
                    HonorificPrefix = f.Random.Bool(0.1f) ? f.PickRandom("Mr.", "Ms.", "Dr.", "Prof.") : null,
                    HonorificSuffix = f.Random.Bool(0.05f) ? f.PickRandom("Jr.", "Sr.", "III", "PhD") : null
                })
                .RuleFor(u => u.Emails, f => new List<Email>
                {
                    new() { Value = f.Internet.Email(), Type = "work" },
                    new() { Value = f.Internet.Email(), Type = "personal" }
                }.Take(f.Random.Int(1, 2)).ToList())
                .RuleFor(u => u.PhoneNumbers, f => new List<PhoneNumber>
                {
                    new() { Value = f.Phone.PhoneNumber(), Type = "work" },
                    new() { Value = f.Phone.PhoneNumber(), Type = "mobile" }
                }.Take(f.Random.Int(0, 2)).ToList())
                .RuleFor(u => u.Ims, f => f.Random.Bool(0.3f) ? new List<InstantMessaging>
                {
                    new() { Value = f.Internet.UserName(), Type = "skype" },
                    new() { Value = f.Internet.UserName(), Type = "teams" }
                }.Take(f.Random.Int(1, 2)).ToList() : null)
                .RuleFor(u => u.Photos, f => f.Random.Bool(0.2f) ? new List<Photo>
                {
                    new() { Value = f.Internet.Avatar(), Type = "photo" }
                }.ToList() : null)
                .RuleFor(u => u.Entitlements, f => f.Random.Bool(0.4f) ? new List<Entitlement>
                {
                    new() { Value = f.Commerce.Product(), Type = "license" },
                    new() { Value = f.Commerce.Product(), Type = "access" }
                }.Take(f.Random.Int(1, 2)).ToList() : null)
                .RuleFor(u => u.Roles, f => f.Random.Bool(0.5f) ? new List<Role>
                {
                    new() { Value = f.Name.JobTitle(), Type = "role" }
                }.ToList() : null)
                .RuleFor(u => u.X509Certificates, f => f.Random.Bool(0.1f) ? new List<X509Certificate>
                {
                    new() { Value = f.Random.Hash(128), Type = "certificate" }
                }.ToList() : null)
                .RuleFor(u => u.Addresses, f => new List<Address>
                {
                    new()
                    {
                        Type = "work",
                        StreetAddress = f.Address.StreetAddress(),
                        Locality = f.Address.City(),
                        Region = f.Address.State(),
                        PostalCode = f.Address.ZipCode(),
                        Country = f.Address.CountryCode(),
                        Formatted = f.Address.FullAddress()
                    }
                }.Take(f.Random.Int(0, 1)).ToList())
                .RuleFor(u => u.Schemas, new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" })
                .RuleFor(u => u.Meta, f => new ScimMeta
                {
                    ResourceType = "User",
                    Created = f.Date.Past(1),
                    LastModified = f.Date.Recent(30),
                    Location = $"/scim/v2/Users/{id ?? f.Random.Guid().ToString()}",
                    Version = f.Random.Hash(8)
                });

            return userFaker.Generate();
        }

        /// <summary>
        /// Generates a list of fake SCIM users
        /// </summary>
        public static List<ScimUser> GenerateUsers(int count, bool mixActiveStatus = false, string? customerId = null)
        {
            var users = new List<ScimUser>();
            for (int i = 0; i < count; i++)
            {
                var active = mixActiveStatus ? Faker.Random.Bool() : true;
                users.Add(GenerateUser(active: active, customerId: customerId));
            }
            return users;
        }

        /// <summary>
        /// Generates a fake SCIM group with realistic data
        /// </summary>
        public static ScimGroup GenerateGroup(string? id = null, string? displayName = null, List<ScimUser>? members = null, string? customerId = null, string? externalId = null)
        {
            var groupFaker = new Faker<ScimGroup>()
                .RuleFor(g => g.Id, f => id ?? f.Random.Guid().ToString())
                .RuleFor(g => g.DisplayName, f => displayName ?? f.Commerce.Department())
                .RuleFor(g => g.ExternalId, f => externalId ?? f.Random.AlphaNumeric(10))
                .RuleFor(g => g.CustomerId, f => customerId ?? DefaultCustomerId)
                .RuleFor(g => g.Members, (f, g) =>
                {
                    if (members != null)
                    {
                        return members.Select(u => new GroupMember
                        {
                            Value = u.Id!,
                            Display = u.DisplayName
                        }).ToList();
                    }

                    var memberCount = f.Random.Int(0, 5);
                    return Enumerable.Range(0, memberCount)
                        .Select(_ => new GroupMember
                        {
                            Value = f.Random.Guid().ToString(),
                            Display = f.Name.FullName()
                        }).ToList();
                })
                .RuleFor(g => g.Schemas, new List<string> { "urn:ietf:params:scim:schemas:core:2.0:Group" })
                .RuleFor(g => g.Meta, f => new ScimMeta
                {
                    ResourceType = "Group",
                    Created = f.Date.Past(1),
                    LastModified = f.Date.Recent(30),
                    Location = $"/scim/v2/Groups/{id ?? f.Random.Guid().ToString()}",
                    Version = f.Random.Hash(8)
                });

            return groupFaker.Generate();
        }

        /// <summary>
        /// Generates a list of fake SCIM groups
        /// </summary>
        public static List<ScimGroup> GenerateGroups(int count, List<ScimUser>? availableUsers = null, string? customerId = null)
        {
            var groups = new List<ScimGroup>();
            for (int i = 0; i < count; i++)
            {
                var memberUsers = availableUsers?.Take(Faker.Random.Int(0, Math.Min(3, availableUsers.Count))).ToList();
                groups.Add(GenerateGroup(members: memberUsers, customerId: customerId));
            }
            return groups;
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
                        Path = path ?? "active",
                        Value = value ?? Faker.Random.Bool()
                    }
                }
            };
        }

        /// <summary>
        /// Generates a realistic JWT token payload for testing
        /// </summary>
        public static Dictionary<string, object> GenerateJwtPayload()
        {
            return new Dictionary<string, object>
            {
                ["sub"] = Faker.Random.Guid().ToString(),
                ["client_id"] = "scim_client",
                ["iss"] = "https://localhost:5001",
                ["aud"] = "https://localhost:5001",
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["scope"] = "scim"
            };
        }

        /// <summary>
        /// Creates a sample list response for testing pagination
        /// </summary>
        public static ScimListResponse<T> CreateListResponse<T>(List<T> items, int startIndex = 1, int count = 10)
        {
            var pagedItems = items.Skip(startIndex - 1).Take(count).ToList();
            
            return new ScimListResponse<T>
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
                TotalResults = items.Count,
                StartIndex = startIndex,
                ItemsPerPage = pagedItems.Count,
                Resources = pagedItems
            };
        }
    }
}
