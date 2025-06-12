using Bogus;
using ScimServiceProvider.Models;

namespace ScimServiceProvider.Tests.Helpers
{
    /// <summary>
    /// Backward compatibility layer for ScimTestDataGenerator
    /// Delegates to the new split helper classes
    /// </summary>
    public static class ScimTestDataGenerator
    {
        /// <summary>
        /// Default test customer ID for testing
        /// </summary>
        public const string DefaultCustomerId = UserTestDataGenerator.DefaultCustomerId;

        /// <summary>
        /// Generates a fake customer for testing
        /// </summary>
        public static Customer GenerateCustomer(string? id = null, string? tenantId = null)
        {
            return CommonTestDataGenerator.GenerateCustomer(id, tenantId);
        }

        /// <summary>
        /// Generates a fake SCIM user with realistic data
        /// </summary>
        public static ScimUser GenerateUser(string? id = null, string? userName = null, bool active = true, string? customerId = null, string? externalId = null)
        {
            return UserTestDataGenerator.GenerateUser(id, userName, active, customerId, externalId);
        }

        /// <summary>
        /// Creates a user with minimal required fields for testing
        /// </summary>
        public static ScimUser CreateUser(string customerId, string? userName = null)
        {
            return UserTestDataGenerator.CreateUser(customerId, userName);
        }

        /// <summary>
        /// Generates multiple fake SCIM users
        /// </summary>
        public static List<ScimUser> GenerateUsers(int count, bool mixActiveStatus = false, string? customerId = null)
        {
            return UserTestDataGenerator.GenerateUsers(count, mixActiveStatus, customerId);
        }

        /// <summary>
        /// Generates a fake SCIM group with realistic data
        /// </summary>
        public static ScimGroup GenerateGroup(string? id = null, string? displayName = null, List<ScimUser>? members = null, string? customerId = null, string? externalId = null)
        {
            return GroupTestDataGenerator.GenerateGroup(id, displayName, members, customerId, externalId);
        }

        /// <summary>
        /// Creates a group with minimal required fields for testing
        /// </summary>
        public static ScimGroup CreateGroup(string customerId, string? displayName = null)
        {
            return GroupTestDataGenerator.CreateGroup(customerId, displayName);
        }

        /// <summary>
        /// Generates multiple fake SCIM groups with optional user members
        /// </summary>
        public static List<ScimGroup> GenerateGroups(int count, List<ScimUser>? availableUsers = null, string? customerId = null)
        {
            return GroupTestDataGenerator.GenerateGroups(count, availableUsers, customerId);
        }

        /// <summary>
        /// Generates a SCIM patch request for testing
        /// </summary>
        public static ScimPatchRequest GeneratePatchRequest(string operation = "replace", string? path = null, object? value = null)
        {
            return CommonTestDataGenerator.GeneratePatchRequest(operation, path, value);
        }

        /// <summary>
        /// Generates fake JWT payload for testing
        /// </summary>
        public static Dictionary<string, object> GenerateJwtPayload()
        {
            return CommonTestDataGenerator.GenerateJwtPayload();
        }

        /// <summary>
        /// Creates a list response for testing
        /// </summary>
        public static ScimListResponse<T> CreateListResponse<T>(List<T> items, int startIndex = 1, int count = 10)
        {
            return CommonTestDataGenerator.CreateListResponse(items, startIndex, count);
        }
    }
}
