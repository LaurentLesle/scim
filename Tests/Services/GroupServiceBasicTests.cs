using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using System.Text.Json;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class GroupServiceBasicTests : IDisposable
    {
        private readonly GroupService _groupService;
        private readonly Data.ScimDbContext _context;
        private readonly string _testCustomerId = UserTestDataGenerator.DefaultCustomerId;

        public GroupServiceBasicTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _groupService = new GroupService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetGroupAsync_WithValidId_ReturnsGroup()
        {
            // Arrange
            var group = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.GetGroupAsync(group.Id!, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(group.Id);
            result.DisplayName.Should().Be(group.DisplayName);
        }

        [Fact]
        public async Task GetGroupAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _groupService.GetGroupAsync(invalidId, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateGroupAsync_WithValidGroup_CreatesAndReturnsGroup()
        {
            // Arrange
            var group = GroupTestDataGenerator.CreateGroup(_testCustomerId);

            // Act
            var result = await _groupService.CreateGroupAsync(group, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().NotBeNullOrEmpty();
            result.DisplayName.Should().Be(group.DisplayName);
            result.CustomerId.Should().Be(_testCustomerId);

            var savedGroup = await _context.Groups.FirstOrDefaultAsync(g => g.Id == result.Id);
            savedGroup.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateGroupAsync_WithDuplicateExternalId_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            existingGroup.ExternalId = "duplicate-external-id";
            _context.Groups.Add(existingGroup);
            await _context.SaveChangesAsync();

            var newGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            newGroup.ExternalId = "duplicate-external-id";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _groupService.CreateGroupAsync(newGroup, _testCustomerId));
        }

        [Fact]
        public async Task CreateGroupAsync_WithMembers_CreatesGroupWithMembers()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var group = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            group.Members = new List<GroupMember>
            {
                new() { Value = user.Id!, Display = user.DisplayName }
            };

            // Act
            var result = await _groupService.CreateGroupAsync(group, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Members.Should().HaveCount(1);
            result.Members!.First().Value.Should().Be(user.Id);
            result.Members.First().Display.Should().Be(user.DisplayName);
            result.Members.First().Ref.Should().Be($"../Users/{user.Id}");
        }

        [Fact]
        public async Task CreateGroupAsync_WithMembersAndExternalId_PopulatesAllAttributes()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var group = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            group.ExternalId = "test-external-id";
            group.Members = new List<GroupMember>
            {
                new() { Value = user.Id!, Display = user.DisplayName }
            };

            // Act
            var result = await _groupService.CreateGroupAsync(group, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.ExternalId.Should().Be("test-external-id");
            result.Members.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateGroupAsync_WithComplexMembers_HandlesNullValues()
        {
            // Arrange
            var user = UserTestDataGenerator.CreateUser(_testCustomerId);
            user.DisplayName = null; // Test null display name
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var group = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            group.Members = new List<GroupMember>
            {
                new() { Value = user.Id!, Display = null }
            };

            // Act
            var result = await _groupService.CreateGroupAsync(group, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Members.Should().HaveCount(1);
            result.Members!.First().Display.Should().BeNull();
            result.Members.First().Value.Should().Be(user.Id);
        }

        [Fact]
        public async Task UpdateGroupAsync_WithValidGroup_UpdatesAndReturnsGroup()
        {
            // Arrange
            var group = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            group.DisplayName = "Updated Group Name";
            group.ExternalId = "updated-external-id";

            // Act
            var result = await _groupService.UpdateGroupAsync(group.Id!, group, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("Updated Group Name");
            result.ExternalId.Should().Be("updated-external-id");

            var updatedGroup = await _context.Groups.FirstOrDefaultAsync(g => g.Id == group.Id);
            updatedGroup!.DisplayName.Should().Be("Updated Group Name");
            updatedGroup.ExternalId.Should().Be("updated-external-id");
        }

        [Fact]
        public async Task UpdateGroupAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var group = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _groupService.UpdateGroupAsync(invalidId, group, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteGroupAsync_WithValidId_DeletesGroup()
        {
            // Arrange
            var group = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.DeleteGroupAsync(group.Id!, _testCustomerId);

            // Assert
            result.Should().BeTrue();

            var deletedGroup = await _context.Groups.FirstOrDefaultAsync(g => g.Id == group.Id);
            deletedGroup.Should().BeNull();
        }

        [Fact]
        public async Task DeleteGroupAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _groupService.DeleteGroupAsync(invalidId, _testCustomerId);

            // Assert
            result.Should().BeFalse();
        }
    }
}
