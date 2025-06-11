using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Data;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class GroupServiceFilterTests : IDisposable
    {
        private readonly ScimDbContext _context;
        private readonly GroupService _groupService;
        private readonly string _testCustomerId = "test-customer-1";

        public GroupServiceFilterTests()
        {
            var options = new DbContextOptionsBuilder<ScimDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ScimDbContext(options);
            _groupService = new GroupService(_context);
        }

        [Fact]
        public async Task GetGroupsAsync_WithIdFilter_ReturnsCorrectGroup()
        {
            // Arrange
            var group = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                ExternalId = "test-external-id",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Act
            var result = await _groupService.GetGroupsAsync(
                _testCustomerId, 
                startIndex: 1, 
                count: 10, 
                filter: $"Id eq \"{group.Id}\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().Id.Should().Be(group.Id);
        }

        [Fact]
        public async Task GetGroupsAsync_WithUnsupportedMemberTypeFilter_ThrowsInvalidOperationException()
        {
            // Arrange
            var group = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Test Group",
                CustomerId = _testCustomerId
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _groupService.GetGroupsAsync(
                    _testCustomerId, 
                    startIndex: 1, 
                    count: 10, 
                    filter: "members[type eq \"untyped\"].value eq \"some-value\""));

            exception.Message.Should().Contain("contains unsupported member type filtering");
            exception.Message.Should().Contain("Group member filtering by type is not supported in GET operations");
        }

        [Fact]
        public async Task GetGroupsAsync_WithSupportedFilters_WorksCorrectly()
        {
            // Arrange
            var group1 = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Engineering Team",
                ExternalId = "eng-001",
                CustomerId = _testCustomerId
            };
            var group2 = new ScimGroup
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Marketing Team",
                ExternalId = "mkt-001",
                CustomerId = _testCustomerId
            };
            _context.Groups.AddRange(group1, group2);
            await _context.SaveChangesAsync();

            // Test Id filter
            var result1 = await _groupService.GetGroupsAsync(
                _testCustomerId, 
                filter: $"Id eq \"{group1.Id}\"");
            result1.TotalResults.Should().Be(1);
            result1.Resources.First().Id.Should().Be(group1.Id);

            // Test displayName filter
            var result2 = await _groupService.GetGroupsAsync(
                _testCustomerId, 
                filter: "displayName eq \"Engineering Team\"");
            result2.TotalResults.Should().Be(1);
            result2.Resources.First().DisplayName.Should().Be("Engineering Team");

            // Test externalId filter
            var result3 = await _groupService.GetGroupsAsync(
                _testCustomerId, 
                filter: "externalId eq \"mkt-001\"");
            result3.TotalResults.Should().Be(1);
            result3.Resources.First().ExternalId.Should().Be("mkt-001");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
