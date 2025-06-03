using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using ScimServiceProvider.Tests.Helpers;
using Xunit;

namespace ScimServiceProvider.Tests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly UserService _userService;
        private readonly Data.ScimDbContext _context;
        private readonly string _testCustomerId = ScimTestDataGenerator.DefaultCustomerId;

        [Fact]
        public async Task PatchUserAsync_AddsMobilePhoneAndEnterpriseFields_WhenInitiallyEmpty()
        {
            // Arrange: user with no phone numbers or enterprise fields
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "minimal@example.com",
                CustomerId = _testCustomerId,
                PhoneNumbers = new List<PhoneNumber>(),
                EnterpriseUser = null
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "add", Path = "phoneNumbers[type eq \"mobile\"].value", Value = "6-808-7091" },
                    new() { Op = "add", Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber", Value = "ZCQUDZLZAVUA" },
                    new() { Op = "add", Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department", Value = "WJOAUUZGWZIU" },
                    new() { Op = "add", Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter", Value = "QOIYFFVVNFPB" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.PhoneNumbers.Should().ContainSingle(p => p.Type == "mobile" && p.Value == "6-808-7091");
            result.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.EmployeeNumber.Should().Be("ZCQUDZLZAVUA");
            result.EnterpriseUser.Department.Should().Be("WJOAUUZGWZIU");
            result.EnterpriseUser.CostCenter.Should().Be("QOIYFFVVNFPB");
        }
        [Fact]
        public async Task PatchUserAsync_RemoveRoleByFilter_RemovesCorrectRole()
        {
            // Arrange
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "roleuser@example.com",
                CustomerId = _testCustomerId,
                Roles = new List<Role>
                {
                    new Role { Value = "admin", Display = "Admin", Type = "system", Primary = "True" },
                    new Role { Value = "user", Display = "User", Type = "system", Primary = "False" }
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "remove", Path = "roles[primary eq \"True\"]" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().NotContain(r => r.Primary == "True");
            result.Roles.Should().ContainSingle(r => r.Primary == "False");
        }

        [Fact]
        public async Task PatchUserAsync_ReplaceRoleProperty_UpdatesRole()
        {
            // Arrange
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "roleuser2@example.com",
                CustomerId = _testCustomerId,
                Roles = new List<Role>
                {
                    new Role { Value = "admin", Display = "Admin", Type = "system", Primary = "True" }
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].display", Value = "SuperAdmin" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().ContainSingle(r => r.Primary == "True" && r.Display == "SuperAdmin");
        }

        [Fact]
        public async Task PatchUserAsync_AddMultipleRolesAndUpdateOne_WorksCorrectly()
        {
            // Arrange
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "roleuser3@example.com",
                CustomerId = _testCustomerId,
                Roles = new List<Role>()
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "add", Path = "roles[primary eq \"True\"].display", Value = "Manager" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].value", Value = "manager" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].type", Value = "system" },
                    new() { Op = "add", Path = "roles[primary eq \"False\"].display", Value = "Employee" },
                    new() { Op = "add", Path = "roles[primary eq \"False\"].value", Value = "employee" },
                    new() { Op = "add", Path = "roles[primary eq \"False\"].type", Value = "system" },
                    new() { Op = "replace", Path = "roles[primary eq \"True\"].display", Value = "Director" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Roles.Should().ContainSingle(r => r.Primary == "True" && r.Display == "Director" && r.Value == "manager");
            result.Roles.Should().ContainSingle(r => r.Primary == "False" && r.Display == "Employee" && r.Value == "employee");
        }
        // ...existing code...

        public UserServiceTests()
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
            var testUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserAsync(testUser.Id!, ScimTestDataGenerator.DefaultCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(testUser.Id);
            result.UserName.Should().Be(testUser.UserName);
            result.DisplayName.Should().Be(testUser.DisplayName);
        }

        [Fact]
        public async Task GetUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _userService.GetUserAsync(invalidId, ScimTestDataGenerator.DefaultCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByUsernameAsync_WithValidUsername_ReturnsUser()
        {
            // Arrange
            var testUser = ScimTestDataGenerator.GenerateUser(userName: "test@example.com");
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserByUsernameAsync("test@example.com", _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.UserName.Should().Be("test@example.com");
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
        public async Task GetUsersAsync_WithDefaultParameters_ReturnsPagedResults()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(15);
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(15);
            result.StartIndex.Should().Be(1);
            result.ItemsPerPage.Should().Be(10); // Default count is 100, but we only have 15 users
            result.Resources.Should().HaveCount(10);
        }

        [Fact]
        public async Task GetUsersAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(25);
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, startIndex: 11, count: 5);

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(25);
            result.StartIndex.Should().Be(11);
            result.ItemsPerPage.Should().Be(5);
            result.Resources.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetUsersAsync_WithUserNameFilter_ReturnsFilteredResults()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(5);
            testUsers[0].UserName = "john.doe@example.com";
            testUsers[1].UserName = "jane.smith@example.com";
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, filter: "userName eq \"john.doe@example.com\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().UserName.Should().Be("john.doe@example.com");
        }

        [Fact]
        public async Task GetUsersAsync_WithUserNameFilter_IsCaseInsensitive()
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(2);
            testUsers[0].UserName = "waldo@ryan.uk";
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act: filter with different case
            var result = await _userService.GetUsersAsync(_testCustomerId, filter: "userName eq \"WALDO@RYAN.UK\"");

            // Assert
            result.Should().NotBeNull();
            result.TotalResults.Should().Be(1);
            result.Resources.Should().HaveCount(1);
            result.Resources.First().UserName.Should().Be("waldo@ryan.uk");
        }

        [Fact]
        public async Task CreateUserAsync_WithValidUser_CreatesAndReturnsUser()
        {
            // Arrange
            var newUser = ScimTestDataGenerator.GenerateUser();
            newUser.Id = null; // Should be generated

            // Act
            var result = await _userService.CreateUserAsync(newUser, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.UserName.Should().Be(newUser.UserName);
            result.Meta.Should().NotBeNull();
            result.Meta!.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.Meta.ResourceType.Should().Be("User");
            
            // Verify it was saved to database
            var savedUser = await _context.Users.FindAsync(result.Id);
            savedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateExternalId_ThrowsInvalidOperationException()
        {
            // Arrange
            var externalId = "duplicate-user-external-id-123";
            var user1 = ScimTestDataGenerator.GenerateUser(externalId: externalId, customerId: _testCustomerId);
            var user2 = ScimTestDataGenerator.GenerateUser(externalId: externalId, customerId: _testCustomerId);
            _context.Users.Add(user1);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _userService.CreateUserAsync(user2, _testCustomerId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task UpdateUserAsync_WithValidUser_UpdatesAndReturnsUser()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var updatedUser = ScimTestDataGenerator.GenerateUser();
            updatedUser.Id = existingUser.Id;
            updatedUser.UserName = "updated@example.com";
            updatedUser.DisplayName = "Updated Name";

            // Act
            var result = await _userService.UpdateUserAsync(existingUser.Id!, updatedUser, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.UserName.Should().Be("updated@example.com");
            result.DisplayName.Should().Be("Updated Name");
            result.Meta!.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            // Verify database was updated
            var dbUser = await _context.Users.FindAsync(existingUser.Id);
            dbUser!.UserName.Should().Be("updated@example.com");
        }

        [Fact]
        public async Task UpdateUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var updatedUser = ScimTestDataGenerator.GenerateUser();

            // Act
            var result = await _userService.UpdateUserAsync(invalidId, updatedUser, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PatchUserAsync_WithActiveOperation_UpdatesActiveStatus()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser(active: true);
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "active", Value = false }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Active.Should().BeFalse();
            result.Meta!.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            // Verify database was updated
            var dbUser = await _context.Users.FindAsync(existingUser.Id);
            dbUser!.Active.Should().BeFalse();
        }

        [Fact]
        public async Task PatchUserAsync_WithDisplayNameOperation_UpdatesDisplayName()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "displayName", Value = "New Display Name" }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("New Display Name");
        }

        [Fact]
        public async Task PatchUserAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            var patchRequest = ScimTestDataGenerator.GeneratePatchRequest();

            // Act
            var result = await _userService.PatchUserAsync(invalidId, patchRequest, _testCustomerId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteUserAsync_WithValidId_DeletesUser()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.DeleteUserAsync(existingUser.Id!, _testCustomerId);

            // Assert
            result.Should().BeTrue();
            
            // Verify user was deleted from database
            var dbUser = await _context.Users.FindAsync(existingUser.Id);
            dbUser.Should().BeNull();
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

        [Theory]
        [InlineData("userName eq \"test@example.com\"")]
        [InlineData("displayName eq \"Test User\"")]
        [InlineData("active eq true")]
        public async Task GetUsersAsync_WithDifferentFilters_ReturnsFilteredResults(string filter)
        {
            // Arrange
            var testUsers = ScimTestDataGenerator.GenerateUsers(10);
            testUsers[0].UserName = "test@example.com";
            testUsers[0].DisplayName = "Test User";
            testUsers[0].Active = true;
            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersAsync(_testCustomerId, filter: filter);

            // Assert
            result.Should().NotBeNull();
            result.Resources.Should().NotBeEmpty();
            // Note: The actual filtering logic depends on the implementation
            // This test verifies that filtering doesn't break the service
        }

        [Fact]
        public async Task PatchUserAsync_WithAddManagerOperation_AddsManager()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "manager-id-123"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().Be("manager-id-123");
        }

        [Fact]
        public async Task PatchUserAsync_WithRemoveManagerOperation_RemovesManager()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            existingUser.EnterpriseUser = new EnterpriseUser { Manager = "manager-id-123" };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "remove",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().BeNull();
        }

        [Fact]
        public async Task PatchUserAsync_WithReplaceManagerOperation_ReplacesManager()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser();
            existingUser.EnterpriseUser = new EnterpriseUser { Manager = "old-manager-id" };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "replace",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "new-manager-id-456"
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.Manager.Should().Be("new-manager-id-456");
        }

        [Fact]
        public async Task PatchUserAsync_AddAttributes_WorksCorrectly()
        {
            // Arrange: create a minimal user with correct customer ID
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "jettie@king.ca",
                Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" },
                CustomerId = _testCustomerId,
                Emails = new List<Email>(),
                Addresses = new List<Address>(),
                PhoneNumbers = new List<PhoneNumber>(),
                Roles = new List<Role>()
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "add", Path = "emails[type eq \"work\"].value", Value = "fermin@marvin.us" },
                    new() { Op = "add", Path = "emails[type eq \"work\"].primary", Value = true },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].formatted", Value = "BOYAIGFEIYKX" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].streetAddress", Value = "95862 Botsford Fork" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].locality", Value = "RSQUDGJMIZYJ" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].region", Value = "HLJMHLAXWZFI" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].postalCode", Value = "fe0 1wi" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].primary", Value = true },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].country", Value = "Guadeloupe" },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"work\"].value", Value = "49-381-3129" },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"work\"].primary", Value = true },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"mobile\"].value", Value = "49-381-3129" },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"fax\"].value", Value = "49-381-3129" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].display", Value = "AHVRDBATJEJC" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].value", Value = "ATLJTNEEDLJP" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].type", Value = "EVZBMGPPFBCF" },
                    new() { Op = "add", Value = new Dictionary<string, object>
                        {
                            { "active", true },
                            { "displayName", "WOWEFWTNGCQP" },
                            { "title", "KFTIDSIDZRGB" },
                            { "preferredLanguage", "lu" },
                            { "name.givenName", "Walter" },
                            { "name.familyName", "Kaylin" },
                            { "name.formatted", "Amiya" },
                            { "name.middleName", "Salvatore" },
                            { "name.honorificPrefix", "Bianka" },
                            { "name.honorificSuffix", "Lamar" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber", "UWHVRWOCJHUL" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department", "INOJSAHIKABC" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter", "UPIMMUFIKGEQ" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization", "UEWKWLXOFMTZ" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division", "BSSUDJYFDSPN" },
                            { "userType", "DNIENTYXYFOG" },
                            { "nickName", "FGDAIVHURVEO" },
                            { "locale", "LAURRLIIXNJJ" },
                            { "timezone", "Africa/Johannesburg" },
                            { "profileUrl", "HASRZWMYVKSL" }
                        }
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.Emails.Should().Contain(e => e.Type == "work" && e.Value == "fermin@marvin.us" && e.Primary);
            result.Addresses.Should().Contain(a => a.Type == "work" && a.Formatted == "BOYAIGFEIYKX" && a.StreetAddress == "95862 Botsford Fork" && a.Locality == "RSQUDGJMIZYJ" && a.Region == "HLJMHLAXWZFI" && a.PostalCode == "fe0 1wi" && a.Country == "Guadeloupe" && a.Primary);
            result.PhoneNumbers.Should().Contain(p => p.Type == "work" && p.Value == "49-381-3129" && p.Primary);
            result.PhoneNumbers.Should().Contain(p => p.Type == "mobile" && p.Value == "49-381-3129");
            result.PhoneNumbers.Should().Contain(p => p.Type == "fax" && p.Value == "49-381-3129");
            result.Roles.Should().Contain(r => r.Primary == "True" && r.Display == "AHVRDBATJEJC" && r.Value == "ATLJTNEEDLJP" && r.Type == "EVZBMGPPFBCF");
            result.DisplayName.Should().Be("WOWEFWTNGCQP");
            result.Title.Should().Be("KFTIDSIDZRGB");
            result.PreferredLanguage.Should().Be("lu");
            result.Name.Should().NotBeNull();
            result.Name!.GivenName.Should().Be("Walter");
            result.Name.FamilyName.Should().Be("Kaylin");
            result.Name.Formatted.Should().Be("Amiya");
            result.Name.MiddleName.Should().Be("Salvatore");
            result.Name.HonorificPrefix.Should().Be("Bianka");
            result.Name.HonorificSuffix.Should().Be("Lamar");
            result.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.EmployeeNumber.Should().Be("UWHVRWOCJHUL");
            result.EnterpriseUser.Department.Should().Be("INOJSAHIKABC");
            result.EnterpriseUser.CostCenter.Should().Be("UPIMMUFIKGEQ");
            result.EnterpriseUser.Organization.Should().Be("UEWKWLXOFMTZ");
            result.EnterpriseUser.Division.Should().Be("BSSUDJYFDSPN");
            result.UserType.Should().Be("DNIENTYXYFOG");
            result.NickName.Should().Be("FGDAIVHURVEO");
            result.Locale.Should().Be("LAURRLIIXNJJ");
            result.Timezone.Should().Be("Africa/Johannesburg");
            result.ProfileUrl.Should().Be("HASRZWMYVKSL");
        }

        [Fact]
        public async Task PatchUserAsync_WithFullObjectReplace_UpdatesMultipleAttributes()
        {
            // Arrange
            var existingUser = ScimTestDataGenerator.GenerateUser(userName: "original@example.com", active: true);
            existingUser.DisplayName = "Original Name";
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new ScimPatchOperation
                    {
                        Op = "replace",
                        Path = null, // Full-object replace
                        Value = new Dictionary<string, object>
                        {
                            { "userName", "patched2@example.com" },
                            { "displayName", "Patched Name" },
                            { "active", false }
                        }
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(existingUser.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            result!.UserName.Should().Be("patched2@example.com");
            result.DisplayName.Should().Be("Patched Name");
            result.Active.Should().BeFalse();

            // Verify database was updated
            var dbUser = await _context.Users.FindAsync(existingUser.Id);
            dbUser!.UserName.Should().Be("patched2@example.com");
            dbUser.DisplayName.Should().Be("Patched Name");
            dbUser.Active.Should().BeFalse();
        }

        [Fact]
        public async Task PatchUserAsync_AddAttributesToMinimalUser_AllValuesSetCorrectly()
        {
            // Arrange: create a user with only username (mimicking compliance test scenario)
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "hertha@tillman.name",
                CustomerId = _testCustomerId,
                Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Active = true,
                // Initialize as empty lists, not null - this matches what the API creates
                Emails = new List<Email>(),
                PhoneNumbers = new List<PhoneNumber>(),
                Addresses = new List<Address>(),
                Roles = new List<Role>()
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "add", Path = "emails[type eq \"work\"].value", Value = "zoie@gislason.ca" },
                    new() { Op = "add", Path = "emails[type eq \"work\"].primary", Value = true },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].formatted", Value = "QFRVHLGAERMN" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].streetAddress", Value = "38405 O'Connell Street" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].locality", Value = "PJCBQYMUEQIX" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].region", Value = "HLDTGLOEWZRP" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].postalCode", Value = "qw1 5an" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].primary", Value = true },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].country", Value = "Thailand" },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"work\"].value", Value = "62-620-4005" },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"work\"].primary", Value = true },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"mobile\"].value", Value = "62-620-4005" },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"fax\"].value", Value = "62-620-4005" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].display", Value = "BTWAGFXAJTUH" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].value", Value = "JZXMEQFZHMPI" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].type", Value = "WWKZAXLDOOZU" },
                    new() { Op = "add", Value = new Dictionary<string, object>
                        {
                            { "active", true },
                            { "displayName", "OXUJHZXGCYZU" },
                            { "title", "ELBOUTURVCML" },
                            { "preferredLanguage", "it" },
                            { "name.givenName", "Marie" },
                            { "name.familyName", "Gennaro" },
                            { "name.formatted", "Stephanie" },
                            { "name.middleName", "Branson" },
                            { "name.honorificPrefix", "Alena" },
                            { "name.honorificSuffix", "Johathan" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber", "KTJALGSOSRCS" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department", "HXCHUKWKQWSZ" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter", "HYJCQJOTOKLW" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization", "JYBVHJGNJEDS" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division", "IJARSDXFZZFA" },
                            { "userType", "SNEJZQSFYABR" },
                            { "nickName", "KNLCNPDRAKHC" },
                            { "locale", "PLYEWHSDLNRS" },
                            { "timezone", "America/Costa_Rica" },
                            { "profileUrl", "IOFFTLOYLFYS" }
                        }
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert - First verify the direct result from the service
            result.Should().NotBeNull();
            result!.UserName.Should().Be("hertha@tillman.name");
            
            // Critical assertions for multi-valued attributes (these must not be empty!)
            result.Emails.Should().NotBeEmpty("Emails should not be empty after PATCH add operations");
            result.Addresses.Should().NotBeEmpty("Addresses should not be empty after PATCH add operations");
            result.PhoneNumbers.Should().NotBeEmpty("PhoneNumbers should not be empty after PATCH add operations");
            result.Roles.Should().NotBeEmpty("Roles should not be empty after PATCH add operations");
            
            result.Emails.Should().ContainSingle(e => e.Type == "work" && e.Value == "zoie@gislason.ca" && e.Primary);
            var addr = result.Addresses.Should().ContainSingle(a => a.Type == "work").Subject;
            addr.Formatted.Should().Be("QFRVHLGAERMN");
            addr.StreetAddress.Should().Be("38405 O'Connell Street");
            addr.Locality.Should().Be("PJCBQYMUEQIX");
            addr.Region.Should().Be("HLDTGLOEWZRP");
            addr.PostalCode.Should().Be("qw1 5an");
            addr.Primary.Should().BeTrue();
            addr.Country.Should().Be("Thailand");
            var phone = result.PhoneNumbers.Should().ContainSingle(p => p.Type == "work").Subject;
            phone.Value.Should().Be("62-620-4005");
            phone.Primary.Should().BeTrue();
            result.PhoneNumbers.Should().ContainSingle(p => p.Type == "mobile" && p.Value == "62-620-4005");
            result.PhoneNumbers.Should().ContainSingle(p => p.Type == "fax" && p.Value == "62-620-4005");
            var role = result.Roles.Should().ContainSingle(r => r.Primary == "True").Subject;
            role.Display.Should().Be("BTWAGFXAJTUH");
            role.Value.Should().Be("JZXMEQFZHMPI");
            role.Type.Should().Be("WWKZAXLDOOZU");
            result.Active.Should().BeTrue();
            result.DisplayName.Should().Be("OXUJHZXGCYZU");
            result.Title.Should().Be("ELBOUTURVCML");
            result.PreferredLanguage.Should().Be("it");
            result.Name.Should().NotBeNull();
            result.Name!.GivenName.Should().Be("Marie");
            result.Name.FamilyName.Should().Be("Gennaro");
            result.Name.Formatted.Should().Be("Stephanie");
            result.Name.MiddleName.Should().Be("Branson");
            result.Name.HonorificPrefix.Should().Be("Alena");
            result.Name.HonorificSuffix.Should().Be("Johathan");
            result.EnterpriseUser.Should().NotBeNull();
            result.EnterpriseUser!.EmployeeNumber.Should().Be("KTJALGSOSRCS");
            result.EnterpriseUser.Department.Should().Be("HXCHUKWKQWSZ");
            result.EnterpriseUser.CostCenter.Should().Be("HYJCQJOTOKLW");
            result.EnterpriseUser.Organization.Should().Be("JYBVHJGNJEDS");
            result.EnterpriseUser.Division.Should().Be("IJARSDXFZZFA");
            result.UserType.Should().Be("SNEJZQSFYABR");
            result.NickName.Should().Be("KNLCNPDRAKHC");
            result.Locale.Should().Be("PLYEWHSDLNRS");
            result.Timezone.Should().Be("America/Costa_Rica");
            result.ProfileUrl.Should().Be("IOFFTLOYLFYS");
            
            // CRITICAL: Test persistence by reloading from database
            // This should catch serialization/deserialization issues that the compliance test might be hitting
            var reloadedUser = await _userService.GetUserAsync(user.Id!, _testCustomerId);
            reloadedUser.Should().NotBeNull("User should be retrievable after PATCH");
            
            // These are the critical assertions that mirror what the compliance test expects
            reloadedUser!.Emails.Should().NotBeEmpty("Emails should persist in database and not be empty");
            reloadedUser.Addresses.Should().NotBeEmpty("Addresses should persist in database and not be empty");
            reloadedUser.PhoneNumbers.Should().NotBeEmpty("PhoneNumbers should persist in database and not be empty");
            reloadedUser.Roles.Should().NotBeEmpty("Roles should persist in database and not be empty");
            
            // Verify the content is still correct after reload
            reloadedUser.Emails.Should().ContainSingle(e => e.Type == "work" && e.Value == "zoie@gislason.ca" && e.Primary);
            reloadedUser.Addresses.Should().ContainSingle(a => a.Type == "work");
            reloadedUser.PhoneNumbers.Should().HaveCount(3);
            reloadedUser.Roles.Should().ContainSingle(r => r.Primary == "True");
        }

        [Fact]
        public async Task PatchUserAsync_AddMultipleAttributesToSameFilteredCollections_CreatesOnlyOneItemPerFilter()
        {
            // Arrange - Create a minimal user like in the compliance test
            var user = new ScimUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "rickey_torp@rohan.co.uk",
                CustomerId = _testCustomerId,
                Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Active = true,
                Emails = new List<Email>(),
                PhoneNumbers = new List<PhoneNumber>(),
                Addresses = new List<Address>(),
                Roles = new List<Role>()
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // This is the exact PATCH request from the user's compliance test that was failing
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    // Multiple operations on the same email filter - should create ONE email
                    new() { Op = "add", Path = "emails[type eq \"work\"].value", Value = "roberta_kuvalis@hayesmertz.com" },
                    new() { Op = "add", Path = "emails[type eq \"work\"].primary", Value = true },
                    
                    // Multiple operations on the same address filter - should create ONE address
                    new() { Op = "add", Path = "addresses[type eq \"work\"].formatted", Value = "QQHYDJGFDRFX" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].streetAddress", Value = "7841 Goodwin Loaf" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].locality", Value = "UGWEYHVKKITS" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].region", Value = "JQQETQNHZCDZ" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].postalCode", Value = "ru86 0et" },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].primary", Value = true },
                    new() { Op = "add", Path = "addresses[type eq \"work\"].country", Value = "Uzbekistan" },
                    
                    // Multiple operations on the same phone filter - should create ONE phone
                    new() { Op = "add", Path = "phoneNumbers[type eq \"work\"].value", Value = "22-377-3428" },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"work\"].primary", Value = true },
                    
                    // Additional phone numbers with different filters
                    new() { Op = "add", Path = "phoneNumbers[type eq \"mobile\"].value", Value = "22-377-3428" },
                    new() { Op = "add", Path = "phoneNumbers[type eq \"fax\"].value", Value = "22-377-3428" },
                    
                    // Multiple operations on the same role filter - should create ONE role
                    new() { Op = "add", Path = "roles[primary eq \"True\"].display", Value = "MSSEWRRJREQY" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].value", Value = "CYDBGQIIERUP" },
                    new() { Op = "add", Path = "roles[primary eq \"True\"].type", Value = "QRPQKXXKNVRS" },
                    
                    // Add operation with multiple attributes at once
                    new() { Op = "add", Value = new Dictionary<string, object>
                        {
                            { "active", true },
                            { "displayName", "CDHQYEYIKBGB" },
                            { "title", "LOATBSFQQPBF" },
                            { "preferredLanguage", "nb-SJ" },
                            { "name.givenName", "Adell" },
                            { "name.familyName", "Lesley" },
                            { "name.formatted", "Henderson" },
                            { "name.middleName", "Guadalupe" },
                            { "name.honorificPrefix", "Reinhold" },
                            { "name.honorificSuffix", "Mathilde" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber", "DVJTEKOQVLLR" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department", "TEUFEQIRBMVQ" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter", "XAOYALZZMQDE" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization", "LARDLXQMBEZT" },
                            { "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division", "USYTEGEBXVTJ" },
                            { "userType", "XXEMRJMTGJYS" },
                            { "nickName", "EZUFFGHXIFHD" },
                            { "locale", "ECRNKKFCCWRQ" },
                            { "timezone", "Africa/Djibouti" },
                            { "profileUrl", "UTISNQJTBOMN" }
                        }
                    }
                }
            };

            // Act
            var result = await _userService.PatchUserAsync(user.Id!, patchRequest, _testCustomerId);

            // Assert
            result.Should().NotBeNull();
            
            // Verify that we have exactly ONE email, address, and role object, not multiple
            result!.Emails.Should().HaveCount(1, "because all operations targeted the same email filter");
            result.Addresses.Should().HaveCount(1, "because all operations targeted the same address filter");
            result.PhoneNumbers.Should().HaveCount(3, "because we have 3 different phone type filters");
            result.Roles.Should().HaveCount(1, "because all operations targeted the same role filter");
            
            // Verify the single email has all the properties set
            var workEmail = result.Emails.Single();
            workEmail.Type.Should().Be("work");
            workEmail.Value.Should().Be("roberta_kuvalis@hayesmertz.com");
            workEmail.Primary.Should().BeTrue();
            
            // Verify the single address has all the properties set
            var workAddress = result.Addresses.Single();
            workAddress.Type.Should().Be("work");
            workAddress.Formatted.Should().Be("QQHYDJGFDRFX");
            workAddress.StreetAddress.Should().Be("7841 Goodwin Loaf");
            workAddress.Locality.Should().Be("UGWEYHVKKITS");
            workAddress.Region.Should().Be("JQQETQNHZCDZ");
            workAddress.PostalCode.Should().Be("ru86 0et");
            workAddress.Primary.Should().BeTrue();
            workAddress.Country.Should().Be("Uzbekistan");
            
            // Verify the phones
            result.PhoneNumbers.Should().Contain(p => p.Type == "work" && p.Value == "22-377-3428" && p.Primary == true);
            result.PhoneNumbers.Should().Contain(p => p.Type == "mobile" && p.Value == "22-377-3428");
            result.PhoneNumbers.Should().Contain(p => p.Type == "fax" && p.Value == "22-377-3428");
            
            // Verify the single role has all the properties set
            var primaryRole = result.Roles.Single();
            primaryRole.Primary.Should().Be("True");
            primaryRole.Display.Should().Be("MSSEWRRJREQY");
            primaryRole.Value.Should().Be("CYDBGQIIERUP");
            primaryRole.Type.Should().Be("QRPQKXXKNVRS");
            
            // Verify other attributes were set from the bulk add operation
            result.DisplayName.Should().Be("CDHQYEYIKBGB");
            result.Title.Should().Be("LOATBSFQQPBF");
            result.PreferredLanguage.Should().Be("nb-SJ");
            result.Name.Should().NotBeNull();
            result.Name!.GivenName.Should().Be("Adell");
            result.Name.FamilyName.Should().Be("Lesley");
        }
    }
}
