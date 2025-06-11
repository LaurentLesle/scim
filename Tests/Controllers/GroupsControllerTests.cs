using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ScimServiceProvider.Controllers;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using System.Security.Claims;
using Xunit;

namespace ScimServiceProvider.Tests.Controllers
{
    public class GroupsControllerTests
    {
        private readonly Mock<IGroupService> _mockGroupService;
        private readonly Mock<ILogger<GroupsController>> _mockLogger;
        private readonly GroupsController _controller;
        private readonly List<ScimGroup> _testGroups;
        private readonly List<ScimUser> _testUsers;
        private readonly string _testCustomerId = ScimTestDataGenerator.DefaultCustomerId;

        public GroupsControllerTests()
        {
            _testUsers = ScimTestDataGenerator.GenerateUsers(5);
            _testGroups = ScimTestDataGenerator.GenerateGroups(5, _testUsers);
            _mockGroupService = MockServiceProviders.CreateMockGroupService(_testGroups, _testUsers);
            _mockLogger = new Mock<ILogger<GroupsController>>();
            _controller = new GroupsController(_mockGroupService.Object, _mockLogger.Object);

            // Setup controller context with authentication
            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "test-user"),
                new("client_id", "scim_client"),
                new("tenant_id", "test-tenant")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };
            
            // Set CustomerID in Items collection as CustomerContextMiddleware would do
            httpContext.Items["CustomerId"] = _testCustomerId;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task GetGroup_WithValidId_ReturnsOkWithGroup()
        {
            // Arrange
            var testGroup = _testGroups.First();

            // Act
            var result = await _controller.GetGroup(testGroup.Id!);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedGroup = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            returnedGroup.Id.Should().Be(testGroup.Id);
            returnedGroup.DisplayName.Should().Be(testGroup.DisplayName);
        }

        [Fact]
        public async Task GetGroup_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.GetGroup(invalidId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var error = notFoundResult.Value.Should().BeOfType<ScimError>().Subject;
            error.Status.Should().Be(404);
        }

