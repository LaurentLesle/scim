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
        /// Generates a fake SCIM user with realistic data
        /// </summary>
        public static ScimUser GenerateUser(string? id = null, string? userName = null, bool active = true)
        {
            var userFaker = new Faker<ScimUser>()
                .RuleFor(u => u.Id, f => id ?? f.Random.Guid().ToString())
                .RuleFor(u => u.UserName, f => userName ?? f.Internet.Email())
                .RuleFor(u => u.DisplayName, f => f.Name.FullName())
                .RuleFor(u => u.Active, active)
                .RuleFor(u => u.ExternalId, f => f.Random.AlphaNumeric(10))
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
                    new() { Value = f.Internet.Email(), Type = "work", Primary = true },
                    new() { Value = f.Internet.Email(), Type = "personal", Primary = false }
                }.Take(f.Random.Int(1, 2)).ToList())
                .RuleFor(u => u.PhoneNumbers, f => new List<PhoneNumber>
                {
                    new() { Value = f.Phone.PhoneNumber(), Type = "work", Primary = true },
                    new() { Value = f.Phone.PhoneNumber(), Type = "mobile", Primary = false }
                }.Take(f.Random.Int(0, 2)).ToList())
                .RuleFor(u => u.Addresses, f => new List<Address>
                {
                    new()
                    {
                        Type = "work",
                        Primary = true,
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
        public static List<ScimUser> GenerateUsers(int count, bool mixActiveStatus = false)
        {
            var users = new List<ScimUser>();
            for (int i = 0; i < count; i++)
            {
                var active = mixActiveStatus ? Faker.Random.Bool() : true;
                users.Add(GenerateUser(active: active));
            }
            return users;
        }

        /// <summary>
        /// Generates a fake SCIM group with realistic data
        /// </summary>
        public static ScimGroup GenerateGroup(string? id = null, string? displayName = null, List<ScimUser>? members = null)
        {
            var groupFaker = new Faker<ScimGroup>()
                .RuleFor(g => g.Id, f => id ?? f.Random.Guid().ToString())
                .RuleFor(g => g.DisplayName, f => displayName ?? f.Commerce.Department())
                .RuleFor(g => g.ExternalId, f => f.Random.AlphaNumeric(10))
                .RuleFor(g => g.Members, (f, g) =>
                {
                    if (members != null)
                    {
                        return members.Select(u => new GroupMember
                        {
                            Value = u.Id!,
                            Display = u.DisplayName,
                            Type = "User"
                        }).ToList();
                    }

                    var memberCount = f.Random.Int(0, 5);
                    return Enumerable.Range(0, memberCount)
                        .Select(_ => new GroupMember
                        {
                            Value = f.Random.Guid().ToString(),
                            Display = f.Name.FullName(),
                            Type = "User"
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
        public static List<ScimGroup> GenerateGroups(int count, List<ScimUser>? availableUsers = null)
        {
            var groups = new List<ScimGroup>();
            for (int i = 0; i < count; i++)
            {
                var memberUsers = availableUsers?.Take(Faker.Random.Int(0, Math.Min(3, availableUsers.Count))).ToList();
                groups.Add(GenerateGroup(members: memberUsers));
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
