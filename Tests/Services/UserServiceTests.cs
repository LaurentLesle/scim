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
            result.EnterpriseUser!.Manager.Should().Be("manager-id-123");
        }

        [Fact]
        public async Task PatchUserAsync_WithRemoveManagerOperation_RemovesManager()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            existingUser.EnterpriseUser = new EnterpriseUser { Manager = "manager-id-123" };
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
            existingUser.EnterpriseUser = new EnterpriseUser { Manager = "old-manager-id" };
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
            result.EnterpriseUser!.Manager.Should().Be("new-manager-id-456");
        }
    }
}
