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
        public async Task PatchGroupAsync_RemoveMemberByValue_RemovesCorrectMember()
        {
            // Arrange
            var member1 = new GroupMember { Value = "user-1", Display = "User 1" };
            var member2 = new GroupMember { Value = "user-2", Display = "User 2" };
            var group = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                Members = new List<GroupMember> { member1, member2 },
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "remove", Path = "members[value eq \"user-1\"]" }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(group.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Members.Should().ContainSingle(m => m.Value == "user-2");
            result.Members.Should().NotContain(m => m.Value == "user-1");
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceDisplayOfMember_UpdatesMember()
        {
            // Arrange
            var member = new GroupMember { Value = "user-1", Display = "User 1" };
            var group = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                Members = new List<GroupMember> { member },
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "members[value eq \"user-1\"].display", Value = "Updated User 1" }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(group.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Members.Should().ContainSingle(m => m.Value == "user-1" && m.Display == "Updated User 1");
        }

        [Fact]
        public async Task PatchGroupAsync_AddMultipleMembersAndUpdateOne_WorksCorrectly()
        {
            // Arrange
            var group = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                Members = new List<GroupMember>(),
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "add", Path = "members", Value = new { value = "user-1", display = "User 1" } },
                    new() { Op = "add", Path = "members", Value = new { value = "user-2", display = "User 2" } },
                    new() { Op = "replace", Path = "members[value eq \"user-2\"].display", Value = "Updated User 2" }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(group.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Members.Should().ContainSingle(m => m.Value == "user-1" && m.Display == "User 1");
            result.Members.Should().ContainSingle(m => m.Value == "user-2" && m.Display == "Updated User 2");
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
                        Value = new { value = testUser.Id, display = testUser.DisplayName }
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
                new() { Value = Guid.NewGuid().ToString(), Display = "User 1" },
                new() { Value = Guid.NewGuid().ToString(), Display = null } // Null display
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

        [Fact]
        public async Task CreateGroupAsync_WithDuplicateExternalId_ThrowsInvalidOperationException()
        {
            // Arrange
            var externalId = "duplicate-external-id-123";
            var group1 = ScimTestDataGenerator.GenerateGroup(externalId: externalId, customerId: _testCustomerId);
            var group2 = ScimTestDataGenerator.GenerateGroup(externalId: externalId, customerId: _testCustomerId);
            _context.Groups.Add(group1);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _groupService.CreateGroupAsync(group2, _testCustomerId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task GetGroupsAsync_WithDisplayNameFilter_IsCaseInsensitive()
        {
            // Arrange
            var testGroups = ScimTestDataGenerator.GenerateGroups(2);
            testGroups[0].DisplayName = "engineering team";
            _context.Groups.AddRange(testGroups);
            await _context.SaveChangesAsync();

            // Act: filter with different case
            var result = await _groupService.GetGroupsAsync(_testCustomerId, filter: "displayName eq \"ENGINEERING TEAM\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().DisplayName.Should().Be("engineering team");
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceOperationWithoutPath_ReplacesEntireResource()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Original Group Name",
                ExternalId = "original-external-id",
                CustomerId = _testCustomerId,
                Members = new List<GroupMember>
                {
                    new() { Value = "user-1", Display = "User 1" }
                }
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace", 
                        // No Path specified - should replace entire resource
                        Value = new ScimGroup
                        {
                            DisplayName = "Completely New Group Name",
                            ExternalId = "new-external-id",
                            Members = new List<GroupMember>
                            {
                                new() { Value = "user-2", Display = "User 2" }
                            }
                        }
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("Completely New Group Name");
            result.ExternalId.Should().Be("new-external-id");
            result.Members.Should().ContainSingle(m => m.Value == "user-2");
            result.Members.Should().NotContain(m => m.Value == "user-1");

            // Verify in database
            var dbGroup = await _context.Groups.FindAsync(originalGroup.Id);
            dbGroup!.DisplayName.Should().Be("Completely New Group Name");
            dbGroup.ExternalId.Should().Be("new-external-id");
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceDisplayNameWithoutPath_UpdatesDisplayNameOnly()
        {
            // Arrange - This test mimics the exact scenario from the compliance test
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "IIKEMXUQUWFG",
                ExternalId = "6610dddd-2773-434f-aff8-c0824bef52df",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace", 
                        // No Path specified, but value contains only displayName - compliance test scenario
                        Value = new { displayName = "UCAMNYBKMBEU" }
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("UCAMNYBKMBEU");
            result.ExternalId.Should().Be("6610dddd-2773-434f-aff8-c0824bef52df"); // Should remain unchanged
            result.Id.Should().Be(originalGroup.Id); // Should remain unchanged

            // Verify in database
            var dbGroup = await _context.Groups.FindAsync(originalGroup.Id);
            dbGroup!.DisplayName.Should().Be("UCAMNYBKMBEU");
            dbGroup.ExternalId.Should().Be("6610dddd-2773-434f-aff8-c0824bef52df");
        }

        [Fact]
        public async Task PatchGroupAsync_MultipleReplaceOperationsWithoutPath_AppliesAllChanges()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Original Name",
                ExternalId = "original-id",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace", 
                        Value = new { displayName = "First Update" }
                    },
                    new() 
                    { 
                        Op = "replace", 
                        Value = new { externalId = "updated-external-id" }
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("First Update");
            result.ExternalId.Should().Be("updated-external-id");
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceWithInvalidOperation_ThrowsException()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Original Name",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "invalid_operation", 
                        Value = new { displayName = "Should Fail" }
                    }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId));
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceExternalIdWithPath_UpdatesExternalId()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                ExternalId = "original-external-id",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "externalId", Value = "new-external-id" }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.ExternalId.Should().Be("new-external-id");
            result.DisplayName.Should().Be("Test Group"); // Should remain unchanged

            // Verify in database
            var dbGroup = await _context.Groups.FindAsync(originalGroup.Id);
            dbGroup!.ExternalId.Should().Be("new-external-id");
            dbGroup.DisplayName.Should().Be("Test Group");
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceMultipleAttributesWithPaths_UpdatesAllAttributes()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Original Name",
                ExternalId = "original-id",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "New Display Name" },
                    new() { Op = "replace", Path = "externalId", Value = "new-external-id" }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("New Display Name");
            result.ExternalId.Should().Be("new-external-id");

            // Verify in database
            var dbGroup = await _context.Groups.FindAsync(originalGroup.Id);
            dbGroup!.DisplayName.Should().Be("New Display Name");
            dbGroup.ExternalId.Should().Be("new-external-id");
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceExternalIdWithoutPath_UpdatesExternalId()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                ExternalId = "original-external-id",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace", 
                        Value = new { externalId = "updated-external-id" }
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.ExternalId.Should().Be("updated-external-id");
            result.DisplayName.Should().Be("Test Group"); // Should remain unchanged
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceCompleteGroupWithoutPath_UpdatesAllProvidedAttributes()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Original Name",
                ExternalId = "original-id",
                CustomerId = _testCustomerId,
                Members = new List<GroupMember>
                {
                    new() { Value = "user-1", Display = "User 1" }
                }
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace", 
                        Value = new 
                        {
                            displayName = "Completely New Name",
                            externalId = "completely-new-id",
                            members = new[]
                            {
                                new { value = "user-2", display = "User 2" },
                                new { value = "user-3", display = "User 3" }
                            }
                        }
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("Completely New Name");
            result.ExternalId.Should().Be("completely-new-id");
            result.Members.Should().HaveCount(2);
            result.Members.Should().Contain(m => m.Value == "user-2" && m.Display == "User 2");
            result.Members.Should().Contain(m => m.Value == "user-3" && m.Display == "User 3");
            result.Members.Should().NotContain(m => m.Value == "user-1");

            // Verify in database
            var dbGroup = await _context.Groups.FindAsync(originalGroup.Id);
            dbGroup!.DisplayName.Should().Be("Completely New Name");
            dbGroup.ExternalId.Should().Be("completely-new-id");
        }

        [Fact]
        public async Task PatchGroupAsync_RemoveExternalIdWithPath_SetsExternalIdToNull()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                ExternalId = "external-id-to-remove",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "remove", Path = "externalId" }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.ExternalId.Should().BeNull();
            result.DisplayName.Should().Be("Test Group"); // Should remain unchanged
        }

        [Fact]
        public async Task CreateGroupAsync_WithMembersAndExternalId_PopulatesAllAttributes()
        {
            // Arrange
            var members = new List<GroupMember>
            {
                new() { Value = "user-1", Display = "User 1" },
                new() { Value = "user-2", Display = "User 2" }
            };
            
            var group = new ScimGroup
            {
                DisplayName = "Test Group with All Attributes",
                ExternalId = "ext-test-group-123",
                Members = members,
                CustomerId = _testCustomerId
            };

            // Act
            var result = await _groupService.CreateGroupAsync(group, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.DisplayName.Should().Be("Test Group with All Attributes");
            result.ExternalId.Should().Be("ext-test-group-123");
            result.Members.Should().HaveCount(2);
            
            foreach (var member in result.Members!)
            {
                member.Ref.Should().NotBeNullOrEmpty();
                member.Ref.Should().StartWith("https://localhost/scim/v2/Users/");
                member.Ref.Should().EndWith(member.Value);
            }
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceAllAttributes_UpdatesCompleteGroup()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Original Group",
                ExternalId = "original-ext-id",
                Members = new List<GroupMember>
                {
                    new() { Value = "user-1", Display = "User 1" }
                },
                CustomerId = _testCustomerId
            };
            
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();
            
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace",
                        Value = new
                        {
                            displayName = "Completely Updated Group",
                            externalId = "new-ext-id-456",
                            members = new[]
                            {
                                new { value = "user-2", display = "User 2" },
                                new { value = "user-3", display = "User 3" },
                                new { value = "user-4", display = "User 4" }
                            }
                        }
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("Completely Updated Group");
            result.ExternalId.Should().Be("new-ext-id-456");
            result.Members.Should().HaveCount(3);
            
            // Verify all new members are present and old one is gone
            result.Members.Should().Contain(m => m.Value == "user-2" && m.Display == "User 2");
            result.Members.Should().Contain(m => m.Value == "user-3" && m.Display == "User 3");
            result.Members.Should().Contain(m => m.Value == "user-4" && m.Display == "User 4");
            result.Members.Should().NotContain(m => m.Value == "user-1");
            
            // Verify $ref properties are populated
            foreach (var member in result.Members!)
            {
                member.Ref.Should().StartWith("https://localhost/scim/v2/Users/");
                member.Ref.Should().EndWith(member.Value);
            }
        }

        [Fact]
        public async Task PatchGroupAsync_UpdateMemberDisplayName_UpdatesSpecificMemberAttribute()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                Members = new List<GroupMember>
                {
                    new() { Value = "user-1", Display = "Original Name" },
                    new() { Value = "user-2", Display = "User 2" }
                },
                CustomerId = _testCustomerId
            };
            
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();
            
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace",
                        Path = "members[value eq \"user-1\"].display",
                        Value = "Updated Display Name"
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Members.Should().HaveCount(2);
            
            var updatedMember = result.Members!.First(m => m.Value == "user-1");
            updatedMember.Display.Should().Be("Updated Display Name");
            
            var unchangedMember = result.Members!.First(m => m.Value == "user-2");
            unchangedMember.Display.Should().Be("User 2");
        }

        [Fact]
        public async Task PatchGroupAsync_UpdateMemberType_UpdatesSpecificMemberAttribute()
        {
            // Arrange
            var originalGroup = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                Members = new List<GroupMember>
                {
                    new() { Value = "group-1", Display = "Nested Group" }, // Note: originally intended as wrong type
                    new() { Value = "user-2", Display = "User 2" }
                },
                CustomerId = _testCustomerId
            };
            
            _context.Groups.Add(originalGroup);
            await _context.SaveChangesAsync();
            
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace",
                        Path = "members[value eq \"group-1\"].display",
                        Value = "Updated Group Display"
                    }
                }
            };

            // Act
            var result = await _groupService.PatchGroupAsync(originalGroup.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Members.Should().HaveCount(2);
            
            var updatedMember = result.Members!.First(m => m.Value == "group-1");
            updatedMember.Display.Should().Be("Updated Group Display"); // Should be updated by the patch
            
            var unchangedMember = result.Members!.First(m => m.Value == "user-2");
            unchangedMember.Display.Should().Be("User 2");
        }

        [Fact]
        public async Task PatchGroupAsync_RemoveMemberWithTypeAttribute_ThrowsInvalidOperationException()
        {
            // Arrange
            var member1 = new GroupMember { Value = "user-1", Display = "User 1" };
            var member2 = new GroupMember { Value = "user-2", Display = "User 2" };
            var group = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                Members = new List<GroupMember> { member1, member2 },
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "remove", Path = "members[type eq \"untyped\"].value" }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _groupService.PatchGroupAsync(group.Id!, patchRequest, _testCustomerId));
            
            exception.Message.Should().Contain("members[type eq \"untyped\"].value for Group is not supported by the SCIM protocol");
        }

        [Fact]
        public async Task PatchGroupAsync_ReplaceMemberWithTypeAttribute_ThrowsInvalidOperationException()
        {
            // Arrange
            var member1 = new GroupMember { Value = "user-1", Display = "User 1" };
            var group = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                Members = new List<GroupMember> { member1 },
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "members[type eq \"User\"].display", Value = "Updated Display" }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _groupService.PatchGroupAsync(group.Id!, patchRequest, _testCustomerId));
            
            exception.Message.Should().Contain("members[type eq \"untyped\"].value for Group is not supported by the SCIM protocol");
        }

        // ...existing tests...
    }
}
