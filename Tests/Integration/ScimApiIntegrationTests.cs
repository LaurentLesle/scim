using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScimServiceProvider.Data;
using ScimServiceProvider.Models;
using ScimServiceProvider.Tests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace ScimServiceProvider.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<ScimServiceProvider.Program>
    {
        // Define test tenant ID to match the one used in tests
        public const string TestTenantId = "test-tenant-id";
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot("/workspaces/scim");
            
            builder.ConfigureServices(services =>
            {
                // Remove the real database context
                services.RemoveAll(typeof(DbContextOptions<ScimDbContext>));
                services.RemoveAll(typeof(ScimDbContext));

                // Add in-memory database for testing
                services.AddDbContext<ScimDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
                
                // Create a scope to initialize the database with test data
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ScimDbContext>();
                
                // Ensure database is created
                db.Database.EnsureCreated();
                
                // Add test customer for tenant
                InitializeTestData(db);
            });
        }
        
    private void InitializeTestData(ScimDbContext dbContext)
    {
        // Create test customer if it doesn't exist
        if (!dbContext.Customers.Any(c => c.TenantId == TestTenantId))
        {
            var testCustomer = new Customer
            {
                Id = ScimTestDataGenerator.DefaultCustomerId, // Use the same ID as the test data generator
                Name = "Test Customer",
                TenantId = TestTenantId,
                IsActive = true,
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
                
                dbContext.Customers.Add(testCustomer);
                dbContext.SaveChanges();
            }
        }
    }

    public class ScimApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private string? _authToken;
        
    public ScimApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Add tenant ID header to all requests
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", CustomWebApplicationFactory.TestTenantId);
        }

        private async Task<string> GetAuthTokenAsync()
        {
            if (_authToken != null) return _authToken;

            var authRequest = new
            {
                clientId = "scim_client",
                clientSecret = "scim_secret",
                grantType = "client_credentials",
                tenantId = CustomWebApplicationFactory.TestTenantId
            };

            var authContent = new StringContent(
                JsonConvert.SerializeObject(authRequest),
                Encoding.UTF8,
                "application/json");

            var authResponse = await _client.PostAsync("/api/auth/token", authContent);
            authResponse.Should().BeSuccessful();

            var authResult = JsonConvert.DeserializeObject<dynamic>(
                await authResponse.Content.ReadAsStringAsync());

            _authToken = authResult!.access_token;
            return _authToken;
        }

        private async Task SetAuthHeaderAsync()
        {
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task ServiceProviderConfig_ReturnsCorrectConfiguration()
        {
            // Act
            var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/scim+json");

            var content = await response.Content.ReadAsStringAsync();
            var jObj = Newtonsoft.Json.Linq.JObject.Parse(content);
            ((bool)jObj["patch"]!["supported"]!).Should().BeTrue();
            ((bool)jObj["bulk"]!["supported"]!).Should().BeFalse();
            ((bool)jObj["filter"]!["supported"]!).Should().BeTrue();
        }

        [Fact]
        public async Task ResourceTypes_ReturnsUserAndGroupTypes()
        {
            // Act
            var response = await _client.GetAsync("/scim/v2/ResourceTypes");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var resourceTypes = JsonConvert.DeserializeObject<dynamic[]>(content);

            resourceTypes!.Should().HaveCount(2);
        }

        [Fact]
        public async Task Schemas_ReturnsUserAndGroupSchemas()
        {
            // Act
            var response = await _client.GetAsync("/Schemas");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var listResponse = JsonConvert.DeserializeObject<JObject>(content);

            listResponse!["resources"]!.Should().HaveCount(2);
        }

        [Fact]
        public async Task ResourceTypes_RootEndpoint_ReturnsUserAndGroupTypes()
        {
            // Act
            var response = await _client.GetAsync("/ResourceTypes");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var resourceTypes = JsonConvert.DeserializeObject<dynamic[]>(content);

            resourceTypes!.Should().HaveCount(2);
        }

        [Fact]
        public async Task Users_FullCrudWorkflow_WorksCorrectly()
        {
            await SetAuthHeaderAsync();

            // 1. Create a user
            var newUser = ScimTestDataGenerator.GenerateUser();
            newUser.Id = null;
            
            var createContent = new StringContent(
                JsonConvert.SerializeObject(newUser),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Users", createContent);
            if (createResponse.StatusCode != HttpStatusCode.Created)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                throw new Exception($"User creation failed: {createResponse.StatusCode} - {errorContent}");
            }
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdUserJson = await createResponse.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);
            createdUser!.Id.Should().NotBeNullOrEmpty();
            createdUser.UserName.Should().Be(newUser.UserName);

            // 2. Get the user
            var getUserResponse = await _client.GetAsync($"/scim/v2/Users/{createdUser.Id}");
            getUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var getUserJson = await getUserResponse.Content.ReadAsStringAsync();
            var retrievedUser = JsonConvert.DeserializeObject<ScimUser>(getUserJson);
            retrievedUser!.Id.Should().Be(createdUser.Id);

            // 3. Update the user
            createdUser.DisplayName = "Updated Display Name";
            var updateContent = new StringContent(
                JsonConvert.SerializeObject(createdUser),
                Encoding.UTF8,
                "application/scim+json");

            var updateResponse = await _client.PutAsync($"/scim/v2/Users/{createdUser.Id}", updateContent);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedUserJson = await updateResponse.Content.ReadAsStringAsync();
            var updatedUser = JsonConvert.DeserializeObject<ScimUser>(updatedUserJson);
            updatedUser!.DisplayName.Should().Be("Updated Display Name");

            // 4. Patch the user
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() { Op = "replace", Path = "active", Value = false }
                }
            };

            var patchContent = new StringContent(
                JsonConvert.SerializeObject(patchRequest),
                Encoding.UTF8,
                "application/scim+json");

            var patchResponse = await _client.PatchAsync($"/scim/v2/Users/{createdUser.Id}", patchContent);
            patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var patchedUserJson = await patchResponse.Content.ReadAsStringAsync();
            var patchedUser = JsonConvert.DeserializeObject<ScimUser>(patchedUserJson);
            patchedUser!.Active.Should().BeFalse();

            // 5. List users
            var listResponse = await _client.GetAsync("/scim/v2/Users");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var listJson = await listResponse.Content.ReadAsStringAsync();
            var userList = JsonConvert.DeserializeObject<ScimListResponse<ScimUser>>(listJson);
            userList!.TotalResults.Should().BeGreaterThan(0);
            userList.Resources.Should().Contain(u => u.Id == createdUser.Id);

            // 6. Delete the user
            var deleteResponse = await _client.DeleteAsync($"/scim/v2/Users/{createdUser.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 7. Verify user is deleted
            var getDeletedResponse = await _client.GetAsync($"/scim/v2/Users/{createdUser.Id}");
            getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Groups_FullCrudWorkflow_WorksCorrectly()
        {
            await SetAuthHeaderAsync();

            // First create some users to be group members
            var testUsers = new List<ScimUser>();
            for (int i = 0; i < 2; i++)
            {
                var user = ScimTestDataGenerator.GenerateUser();
                user.Id = null;

                var createUserContent = new StringContent(
                    JsonConvert.SerializeObject(user),
                    Encoding.UTF8,
                    "application/scim+json");

                var createUserResponse = await _client.PostAsync("/scim/v2/Users", createUserContent);
                var createdUserJson = await createUserResponse.Content.ReadAsStringAsync();
                var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);
                testUsers.Add(createdUser!);
            }

            // 1. Create a group
            var newGroup = ScimTestDataGenerator.GenerateGroup(members: testUsers);
            newGroup.Id = null;

            var createContent = new StringContent(
                JsonConvert.SerializeObject(newGroup),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Groups", createContent);
            if (createResponse.StatusCode != HttpStatusCode.Created)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                throw new Exception($"Group creation failed: {createResponse.StatusCode} - {errorContent}");
            }
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdGroupJson = await createResponse.Content.ReadAsStringAsync();
            var createdGroup = JsonConvert.DeserializeObject<ScimGroup>(createdGroupJson);
            createdGroup!.Id.Should().NotBeNullOrEmpty();
            createdGroup.DisplayName.Should().Be(newGroup.DisplayName);
            createdGroup.Members.Should().HaveCount(2);

            // 2. Get the group
            var getGroupResponse = await _client.GetAsync($"/scim/v2/Groups/{createdGroup.Id}");
            getGroupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 3. Update the group
            createdGroup.DisplayName = "Updated Group Name";
            var updateContent = new StringContent(
                JsonConvert.SerializeObject(createdGroup),
                Encoding.UTF8,
                "application/scim+json");

            var updateResponse = await _client.PutAsync($"/scim/v2/Groups/{createdGroup.Id}", updateContent);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedGroupJson = await updateResponse.Content.ReadAsStringAsync();
            var updatedGroup = JsonConvert.DeserializeObject<ScimGroup>(updatedGroupJson);
            updatedGroup!.DisplayName.Should().Be("Updated Group Name");

            // 4. List groups
            var listResponse = await _client.GetAsync("/scim/v2/Groups");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var listJson = await listResponse.Content.ReadAsStringAsync();
            var groupList = JsonConvert.DeserializeObject<ScimListResponse<ScimGroup>>(listJson);
            groupList!.TotalResults.Should().BeGreaterThan(0);
            groupList.Resources.Should().Contain(g => g.Id == createdGroup.Id);

            // 5. Delete the group
            var deleteResponse = await _client.DeleteAsync($"/scim/v2/Groups/{createdGroup.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 6. Verify group is deleted
            var getDeletedResponse = await _client.GetAsync($"/scim/v2/Groups/{createdGroup.Id}");
            getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Users_WithPagination_ReturnsCorrectResults()
        {
            await SetAuthHeaderAsync();

            // Create multiple users
            var createdUsers = new List<ScimUser>();
            for (int i = 0; i < 15; i++)
            {
                var user = ScimTestDataGenerator.GenerateUser();
                user.Id = null;

                var createContent = new StringContent(
                    JsonConvert.SerializeObject(user),
                    Encoding.UTF8,
                    "application/scim+json");

                var createResponse = await _client.PostAsync("/scim/v2/Users", createContent);
                var createdUserJson = await createResponse.Content.ReadAsStringAsync();
                var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);
                createdUsers.Add(createdUser!);
            }

            // Test pagination
            var paginatedResponse = await _client.GetAsync("/scim/v2/Users?startIndex=6&count=5");
            paginatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var paginatedJson = await paginatedResponse.Content.ReadAsStringAsync();
            var paginatedResult = JsonConvert.DeserializeObject<ScimListResponse<ScimUser>>(paginatedJson);

            paginatedResult!.StartIndex.Should().Be(6);
            paginatedResult.ItemsPerPage.Should().Be(5);
            paginatedResult.TotalResults.Should().BeGreaterThanOrEqualTo(15);
        }

        [Fact]
        public async Task Users_WithFilter_ReturnsFilteredResults()
        {
            await SetAuthHeaderAsync();

            // Create a user with specific username
            var testUser = ScimTestDataGenerator.GenerateUser(userName: "filter.test@example.com");
            testUser.Id = null;

            var createContent = new StringContent(
                JsonConvert.SerializeObject(testUser),
                Encoding.UTF8,
                "application/scim+json");

            await _client.PostAsync("/scim/v2/Users", createContent);

            // Test filtering
            var filterResponse = await _client.GetAsync("/scim/v2/Users?filter=userName%20eq%20%22filter.test@example.com%22");
            filterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var filterJson = await filterResponse.Content.ReadAsStringAsync();
            var filterResult = JsonConvert.DeserializeObject<ScimListResponse<ScimUser>>(filterJson);

            filterResult!.TotalResults.Should().Be(1);
            filterResult.Resources.First().UserName.Should().Be("filter.test@example.com");
        }

        [Fact]
        public async Task UnauthorizedRequest_ReturnsUnauthorized()
        {
            // Don't set auth header
            var response = await _client.GetAsync("/scim/v2/Users");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task InvalidResource_ReturnsNotFound()
        {
            await SetAuthHeaderAsync();

            var response = await _client.GetAsync("/scim/v2/Users/invalid-id");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var errorJson = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<ScimError>(errorJson);
            error!.Status.Should().Be(404);
        }

        [Fact]
        public async Task PatchUser_WithManagerObjectOperation_ReturnsCorrectManagerData()
        {
            await SetAuthHeaderAsync();

            // 1. Create a user
            var user = ScimTestDataGenerator.GenerateUser();
            var content = new StringContent(
                JsonConvert.SerializeObject(user),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Users", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdUserJson = await createResponse.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);

            // 2. Patch with Manager object
            var managerJson = """{"value":"manager-api-test-123","$ref":"../Users/manager-api-test-123","displayName":"API Test Manager"}""";

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = managerJson
                    }
                }
            };

            var patchContent = new StringContent(
                JsonConvert.SerializeObject(patchRequest),
                Encoding.UTF8,
                "application/scim+json");

            var patchResponse = await _client.PatchAsync($"/scim/v2/Users/{createdUser!.Id}", patchContent);
            patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var patchedUserJson = await patchResponse.Content.ReadAsStringAsync();
            var patchedUser = JsonConvert.DeserializeObject<ScimUser>(patchedUserJson);

            // Assert Manager object properties
            patchedUser!.EnterpriseUser.Should().NotBeNull();
            patchedUser.EnterpriseUser!.Manager.Should().NotBeNull();
            patchedUser.EnterpriseUser!.Manager!.Value.Should().Be("manager-api-test-123");
            patchedUser.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/manager-api-test-123");
            patchedUser.EnterpriseUser!.Manager!.DisplayName.Should().Be("API Test Manager");

            // Verify schemas include enterprise extension
            patchedUser.Schemas.Should().Contain("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
        }

        [Fact]
        public async Task PatchUser_WithLegacyManagerString_HandlesBackwardCompatibility()
        {
            await SetAuthHeaderAsync();

            // 1. Create a user
            var user = ScimTestDataGenerator.GenerateUser();
            var content = new StringContent(
                JsonConvert.SerializeObject(user),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Users", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdUserJson = await createResponse.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);

            // 2. Patch with legacy string manager value
            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "add",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = "legacy-manager-string-123"
                    }
                }
            };

            var patchContent = new StringContent(
                JsonConvert.SerializeObject(patchRequest),
                Encoding.UTF8,
                "application/scim+json");

            var patchResponse = await _client.PatchAsync($"/scim/v2/Users/{createdUser!.Id}", patchContent);
            patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var patchedUserJson = await patchResponse.Content.ReadAsStringAsync();
            var patchedUser = JsonConvert.DeserializeObject<ScimUser>(patchedUserJson);

            // Assert RFC 7643 compliance - $ref should be populated for manager references
            patchedUser!.EnterpriseUser.Should().NotBeNull();
            patchedUser.EnterpriseUser!.Manager.Should().NotBeNull();
            patchedUser.EnterpriseUser!.Manager!.Value.Should().Be("legacy-manager-string-123");
            patchedUser.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/legacy-manager-string-123");
            patchedUser.EnterpriseUser!.Manager!.DisplayName.Should().BeNull();
        }

        [Fact]
        public async Task PatchUser_WithReplaceManagerOperation_ReplacesManagerCorrectly()
        {
            await SetAuthHeaderAsync();

            // 1. Create a user with initial manager
            var user = ScimTestDataGenerator.GenerateUser();
            user.EnterpriseUser = new EnterpriseUser 
            { 
                Manager = new Manager 
                { 
                    Value = "initial-manager-123",
                    DisplayName = "Initial Manager"
                }
            };
            user.Schemas.Add("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");

            var content = new StringContent(
                JsonConvert.SerializeObject(user),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Users", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdUserJson = await createResponse.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);

            // 2. Replace with new manager
            var newManagerJson = """{"value":"new-manager-456","$ref":"../Users/new-manager-456","displayName":"New Manager"}""";

            var patchRequest = new ScimPatchRequest
            {
                Schemas = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                Operations = new List<ScimPatchOperation>
                {
                    new() {
                        Op = "replace",
                        Path = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager",
                        Value = newManagerJson
                    }
                }
            };

            var patchContent = new StringContent(
                JsonConvert.SerializeObject(patchRequest),
                Encoding.UTF8,
                "application/scim+json");

            var patchResponse = await _client.PatchAsync($"/scim/v2/Users/{createdUser!.Id}", patchContent);
            patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var patchedUserJson = await patchResponse.Content.ReadAsStringAsync();
            var patchedUser = JsonConvert.DeserializeObject<ScimUser>(patchedUserJson);

            // Assert manager was replaced
            patchedUser!.EnterpriseUser.Should().NotBeNull();
            patchedUser.EnterpriseUser!.Manager.Should().NotBeNull();
            patchedUser.EnterpriseUser!.Manager!.Value.Should().Be("new-manager-456");
            patchedUser.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/new-manager-456");
            patchedUser.EnterpriseUser!.Manager!.DisplayName.Should().Be("New Manager");
        }

        [Fact]
        public async Task PatchUser_WithRemoveManagerOperation_RemovesManagerCorrectly()
        {
            await SetAuthHeaderAsync();

            // 1. Create a user with manager
            var user = ScimTestDataGenerator.GenerateUser();
            user.EnterpriseUser = new EnterpriseUser 
            { 
                Manager = new Manager 
                { 
                    Value = "manager-to-remove-123",
                    DisplayName = "Manager To Remove"
                }
            };
            user.Schemas.Add("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");

            var content = new StringContent(
                JsonConvert.SerializeObject(user),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Users", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdUserJson = await createResponse.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);

            // 2. Remove manager
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

            var patchContent = new StringContent(
                JsonConvert.SerializeObject(patchRequest),
                Encoding.UTF8,
                "application/scim+json");

            var patchResponse = await _client.PatchAsync($"/scim/v2/Users/{createdUser!.Id}", patchContent);
            patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var patchedUserJson = await patchResponse.Content.ReadAsStringAsync();
            var patchedUser = JsonConvert.DeserializeObject<ScimUser>(patchedUserJson);

            // Assert manager was removed
            patchedUser!.EnterpriseUser.Should().NotBeNull();
            patchedUser.EnterpriseUser!.Manager.Should().BeNull();
        }

        [Fact]
        public async Task GetUser_WithManagerObject_ReturnsCorrectManagerStructure()
        {
            await SetAuthHeaderAsync();

            // 1. Create a user with manager
            var user = ScimTestDataGenerator.GenerateUser();
            user.EnterpriseUser = new EnterpriseUser 
            { 
                Manager = new Manager 
                { 
                    Value = "get-test-manager-123",
                    Ref = "../Users/get-test-manager-123",
                    DisplayName = "Get Test Manager"
                }
            };
            user.Schemas.Add("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");

            var content = new StringContent(
                JsonConvert.SerializeObject(user),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Users", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdUserJson = await createResponse.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);

            // 2. Get the user to verify manager structure persists
            var getResponse = await _client.GetAsync($"/scim/v2/Users/{createdUser!.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var getUserJson = await getResponse.Content.ReadAsStringAsync();
            var retrievedUser = JsonConvert.DeserializeObject<ScimUser>(getUserJson);

            // Assert manager object structure is preserved
            retrievedUser!.EnterpriseUser.Should().NotBeNull();
            retrievedUser.EnterpriseUser!.Manager.Should().NotBeNull();
            retrievedUser.EnterpriseUser!.Manager!.Value.Should().Be("get-test-manager-123");
            retrievedUser.EnterpriseUser!.Manager!.Ref.Should().Be("../Users/get-test-manager-123");
            retrievedUser.EnterpriseUser!.Manager!.DisplayName.Should().Be("Get Test Manager");

            // Verify schemas include enterprise extension
            retrievedUser.Schemas.Should().Contain("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
        }

        [Fact]
        public async Task GetUsers_WithAttributesParameter_ReturnsFilteredAttributes()
        {
            await SetAuthHeaderAsync();
            
            // Arrange - Create a test user first with a unique username to avoid conflicts
            var uniqueUserName = $"test-attributes-{Guid.NewGuid()}@example.com";
            var newUser = ScimTestDataGenerator.GenerateUser(userName: uniqueUserName);
            newUser.Id = null;
            
            var createContent = new StringContent(
                JsonConvert.SerializeObject(newUser),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Users", createContent);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdUserJson = await createResponse.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);

            // Act - Use a filter to get only our specific user, testing both attributes and filtering
            var response = await _client.GetAsync($"/scim/v2/Users?filter=userName eq \"{uniqueUserName}\"&attributes=userName,displayName");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var listResponse = JsonConvert.DeserializeObject<ScimListResponse<ScimUser>>(content);
            
            listResponse.Should().NotBeNull();
            listResponse!.TotalResults.Should().Be(1, "exactly one user should match our unique filter");
            listResponse.Resources.Should().NotBeNull();
            listResponse.Resources!.Should().HaveCount(1);
            
            var user = listResponse.Resources![0];
            
            // Should have the requested attributes
            user.UserName.Should().Be(uniqueUserName);
            user.DisplayName.Should().NotBeNullOrEmpty();
            
            // Should always have core attributes regardless of attributes parameter
            user.Id.Should().NotBeNullOrEmpty();
            user.Schemas.Should().NotBeNull();
            user.Meta.Should().NotBeNull();
        }

        [Fact]
        public async Task PostUsersSearch_WithValidSearchRequest_ReturnsResults()
        {
            await SetAuthHeaderAsync();
            
            // Arrange - Create a test user first
            var newUser = ScimTestDataGenerator.GenerateUser();
            newUser.Id = null;
            
            var createContent = new StringContent(
                JsonConvert.SerializeObject(newUser),
                Encoding.UTF8,
                "application/scim+json");

            var createResponse = await _client.PostAsync("/scim/v2/Users", createContent);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdUserJson = await createResponse.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(createdUserJson);

            var searchRequest = new ScimSearchRequest
            {
                Filter = $"userName eq \"{createdUser!.UserName}\"",
                Attributes = "userName,displayName",
                StartIndex = 1,
                Count = 10
            };

            var requestJson = JsonConvert.SerializeObject(searchRequest);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/scim+json");

            // Act
            var response = await _client.PostAsync("/scim/v2/Users/.search", requestContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var listResponse = JsonConvert.DeserializeObject<ScimListResponse<ScimUser>>(content);
            
            listResponse.Should().NotBeNull();
            listResponse!.TotalResults.Should().Be(1);
            var user = listResponse.Resources!.First();
            user.UserName.Should().Be(createdUser.UserName);
        }
    }
}
