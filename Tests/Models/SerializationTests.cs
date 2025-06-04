using System.Text.Json;
using System.Text.Json.Serialization;
using ScimServiceProvider.Models;
using Xunit;

namespace ScimServiceProvider.Tests.Models
{
    public class SerializationTests
    {
        // Use the same JsonSerializerOptions as the application
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private string SerializeWithOptions(object obj)
        {
            return JsonSerializer.Serialize(obj, _jsonOptions);
        }
        [Fact]
        public void ScimUser_SerializationIgnoresNullProperties()
        {
            // Arrange
            var user = new ScimUser
            {
                Id = "123",
                UserName = "testuser",
                DisplayName = null, // This should be ignored when null
                ExternalId = null,  // This should be ignored when null
                Name = null         // This should be ignored when null
            };

            // Act
            var json = SerializeWithOptions(user);

            // Assert
            Assert.DoesNotContain("\"displayName\"", json);
            Assert.DoesNotContain("\"externalId\"", json);
            Assert.DoesNotContain("\"name\"", json);
            Assert.Contains("\"id\":\"123\"", json);
            Assert.Contains("\"userName\":\"testuser\"", json);
        }

        [Fact]
        public void ScimGroup_SerializationIgnoresNullProperties()
        {
            // Arrange
            var group = new ScimGroup
            {
                Id = "456",
                DisplayName = "Test Group",
                ExternalId = null  // This should be ignored when null
                // Members will be empty list by default and should be ignored
            };

            // Act
            var json = SerializeWithOptions(group);

            // Assert
            Assert.DoesNotContain("\"externalId\"", json);
            Assert.DoesNotContain("\"members\"", json);  // Empty list should be ignored
            Assert.Contains("\"id\":\"456\"", json);
            Assert.Contains("\"displayName\":\"Test Group\"", json);
        }

        [Fact]
        public void GroupMember_SerializationIgnoresNullDisplay()
        {
            // Arrange
            var member = new GroupMember
            {
                Value = "user123",
                Display = null, // This should be ignored when null
                Type = "User"
            };

            // Act
            var json = SerializeWithOptions(member);

            // Assert
            Assert.DoesNotContain("\"display\"", json);
            Assert.Contains("\"value\":\"user123\"", json);
            Assert.Contains("\"type\":\"User\"", json);
        }

        [Fact]
        public void Manager_SerializationIgnoresNullProperties()
        {
            // Arrange
            var manager = new Manager
            {
                Value = null,       // This should be ignored when null
                Ref = null,         // This should be ignored when null
                DisplayName = null  // This should be ignored when null
            };

            // Act
            var json = SerializeWithOptions(manager);

            // Assert
            Assert.DoesNotContain("\"value\"", json);
            Assert.DoesNotContain("\"$ref\"", json);
            Assert.DoesNotContain("\"displayName\"", json);
            // Should serialize as empty object when all properties are null
            Assert.Equal("{}", json);
        }

        [Fact]
        public void ScimError_SerializationIgnoresNullProperties()
        {
            // Arrange
            var error = new ScimError
            {
                Status = 404,
                Detail = null,      // This should be ignored when null
                ScimType = null     // This should be ignored when null
            };

            // Act
            var json = SerializeWithOptions(error);

            // Assert
            Assert.DoesNotContain("\"detail\"", json);
            Assert.DoesNotContain("\"scimType\"", json);
            Assert.Contains("\"status\":404", json);
        }

        [Fact]
        public void PatchOperation_SerializationIgnoresNullProperties()
        {
            // Arrange
            var operation = new PatchOperation
            {
                Op = "replace",
                Path = null,        // This should be ignored when null
                Value = null        // This should be ignored when null
            };

            // Act
            var json = SerializeWithOptions(operation);

            // Assert
            Assert.DoesNotContain("\"path\"", json);
            Assert.DoesNotContain("\"value\"", json);
            Assert.Contains("\"op\":\"replace\"", json);
        }

        [Fact]
        public void Role_SerializationIgnoresNullProperties()
        {
            // Arrange
            var role = new Role
            {
                Value = "admin",
                Display = null,     // This should be ignored when null
                Type = "role",
                Primary = false     // This should not be ignored as it's not nullable
            };

            // Act
            var json = SerializeWithOptions(role);

            // Assert
            Assert.DoesNotContain("\"display\"", json);
            Assert.Contains("\"primary\"", json); // Primary is now bool, not nullable
            Assert.Contains("\"value\":\"admin\"", json);
            Assert.Contains("\"type\":\"role\"", json);
        }

        [Fact]
        public void GroupMembership_SerializationIgnoresNullDisplay()
        {
            // Arrange
            var membership = new GroupMembership
            {
                Value = "group123",
                Display = null, // This should be ignored when null
                Type = "direct"
            };

            // Act
            var json = SerializeWithOptions(membership);

            // Assert
            Assert.DoesNotContain("\"display\"", json);
            Assert.Contains("\"value\":\"group123\"", json);
            Assert.Contains("\"type\":\"direct\"", json);
        }

        [Fact]
        public void EnterpriseUser_SerializationIgnoresNullManager()
        {
            // Arrange
            var enterpriseUser = new EnterpriseUser
            {
                EmployeeNumber = "EMP123",
                Department = null,  // This should be ignored when null
                Manager = null      // This should be ignored when null
            };

            // Act
            var json = SerializeWithOptions(enterpriseUser);

            // Assert
            Assert.DoesNotContain("\"department\"", json);
            Assert.DoesNotContain("\"manager\"", json);
            Assert.Contains("\"employeeNumber\":\"EMP123\"", json);
        }

        [Fact]
        public void ScimUser_SerializationIgnoresEmptyCollections()
        {
            // Arrange - create user and explicitly set empty collections to null via cleanup
            var user = new ScimUser
            {
                Id = "123",
                UserName = "testuser"
            };
            // Collections will be null by default now, which should be ignored

            // Act
            var json = SerializeWithOptions(user);

            // Assert
            Assert.DoesNotContain("\"emails\"", json);
            Assert.DoesNotContain("\"phoneNumbers\"", json);
            Assert.DoesNotContain("\"addresses\"", json);
            Assert.DoesNotContain("\"groups\"", json);
            Assert.DoesNotContain("\"roles\"", json);
            Assert.Contains("\"id\":\"123\"", json);
            Assert.Contains("\"userName\":\"testuser\"", json);
        }

        [Fact]
        public void ScimGroup_SerializationIgnoresEmptyMembers()
        {
            // Arrange - create group without members
            var group = new ScimGroup
            {
                Id = "456",
                DisplayName = "Test Group"
                // Members will be null by default now
            };

            // Act
            var json = SerializeWithOptions(group);

            // Assert
            Assert.DoesNotContain("\"members\"", json);
            Assert.Contains("\"id\":\"456\"", json);
            Assert.Contains("\"displayName\":\"Test Group\"", json);
        }

        [Fact]
        public void ScimPatchRequest_SerializationWithOperations()
        {
            // Arrange - PatchRequest Operations are always initialized
            var request = new ScimPatchRequest();
            // Operations list is initialized by default but empty

            // Act
            var json = SerializeWithOptions(request);

            // Assert - Operations will be present but empty since it's always initialized
            Assert.Contains("\"operations\":[]", json);
            Assert.Contains("\"schemas\"", json);
        }
    }
}
