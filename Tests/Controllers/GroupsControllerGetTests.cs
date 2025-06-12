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
    public class GroupsControllerGetTests
    {
        private readonly Mock<IGroupService> _mockGroupService;
        private readonly Mock<ILogger<GroupsController>> _mockLogger;
        private readonly GroupsController _controller;
        private readonly List<ScimGroup> _testGroups;
        private readonly List<ScimUser> _testUsers;
        private readonly string _testCustomerId = UserTestDataGenerator.DefaultCustomerId;

        public GroupsControllerGetTests()
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
        public async Task GetGroup_WithValidId_ReturnsOkWithGroup()
        {
            // Arrange
            var groupId = _testGroups.First().Id!;

            // Act
            var result = await _controller.GetGroup(groupId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var group = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            group.Id.Should().Be(groupId);
        }

        [Fact]
        public async Task GetGroup_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            _mockGroupService.Setup(x => x.GetGroupAsync(invalidId, _testCustomerId))
                .ReturnsAsync((ScimGroup?)null);

            // Act
            var result = await _controller.GetGroup(invalidId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetGroups_WithDefaultParameters_ReturnsOkWithGroupList()
        {
            // Act
            var result = await _controller.GetGroups();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var groupList = okResult.Value.Should().BeOfType<ScimListResponse<ScimGroup>>().Subject;
            groupList.Resources.Should().HaveCount(_testGroups.Count);
        }

        [Fact]
        public async Task GetGroups_WithPagination_ReturnsCorrectPage()
        {
            // Act
            var result = await _controller.GetGroups(startIndex: 2, count: 2);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var groupList = okResult.Value.Should().BeOfType<ScimListResponse<ScimGroup>>().Subject;
            groupList.StartIndex.Should().Be(2);
            groupList.ItemsPerPage.Should().Be(2);
        }

        [Fact]
        public async Task GetGroups_WithFilter_ReturnsFilteredResults()
        {
            // Act
            var result = await _controller.GetGroups(filter: "displayName eq \"Test Group\"");

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var groupList = okResult.Value.Should().BeOfType<ScimListResponse<ScimGroup>>().Subject;
            groupList.Should().NotBeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetGroups_WithInvalidStartIndex_ReturnsBadRequest(int invalidStartIndex)
        {
            // Act
            var result = await _controller.GetGroups(startIndex: invalidStartIndex);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetGroups_WithInvalidCount_ReturnsBadRequest(int invalidCount)
        {
            // Act
            var result = await _controller.GetGroups(count: invalidCount);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetGroup_Should_Not_Include_Members_Property_In_Response()
        {
            // Arrange
            var group = _testGroups.First();
            group.Members = new List<GroupMember>
            {
                new() { Value = _testUsers.First().Id!, Display = _testUsers.First().DisplayName }
            };

            // Act
            var result = await _controller.GetGroup(group.Id!);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedGroup = okResult.Value.Should().BeOfType<ScimGroup>().Subject;
            returnedGroup.Members.Should().BeNull("Groups endpoint should not include members property");
        }
    }
}