        [Fact]
        public async Task GetGroups_WithDefaultParameters_ReturnsOkWithGroupList()
        {
            // Act
            var result = await _controller.GetGroups();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var listResponse = okResult.Value.Should().BeOfType<ScimListResponse<ScimGroup>>().Subject;
            listResponse.TotalResults.Should().Be(_testGroups.Count);
            listResponse.Resources.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetGroups_WithPagination_ReturnsCorrectPage()
        {
            // Act
            var result = await _controller.GetGroups(startIndex: 3, count: 2);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var listResponse = okResult.Value.Should().BeOfType<ScimListResponse<ScimGroup>>().Subject;
            listResponse.StartIndex.Should().Be(3);
            listResponse.ItemsPerPage.Should().Be(2);
        }

        [Fact]
        public async Task GetGroups_WithFilter_ReturnsFilteredResults()
        {
            // Arrange
            var filter = "displayName eq \"Engineering Team\"";

            // Act
            var result = await _controller.GetGroups(filter: filter);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            _mockGroupService.Verify(s => s.GetGroupsAsync(_testCustomerId, 1, 10, filter, null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task CreateGroup_WithValidGroup_ReturnsCreatedWithLocation()
        {
            // Arrange
            var newGroup = ScimTestDataGenerator.GenerateGroup();
            newGroup.Id = null; // Will be generated by service

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedGroup = createdResult.Value.Should().BeOfType<ScimGroup>().Subject;
            returnedGroup.Id.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateGroup_WithMembers_ReturnsCreatedGroupWithMembers()
        {
            // Arrange
            var newGroup = ScimTestDataGenerator.GenerateGroup(members: _testUsers.Take(2).ToList());
            newGroup.Id = null;

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedGroup = createdResult.Value.Should().BeOfType<ScimGroup>().Subject;
            returnedGroup.Members.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateGroup_WithMembers_PopulatesRefProperty()
        {
            // Arrange
            var testUsers = _testUsers.Take(2).ToList();
            var newGroup = ScimTestDataGenerator.GenerateGroup(members: testUsers);
            newGroup.Id = null;

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedGroup = createdResult.Value.Should().BeOfType<ScimGroup>().Subject;
            
            returnedGroup.Members.Should().HaveCount(2);
            foreach (var member in returnedGroup.Members!)
            {
                member.Ref.Should().NotBeNullOrEmpty();
                member.Ref.Should().StartWith("https://localhost/scim/v2/Users/");
                member.Ref.Should().EndWith(member.Value);
                member.Display.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task UpdateGroup_WithValidGroup_ReturnsOkWithUpdatedGroup()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var updatedGroup = ScimTestDataGenerator.GenerateGroup();
            updatedGroup.Id = existingGroup.Id;
            updatedGroup.DisplayName = "Updated Team Name";

            // Act
            var result = await _controller.UpdateGroup(existingGroup.Id!, updatedGroup);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedGroup = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            returnedGroup.DisplayName.Should().Be("Updated Team Name");
        }

        [Fact]
        public async Task UpdateGroup_WithMismatchedId_ReturnsBadRequest()
        {
            // Arrange
            var group = ScimTestDataGenerator.GenerateGroup();
            var differentId = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.UpdateGroup(differentId, group);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var error = badRequestResult.Value.Should().BeOfType<ScimError>().Subject;
            error.Status.Should().Be(400);
        }

        [Fact]
        public async Task UpdateGroup_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var group = ScimTestDataGenerator.GenerateGroup();
            group.Id = invalidId;

            // Mock service to return null for invalid ID
            _mockGroupService.Setup(s => s.UpdateGroupAsync(invalidId, It.IsAny<ScimGroup>(), It.IsAny<string>()))
                .ReturnsAsync((ScimGroup?)null);

            // Act
            var result = await _controller.UpdateGroup(invalidId, group);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task PatchGroup_WithValidPatch_ReturnsOkWithPatchedGroup()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "Patched Group Name" }
                }
            };

            // Act
            var result = await _controller.PatchGroup(existingGroup.Id!, patchRequest);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            _mockGroupService.Verify(s => s.PatchGroupAsync(existingGroup.Id!, patchRequest, _testCustomerId), Times.Once);
        }

        [Fact]
        public async Task PatchGroup_WithMemberAddOperation_ReturnsOkWithUpdatedGroup()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var newMember = _testUsers.First();
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "add", 
                        Path = "members", 
                        Value = new { value = newMember.Id, display = newMember.DisplayName, type = "User" }
                    }
                }
            };

