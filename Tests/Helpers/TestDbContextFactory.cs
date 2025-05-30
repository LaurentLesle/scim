using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Data;
using ScimServiceProvider.Models;

namespace ScimServiceProvider.Tests.Helpers
{
    /// <summary>
    /// Helper class for creating test database contexts with in-memory databases
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates a new in-memory database context for testing
        /// </summary>
        public static ScimDbContext CreateInMemoryContext(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<ScimDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options;

            return new ScimDbContext(options);
        }

        /// <summary>
        /// Seeds the database with test data
        /// </summary>
        public static async Task SeedTestDataAsync(ScimDbContext context, int userCount = 5, int groupCount = 2)
        {
            // Generate test users
            var users = ScimTestDataGenerator.GenerateUsers(userCount, mixActiveStatus: true);
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // Generate test groups with some of the users as members
            var groups = ScimTestDataGenerator.GenerateGroups(groupCount, users.Take(3).ToList());
            context.Groups.AddRange(groups);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a context with specific test data
        /// </summary>
        public static async Task<ScimDbContext> CreateContextWithDataAsync(
            List<ScimUser>? users = null, 
            List<ScimGroup>? groups = null)
        {
            var context = CreateInMemoryContext();

            if (users != null)
            {
                context.Users.AddRange(users);
            }

            if (groups != null)
            {
                context.Groups.AddRange(groups);
            }

            await context.SaveChangesAsync();
            return context;
        }

        /// <summary>
        /// Creates a context pre-populated with a specific user for testing
        /// </summary>
        public static async Task<(ScimDbContext context, ScimUser user)> CreateContextWithUserAsync(
            string? userId = null, 
            string? userName = null, 
            bool active = true)
        {
            var context = CreateInMemoryContext();
            var user = ScimTestDataGenerator.GenerateUser(userId, userName, active);
            
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return (context, user);
        }

        /// <summary>
        /// Creates a context pre-populated with a specific group for testing
        /// </summary>
        public static async Task<(ScimDbContext context, ScimGroup group)> CreateContextWithGroupAsync(
            string? groupId = null, 
            string? displayName = null, 
            List<ScimUser>? members = null)
        {
            var context = CreateInMemoryContext();
            
            // Add member users first if provided
            if (members != null)
            {
                context.Users.AddRange(members);
            }

            var group = ScimTestDataGenerator.GenerateGroup(groupId, displayName, members);
            context.Groups.Add(group);
            await context.SaveChangesAsync();

            return (context, group);
        }
    }
}
