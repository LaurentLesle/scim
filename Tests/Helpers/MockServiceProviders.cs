using Moq;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;

namespace ScimServiceProvider.Tests.Helpers
{
    /// <summary>
    /// Provides mock implementations of SCIM services for testing
    /// </summary>
    public static class MockServiceProviders
    {
        /// <summary>
        /// Creates a mock user service with predefined test data
        /// </summary>
        public static Mock<IUserService> CreateMockUserService(List<ScimUser>? testUsers = null)
        {
            var mockService = new Mock<IUserService>();
            var users = testUsers ?? ScimTestDataGenerator.GenerateUsers(5);

            // Setup GetUserAsync
            mockService.Setup(s => s.GetUserAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => users.FirstOrDefault(u => u.Id == id));

            // Setup GetUserByUsernameAsync
            mockService.Setup(s => s.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) => users.FirstOrDefault(u => u.UserName == username));

            // Setup GetUsersAsync
            mockService.Setup(s => s.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
                .ReturnsAsync((int startIndex, int count, string? filter) =>
                {
                    var filteredUsers = users.AsQueryable();

                    // Apply simple filter if provided
                    if (!string.IsNullOrEmpty(filter))
                    {
                        // Simple userName filter for testing
                        if (filter.Contains("userName"))
                        {
                            var userName = ExtractFilterValue(filter);
                            if (!string.IsNullOrEmpty(userName))
                            {
                                filteredUsers = filteredUsers.Where(u => u.UserName == userName);
                            }
                        }
                    }

                    return ScimTestDataGenerator.CreateListResponse(
                        filteredUsers.ToList(), 
                        startIndex, 
                        count);
                });

            // Setup CreateUserAsync
            mockService.Setup(s => s.CreateUserAsync(It.IsAny<ScimUser>()))
                .ReturnsAsync((ScimUser user) =>
                {
                    user.Id = Guid.NewGuid().ToString();
                    user.Meta = new ScimMeta
                    {
                        ResourceType = "User",
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Location = $"/scim/v2/Users/{user.Id}",
                        Version = Guid.NewGuid().ToString("N")[..8]
                    };
                    users.Add(user);
                    return user;
                });

            // Setup UpdateUserAsync
            mockService.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<ScimUser>()))
                .ReturnsAsync((string id, ScimUser updatedUser) =>
                {
                    var existingUser = users.FirstOrDefault(u => u.Id == id);
                    if (existingUser == null) return null;

                    // Update properties
                    existingUser.UserName = updatedUser.UserName;
                    existingUser.DisplayName = updatedUser.DisplayName;
                    existingUser.Active = updatedUser.Active;
                    existingUser.Name = updatedUser.Name;
                    existingUser.Emails = updatedUser.Emails;
                    existingUser.PhoneNumbers = updatedUser.PhoneNumbers;
                    existingUser.Addresses = updatedUser.Addresses;
                    
                    if (existingUser.Meta != null)
                    {
                        existingUser.Meta.LastModified = DateTime.UtcNow;
                    }

                    return existingUser;
                });

            // Setup PatchUserAsync
            mockService.Setup(s => s.PatchUserAsync(It.IsAny<string>(), It.IsAny<ScimPatchRequest>()))
                .ReturnsAsync((string id, ScimPatchRequest patchRequest) =>
                {
                    var user = users.FirstOrDefault(u => u.Id == id);
                    if (user == null) return null;

                    // Apply patch operations
                    foreach (var operation in patchRequest.Operations)
                    {
                        if (operation.Path == "active" && operation.Value is bool activeValue)
                        {
                            user.Active = activeValue;
                        }
                        else if (operation.Path == "displayName" && operation.Value is string displayName)
                        {
                            user.DisplayName = displayName;
                        }
                    }

                    if (user.Meta != null)
                    {
                        user.Meta.LastModified = DateTime.UtcNow;
                    }

                    return user;
                });

