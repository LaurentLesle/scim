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
    public class GroupsControllerPatchTests
    {
        private readonly Mock<IGroupService> _mockGroupService;
        private readonly Mock<ILogger<GroupsController>> _mockLogger;
        private readonly GroupsController _controller;
        private readonly List<ScimGroup> _testGroups;
        private readonly List<ScimUser> _testUsers;
        private readonly string _testCustomerId = UserTestDataGenerator.DefaultCustomerId;

        public GroupsControllerPatchTests()
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
        public async Task PatchGroup_WithValidPatch_ReturnsOkWithPatchedGroup()
        {
            // Arrange
            var groupId = _testGroups.First().Id!;
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "Updated Group Name" }
                }
            };

            // Act
            var result = await _controller.PatchGroup(groupId, patchRequest);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var group = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            group.Id.Should().Be(groupId);
        }

        [Fact]
        public async Task PatchGroup_WithMemberAddOperation_ReturnsOkWithUpdatedGroup()
        {
            // Arrange
            var groupId = _testGroups.First().Id!;
            var userId = _testUsers.First().Id!;
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() 
                    { 
                        Op = "add", 
                        Path = "members", 
                        Value = new { value = userId, display = "Test User" } 
                    }
                }
            };

            // Act
            var result = await _controller.PatchGroup(groupId, patchRequest);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var group = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            group.Id.Should().Be(groupId);
        }

        [Fact]
        public async Task PatchGroup_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "Updated Name" }
                }
            };

            _mockGroupService.Setup(x => x.PatchGroupAsync(invalidId, patchRequest, _testCustomerId))
                .ReturnsAsync((ScimGroup?)null);

            // Act
            var result = await _controller.PatchGroup(invalidId, patchRequest);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