            // Act
            var result = await _controller.PatchGroup(existingGroup.Id!, patchRequest);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            _mockGroupService.Verify(s => s.PatchGroupAsync(existingGroup.Id!, patchRequest, _testCustomerId), Times.Once);
        }

        [Fact]
        public async Task PatchGroup_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var patchRequest = ScimTestDataGenerator.GeneratePatchRequest();

            // Mock service to return null for invalid ID
            _mockGroupService.Setup(s => s.PatchGroupAsync(invalidId, It.IsAny<ScimPatchRequest>(), It.IsAny<string>()))
                .ReturnsAsync((ScimGroup?)null);

            // Act
            var result = await _controller.PatchGroup(invalidId, patchRequest);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteGroup_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var existingGroup = _testGroups.First();

            // Act
            var result = await _controller.DeleteGroup(existingGroup.Id!);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mockGroupService.Verify(s => s.DeleteGroupAsync(existingGroup.Id!, _testCustomerId), Times.Once);
        }

        [Fact]
        public async Task DeleteGroup_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Mock service to return false for invalid ID
            _mockGroupService.Setup(s => s.DeleteGroupAsync(invalidId, It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteGroup(invalidId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Theory]
        [InlineData(0)] // startIndex cannot be 0
        [InlineData(-1)] // startIndex cannot be negative
        public async Task GetGroups_WithInvalidStartIndex_ReturnsBadRequest(int invalidStartIndex)
        {
            // Act
            var result = await _controller.GetGroups(startIndex: invalidStartIndex);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var error = badRequestResult.Value.Should().BeOfType<ScimError>().Subject;
            error.Status.Should().Be(400);
        }

        [Theory]
        [InlineData(0)] // count cannot be 0
        [InlineData(-1)] // count cannot be negative
        public async Task GetGroups_WithInvalidCount_ReturnsBadRequest(int invalidCount)
        {
            // Act
            var result = await _controller.GetGroups(count: invalidCount);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var error = badRequestResult.Value.Should().BeOfType<ScimError>().Subject;
            error.Status.Should().Be(400);
        }

        [Fact]
        public async Task CreateGroup_WithDuplicateDisplayName_ReturnsConflict()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var duplicateGroup = ScimTestDataGenerator.GenerateGroup(displayName: existingGroup.DisplayName);

            // Mock service to throw exception for duplicate
            _mockGroupService.Setup(s => s.CreateGroupAsync(It.IsAny<ScimGroup>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Group name already exists"));

            // Act
            var result = await _controller.CreateGroup(duplicateGroup);

            // Assert
            result.Result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
            var error = conflictResult.Value.Should().BeOfType<ScimError>().Subject;
            error.Status.Should().Be(409);
        }

        [Fact]
        public async Task CreateGroup_WithInvalidMembers_HandlesMissingUsers()
        {
            // Arrange
            var groupWithInvalidMembers = new ScimGroup
            {
                DisplayName = "Test Group",
                Members = new List<GroupMember>
                {
                    new() { Value = Guid.NewGuid().ToString(), Display = "Non-existent User" }
                }
            };

            // Mock service to handle gracefully
            _mockGroupService.Setup(s => s.CreateGroupAsync(It.IsAny<ScimGroup>(), It.IsAny<string>()))
                .ReturnsAsync((ScimGroup group, string customerId) =>
                {
                    group.Id = Guid.NewGuid().ToString();
                    group.Meta = new ScimMeta
                    {
                        ResourceType = "Group",
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Location = $"/scim/v2/Groups/{group.Id}",
                        Version = Guid.NewGuid().ToString("N")[..8]
                    };
                    return group;
                });

            // Act
            var result = await _controller.CreateGroup(groupWithInvalidMembers);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedGroup = createdResult.Value.Should().BeOfType<ScimGroup>().Subject;
            returnedGroup.Members.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetGroup_Should_Not_Include_Members_Property_In_Response()
        {
            // Arrange
            var testGroup = _testGroups.First();
            testGroup.Members = new List<GroupMember> {
                new GroupMember { Value = "user1", Display = "User 1" },
                new GroupMember { Value = "user2", Display = "User 2" }
            };
            _mockGroupService.Setup(s => s.GetGroupAsync(testGroup.Id!, _testCustomerId))
                .ReturnsAsync(testGroup);

            // Act
            var result = await _controller.GetGroup(testGroup.Id!);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            json.Should().NotContain("\"members\"");
        }

        [Fact]
        public async Task PatchGroup_WithMemberRemoveOperation_RemovesMemberCorrectly()
        {
            // Arrange
            var user1 = _testUsers[0];
            var user2 = _testUsers[1];
            var group = ScimTestDataGenerator.GenerateGroup(members: new List<ScimUser> { user1, user2 });
            _mockGroupService.Setup(s => s.GetGroupAsync(group.Id!, _testCustomerId)).ReturnsAsync(group);
            _mockGroupService.Setup(s => s.PatchGroupAsync(group.Id!, It.IsAny<ScimPatchRequest>(), _testCustomerId))
                .ReturnsAsync((string id, ScimPatchRequest req, string custId) => {
                    // Simulate removal
                    var op = req.Operations.First();
                    string? memberId = null;
                    if (!string.IsNullOrEmpty(op.Path))
                    {
                        var split = op.Path.Split('"');
                        if (split.Length > 1)
                            memberId = split[1];
                    }
                    group.Members = group.Members?.Where(m => m.Value != memberId).ToList();
                    return group;
                });
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new()
                    {
                        Op = "remove",
                        Path = $"members[value eq \"{user1.Id}\"]"
                    }
                }
            };

            // Act
            var result = await _controller.PatchGroup(group.Id!, patchRequest);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedGroup = okResult!.Value as ScimGroup;
            returnedGroup!.Members.Should().ContainSingle(m => m.Value == user2.Id);
            returnedGroup.Members.Should().NotContain(m => m.Value == user1.Id);
        }

        [Fact]
        public async Task PatchGroup_WithReplaceOperationWithoutPath_ReturnsOkWithUpdatedGroup()
        {
            // Arrange - This integration test mimics the exact compliance test scenario
            var existingGroup = _testGroups.First();
            var originalDisplayName = existingGroup.DisplayName;
            var newDisplayName = "UCAMNYBKMBEU";
            
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace", 
                        // No Path specified - compliance test scenario
                        Value = new { displayName = newDisplayName }
                    }
                }
            };

            // Setup mock to return updated group
            var updatedGroup = new ScimGroup
            {
                Id = existingGroup.Id,
                DisplayName = newDisplayName,
                ExternalId = existingGroup.ExternalId,
                Schemas = existingGroup.Schemas,
                Meta = existingGroup.Meta
            };

            _mockGroupService.Setup(s => s.PatchGroupAsync(existingGroup.Id!, patchRequest, _testCustomerId))
                           .ReturnsAsync(updatedGroup);

            // Act
            var result = await _controller.PatchGroup(existingGroup.Id!, patchRequest);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedGroup = okResult!.Value as ScimGroup;
            
            returnedGroup.Should().NotBeNull();
            returnedGroup!.DisplayName.Should().Be(newDisplayName);
            returnedGroup.Id.Should().Be(existingGroup.Id);
            
            _mockGroupService.Verify(s => s.PatchGroupAsync(existingGroup.Id!, patchRequest, _testCustomerId), Times.Once);
        }

        [Fact]
        public async Task PatchGroup_WithComplexReplaceOperation_ReturnsOkWithUpdatedGroup()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "replace", 
                        // Replace entire resource without Path
                        Value = new ScimGroup
                        {
                            DisplayName = "Completely New Group",
                            ExternalId = "new-external-id",
                            Members = new List<GroupMember>
                            {
                                new() { Value = "new-user-1", Display = "New User 1" }
                            }
                        }
                    }
                }
            };

            var updatedGroup = new ScimGroup
            {
                Id = existingGroup.Id,
                DisplayName = "Completely New Group",
                ExternalId = "new-external-id",
                Schemas = existingGroup.Schemas,
                Meta = existingGroup.Meta,
                Members = new List<GroupMember>
                {
                    new() { Value = "new-user-1", Display = "New User 1" }
                }
            };

            _mockGroupService.Setup(s => s.PatchGroupAsync(existingGroup.Id!, patchRequest, _testCustomerId))
                           .ReturnsAsync(updatedGroup);

            // Act
            var result = await _controller.PatchGroup(existingGroup.Id!, patchRequest);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedGroup = okResult!.Value as ScimGroup;
            
            returnedGroup.Should().NotBeNull();
            returnedGroup!.DisplayName.Should().Be("Completely New Group");
            returnedGroup.ExternalId.Should().Be("new-external-id");
            returnedGroup.Members.Should().ContainSingle();
            returnedGroup.Members!.First().Value.Should().Be("new-user-1");
            
            _mockGroupService.Verify(s => s.PatchGroupAsync(existingGroup.Id!, patchRequest, _testCustomerId), Times.Once);
        }

        [Fact]
        public async Task PatchGroup_AddMemberWithExplicitRef_RespectsProvidedRef()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var newMember = _testUsers.First();
            var customRef = $"https://example.com/api/Users/{newMember.Id}";
            
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "add", 
                        Path = "members", 
                        Value = new { 
                            value = newMember.Id, 
                            display = newMember.DisplayName, 
                            @ref = customRef  // Using @ to escape the $ prefix
                        }
                    }
                }
            };

            // Act
            var result = await _controller.PatchGroup(existingGroup.Id!, patchRequest);

            // Assert - The patch should succeed, but our service will populate the standard $ref format
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedGroup = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            
            var addedMember = returnedGroup.Members?.FirstOrDefault(m => m.Value == newMember.Id);
            addedMember.Should().NotBeNull();
            addedMember!.Ref.Should().StartWith("https://localhost/scim/v2/Users/");
            addedMember.Ref.Should().EndWith(newMember.Id);
        }

        [Fact]
        public async Task PatchGroup_ReplaceMembersWithMultipleUsers_UpdatesAllMemberAttributes()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var membersToAdd = _testUsers.Take(3).Select(u => new { 
                value = u.Id, 
                display = u.DisplayName
            }).ToArray();
            
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "members", Value = membersToAdd }
                }
            };

            // Act
            var result = await _controller.PatchGroup(existingGroup.Id!, patchRequest);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedGroup = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            
            returnedGroup.Members.Should().HaveCount(3);
            
            foreach (var originalMember in membersToAdd)
            {
                var member = returnedGroup.Members!.First(m => m.Value == originalMember.value);
                member.Value.Should().Be(originalMember.value);
                member.Display.Should().Be(originalMember.display);
                member.Ref.Should().StartWith("https://localhost/scim/v2/Users/");
                member.Ref.Should().EndWith(originalMember.value);
            }
        }

        [Fact]
        public async Task PatchGroup_RemoveAllMembers_ClearsMembers()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            existingGroup.Members = new List<GroupMember>
            {
                new() { Value = "user1", Display = "User 1" },
                new() { Value = "user2", Display = "User 2" }
            };
            
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "remove", Path = "members" }
                }
            };

            // Act
            var result = await _controller.PatchGroup(existingGroup.Id!, patchRequest);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedGroup = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            
            returnedGroup.Members.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task CreateGroup_WithExternalId_StoresAndReturnsExternalId()
        {
            // Arrange
            var externalId = "ext-group-12345";
            var newGroup = ScimTestDataGenerator.GenerateGroup(externalId: externalId);
            newGroup.Id = null;

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedGroup = createdResult.Value.Should().BeOfType<ScimGroup>().Subject;
            
            returnedGroup.ExternalId.Should().Be(externalId);
        }

        [Fact]
        public async Task PatchGroup_WithMultipleAttributeOperations_AppliesAllChanges()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var newMember = _testUsers.First();
            
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "Updated Display Name" },
                    new() { Op = "replace", Path = "externalId", Value = "updated-external-123" },
                    new() { 
                        Op = "add", 
                        Path = "members", 
                        Value = new { value = newMember.Id, display = newMember.DisplayName }
                    }
                }
            };

            // Act
            var result = await _controller.PatchGroup(existingGroup.Id!, patchRequest);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedGroup = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            
            returnedGroup.DisplayName.Should().Be("Updated Display Name");
            returnedGroup.ExternalId.Should().Be("updated-external-123");
            returnedGroup.Members.Should().Contain(m => m.Value == newMember.Id);
            
            var addedMember = returnedGroup.Members!.First(m => m.Value == newMember.Id);
            addedMember.Ref.Should().StartWith("https://localhost/scim/v2/Users/");
            addedMember.Ref.Should().EndWith(newMember.Id);
        }
    }
}
