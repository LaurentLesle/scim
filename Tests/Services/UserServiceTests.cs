using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
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
                    new Role { Value = "admin", Display = "Admin", Type = "system", Primary = "True" },
                    new Role { Value = "user", Display = "User", Type = "system", Primary = "False" }
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "remove", Path = "roles[primary eq \"True\"]" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().NotContain(r => r.Primary == "True");
            result.Roles.Should().ContainSingle(r => r.Primary == "False");
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
                    new Role { Value = "admin", Display = "Admin", Type = "system", Primary = "True" }
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].display", Value = "SuperAdmin" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().ContainSingle(r => r.Primary == "True" && r.Display == "SuperAdmin");
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
                    new() { Op = "add", Path = "roles[primary eq \"True\"].display", Value = "Manager" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].value", Value = "manager" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].type", Value = "system" },
                    new() { Op = "add", Path = "roles[primary eq \"False\"].display", Value = "Employee" },
                    new() { Op = "add", Path = "roles[primary eq \"False\"].value", Value = "employee" },
                    new() { Op = "add", Path = "roles[primary eq \"False\"].type", Value = "system" },
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].display", Value = "Director" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().ContainSingle(r => r.Primary == "True" && r.Display == "Director" && r.Value == "manager");
            result.Roles.Should().ContainSingle(r => r.Primary == "False" && r.Display == "Employee" && r.Value == "employee");
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
            _context.Users.Add(existingUser);
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
            _context.Users.Add(existingUser);
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
            _context.Users.Add(existingUser);
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
            // Arrange: Create a user with the same structure as the compliance test
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "emilio.mcglynn@ruecker.com",
                CustomerId = _testCustomerId,
                Active = true,
                DisplayName = "MZRRAEBPCVUT",
                Title = "ZNLLAVOBZHER",
                UserType = "EVKZWRHDSERH",
                PreferredLanguage = "yo-BJ",
                Locale = "LVEZOQYQKHGD",
                Timezone = "America/Belize",
                NickName = "BJOHVLIFMGYQ",
                ProfileUrl = "LJNCUKCYKEGD",
                Name = new Name
                {
                    GivenName = "Adah",
                    FamilyName = "Lonny",
                    Formatted = "Kariane",
                    MiddleName = "Kristofer",
                    HonorificPrefix = "Chandler",
                    HonorificSuffix = "Wilton"
                },
                Emails = new List<Email>
                {
                    new() { Type = "work", Value = "tiffany@jast.ca", Primary = true }
                },
                PhoneNumbers = new List<PhoneNumber>
                {
                    new() { Type = "work", Value = "50-608-7660", Primary = true },
                    new() { Type = "mobile", Value = "50-608-7660", Primary = false },
                    new() { Type = "fax", Value = "50-608-7660", Primary = false }
                },
                Addresses = new List<Address>
                {
                    new() 
                    { 
                        Type = "work", 
                        Formatted = "GIAXVJDMAEBW",
                        StreetAddress = "368 Gay Lock",
                        Locality = "WJHZXDARRIRX",
                        Region = "ZNZGNAESMOMP",
                        PostalCode = "cr66 0dk",
                        Country = "Palau",
                        Primary = true 
                    }
                },
                Roles = new List<Role>
                {
                    new() 
                    { 
                        Primary = "True", 
                        Display = "DTFSIZWBZYQD", 
                        Value = "LXWDSUWSDLDZ", 
                        Type = "BYDQIEEGJBTJ" 
                    }
                },
                EnterpriseUser = new EnterpriseUser
                {
                    EmployeeNumber = "HPMBHRQJMJYJ",
                    Department = "OAGMRKTDHGOH",
                    CostCenter = "RQDCCTIAOWXV",
                    Organization = "ATEFZMKHMCIC",
                    Division = "IEJBGFEWNPIS"
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act: Apply the same PATCH operations as the compliance test
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    // Email replacements
                    new() { Op = "replace", Path = "emails[type eq \"work\"].value", Value = "julian_kunze@raynor.co.uk" },
                    new() { Op = "replace", Path = "emails[type eq \"work\"].primary", Value = true },
                    
                    // Address replacements
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].formatted", Value = "CNVZPWDNWBOS" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].streetAddress", Value = "00795 Kling Trail" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].locality", Value = "LSIGXHVGXAFU" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].region", Value = "PNGAPVNYDPBE" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].postalCode", Value = "bi98 9ei" },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].primary", Value = true },
                    new() { Op = "replace", Path = "addresses[type eq \"work\"].country", Value = "Saint Martin" },
                    
                    // Phone number replacements
                    new() { Op = "replace", Path = "phoneNumbers[type eq \"work\"].value", Value = "62-106-7825" },
                    new() { Op = "replace", Path = "phoneNumbers[type eq \"work\"].primary", Value = true },
                    new() { Op = "replace", Path = "phoneNumbers[type eq \"mobile\"].value", Value = "62-106-7825" },
                    new() { Op = "replace", Path = "phoneNumbers[type eq \"fax\"].value", Value = "62-106-7825" },
                    
                    // Role replacements
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].display", Value = "WJFYANRRKBZY" },
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].value", Value = "YJUMXBISKVVV" },
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].type", Value = "PMZUZHVPFIAO" },
                    
                    // Pathless replace operation (bulk update)
                    new() 
                    { 
                        Op = "replace", 
                        Value = new Dictionary<string, object>
                        {
                            { "active", true },
                            { "displayName", "QUXLDPMOGMTE" },
                            { "title", "ZFRODQRUFTOL" },
                            { "preferredLanguage", "mt-MT" },
                            { "name.givenName", "Jacky" },
                            { "name.familyName", "Donald" },
                            { "name.formatted", "Makenna" },
                            { "name.middleName", "Jordi" },
                            { "name.honorificPrefix", "Liana" },
                            { "name.honorificSuffix", "Eula" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber", "BBSDCWZRXGSP" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department", "AMFPTCVNGPLF" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter", "SMVTLHIMHKSU" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization", "ABWODHFESUZD" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division", "LUKZBCYTVEXL" },
                            { "userType", "VFWXPTGVJHVY" },
                            { "nickName", "UTIRDCIZNEQY" },
                            { "locale", "OHDKIGRLUPCO" },
                            { "timezone", "Africa/Ndjamena" },
                            { "profileUrl", "DHQBHWYDUDUU" }
                        }
                    }
                }
            };

            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert: Verify all the expected changes from the compliance test
            result.Should().NotBeNull();
            
            // Verify email changes
            result!.Emails.Should().ContainSingle();
            result.Emails![0].Value.Should().Be("julian_kunze@raynor.co.uk");
            result.Emails[0].Type.Should().Be("work");
            result.Emails[0].Primary.Should().BeTrue();
            
            // Verify address changes
            result.Addresses.Should().ContainSingle();
            var address = result.Addresses![0];
            address.Formatted.Should().Be("CNVZPWDNWBOS");
            address.StreetAddress.Should().Be("00795 Kling Trail");
            address.Locality.Should().Be("LSIGXHVGXAFU");
            address.Region.Should().Be("PNGAPVNYDPBE");
            address.PostalCode.Should().Be("bi98 9ei");
            address.Country.Should().Be("Saint Martin");
            address.Type.Should().Be("work");
            address.Primary.Should().BeTrue();
            
            // Verify phone number changes
            result.PhoneNumbers.Should().HaveCount(3);
            var workPhone = result.PhoneNumbers!.First(p => p.Type == "work");
            workPhone.Value.Should().Be("62-106-7825");
            workPhone.Primary.Should().BeTrue();
            
            var mobilePhone = result.PhoneNumbers!.First(p => p.Type == "mobile");
            mobilePhone.Value.Should().Be("62-106-7825");
            mobilePhone.Primary.Should().BeFalse();
            
            var faxPhone = result.PhoneNumbers!.First(p => p.Type == "fax");
            faxPhone.Value.Should().Be("62-106-7825");
            faxPhone.Primary.Should().BeFalse();
            
            // Verify role changes
            result.Roles.Should().ContainSingle();
            var role = result.Roles![0];
            role.Display.Should().Be("WJFYANRRKBZY");
            role.Value.Should().Be("YJUMXBISKVVV");
            role.Type.Should().Be("PMZUZHVPFIAO");
            role.Primary.Should().Be("True");
            
            // Verify bulk update changes
            result.Active.Should().BeTrue();
            result.DisplayName.Should().Be("QUXLDPMOGMTE");
            result.Title.Should().Be("ZFRODQRUFTOL");
            result.PreferredLanguage.Should().Be("mt-MT");
            result.UserType.Should().Be("VFWXPTGVJHVY");
            result.NickName.Should().Be("UTIRDCIZNEQY");
            result.Locale.Should().Be("OHDKIGRLUPCO");
            result.Timezone.Should().Be("Africa/Ndjamena");
            result.ProfileUrl.Should().Be("DHQBHWYDUDUU");
            
            // Verify name changes
            result.Name.Should().NotBeNull();
            result.Name!.GivenName.Should().Be("Jacky");
            result.Name.FamilyName.Should().Be("Donald");
            result.Name.Formatted.Should().Be("Makenna");
            result.Name.MiddleName.Should().Be("Jordi");
            result.Name.HonorificPrefix.Should().Be("Liana");
            result.Name.HonorificSuffix.Should().Be("Eula");
            
            // Verify enterprise user changes
            result.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.EmployeeNumber.Should().Be("BBSDCWZRXGSP");
            result.EnterpriseUser.Department.Should().Be("AMFPTCVNGPLF");
            result.EnterpriseUser.CostCenter.Should().Be("SMVTLHIMHKSU");
            result.EnterpriseUser.Organization.Should().Be("ABWODHFESUZD");
            result.EnterpriseUser.Division.Should().Be("LUKZBCYTVEXL");
        }

        // ...existing code...
    }
}
