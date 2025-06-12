using Bogus;
using ScimServiceProvider.Models;

namespace ScimServiceProvider.Tests.Helpers
{
    /// <summary>
    /// Provides fake test data generation for SCIM users using Bogus library
    /// </summary>
    public static class UserTestDataGenerator
    {
        private static readonly Faker Faker = new();
        
        /// <summary>
        /// Default test customer ID for testing
        /// </summary>
        public const string DefaultCustomerId = "test-customer-id";

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
                    MiddleName = f.Random.Bool() ? f.Name.FirstName() : null,
                    HonorificPrefix = f.Random.Bool() ? f.PickRandom("Mr.", "Ms.", "Dr.", "Prof.") : null,
                    HonorificSuffix = f.Random.Bool() ? f.PickRandom("Jr.", "Sr.", "III") : null,
                    Formatted = f.Name.FullName()
                })
                .RuleFor(u => u.Emails, f => new List<Email>
                {
                    new()
                    {
                        Value = f.Internet.Email(),
                        Type = "work",
                        Primary = true
                    },
                    new()
                    {
                        Value = f.Internet.Email(),
                        Type = "personal",
                        Primary = false
                    }
                })
                .RuleFor(u => u.PhoneNumbers, f => new List<PhoneNumber>
                {
                    new()
                    {
                        Value = f.Phone.PhoneNumber(),
                        Type = "work"
                    },
                    new()
                    {
                        Value = f.Phone.PhoneNumber(),
                        Type = "mobile"
                    }
                })
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
                        Formatted = f.Address.FullAddress(),
                        Primary = true
                    }
                })
                .RuleFor(u => u.Roles, f => new List<Role>
                {
                    new()
                    {
                        Value = f.PickRandom("admin", "user", "manager", "developer"),
                        Display = f.PickRandom("Administrator", "User", "Manager", "Developer"),
                        Type = "system",
                        Primary = "true"
                    }
                })
                .RuleFor(u => u.EnterpriseUser, f => new EnterpriseUser
                {
                    EmployeeNumber = f.Random.AlphaNumeric(8),
                    CostCenter = f.Random.AlphaNumeric(6),
                    Organization = f.Company.CompanyName(),
                    Division = f.Commerce.Department(),
                    Department = f.Commerce.Department(),
                    Manager = null // Will be set separately if needed
                })
                .RuleFor(u => u.Meta, f => new ScimMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow.AddDays(-f.Random.Int(1, 365)),
                    LastModified = DateTime.UtcNow.AddHours(-f.Random.Int(1, 24)),
                    Version = f.Random.Int(1, 10).ToString(),
                    Location = "/Users/" + (id ?? f.Random.Guid().ToString())
                })
                .RuleFor(u => u.Schemas, new List<string>
                {
                    "urn:ietf:params:scim:schemas:core:2.0:User",
                    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
                });

            return userFaker.Generate();
        }

        /// <summary>
        /// Generates multiple fake SCIM users
        /// </summary>
        public static List<ScimUser> GenerateUsers(int count, bool mixActiveStatus = false, string? customerId = null)
        {
            var users = new List<ScimUser>();
            for (int i = 0; i < count; i++)
            {
                bool active = mixActiveStatus ? Faker.Random.Bool() : true;
                users.Add(GenerateUser(active: active, customerId: customerId));
            }
            return users;
        }

        /// <summary>
        /// Creates a user with minimal required fields for testing
        /// </summary>
        public static ScimUser CreateUser(string customerId, string? userName = null)
        {
            return new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName ?? Faker.Internet.Email(),
                DisplayName = Faker.Name.FullName(),
                Active = true,
                CustomerId = customerId,
                Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Meta = new ScimMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                }
            };
        }
    }
}
