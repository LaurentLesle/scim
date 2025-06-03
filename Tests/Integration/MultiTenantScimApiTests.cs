using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ScimServiceProvider.Data;
using ScimServiceProvider.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace ScimServiceProvider.Tests.Integration
{
    public class MultiTenantScimApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public MultiTenantScimApiTests(WebApplicationFactory<Program> factory)
        {
            // Create a customized web application factory
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot("/workspaces/scim");
                
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ScimDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add a fresh in-memory database for each test
                    services.AddDbContext<ScimDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestScimDatabase");
                    });

                    // Build service provider and initialize the database
                    var sp = services.BuildServiceProvider();
                    using (var scope = sp.CreateScope())
                    {
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<ScimDbContext>();
                        db.Database.EnsureCreated();

                        // Initialize with test data
                        InitializeTestData(db);
                    }
                });
            });

            _client = _factory.CreateClient();
        }

        private void InitializeTestData(ScimDbContext dbContext)
        {
            // Check if data already exists
            if (dbContext.Customers.Any())
            {
                // Data already exists, no need to initialize
                return;
            }
            
            // Create test customers
            var customer1 = new Customer
            {
                Id = "cust1",
                Name = "Customer One",
                TenantId = "tenant1",
                IsActive = true
            };

            var customer2 = new Customer
            {
                Id = "cust2",
                Name = "Customer Two",
                TenantId = "tenant2",
                IsActive = true
            };

            dbContext.Customers.AddRange(customer1, customer2);

            // Create test users for each customer
            var user1 = new ScimUser
            {
                Id = "user1",
                UserName = "user1@customer1.com",
                DisplayName = "User 1",
                CustomerId = "cust1",
                Active = true
            };

            var user2 = new ScimUser
            {
                Id = "user2",
                UserName = "user2@customer2.com",
                DisplayName = "User 2",
                CustomerId = "cust2",
                Active = true
            };

            dbContext.Users.AddRange(user1, user2);

            // Create test groups for each customer
            var group1 = new ScimGroup
            {
                Id = "group1",
                DisplayName = "Group 1",
                CustomerId = "cust1"
            };

            var group2 = new ScimGroup
            {
                Id = "group2",
                DisplayName = "Group 2",
                CustomerId = "cust2"
            };

            dbContext.Groups.AddRange(group1, group2);

            dbContext.SaveChanges();
        }

        // This method has been replaced by GetAuthTokenAsync

        // Add methods to get an auth token and set auth header
        private async Task<string> GetAuthTokenAsync(string? tenantId = null)
        {
            var authRequest = new
            {
                clientId = "scim_client",
                clientSecret = "scim_secret",
                grantType = "client_credentials",
                tenantId = tenantId ?? "tenant1" // Default to tenant1 if not specified
            };

            var authContent = new StringContent(
                JsonConvert.SerializeObject(authRequest),
                Encoding.UTF8,
                "application/json");

            var authResponse = await _client.PostAsync("/api/auth/token", authContent);
            if (!authResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get auth token: {authResponse.StatusCode}");
            }

            var authResult = JsonConvert.DeserializeObject<dynamic>(
                await authResponse.Content.ReadAsStringAsync());

            return authResult!.access_token;
        }

        private async Task SetAuthHeaderAsync()
        {
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        private HttpClient CreateClientWithTenantHeader(string tenantId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId);
            return client;
        }

        private async Task<HttpClient> CreateAuthenticatedClientWithTenantHeader(string tenantId)
        {
            var client = CreateClientWithTenantHeader(tenantId);
            var token = await GetAuthTokenAsync(tenantId); // Pass tenantId to get correct JWT token
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task GetUsers_WithTenantHeader_ReturnsOnlyTenantUsers()
        {
            // Arrange
            var client = await CreateAuthenticatedClientWithTenantHeader("tenant1");

            // Act
            var response = await client.GetAsync("/scim/v2/Users");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ScimListResponse<ScimUser>>(content);

            // Should have at least 1 user (original user1), but may have more if other tests created users
            Assert.True(result?.TotalResults >= 1);
            // All users should belong to customer1
            Assert.True(result?.Resources?.All(u => u.CustomerId == "cust1"));
        }

        [Fact]
        public async Task GetUsers_DifferentTenantHeader_ReturnsOtherTenantUsers()
        {
            // Arrange
            var client = await CreateAuthenticatedClientWithTenantHeader("tenant2");

            // Act
            var response = await client.GetAsync("/scim/v2/Users");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ScimListResponse<ScimUser>>(content);

            Assert.Equal(1, result?.TotalResults);
            Assert.Equal("user2", result?.Resources?.FirstOrDefault()?.Id);
            Assert.Equal("cust2", result?.Resources?.FirstOrDefault()?.CustomerId);
        }

        [Fact]
        public async Task CreateUser_WithTenantContext_AssignsCorrectCustomerId()
        {
            // Arrange
            var client = await CreateAuthenticatedClientWithTenantHeader("tenant1");

            var newUser = new ScimUser
            {
                UserName = "newuser@customer1.com",
                DisplayName = "New User",
                Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Active = true
            };

            // Log the request body
            var jsonBody = JsonConvert.SerializeObject(newUser);
            Console.WriteLine($"Request body: {jsonBody}");
            
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/scim+json");

            // Act
            var response = await client.PostAsync("/scim/v2/Users", content);

            // For debugging
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Status: {response.StatusCode}");
            Console.WriteLine($"Response Body: {responseBody}");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdUser = JsonConvert.DeserializeObject<ScimUser>(responseContent);

            Assert.Equal("newuser@customer1.com", createdUser?.UserName);
            Assert.Equal("cust1", createdUser?.CustomerId);
        }

        [Fact]
        public async Task GetUser_FromDifferentTenant_ReturnsNotFound()
        {
            // Arrange - User from tenant1 tries to access tenant2's user
            var client = await CreateAuthenticatedClientWithTenantHeader("tenant1");

            // Act
            var response = await client.GetAsync("/scim/v2/Users/user2");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
