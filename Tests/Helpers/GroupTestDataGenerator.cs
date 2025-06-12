using Bogus;
using ScimServiceProvider.Models;

namespace ScimServiceProvider.Tests.Helpers
{
    /// <summary>
    /// Provides fake test data generation for SCIM groups using Bogus library
    /// </summary>
    public static class GroupTestDataGenerator
    {
        private static readonly Faker Faker = new();
        
        /// <summary>
        /// Default test customer ID for testing
        /// </summary>
        public const string DefaultCustomerId = "test-customer-id";

        /// <summary>
        /// Generates a fake SCIM group with realistic data
        /// </summary>
        public static ScimGroup GenerateGroup(string? id = null, string? displayName = null, List<ScimUser>? members = null, string? customerId = null, string? externalId = null)
        {
            var groupFaker = new Faker<ScimGroup>()
                .RuleFor(g => g.Id, f => id ?? f.Random.Guid().ToString())
                .RuleFor(g => g.DisplayName, f => displayName ?? $"{f.Commerce.Department()} {f.PickRandom("Team", "Group", "Division")}")
                .RuleFor(g => g.ExternalId, f => externalId ?? f.Random.AlphaNumeric(10))
                .RuleFor(g => g.CustomerId, f => customerId ?? DefaultCustomerId)
                .RuleFor(g => g.Members, f => members?.Select(m => new GroupMember
                {
                    Value = m.Id!,
                    Display = m.DisplayName,
                    Ref = $"../Users/{m.Id}"
                }).ToList())
                .RuleFor(g => g.Meta, f => new ScimMeta
                {
                    ResourceType = "Group",
                    Created = DateTime.UtcNow.AddDays(-f.Random.Int(1, 365)),
                    LastModified = DateTime.UtcNow.AddHours(-f.Random.Int(1, 24)),
                    Version = f.Random.Int(1, 10).ToString(),
                    Location = "/Groups/" + (id ?? f.Random.Guid().ToString())
                })
                .RuleFor(g => g.Schemas, new List<string>
                {
                    "urn:ietf:params:scim:schemas:core:2.0:Group"
                });

            return groupFaker.Generate();
        }

        /// <summary>
        /// Generates multiple fake SCIM groups with optional user members
        /// </summary>
        public static List<ScimGroup> GenerateGroups(int count, List<ScimUser>? availableUsers = null, string? customerId = null)
        {
            var groups = new List<ScimGroup>();
            for (int i = 0; i < count; i++)
            {
                List<ScimUser>? groupMembers = null;
                if (availableUsers != null && availableUsers.Any())
                {
                    // Randomly assign some users to this group
                    var memberCount = Faker.Random.Int(0, Math.Min(3, availableUsers.Count));
                    groupMembers = Faker.PickRandom(availableUsers, memberCount).ToList();
                }
                groups.Add(GenerateGroup(members: groupMembers, customerId: customerId));
            }
            return groups;
        }

        /// <summary>
        /// Creates a group with minimal required fields for testing
        /// </summary>
        public static ScimGroup CreateGroup(string customerId, string? displayName = null)
        {
            return new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = displayName ?? $"{Faker.Commerce.Department()} Team",
                CustomerId = customerId,
                Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:Group" },
                Meta = new ScimMeta
                {
                    ResourceType = "Group",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                }
            };
        }
    }
}
