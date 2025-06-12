using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using System.Text.Json;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class UserServiceManagerTests : IDisposable
    {
        private readonly UserService _userService;
        private readonly Data.ScimDbContext _context;
        private readonly string _testCustomerId = UserTestDataGenerator.DefaultCustomerId;

        public UserServiceManagerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _userService = new UserService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task PatchUserAsync_WithAddManagerOperation_AddsManager()
        {
            // Arrange
            // Create the manager user that will be referenced
            var managerUser = new ScimUser
            {
                Id = "manager-id-123",
                UserName = "manager@example.com",
                CustomerId = _testCustomerId,
                DisplayName = "Manager User"
            };

            // Create the employee user
            var employeeUser = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(managerUser);
            _context.Users.Add(employeeUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new()
                    {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "manager-id-123"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(employeeUser.Id!, patchRequest, _testCustomerId);

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
            var existingUser = UserTestDataGenerator.CreateUser(_testCustomerId);
            existingUser.EnterpriseUser = new EnterpriseUser { Manager = new Manager { Value = "manager-id-123" } };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new()
                    {
                        Op = "remove",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser!.Manager.Should().BeNull();
        }

        [Fact]
        public async Task PatchUserAsync_WithReplaceManagerOperation_ReplacesManager()
        {
            // Arrange
            var existingUser = UserTestDataGenerator.CreateUser(_testCustomerId);
            existingUser.EnterpriseUser = new EnterpriseUser { Manager = new Manager { Value = "old-manager-id" } };

            // Create both old and new manager users
            var oldManagerUser = new ScimUser
            {
                Id = "old-manager-id",
                UserName = "old.manager@example.com",
                CustomerId = _testCustomerId,
                DisplayName = "Old Manager"
            };

            var newManagerUser = new ScimUser
            {
                Id = "new-manager-id-456",
                UserName = "new.manager@example.com",
                CustomerId = _testCustomerId,
                DisplayName = "New Manager"
            };

            _context.Users.Add(oldManagerUser);
            _context.Users.Add(newManagerUser);
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new()
                    {
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
            result!.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("new-manager-id-456");
        }

        [Fact]
        public async Task PatchUserAsync_AddExternalManager_AllowsExternalReference()
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
                    new()
                    {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "external-manager-id"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("external-manager-id");
        }

        [Fact]
        public async Task PatchUserAsync_WithManagerJsonObject_HandlesComplexManagerObject()
        {
            // Arrange
            var managerUser = new ScimUser
            {
                Id = "manager-obj-123",
                UserName = "manager.obj@example.com",
                CustomerId = _testCustomerId,
                DisplayName = "Manager Object"
            };

            var employeeUser = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(managerUser);
            _context.Users.Add(employeeUser);
            await _context.SaveChangesAsync();

            var managerObject = new
            {
                value = "manager-obj-123",
                @ref = "../Users/manager-obj-123",
                displayName = "Manager Object"
            };

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new()
                    {
                        Op = "replace",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = JsonSerializer.Serialize(managerObject)
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(employeeUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("manager-obj-123");
            result.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/manager-obj-123");
        }

        [Fact]
        public async Task PatchUserAsync_AddManager_ComplianceScenario()
        {
            // Arrange: Create a manager user first
            var managerUser = new ScimUser
            {
                Id = "manager-123-456-789",
                UserName = "manager@company.com",
                CustomerId = _testCustomerId,
                DisplayName = "Test Manager",
                Active = true
            };
            _context.Users.Add(managerUser);
            await _context.SaveChangesAsync();

            // Create employee user
            var employeeUser = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(employeeUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new()
                    {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "manager-123-456-789"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(employeeUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().NotBeNull();
            result.EnterpriseUser!.Manager!.Value.Should().Be("manager-123-456-789");

            // Verify the referenced manager actually exists
            var referencedManager = await _userService.GetUserAsync("manager-123-456-789", _testCustomerId);
            referencedManager.Should().NotBeNull("because the referenced manager user should exist in the system");
            referencedManager!.Id.Should().Be("manager-123-456-789");
            referencedManager.UserName.Should().Be("manager@company.com");
        }

        [Fact]
        public async Task PatchUserAsync_AddInvalidManager_WithStrictValidation_ThrowsException()
        {
            // Arrange
            _userService.EnableStrictManagerValidation = true;
            
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new()
                    {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "nonexistent-manager-id"
                    }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId));
        }
    }
}
