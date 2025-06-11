using Moq;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;
using Newtonsoft.Json.Linq;

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
            mockService.Setup(s => s.GetUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string id, string customerId) => users.FirstOrDefault(u => u.Id == id && u.CustomerId == customerId));

            // Setup GetUserByUsernameAsync
            mockService.Setup(s => s.GetUserByUsernameAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string username, string customerId) => users.FirstOrDefault(u => u.UserName == username && u.CustomerId == customerId));

            // Setup GetUsersAsync
            mockService.Setup(s => s.GetUsersAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync((string customerId, int startIndex, int count, string? filter, string? attributes, string? excludedAttributes, string? sortBy, string? sortOrder) =>
                {
                    var filteredUsers = users.Where(u => u.CustomerId == customerId).AsQueryable();

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
            mockService.Setup(s => s.CreateUserAsync(It.IsAny<ScimUser>(), It.IsAny<string>()))
                .ReturnsAsync((ScimUser user, string customerId) =>
                {
                    user.Id = Guid.NewGuid().ToString();
                    user.CustomerId = customerId;
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
            mockService.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<ScimUser>(), It.IsAny<string>()))
                .ReturnsAsync((string id, ScimUser updatedUser, string customerId) =>
                {
                    var existingUser = users.FirstOrDefault(u => u.Id == id && u.CustomerId == customerId);
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
            mockService.Setup(s => s.PatchUserAsync(It.IsAny<string>(), It.IsAny<ScimPatchRequest>(), It.IsAny<string>()))
                .ReturnsAsync((string id, ScimPatchRequest patchRequest, string customerId) =>
                {
                    var user = users.FirstOrDefault(u => u.Id == id && u.CustomerId == customerId);
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
            mockService.Setup(s => s.DeleteUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string id, string customerId) =>
                {
                    var user = users.FirstOrDefault(u => u.Id == id && u.CustomerId == customerId);
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
            mockService.Setup(s => s.GetGroupAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string id, string customerId) => groups.FirstOrDefault(g => g.Id == id && g.CustomerId == customerId));

            // Setup GetGroupsAsync
            mockService.Setup(s => s.GetGroupsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync((string customerId, int startIndex, int count, string? filter, string? attributes, string? excludedAttributes, string? sortBy, string? sortOrder) =>
                {
                    var filteredGroups = groups.Where(g => g.CustomerId == customerId).AsQueryable();

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
            mockService.Setup(s => s.CreateGroupAsync(It.IsAny<ScimGroup>(), It.IsAny<string>()))
                .ReturnsAsync((ScimGroup group, string customerId) =>
                {
                    group.Id = Guid.NewGuid().ToString();
                    group.CustomerId = customerId;
                    group.Meta = new ScimMeta
                    {
                        ResourceType = "Group",
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Location = $"/scim/v2/Groups/{group.Id}",
                        Version = Guid.NewGuid().ToString("N")[..8]
                    };
                    
                    // Populate $ref for members after patching (always override to ensure consistency)
                    if (group.Members != null)
                    {
                        foreach (var member in group.Members)
                        {
                            member.Ref = $"https://localhost/scim/v2/Users/{member.Value}";
                        }
                    }
                    
                    groups.Add(group);
                    return group;
                });

            // Setup UpdateGroupAsync
            mockService.Setup(s => s.UpdateGroupAsync(It.IsAny<string>(), It.IsAny<ScimGroup>(), It.IsAny<string>()))
                .ReturnsAsync((string id, ScimGroup updatedGroup, string customerId) =>
                {
                    var existingGroup = groups.FirstOrDefault(g => g.Id == id && g.CustomerId == customerId);
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
            mockService.Setup(s => s.PatchGroupAsync(It.IsAny<string>(), It.IsAny<ScimPatchRequest>(), It.IsAny<string>()))
                .ReturnsAsync((string id, ScimPatchRequest patchRequest, string customerId) =>
                {
                    var group = groups.FirstOrDefault(g => g.Id == id && g.CustomerId == customerId);
                    if (group == null) return null;

                    // Apply patch operations (simplified version of real service logic)
                    foreach (var operation in patchRequest.Operations)
                    {
                        switch (operation.Op.ToLower())
                        {
                            case "replace":
                                if (string.IsNullOrEmpty(operation.Path))
                                {
                                    // Full resource replacement
                                    if (operation.Value is Newtonsoft.Json.Linq.JObject jObj)
                                    {
                                        var replacement = jObj.ToObject<ScimGroup>();
                                        if (replacement != null)
                                        {
                                            group.DisplayName = replacement.DisplayName;
                                            group.ExternalId = replacement.ExternalId;
                                            group.Members = replacement.Members;
                                        }
                                    }
                                    else
                                    {
                                        // Handle anonymous objects or other types
                                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(operation.Value);
                                        var replacement = Newtonsoft.Json.JsonConvert.DeserializeObject<ScimGroup>(json);
                                        if (replacement != null)
                                        {
                                            group.DisplayName = replacement.DisplayName;
                                            group.ExternalId = replacement.ExternalId;
                                            group.Members = replacement.Members;
                                        }
                                    }
                                }
                                else if (operation.Path == "displayName" && operation.Value is string displayName)
                                {
                                    group.DisplayName = displayName;
                                }
                                else if (operation.Path == "externalId" && operation.Value is string externalId)
                                {
                                    group.ExternalId = externalId;
                                }
                                else if (operation.Path == "members")
                                {
                                    if (operation.Value is System.Collections.IEnumerable enumerable && !(operation.Value is string))
                                    {
                                        var membersList = new List<GroupMember>();
                                        foreach (var item in enumerable)
                                        {
                                            GroupMember? member = null;
                                            if (item is GroupMember existingMember)
                                            {
                                                member = existingMember;
                                            }
                                            else
                                            {
                                                // Handle anonymous objects or JObjects
                                                var json = Newtonsoft.Json.JsonConvert.SerializeObject(item);
                                                member = Newtonsoft.Json.JsonConvert.DeserializeObject<GroupMember>(json);
                                            }
                                            if (member != null)
                                            {
                                                membersList.Add(member);
                                            }
                                        }
                                        group.Members = membersList;
                                    }
                                    else if (operation.Value is Newtonsoft.Json.Linq.JArray membersArray)
                                    {
                                        group.Members = membersArray.ToObject<List<GroupMember>>();
                                    }
                                }
                                break;

                            case "add":
                                if (operation.Path == "members")
                                {
                                    group.Members ??= new List<GroupMember>();
                                    
                                    if (operation.Value is System.Collections.IEnumerable enumerable && !(operation.Value is string))
                                    {
                                        foreach (var item in enumerable)
                                        {
                                            GroupMember? member = null;
                                            if (item is GroupMember existingMember)
                                            {
                                                member = existingMember;
                                            }
                                            else
                                            {
                                                // Handle anonymous objects or JObjects
                                                var json = Newtonsoft.Json.JsonConvert.SerializeObject(item);
                                                member = Newtonsoft.Json.JsonConvert.DeserializeObject<GroupMember>(json);
                                            }
                                            if (member != null)
                                            {
                                                group.Members.Add(member);
                                            }
                                        }
                                    }
                                    else if (operation.Value is Newtonsoft.Json.Linq.JArray membersArray)
                                    {
                                        var newMembers = membersArray.ToObject<List<GroupMember>>();
                                        if (newMembers != null)
                                        {
                                            group.Members.AddRange(newMembers);
                                        }
                                    }
                                    else if (operation.Value is Newtonsoft.Json.Linq.JObject memberObj)
                                    {
                                        var newMember = memberObj.ToObject<GroupMember>();
                                        if (newMember != null)
                                        {
                                            group.Members.Add(newMember);
                                        }
                                    }
                                    else
                                    {
                                        // Single member object
                                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(operation.Value);
                                        var member = Newtonsoft.Json.JsonConvert.DeserializeObject<GroupMember>(json);
                                        if (member != null)
                                        {
                                            group.Members.Add(member);
                                        }
                                    }
                                }
                                break;

                            case "remove":
                                if (operation.Path == "externalId")
                                {
                                    group.ExternalId = null;
                                }
                                else if (operation.Path == "members")
                                {
                                    group.Members = null;
                                }
                                break;
                        }
                    }

                    // Populate $ref for members after creation (always override to ensure consistency)
                    if (group.Members != null)
                    {
                        foreach (var member in group.Members)
                        {
                            member.Ref = $"https://localhost/scim/v2/Users/{member.Value}";
                        }
                    }

                    if (group.Meta != null)
                    {
                        group.Meta.LastModified = DateTime.UtcNow;
                    }

                    return group;
                });

            // Setup DeleteGroupAsync
            mockService.Setup(s => s.DeleteGroupAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string id, string customerId) =>
                {
                    var group = groups.FirstOrDefault(g => g.Id == id && g.CustomerId == customerId);
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
