using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using System.Text.Json;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class UserServiceQueryTests : IDisposable
    {
        private readonly UserService _userService;
        private readonly Data.ScimDbContext _context;
        private readonly string _testCustomerId = UserTestDataGenerator.DefaultCustomerId;

        public UserServiceQueryTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _userService = new UserService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetUsersAsync_WithDefaultParameters_ReturnsPagedResults()
        {
            // Arrange
            var users = new List<ScimUser>();
            for (int i = 0; i < 5; i++)
            {
                users.Add(UserTestDataGenerator.CreateUser(_testCustomerId, $"user{i}@example.com"));
            }
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(5);
            result.Resources.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetUsersAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var users = new List<ScimUser>();
            for (int i = 0; i < 10; i++)
            {
                users.Add(UserTestDataGenerator.CreateUser(_testCustomerId, $"user{i:D2}@example.com"));
            }
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, startIndex: 3, count: 2);

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(10);
            result.Resources.Should().HaveCount(2);
            result.StartIndex.Should().Be(3);
        }

        [Fact]
        public async Task GetUsersAsync_WithUserNameFilter_ReturnsFilteredResults()
        {
            // Arrange
            var user1 = UserTestDataGenerator.CreateUser(_testCustomerId, "john@example.com");
            var user2 = UserTestDataGenerator.CreateUser(_testCustomerId, "jane@example.com");
            var user3 = UserTestDataGenerator.CreateUser(_testCustomerId, "bob@example.com");
            _context.Users.AddRange(user1, user2, user3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, filter: "userName eq \"john@example.com\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().UserName.Should().Be("john@example.com");
        }

        [Fact]
        public async Task GetUsersAsync_WithUserNameFilter_IsCaseInsensitive()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId, "John.Doe@Example.COM");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, filter: "userName eq \"john.doe@example.com\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().UserName.Should().Be("John.Doe@Example.COM");
        }

        [Fact]
        public async Task GetUsersAsync_WithDifferentFilters_ReturnsFilteredResults()
        {
            // Arrange
            var user1 = UserTestDataGenerator.CreateUser(_testCustomerId, "active@example.com");
            user1.Active = true;
            var user2 = UserTestDataGenerator.CreateUser(_testCustomerId, "inactive@example.com");
            user2.Active = false;
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            // Act - Filter by active status
            var activeResult = await _userService.GetUsersAsync(_testCustomerId, filter: "active eq true");
            var inactiveResult = await _userService.GetUsersAsync(_testCustomerId, filter: "active eq false");

            // Assert
            activeResult.Should().NotBeNull();
            activeResult.TotalResults.Should().Be(1);
            activeResult.Resources.First().Active.Should().BeTrue();

            inactiveResult.Should().NotBeNull();
            inactiveResult.TotalResults.Should().Be(1);
            inactiveResult.Resources.First().Active.Should().BeFalse();
        }
    }
}
