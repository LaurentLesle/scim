using FluentAssertions;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class GroupServiceTests : IDisposable
    {
        private readonly GroupService _groupService;
        private readonly Data.ScimDbContext _context;
        private readonly string _testCustomerId = ScimTestDataGenerator.DefaultCustomerId;

        public GroupServiceTests()
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
            var testGroup = ScimTestDataGenerator.GenerateGroup();
            _context.Groups.Add(testGroup);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.GetGroupAsync(testGroup.Id!, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(testGroup.Id);
            result.DisplayName.Should().Be(testGroup.DisplayName);
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
        public async Task GetGroupsAsync_WithDefaultParameters_ReturnsPagedResults()
        {
            // Arrange
            var testGroups = ScimTestDataGenerator.GenerateGroups(8);
            _context.Groups.AddRange(testGroups);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.GetGroupsAsync(_testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(8);
            result.StartIndex.Should().Be(1);
            result.ItemsPerPage.Should().Be(8); // All groups fit in default page size
            result.Resources.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetGroupsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var testGroups = ScimTestDataGenerator.GenerateGroups(15);
            _context.Groups.AddRange(testGroups);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.GetGroupsAsync(_testCustomerId, startIndex: 6, count: 5);

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(15);
            result.StartIndex.Should().Be(6);
            result.ItemsPerPage.Should().Be(5);
            result.Resources.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetGroupsAsync_WithDisplayNameFilter_ReturnsFilteredResults()
        {
            // Arrange
            var testGroups = ScimTestDataGenerator.GenerateGroups(5);
            testGroups[0].DisplayName = "Engineering Team";
            testGroups[1].DisplayName = "Marketing Team";
            _context.Groups.AddRange(testGroups);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.GetGroupsAsync(_testCustomerId, filter: "displayName eq \"Engineering Team\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().DisplayName.Should().Be("Engineering Team");
        }

        [Fact]
        public async Task CreateGroupAsync_WithValidGroup_CreatesAndReturnsGroup()
        {
            // Arrange
            var newGroup = ScimTestDataGenerator.GenerateGroup();
            newGroup.Id = null; // Should be generated

            // Act
            var result = await _groupService.CreateGroupAsync(newGroup, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.DisplayName.Should().Be(newGroup.DisplayName);
            result.Meta.Should().NotBeNull();
            result.Meta!.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.Meta.ResourceType.Should().Be("Group");
            
            // Verify it was saved to database
            var savedGroup = await _context.Groups.FindAsync(result.Id);
            savedGroup.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateGroupAsync_WithMembers_CreatesGroupWithMembers()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(3);
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            var newGroup = ScimTestDataGenerator.GenerateGroup(members: testUsers);
            newGroup.Id = null;

            // Act
            var result = await _groupService.CreateGroupAsync(newGroup, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.Members.Should().HaveCount(3);
            result.Members.Should().AllSatisfy(member => 
            {
                member.Type.Should().Be("User");
                member.Value.Should().NotBeNullOrEmpty();
                member.Display.Should().NotBeNullOrEmpty();
            });
        }

        [Fact]
        public async Task UpdateGroupAsync_WithValidGroup_UpdatesAndReturnsGroup()
        {
            // Arrange
            var existingGroup = ScimTestDataGenerator.GenerateGroup();
            _context.Groups.Add(existingGroup);
            await _context.SaveChangesAsync();

            var updatedGroup = ScimTestDataGenerator.GenerateGroup();
            updatedGroup.Id = existingGroup.Id;
            updatedGroup.DisplayName = "Updated Group Name";

            // Act
            var result = await _groupService.UpdateGroupAsync(existingGroup.Id!, updatedGroup, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("Updated Group Name");
            result.Meta!.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            // Verify database was updated
            var dbGroup = await _context.Groups.FindAsync(existingGroup.Id);
            dbGroup!.DisplayName.Should().Be("Updated Group Name");
        }

        [Fact]
        public async Task UpdateGroupAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var updatedGroup = ScimTestDataGenerator.GenerateGroup();

            // Act
            var result = await _groupService.UpdateGroupAsync(invalidId, updatedGroup, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PatchGroupAsync_WithDisplayNameOperation_UpdatesDisplayName()
        {
            // Arrange
            var existingGroup = ScimTestDataGenerator.GenerateGroup();
            _context.Groups.Add(existingGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "Patched Group Name" }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(existingGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("Patched Group Name");
            result.Meta!.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            // Verify database was updated
            var dbGroup = await _context.Groups.FindAsync(existingGroup.Id);
            dbGroup!.DisplayName.Should().Be("Patched Group Name");
        }

        [Fact]
        public async Task PatchGroupAsync_WithMembersAddOperation_AddsMember()
        {
            // Arrange
            var testUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(testUser);
            
            var existingGroup = ScimTestDataGenerator.GenerateGroup();
            _context.Groups.Add(existingGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "add", 
                        Path = "members", 
                        Value = new { value = testUser.Id, display = testUser.DisplayName, type = "User" }
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(existingGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Members.Should().Contain(m => m.Value == testUser.Id);
        }

        [Fact]
        public async Task PatchGroupAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var patchRequest = ScimTestDataGenerator.GeneratePatchRequest();

            // Act
            var result = await _groupService.PatchGroupAsync(invalidId, patchRequest, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteGroupAsync_WithValidId_DeletesGroup()
        {
            // Arrange
            var existingGroup = ScimTestDataGenerator.GenerateGroup();
            _context.Groups.Add(existingGroup);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.DeleteGroupAsync(existingGroup.Id!, _testCustomerId);

            // Assert
            result.Should().BeTrue();
            
            // Verify group was deleted from database
            var dbGroup = await _context.Groups.FindAsync(existingGroup.Id);
            dbGroup.Should().BeNull();
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

    [Fact]
    public async Task CreateGroupAsync_WithComplexMembers_HandlesNullValues()
    {
        // Arrange
        var newGroup = new ScimGroup
        {
            DisplayName = "Test Group",
            Members = new List<GroupMember>
            {
                new() { Value = Guid.NewGuid().ToString(), Display = "User 1", Type = "User" },
                new() { Value = Guid.NewGuid().ToString(), Display = null, Type = "User" } // Null display
            }
        };

        // Act
        var result = await _groupService.CreateGroupAsync(newGroup, _testCustomerId);

        // Assert
        result.Should().NotBeNull();
        result.Members.Should().HaveCount(2);
        result.Members.Should().Contain(m => m.Display == null);
    }

        [Theory]
        [InlineData("displayName eq \"Engineering\"")]
        [InlineData("displayName sw \"Test\"")]
        public async Task GetGroupsAsync_WithDifferentFilters_HandlesGracefully(string filter)
        {
            // Arrange
            var testGroups = ScimTestDataGenerator.GenerateGroups(5);
            testGroups[0].DisplayName = "Engineering Team";
            testGroups[1].DisplayName = "Test Group";
            _context.Groups.AddRange(testGroups);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.GetGroupsAsync(_testCustomerId, filter: filter);

            // Assert
            result.Should().NotBeNull();
            // Note: The actual filtering logic depends on the implementation
            // This test verifies that filtering doesn't break the service
        }
    }
}
