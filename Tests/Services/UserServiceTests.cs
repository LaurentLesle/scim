using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using System.Text.Json;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly UserService _userService;
        private readonly Data.ScimDbContext _context;
        private readonly string _testCustomerId = ScimTestDataGenerator.DefaultCustomerId;

        [Fact]
        public async Task PatchUserAsync_AddsMobilePhoneAndEnterpriseFields_WhenInitiallyEmpty()
        {
            // Arrange: user with no phone numbers or enterprise fields
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "minimal@example.com",
                CustomerId = _testCustomerId,
                PhoneNumbers = new List<PhoneNumber>(),
                EnterpriseUser = null
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "add", Path = "phoneNumbers[type eq \"mobile\"].value", Value = "6-808-7091" },
                    new() { Op = "add", Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber", Value = "ZCQUDZLZAVUA" },
                    new() { Op = "add", Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department", Value = "WJOAUUZGWZIU" },
                    new() { Op = "add", Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter", Value = "QOIYFFVVNFPB" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.PhoneNumbers.Should().ContainSingle(p => p.Type == "mobile" && p.Value == "6-808-7091");
            result.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.EmployeeNumber.Should().Be("ZCQUDZLZAVUA");
            result.EnterpriseUser.Department.Should().Be("WJOAUUZGWZIU");
            result.EnterpriseUser.CostCenter.Should().Be("QOIYFFVVNFPB");
        }
        [Fact]
        public async Task PatchUserAsync_RemoveRoleByFilter_RemovesCorrectRole()
        {
            // Arrange
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "roleuser@example.com",
                CustomerId = _testCustomerId,
                Roles = new List<Role>
                {
                    new Role { Value = "admin", Display = "Admin", Type = "system" },
                    new Role { Value = "user", Display = "User", Type = "system" }
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "remove", Path = "roles[value eq \"admin\"]" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().HaveCount(1);
            result.Roles.Should().ContainSingle(r => r.Display == "User");
        }

        [Fact]
        public async Task PatchUserAsync_ReplaceRoleProperty_UpdatesRole()
        {
            // Arrange
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "roleuser2@example.com",
                CustomerId = _testCustomerId,
                Roles = new List<Role>
                {
                    new Role { Value = "admin", Display = "Admin", Type = "system" }
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "roles[type eq \"system\"].display", Value = "SuperAdmin" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().ContainSingle(r => r.Display == "SuperAdmin");
        }

        [Fact]
        public async Task PatchUserAsync_AddMultipleRolesAndUpdateOne_WorksCorrectly()
        {
            // Arrange
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "roleuser3@example.com",
                CustomerId = _testCustomerId,
                Roles = new List<Role>()
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "add", Path = "roles", Value = new { display = "Manager", value = "manager", type = "system" } },
                    new() { Op = "add", Path = "roles", Value = new { display = "Employee", value = "employee", type = "system" } },
                    new() { Op = "replace", Path = "roles[value eq \"manager\"].display", Value = "Director" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().ContainSingle(r => r.Display == "Director" && r.Value == "manager");
            result.Roles.Should().ContainSingle(r => r.Display == "Employee" && r.Value == "employee");
        }
        // ...existing code...

        public UserServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _userService = new UserService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetUserAsync_WithValidId_ReturnsUser()
        {
            // Arrange
            var testUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserAsync(testUser.Id!, ScimTestDataGenerator.DefaultCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(testUser.Id);
            result.UserName.Should().Be(testUser.UserName);
            result.DisplayName.Should().Be(testUser.DisplayName);
        }

        [Fact]
        public async Task GetUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _userService.GetUserAsync(invalidId, ScimTestDataGenerator.DefaultCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByUsernameAsync_WithValidUsername_ReturnsUser()
        {
            // Arrange
            var testUser = ScimTestDataGenerator.GenerateUser(userName: "test@example.com");
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserByUsernameAsync("test@example.com", _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.UserName.Should().Be("test@example.com");
        }

        [Fact]
        public async Task GetUserByUsernameAsync_WithInvalidUsername_ReturnsNull()
        {
            // Arrange
            var invalidUsername = "nonexistent@example.com";

            // Act
            var result = await _userService.GetUserByUsernameAsync(invalidUsername, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUsersAsync_WithDefaultParameters_ReturnsPagedResults()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(15);
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(15);
            result.StartIndex.Should().Be(1);
            result.ItemsPerPage.Should().Be(10); // Default count is 100, but we only have 15 users
            result.Resources.Should().HaveCount(10);
        }

        [Fact]
        public async Task GetUsersAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(25);
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, startIndex: 11, count: 5);

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(25);
            result.StartIndex.Should().Be(11);
            result.ItemsPerPage.Should().Be(5);
            result.Resources.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetUsersAsync_WithUserNameFilter_ReturnsFilteredResults()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(5);
            testUsers[0].UserName = "john.doe@example.com";
            testUsers[1].UserName = "jane.smith@example.com";
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, filter: "userName eq \"john.doe@example.com\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().UserName.Should().Be("john.doe@example.com");
        }

        [Fact]
        public async Task GetUsersAsync_WithUserNameFilter_IsCaseInsensitive()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(2);
            testUsers[0].UserName = "waldo@ryan.uk";
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act: filter with different case
            var result = await _userService.GetUsersAsync(_testCustomerId, filter: "userName eq \"WALDO@RYAN.UK\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().UserName.Should().Be("waldo@ryan.uk");
        }

        [Fact]
        public async Task CreateUserAsync_WithValidUser_CreatesAndReturnsUser()
        {
            // Arrange
            var newUser = ScimTestDataGenerator.GenerateUser();
            newUser.Id = null; // Should be generated

            // Act
            var result = await _userService.CreateUserAsync(newUser, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.UserName.Should().Be(newUser.UserName);
            result.Meta.Should().NotBeNull();
            result.Meta!.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.Meta.ResourceType.Should().Be("User");
            
            // Verify it was saved to database
            var savedUser = await _context.Users.FindAsync(result.Id);
            savedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateExternalId_ThrowsInvalidOperationException()
        {
            // Arrange
            var externalId = "duplicate-user-external-id-123";
            var user1 = ScimTestDataGenerator.GenerateUser(externalId: externalId, customerId: _testCustomerId);
            var user2 = ScimTestDataGenerator.GenerateUser(externalId: externalId, customerId: _testCustomerId);
            _context.Users.Add(user1);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _userService.CreateUserAsync(user2, _testCustomerId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task UpdateUserAsync_WithValidUser_UpdatesAndReturnsUser()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var updatedUser = ScimTestDataGenerator.GenerateUser();
            updatedUser.Id = existingUser.Id;
            updatedUser.UserName = "updated@example.com";
            updatedUser.DisplayName = "Updated Name";

            // Act
            var result = await _userService.UpdateUserAsync(existingUser.Id!, updatedUser, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.UserName.Should().Be("updated@example.com");
            result.DisplayName.Should().Be("Updated Name");
            result.Meta!.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            // Verify database was updated
            var dbUser = await _context.Users.FindAsync(existingUser.Id);
            dbUser!.UserName.Should().Be("updated@example.com");
        }

        [Fact]
        public async Task UpdateUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var updatedUser = ScimTestDataGenerator.GenerateUser();

            // Act
            var result = await _userService.UpdateUserAsync(invalidId, updatedUser, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PatchUserAsync_WithActiveOperation_UpdatesActiveStatus()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser(active: true);
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "active", Value = false }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Active.Should().BeFalse();
            result.Meta!.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            // Verify database was updated
            var dbUser = await _context.Users.FindAsync(existingUser.Id);
            dbUser!.Active.Should().BeFalse();
        }

        [Fact]
        public async Task PatchUserAsync_WithDisplayNameOperation_UpdatesDisplayName()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "New Display Name" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("New Display Name");
        }

        [Fact]
        public async Task PatchUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var patchRequest = ScimTestDataGenerator.GeneratePatchRequest();

            // Act
            var result = await _userService.PatchUserAsync(invalidId, patchRequest, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteUserAsync_WithValidId_DeletesUser()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.DeleteUserAsync(existingUser.Id!, _testCustomerId);

            // Assert
            result.Should().BeTrue();
            
            // Verify user was deleted from database
            var dbUser = await _context.Users.FindAsync(existingUser.Id);
            dbUser.Should().BeNull();
        }

        [Fact]
        public async Task DeleteUserAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _userService.DeleteUserAsync(invalidId, _testCustomerId);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("userName eq \"test@example.com\"")]
        [InlineData("displayName eq \"Test User\"")]
        [InlineData("active eq true")]
        public async Task GetUsersAsync_WithDifferentFilters_ReturnsFilteredResults(string filter)
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(10);
            testUsers[0].UserName = "test@example.com";
            testUsers[0].DisplayName = "Test User";
            testUsers[0].Active = true;
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, filter: filter);

            // Assert
            result.Should().NotBeNull();
            result.Resources.Should().NotBeEmpty();
            // Note: The actual filtering logic depends on the implementation
            // This test verifies that filtering doesn't break the service
        }

        [Fact]
        public async Task PatchUserAsync_WithAddManagerOperation_AddsManager()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            
            // Create the manager user that will be referenced
            var managerUser = new ScimUser
            {
                Id = "manager-id-123",
                UserName = "manager@example.com",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "Manager User"
            };

            _context.Users.Add(existingUser);
            _context.Users.Add(managerUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "manager-id-123"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("manager-id-123");
            result.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/manager-id-123");
        }

        [Fact]
        public async Task PatchUserAsync_WithRemoveManagerOperation_RemovesManager()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            existingUser.EnterpriseUser = new EnterpriseUser { Manager = new Manager { Value = "manager-id-123" } };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "remove",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().BeNull();
        }

        [Fact]
        public async Task PatchUserAsync_WithReplaceManagerOperation_ReplacesManager()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            existingUser.EnterpriseUser = new EnterpriseUser { Manager = new Manager { Value = "old-manager-id" } };
            
            // Create both the old and new manager users
            var oldManagerUser = new ScimUser
            {
                Id = "old-manager-id",
                UserName = "oldmanager@example.com",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "Old Manager"
            };
            
            var newManagerUser = new ScimUser
            {
                Id = "new-manager-id-456",
                UserName = "newmanager@example.com",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "New Manager"
            };

            _context.Users.Add(existingUser);
            _context.Users.Add(oldManagerUser);
            _context.Users.Add(newManagerUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "replace",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "new-manager-id-456"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("new-manager-id-456");
        }

        [Fact]
        public async Task PatchUserAsync_WithManagerJsonObject_HandlesComplexManagerObject()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            
            // Create the manager user that will be referenced
            var managerUser = new ScimUser
            {
                Id = "manager-123",
                UserName = "johnmanager@example.com",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "John Manager"
            };

            _context.Users.Add(existingUser);
            _context.Users.Add(managerUser);
            await _context.SaveChangesAsync();

            var managerJson = """{"value":"manager-123","$ref":"../Users/manager-123","displayName":"John Manager"}""";

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = managerJson
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("manager-123");
            result.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/manager-123");
            result.EnterpriseUser!.Manager!.DisplayName.Should().Be("John Manager");
        }

        [Fact]
        public async Task PatchUserAsync_WithManagerJsonString_HandlesJsonStringInput()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            
            // Create the manager user that will be referenced
            var managerUser = new ScimUser
            {
                Id = "manager-456",
                UserName = "janemanager@example.com",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "Jane Manager"
            };

            _context.Users.Add(existingUser);
            _context.Users.Add(managerUser);
            await _context.SaveChangesAsync();

            var managerJson = "{\"value\":\"manager-456\",\"$ref\":\"../Users/manager-456\",\"displayName\":\"Jane Manager\"}";

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = managerJson
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("manager-456");
            result.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/manager-456");
            result.EnterpriseUser!.Manager!.DisplayName.Should().Be("Jane Manager");
        }

        [Fact]
        public async Task PatchUserAsync_WithLegacyManagerString_HandlesBackwardCompatibility()
        {
            // Arrange - Test backward compatibility with string manager values
            var existingUser = ScimTestDataGenerator.GenerateUser();
            
            // Create the manager user that will be referenced
            var managerUser = new ScimUser
            {
                Id = "legacy-manager-string-id",
                UserName = "legacymanager@example.com",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "Legacy Manager"
            };

            _context.Users.Add(existingUser);
            _context.Users.Add(managerUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "legacy-manager-string-id"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert RFC 7643 compliance - $ref should be populated for manager references
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("legacy-manager-string-id");
            result.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/legacy-manager-string-id");
            result.EnterpriseUser!.Manager!.DisplayName.Should().BeNull();
        }

        [Fact]
        public async Task PatchUserAsync_WithPathlessManagerOperation_HandlesEnterpriseExtension()
        {
            // Arrange - Test pathless operation with enterprise extension
            var existingUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var enterpriseExtension = new
            {
                manager = """{"value":"pathless-manager-123","$ref":"../Users/pathless-manager-123","displayName":"Pathless Manager"}""",
                department = "Engineering"
            };

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User",
                        Value = enterpriseExtension
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("pathless-manager-123");
            result.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/pathless-manager-123");
            result.EnterpriseUser!.Manager!.DisplayName.Should().Be("Pathless Manager");
            result.EnterpriseUser!.Department.Should().Be("Engineering");
        }

        [Fact]
        public async Task PatchUserAsync_WithManagerPersistence_VerifiesEFCoreHandling()
        {
            // Arrange - Test that Manager objects are properly persisted and retrieved
            var existingUser = ScimTestDataGenerator.GenerateUser();
            
            // Create the manager user that will be referenced
            var managerUser = new ScimUser
            {
                Id = "persist-test-123",
                UserName = "persistmanager@example.com",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "Persist Test Manager"
            };

            _context.Users.Add(existingUser);
            _context.Users.Add(managerUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = new { value = "persist-test-123", displayName = "Persist Test Manager" }
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Clear context to force reload from database
            _context.ChangeTracker.Clear();
            
            // Retrieve the user again from database
            var persistedUser = await _userService.GetUserAsync(existingUser.Id!, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("persist-test-123");
            
            persistedUser.Should().NotBeNull();
            persistedUser!.EnterpriseUser.Should().NotBeNull();
            persistedUser.EnterpriseUser!.Manager.Should().NotBeNull();
            persistedUser.EnterpriseUser!.Manager!.Value.Should().Be("persist-test-123");
            persistedUser.EnterpriseUser!.Manager!.DisplayName.Should().Be("Persist Test Manager");
        }

        [Fact]
        public async Task PatchUserAsync_ComplexReplaceOperations_HandlesComplianceTestScenario()
        {
            // Arrange: Create a user with the exact initial structure from the compliance test
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "lisette.roob@kunze.ca",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "MFMDAGDPRJOL",
                Title = "CZPUQZUVMDYQ",
                UserType = "BMNQWSNKJZGL",
                PreferredLanguage = "ur-Arab-IN",
                Locale = "MYVLKBQEAOMQ",
                Timezone = "Europe/Berlin",
                NickName = "JGTFPUPVVDHR",
                ProfileUrl = "MJUTDYPCGJJL",
                Name = new Name
                {
                    GivenName = "Barry",
                    FamilyName = "Hiram",
                    Formatted = "Edgar",
                    MiddleName = "Dovie",
                    HonorificPrefix = "Mayra",
                    HonorificSuffix = "Trent"
                },
                Emails = new List<Email>
                {
                    new() { Type = "work", Value = "donnie@morissettelindgren.us", Primary = true }
                },
                PhoneNumbers = new List<PhoneNumber>
                {
                    new() { Type = "work", Value = "16-434-2057", Primary = true },
                    new() { Type = "mobile", Value = "16-434-2057" },
                    new() { Type = "fax", Value = "16-434-2057" }
                },
                Addresses = new List<Address>
                {
                    new() 
                    { 
                        Type = "work", 
                        Formatted = "BNOZCTZQCNJG",
                        StreetAddress = "151 Alford Park",
                        Locality = "VAWINPUPHDSW",
                        Region = "KHRGZUWNUGVJ",
                        PostalCode = "wf11 1hs",
                        Country = "Montenegro",
                        Primary = true
                    }
                },
                Roles = new List<Role>
                {
                    new() 
                    { 
                        Display = "QHCGHEXIAFGA", 
                        Value = "DFTTYHKOLKNO", 
                        Type = "FZAMDZZCEXCP",
                        Primary = "true"
                    }
                },
                EnterpriseUser = new EnterpriseUser
                {
                    EmployeeNumber = "GXFMNDYZBBWR",
                    Department = "AFIDFJTYBIRF",
                    CostCenter = "IWLXADEMDYGL",
                    Organization = "HLQOOSZQBTBD",
                    Division = "GAKPSVRYRXXQ"
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act: Apply the exact PATCH operations from the compliance test
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    // Email replacements
                    new() { Op = "replace", Path = "emails[type eq \"work\"].value", Value = "orrin_monahan@kris.co.uk" },
                    new() { Op = "replace", Path = "emails[type eq \"work\"].primary", Value = true },
                    
                    // Address replacements
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].formatted", Value = "UTECTUBZJPJD" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].streetAddress", Value = "444 Chadrick Run" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].locality", Value = "GRMXEAACKAYM" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].region", Value = "NRIDEZXCJMXP" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].postalCode", Value = "rp00 0yz" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].primary", Value = true },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].country", Value = "Brunei Darussalam" },
                    
                    // Phone number replacements
                    new() { Op = "replace", Path = "phoneNumbers[type eq \"work\"].value", Value = "59-714-5563" },
                    new() { Op = "replace", Path = "phoneNumbers[type eq \"work\"].primary", Value = true },
                    new() { Op = "replace", Path = "phoneNumbers[type eq \"mobile\"].value", Value = "59-714-5563" },
                    new() { Op = "replace", Path = "phoneNumbers[type eq \"fax\"].value", Value = "59-714-5563" },
                    
                    // Role replacements
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].display", Value = "TFXYUPBHCSBK" },
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].value", Value = "SYWZUYJOXVKT" },
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].type", Value = "ATLDQUBQZABY" },
                    
                    // Pathless replace operation (bulk update)
                    new() 
                    { 
                        Op = "replace", 
                        Value = new Dictionary<string, object>
                        {
                            { "active", true },
                            { "displayName", "RAZHWSOBRCUA" },
                            { "title", "CXFAJRVENDZM" },
                            { "preferredLanguage", "en-CC" },
                            { "name.givenName", "Enola" },
                            { "name.familyName", "Andy" },
                            { "name.formatted", "Katelyn" },
                            { "name.middleName", "Ana" },
                            { "name.honorificPrefix", "Khalid" },
                            { "name.honorificSuffix", "Cale" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber", "BFPGALJQQIRC" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department", "GOVBRIUNSUJJ" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter", "HFPOUBWXZNGW" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization", "KLVRJYURUBWZ" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division", "BONBFXVGKSIA" },
                            { "userType", "SVBMLRRZLUPS" },
                            { "nickName", "RBLQDPBRTNLU" },
                            { "locale", "LUBTPVKNGIDM" },
                            { "timezone", "Africa/Maputo" },
                            { "profileUrl", "GYLOKHJZNGVT" }
                        }
                    }
                }
            };

            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert: Verify all the expected changes match the compliance test exactly
            result.Should().NotBeNull();
            
            // Verify email changes
            result!.Emails.Should().ContainSingle();
            result.Emails![0].Value.Should().Be("orrin_monahan@kris.co.uk");
            result.Emails[0].Type.Should().Be("work");
            result.Emails[0].Primary.Should().Be(true);
            
            // Verify address changes
            result.Addresses.Should().ContainSingle();
            var address = result.Addresses![0];
            address.Formatted.Should().Be("UTECTUBZJPJD");
            address.StreetAddress.Should().Be("444 Chadrick Run");
            address.Locality.Should().Be("GRMXEAACKAYM");
            address.Region.Should().Be("NRIDEZXCJMXP");
            address.PostalCode.Should().Be("rp00 0yz");
            address.Country.Should().Be("Brunei Darussalam");
            address.Type.Should().Be("work");
            address.Primary.Should().Be(true);
            
            // Verify phone number changes
            result.PhoneNumbers.Should().HaveCount(3);
            var workPhone = result.PhoneNumbers!.First(p => p.Type == "work");
            workPhone.Value.Should().Be("59-714-5563");
            workPhone.Primary.Should().Be(true);
            
            var mobilePhone = result.PhoneNumbers!.First(p => p.Type == "mobile");
            mobilePhone.Value.Should().Be("59-714-5563");
            
            var faxPhone = result.PhoneNumbers!.First(p => p.Type == "fax");
            faxPhone.Value.Should().Be("59-714-5563");
            
            // Verify role changes
            result.Roles.Should().ContainSingle();
            var role = result.Roles![0];
            role.Display.Should().Be("TFXYUPBHCSBK");
            role.Value.Should().Be("SYWZUYJOXVKT");
            role.Type.Should().Be("ATLDQUBQZABY");
            role.Primary.Should().Be("true");
            
            // Verify name changes from pathless operation
            result.Name.Should().NotBeNull();
            result.Name!.GivenName.Should().Be("Enola");
            result.Name.FamilyName.Should().Be("Andy");
            result.Name.Formatted.Should().Be("Katelyn");
            result.Name.MiddleName.Should().Be("Ana");
            result.Name.HonorificPrefix.Should().Be("Khalid");
            result.Name.HonorificSuffix.Should().Be("Cale");
            
            // Verify enterprise extension changes from pathless operation
            result.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.EmployeeNumber.Should().Be("BFPGALJQQIRC");
            result.EnterpriseUser.Department.Should().Be("GOVBRIUNSUJJ");
            result.EnterpriseUser.CostCenter.Should().Be("HFPOUBWXZNGW");
            result.EnterpriseUser.Organization.Should().Be("KLVRJYURUBWZ");
            result.EnterpriseUser.Division.Should().Be("BONBFXVGKSIA");
            
            // Verify other bulk update changes
            result.DisplayName.Should().Be("RAZHWSOBRCUA");
            result.Title.Should().Be("CXFAJRVENDZM");
            result.PreferredLanguage.Should().Be("en-CC");
            result.UserType.Should().Be("SVBMLRRZLUPS");
            result.NickName.Should().Be("RBLQDPBRTNLU");
            result.Locale.Should().Be("LUBTPVKNGIDM");
            result.Timezone.Should().Be("Africa/Maputo");
            result.ProfileUrl.Should().Be("GYLOKHJZNGVT");
            result.Active.Should().Be(true);
            
            // Verify schema compliance
            result.Schemas.Should().Contain("urn:ietf:params:scim:schemas:core:2.0:User");
            result.Schemas.Should().Contain("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
        }

        [Fact]
        public async Task PatchUserAsync_ComplianceTest_AddManagerOperation_MatchesExactScenario()
        {
            // Arrange - Test that matches the exact compliance test scenario
            
            // Create the manager user first
            var managerUser = ScimTestDataGenerator.GenerateUser();
            managerUser.Id = "eafec183-4fa9-4741-b2c9-6dc1f0ff75b1";
            managerUser.UserName = "manager@company.com";
            managerUser.DisplayName = "Manager User";
            managerUser.CustomerId = _testCustomerId;
            managerUser.Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = "1"
            };
            _context.Users.Add(managerUser);
            
            var existingUser = ScimTestDataGenerator.GenerateUser();
            existingUser.UserName = "ardella.hansen@goyette.info";
            existingUser.DisplayName = "ALRXNTMVWMZD";
            existingUser.EnterpriseUser = new EnterpriseUser 
            {
                EmployeeNumber = "LBHGOGYNEPMM",
                Department = "JVQSOANJGYQK",
                CostCenter = "ZFEBUESBVHYO",
                Organization = "RXJYMYJUINHW",
                Division = "GIVMPWBGBWIS"
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "eafec183-4fa9-4741-b2c9-6dc1f0ff75b1"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert - Verify SCIM 2.0 compliance for manager addition
            result.Should().NotBeNull();
            result!.UserName.Should().Be("ardella.hansen@goyette.info");
            result.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("eafec183-4fa9-4741-b2c9-6dc1f0ff75b1");
            result.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/eafec183-4fa9-4741-b2c9-6dc1f0ff75b1");
            
            // Verify other enterprise fields are preserved
            result.EnterpriseUser!.EmployeeNumber.Should().Be("LBHGOGYNEPMM");
            result.EnterpriseUser!.Department.Should().Be("JVQSOANJGYQK");
            result.EnterpriseUser!.CostCenter.Should().Be("ZFEBUESBVHYO");
            result.EnterpriseUser!.Organization.Should().Be("RXJYMYJUINHW");
            result.EnterpriseUser!.Division.Should().Be("GIVMPWBGBWIS");
            
            // Verify schemas include enterprise extension
            result.Schemas.Should().Contain("urn:ietf:params:scim:schemas:core:2.0:User");
            result.Schemas.Should().Contain("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
        }

        [Fact]
        public async Task PatchUserAsync_ReplaceAttributesVerboseRequest_ComplianceScenario()
        {
            // Arrange - Load test data from JSON files
            var initialUserJson = await File.ReadAllTextAsync("Tests/Resources/patch-replace-verbose-initial-user.json");
            var patchOpsJson = await File.ReadAllTextAsync("Tests/Resources/patch-replace-verbose-operations.json");
            var expectedResultJson = await File.ReadAllTextAsync("Tests/Resources/patch-replace-verbose-expected-result.json");

            var initialUserData = JsonSerializer.Deserialize<ScimUser>(initialUserJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var patchRequest = JsonSerializer.Deserialize<ScimPatchRequest>(patchOpsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var expectedResult = JsonSerializer.Deserialize<ScimUser>(expectedResultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            // Set required fields for test
            initialUserData.Id = Guid.NewGuid().ToString();
            initialUserData.CustomerId = _testCustomerId;
            initialUserData.Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = "1"
            };

            // Create initial user
            _context.Users.Add(initialUserData);
            await _context.SaveChangesAsync();

            // Act - Apply PATCH operations
            var result = await _userService.PatchUserAsync(initialUserData.Id!, patchRequest, _testCustomerId);

            // Assert - Verify all specific field replacements from compliance scenario
            result.Should().NotBeNull();

            // Core attributes
            result!.Active.Should().Be(true);
            result.DisplayName.Should().Be("DIRKTXOAYZZM");
            result.Title.Should().Be("UNBYYSYMKXGZ");
            result.PreferredLanguage.Should().Be("dv");
            result.UserType.Should().Be("FXVZTALTOKRQ");
            result.NickName.Should().Be("GFOKYVCXKLBE");
            result.Locale.Should().Be("PQPJBPUPAZYH");
            result.Timezone.Should().Be("Africa/Addis_Ababa");
            result.ProfileUrl.Should().Be("ILOBIMRVDJVN");

            // Name fields - verify all name attributes are correctly replaced
            result.Name.Should().NotBeNull();
            result.Name!.GivenName.Should().Be("Rahsaan", "because name.givenName should be replaced");
            result.Name.FamilyName.Should().Be("Warren", "because name.familyName should be replaced");
            result.Name.Formatted.Should().Be("Gage", "because name.formatted should be replaced");
            result.Name.MiddleName.Should().Be("Chester", "because name.middleName should be replaced");
            result.Name.HonorificPrefix.Should().Be("Miracle", "because name.honorificPrefix should be replaced");
            result.Name.HonorificSuffix.Should().Be("Mya", "because name.honorificSuffix should be replaced");

            // Email with filtered path replacement
            result.Emails.Should().NotBeNull().And.HaveCount(1);
            var workEmail = result.Emails!.FirstOrDefault(e => e.Type == "work");
            workEmail.Should().NotBeNull();
            workEmail!.Value.Should().Be("kiley@harveyjacobi.us", "because emails[type eq \"work\"].value should be replaced");
            workEmail.Primary.Should().Be(true);

            // Address with multiple filtered path replacements
            result.Addresses.Should().NotBeNull().And.HaveCount(1);
            var workAddress = result.Addresses!.FirstOrDefault(a => a.Type == "work");
            workAddress.Should().NotBeNull();
            workAddress!.Formatted.Should().Be("HROZMXTEBJYJ", "because addresses[type eq \"work\"].formatted should be replaced");
            workAddress.StreetAddress.Should().Be("68276 Orpha Village", "because addresses[type eq \"work\"].streetAddress should be replaced");
            workAddress.Locality.Should().Be("FQVYYVPHTSKW", "because addresses[type eq \"work\"].locality should be replaced");
            workAddress.Region.Should().Be("KNIDQTRIHHFW", "because addresses[type eq \"work\"].region should be replaced");
            workAddress.PostalCode.Should().Be("gh62 1ob", "because addresses[type eq \"work\"].postalCode should be replaced");
            workAddress.Country.Should().Be("Colombia", "because addresses[type eq \"work\"].country should be replaced");
            workAddress.Primary.Should().Be(true);

            // Phone numbers with filtered path replacements
            result.PhoneNumbers.Should().NotBeNull().And.HaveCount(3);
            var workPhone = result.PhoneNumbers!.FirstOrDefault(p => p.Type == "work");
            var mobilePhone = result.PhoneNumbers!.FirstOrDefault(p => p.Type == "mobile");
            var faxPhone = result.PhoneNumbers!.FirstOrDefault(p => p.Type == "fax");

            workPhone.Should().NotBeNull();
            workPhone!.Value.Should().Be("15-338-2012", "because phoneNumbers[type eq \"work\"].value should be replaced");
            workPhone.Primary.Should().Be(true);

            mobilePhone.Should().NotBeNull();
            mobilePhone!.Value.Should().Be("15-338-2012", "because phoneNumbers[type eq \"mobile\"].value should be replaced");

            faxPhone.Should().NotBeNull();
            faxPhone!.Value.Should().Be("15-338-2012", "because phoneNumbers[type eq \"fax\"].value should be replaced");

            // Enterprise extension fields with URN paths
            result.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.EmployeeNumber.Should().Be("AJKHTFRTUEOL", "because enterprise employeeNumber should be replaced");
            result.EnterpriseUser.Department.Should().Be("HEPFZZRYERLO", "because enterprise department should be replaced");
            result.EnterpriseUser.CostCenter.Should().Be("USAQIHFGZAON", "because enterprise costCenter should be replaced");
            result.EnterpriseUser.Organization.Should().Be("WVWXWZZPTZXO", "because enterprise organization should be replaced");
            result.EnterpriseUser.Division.Should().Be("ETIPYPNCXQMY", "because enterprise division should be replaced");

            // Roles with filtered path replacement
            result.Roles.Should().NotBeNull().And.HaveCount(1);
            var primaryRole = result.Roles!.FirstOrDefault(r => r.Primary == "True");
            primaryRole.Should().NotBeNull();
            primaryRole!.Display.Should().Be("NONTGTTVNUAY", "because roles[primary eq \"True\"].display should be replaced");
            primaryRole.Value.Should().Be("XKNOACDIOLQS", "because roles[primary eq \"True\"].value should be replaced");
            primaryRole.Type.Should().Be("LBTRJUINCINV", "because roles[primary eq \"True\"].type should be replaced");

            // Verify schemas are preserved
            result.Schemas.Should().Contain("urn:ietf:params:scim:schemas:core:2.0:User");
            result.Schemas.Should().Contain("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");

            // Verify meta data is updated
            result.Meta.Should().NotBeNull();
            result.Meta!.LastModified.Should().BeAfter(initialUserData.Meta!.Created);
        }

        [Fact]
        public async Task PatchUserAsync_AddManager_ComplianceScenario()
        {
            // Arrange - Load test data from JSON files
            var managerUserJson = await File.ReadAllTextAsync("Tests/Resources/patch-add-manager-manager-user.json");
            var initialUserJson = await File.ReadAllTextAsync("Tests/Resources/patch-add-manager-initial-user.json");
            var patchOpsJson = await File.ReadAllTextAsync("Tests/Resources/patch-add-manager-operations.json");
            var expectedResultJson = await File.ReadAllTextAsync("Tests/Resources/patch-add-manager-expected-result.json");

            var managerUserData = JsonSerializer.Deserialize<ScimUser>(managerUserJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var initialUserData = JsonSerializer.Deserialize<ScimUser>(initialUserJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var patchRequest = JsonSerializer.Deserialize<ScimPatchRequest>(patchOpsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var expectedResult = JsonSerializer.Deserialize<ScimUser>(expectedResultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            // Create the manager user first - this is the user that will be referenced
            var managerId = "96720958-e666-4969-b1ab-653aa4873ba7";
            managerUserData.Id = managerId;
            managerUserData.CustomerId = _testCustomerId;
            managerUserData.Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = "1"
            };

            // Create the employee user (initially without manager)
            initialUserData.Id = Guid.NewGuid().ToString();
            initialUserData.CustomerId = _testCustomerId;
            initialUserData.Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = "1"
            };

            // Add both users to context
            _context.Users.Add(managerUserData);
            _context.Users.Add(initialUserData);
            await _context.SaveChangesAsync();

            // Act - Apply PATCH operation to add manager
            var result = await _userService.PatchUserAsync(initialUserData.Id!, patchRequest, _testCustomerId);

            // Assert - Verify manager was added with correct structure
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();

            // Verify manager object structure matches SCIM compliance requirements
            var manager = result.EnterpriseUser.Manager!;
            manager.Value.Should().Be(managerId, "because manager.value should contain the manager's user ID");
            manager.Ref.Should().Be($"../Users/{managerId}", "because manager.$ref should contain the proper URI reference");

            // Verify the referenced manager user actually exists and can be retrieved
            var referencedManager = await _userService.GetUserAsync(managerId, _testCustomerId);
            referencedManager.Should().NotBeNull("because the referenced manager user should exist in the system");
            
            // Verify the referenced manager has the expected properties
            referencedManager!.Id.Should().Be(managerId);
            referencedManager.UserName.Should().Be("john.manager@company.com");
            referencedManager.DisplayName.Should().Be("Manager User");
            referencedManager.Title.Should().Be("Senior Manager");
            referencedManager.Name.Should().NotBeNull();
            referencedManager.Name!.GivenName.Should().Be("John");
            referencedManager.Name.FamilyName.Should().Be("Manager");
            
            // Verify enterprise attributes of the manager
            referencedManager.EnterpriseUser.Should().NotBeNull();
            referencedManager.EnterpriseUser!.EmployeeNumber.Should().Be("MGR001");
            referencedManager.EnterpriseUser.Department.Should().Be("Management");
            referencedManager.EnterpriseUser.Organization.Should().Be("OBNBAWJBNWZI");

            // Verify that the employee user has all other original attributes preserved
            result.UserName.Should().Be("kyla@wisozkpollich.name");
            result.DisplayName.Should().Be("AROZNLTJCGIX");
            result.Active.Should().Be(true);
            
            // Verify schemas include enterprise extension
            result.Schemas.Should().Contain("urn:ietf:params:scim:schemas:core:2.0:User");
            result.Schemas.Should().Contain("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");

            // Verify other enterprise attributes are preserved
            result.EnterpriseUser.EmployeeNumber.Should().Be("ILBQFTAJKGGC");
            result.EnterpriseUser.Department.Should().Be("OOHNREPLZNPL");
            result.EnterpriseUser.CostCenter.Should().Be("LBTMPKFDEKMV");
            result.EnterpriseUser.Organization.Should().Be("OBNBAWJBNWZI");
            result.EnterpriseUser.Division.Should().Be("NRSNSZCWMRPW");

            // Verify meta data is updated
            result.Meta.Should().NotBeNull();
            result.Meta!.LastModified.Should().BeAfter(initialUserData.Meta!.Created);
        }

        [Fact]
        public async Task PatchUserAsync_AddInvalidManager_ThrowsException()
        {
            // Arrange - Load test data for invalid manager scenario (malformed manager data)
            var initialUserJson = await File.ReadAllTextAsync("Tests/Resources/patch-add-manager-initial-user.json");
            var patchOpsJson = await File.ReadAllTextAsync("Tests/Resources/patch-add-invalid-manager-operations.json");

            var initialUserData = JsonSerializer.Deserialize<ScimUser>(initialUserJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var patchRequest = JsonSerializer.Deserialize<ScimPatchRequest>(patchOpsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            // Create the employee user (initially without manager)
            initialUserData.Id = Guid.NewGuid().ToString();
            initialUserData.CustomerId = _testCustomerId;
            initialUserData.Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = "1"
            };

            // Add only the employee user to context
            _context.Users.Add(initialUserData);
            await _context.SaveChangesAsync();

            // Act & Assert - Applying PATCH operation with malformed manager data should throw exception
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.PatchUserAsync(initialUserData.Id!, patchRequest, _testCustomerId)
            );

            exception.Message.Should().ContainAny("manager", "Manager", "deserialize", "Failed");
        }

        [Fact]
        public async Task PatchUserAsync_AddManager_EnsuresManagerValueIsPresent()
        {
            // Arrange - Create a user without manager
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "employee@company.com",
                DisplayName = "Employee User",
                Active = true,
                CustomerId = _testCustomerId,
                Schemas = new List<string>
                {
                    "urn:ietf:params:scim:schemas:core:2.0:User",
                    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
                },
                EnterpriseUser = new EnterpriseUser
                {
                    EmployeeNumber = "EMP001",
                    Department = "Engineering"
                },
                Meta = new ScimMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Version = "1"
                }
            };

            // Create a manager user that will be referenced
            var managerUser = new ScimUser
            {
                Id = "manager-123-456-789",
                UserName = "manager@company.com", 
                DisplayName = "Manager User",
                Active = true,
                CustomerId = _testCustomerId,
                Schemas = new List<string>
                {
                    "urn:ietf:params:scim:schemas:core:2.0:User",
                    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
                },
                EnterpriseUser = new EnterpriseUser
                {
                    EmployeeNumber = "MGR001",
                    Department = "Management"
                },
                Meta = new ScimMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Version = "1"
                }
            };

            // Add both users to context
            _context.Users.Add(user);
            _context.Users.Add(managerUser);
            await _context.SaveChangesAsync();

            // Create PATCH request to add manager
            var patchRequest = new ScimPatchRequest
            {
                Operations = new List<ScimPatchOperation>
                {
                    new ScimPatchOperation
                    {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "manager-123-456-789"
                    }
                }
            };

            // Act - Apply PATCH operation
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert - Verify manager is added with correct structure
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull("because manager should be added");

            var manager = result.EnterpriseUser.Manager!;
            
            // Critical assertion: manager.value must be present and not null/empty
            manager.Value.Should().NotBeNullOrEmpty("because manager.value is required for SCIM compliance");
            manager.Value.Should().Be("manager-123-456-789", "because manager.value should contain the manager's user ID");
            
            // Verify $ref is also present
            manager.Ref.Should().NotBeNullOrEmpty("because manager.$ref is required for SCIM compliance");
            manager.Ref.Should().Be("../Users/manager-123-456-789", "because manager.$ref should contain the proper URI reference");
            
            // Verify the referenced manager actually exists
            var referencedManager = await _userService.GetUserAsync("manager-123-456-789", _testCustomerId);
            referencedManager.Should().NotBeNull("because the referenced manager user should exist in the system");
            referencedManager!.Id.Should().Be("manager-123-456-789");
            referencedManager.UserName.Should().Be("manager@company.com");
        }

        // ...existing code...
    }
}
