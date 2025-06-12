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
    public class GroupsControllerCrudTests
    {
        private readonly Mock<IGroupService> _mockGroupService;
        private readonly Mock<ILogger<GroupsController>> _mockLogger;
        private readonly GroupsController _controller;
        private readonly List<ScimGroup> _testGroups;
        private readonly List<ScimUser> _testUsers;
        private readonly string _testCustomerId = UserTestDataGenerator.DefaultCustomerId;

        public GroupsControllerCrudTests()
        {
            _testUsers = UserTestDataGenerator.GenerateUsers(5);
            _testGroups = GroupTestDataGenerator.GenerateGroups(5, _testUsers);
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

            httpContext.Request.Headers["Host"] = "localhost:5000";
            httpContext.Request.Scheme = "https";
            
            // Set CustomerID in Items collection as CustomerContextMiddleware would do
            httpContext.Items["CustomerId"] = _testCustomerId;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task CreateGroup_WithValidGroup_ReturnsCreatedWithLocation()
        {
            // Arrange
            var newGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var createdGroup = createdResult.Value.Should().BeOfType<ScimGroup>().Subject;
            createdGroup.DisplayName.Should().Be(newGroup.DisplayName);
            createdResult.ActionName.Should().Be("GetGroup");
        }

        [Fact]
        public async Task CreateGroup_WithMembers_ReturnsCreatedGroupWithMembers()
        {
            // Arrange
            var newGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            newGroup.Members = new List<GroupMember>
            {
                new() { Value = _testUsers.First().Id!, Display = _testUsers.First().DisplayName }
            };

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var createdGroup = createdResult.Value.Should().BeOfType<ScimGroup>().Subject;
            createdGroup.Members.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateGroup_WithMembers_PopulatesRefProperty()
        {
            // Arrange
            var newGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            var userId = _testUsers.First().Id!;
            newGroup.Members = new List<GroupMember>
            {
                new() { Value = userId, Display = _testUsers.First().DisplayName }
            };

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var createdGroup = createdResult.Value.Should().BeOfType<ScimGroup>().Subject;
            createdGroup.Members!.First().Ref.Should().Be($"../Users/{userId}");
        }

        [Fact]
        public async Task UpdateGroup_WithValidGroup_ReturnsOkWithUpdatedGroup()
        {
            // Arrange
            var groupId = _testGroups.First().Id!;
            var updatedGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            updatedGroup.Id = groupId;

            // Act
            var result = await _controller.UpdateGroup(groupId, updatedGroup);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var group = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            group.Id.Should().Be(groupId);
        }

        [Fact]
        public async Task UpdateGroup_WithMismatchedId_ReturnsBadRequest()
        {
            // Arrange
            var groupId = _testGroups.First().Id!;
            var updatedGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            updatedGroup.Id = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.UpdateGroup(groupId, updatedGroup);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateGroup_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var updatedGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            updatedGroup.Id = invalidId;

            _mockGroupService.Setup(x => x.UpdateGroupAsync(invalidId, updatedGroup, _testCustomerId))
                .ReturnsAsync((ScimGroup?)null);

            // Act
            var result = await _controller.UpdateGroup(invalidId, updatedGroup);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteGroup_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var groupId = _testGroups.First().Id!;

            // Act
            var result = await _controller.DeleteGroup(groupId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteGroup_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            _mockGroupService.Setup(x => x.DeleteGroupAsync(invalidId, _testCustomerId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteGroup(invalidId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateGroup_WithDuplicateDisplayName_ReturnsConflict()
        {
            // Arrange
            var existingGroup = _testGroups.First();
            var newGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            newGroup.DisplayName = existingGroup.DisplayName;

            _mockGroupService.Setup(x => x.CreateGroupAsync(It.IsAny<ScimGroup>(), _testCustomerId))
                .ThrowsAsync(new InvalidOperationException("Group with this display name already exists"));

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateGroup_WithInvalidMembers_HandlesMissingUsers()
        {
            // Arrange
            var newGroup = GroupTestDataGenerator.CreateGroup(_testCustomerId);
            newGroup.Members = new List<GroupMember>
            {
                new() { Value = "nonexistent-user-id", Display = "Nonexistent User" }
            };

            _mockGroupService.Setup(x => x.CreateGroupAsync(It.IsAny<ScimGroup>(), _testCustomerId))
                .ThrowsAsync(new ArgumentException("Referenced user does not exist"));

            // Act
            var result = await _controller.CreateGroup(newGroup);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }
    }
}
