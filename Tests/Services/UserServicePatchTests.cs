using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using System.Text.Json;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class UserServicePatchTests : IDisposable
    {
        private readonly UserService _userService;
        private readonly Data.ScimDbContext _context;
        private readonly string _testCustomerId = UserTestDataGenerator.DefaultCustomerId;

        public UserServicePatchTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _userService = new UserService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task PatchUserAsync_WithActiveOperation_UpdatesActiveStatus()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            user.Active = true;
            _context.Users.Add(user);
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
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Active.Should().BeFalse();
        }

        [Fact]
        public async Task PatchUserAsync_WithDisplayNameOperation_UpdatesDisplayName()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(user);
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
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("New Display Name");
        }

        [Fact]
        public async Task PatchUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "Test" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(invalidId, patchRequest, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

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
                    new Role { Value = "admin", Display = "Administrator", Type = "system" }
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "roles[value eq \"admin\"].display", Value = "Super Admin" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().HaveCount(1);
            result.Roles.First().Display.Should().Be("Super Admin");
            result.Roles.First().Value.Should().Be("admin");
        }

        [Fact]
        public async Task PatchUserAsync_AddMultipleRolesAndUpdateOne_WorksCorrectly()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            user.Roles = new List<Role>();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "add", Path = "roles", Value = JsonSerializer.Serialize(new[]
                    {
                        new { value = "user", display = "User", type = "system" },
                        new { value = "manager", display = "Manager", type = "business" }
                    }) },
                    new() { Op = "replace", Path = "roles[value eq \"user\"].display", Value = "Standard User" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().HaveCount(2);
            result.Roles.Should().Contain(r => r.Value == "user" && r.Display == "Standard User");
            result.Roles.Should().Contain(r => r.Value == "manager" && r.Display == "Manager");
        }
    }
}