            // Setup DeleteUserAsync
            mockService.Setup(s => s.DeleteUserAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) =>
                {
                    var user = users.FirstOrDefault(u => u.Id == id);
                    if (user == null) return false;
                    
                    users.Remove(user);
                    return true;
                });

            return mockService;
        }

        /// <summary>
        /// Creates a mock group service with predefined test data
        /// </summary>
        public static Mock<IGroupService> CreateMockGroupService(List<ScimGroup>? testGroups = null, List<ScimUser>? testUsers = null)
        {
            var mockService = new Mock<IGroupService>();
            var groups = testGroups ?? ScimTestDataGenerator.GenerateGroups(3, testUsers);

            // Setup GetGroupAsync
            mockService.Setup(s => s.GetGroupAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => groups.FirstOrDefault(g => g.Id == id));

            // Setup GetGroupsAsync
            mockService.Setup(s => s.GetGroupsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
                .ReturnsAsync((int startIndex, int count, string? filter) =>
                {
                    var filteredGroups = groups.AsQueryable();

                    // Apply simple filter if provided
                    if (!string.IsNullOrEmpty(filter))
                    {
                        if (filter.Contains("displayName"))
                        {
                            var displayName = ExtractFilterValue(filter);
                            if (!string.IsNullOrEmpty(displayName))
                            {
                                filteredGroups = filteredGroups.Where(g => g.DisplayName == displayName);
                            }
                        }
                    }

                    return ScimTestDataGenerator.CreateListResponse(
                        filteredGroups.ToList(), 
                        startIndex, 
                        count);
                });

            // Setup CreateGroupAsync
            mockService.Setup(s => s.CreateGroupAsync(It.IsAny<ScimGroup>()))
                .ReturnsAsync((ScimGroup group) =>
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
                    groups.Add(group);
                    return group;
                });

            // Setup UpdateGroupAsync
            mockService.Setup(s => s.UpdateGroupAsync(It.IsAny<string>(), It.IsAny<ScimGroup>()))
                .ReturnsAsync((string id, ScimGroup updatedGroup) =>
                {
                    var existingGroup = groups.FirstOrDefault(g => g.Id == id);
                    if (existingGroup == null) return null;

                    existingGroup.DisplayName = updatedGroup.DisplayName;
                    existingGroup.Members = updatedGroup.Members;
                    
                    if (existingGroup.Meta != null)
                    {
                        existingGroup.Meta.LastModified = DateTime.UtcNow;
                    }

                    return existingGroup;
                });

            // Setup PatchGroupAsync
            mockService.Setup(s => s.PatchGroupAsync(It.IsAny<string>(), It.IsAny<ScimPatchRequest>()))
                .ReturnsAsync((string id, ScimPatchRequest patchRequest) =>
                {
                    var group = groups.FirstOrDefault(g => g.Id == id);
                    if (group == null) return null;

                    // Apply patch operations
                    foreach (var operation in patchRequest.Operations)
                    {
                        if (operation.Path == "displayName" && operation.Value is string displayName)
                        {
                            group.DisplayName = displayName;
                        }
                    }

                    if (group.Meta != null)
                    {
                        group.Meta.LastModified = DateTime.UtcNow;
                    }

                    return group;
                });

            // Setup DeleteGroupAsync
            mockService.Setup(s => s.DeleteGroupAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) =>
                {
                    var group = groups.FirstOrDefault(g => g.Id == id);
                    if (group == null) return false;
                    
                    groups.Remove(group);
                    return true;
                });

            return mockService;
        }

        /// <summary>
        /// Extracts value from a simple SCIM filter expression
        /// </summary>
        private static string? ExtractFilterValue(string filter)
        {
            // Simple extraction for testing - handles patterns like 'userName eq "value"'
            var parts = filter.Split(new[] { " eq " }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                return parts[1].Trim('"', '\'');
            }
            return null;
        }
    }
}
