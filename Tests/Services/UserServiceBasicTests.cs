using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using System.Text.Json;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class UserServiceBasicTests : IDisposable
    {
        private readonly UserService _userService;
        private readonly Data.ScimDbContext _context;
        private readonly string _testCustomerId = UserTestDataGenerator.DefaultCustomerId;

        public UserServiceBasicTests()
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
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserAsync(user.Id!, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.UserName.Should().Be(user.UserName);
        }

        [Fact]
        public async Task GetUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _userService.GetUserAsync(invalidId, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByUsernameAsync_WithValidUsername_ReturnsUser()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserByUsernameAsync(user.UserName!, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.UserName.Should().Be(user.UserName);
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
        public async Task CreateUserAsync_WithValidUser_CreatesAndReturnsUser()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);

            // Act
            var result = await _userService.CreateUserAsync(user, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().NotBeNullOrEmpty();
            result.UserName.Should().Be(user.UserName);
            result.CustomerId.Should().Be(_testCustomerId);

            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
            savedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateExternalId_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingUser = UserTestDataGenerator.CreateUser(_testCustomerId);
            existingUser.ExternalId = "duplicate-external-id";
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var newUser = UserTestDataGenerator.CreateUser(_testCustomerId);
            newUser.ExternalId = "duplicate-external-id";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.CreateUserAsync(newUser, _testCustomerId));
        }

        [Fact]
        public async Task UpdateUserAsync_WithValidUser_UpdatesAndReturnsUser()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            user.DisplayName = "Updated Display Name";
            user.Active = false;

            // Act
            var result = await _userService.UpdateUserAsync(user.Id!, user, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("Updated Display Name");
            result.Active.Should().BeFalse();

            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            updatedUser!.DisplayName.Should().Be("Updated Display Name");
            updatedUser.Active.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _userService.UpdateUserAsync(invalidId, user, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteUserAsync_WithValidId_DeletesUser()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.DeleteUserAsync(user.Id!, _testCustomerId);

            // Assert
            result.Should().BeTrue();

            var deletedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            deletedUser.Should().BeNull();
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
    }
}
