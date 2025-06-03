using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Data;
using ScimServiceProvider.Models;
using System.Text.RegularExpressions;

namespace ScimServiceProvider.Services
{
    public class GroupService : IGroupService
    {
        private readonly ScimDbContext _context;

        public GroupService(ScimDbContext context)
        {
            _context = context;
        }

        public async Task<ScimGroup?> GetGroupAsync(string id, string customerId)
        {
            return await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);
        }

        public async Task<ScimListResponse<ScimGroup>> GetGroupsAsync(string customerId, int startIndex = 1, int count = 10, string? filter = null)
        {
            var query = _context.Groups
                .Where(g => g.CustomerId == customerId)
                .AsQueryable();

            // Apply filter if provided
            if (!string.IsNullOrEmpty(filter))
            {
                query = ApplyFilter(query, filter);
            }

            var totalResults = await query.CountAsync();
            var groups = await query
                .Skip(startIndex - 1)
                .Take(count)
                .ToListAsync();

            return new ScimListResponse<ScimGroup>
            {
                TotalResults = totalResults,
                StartIndex = startIndex,
                ItemsPerPage = Math.Min(count, groups.Count),
                Resources = groups
            };
        }

        public async Task<ScimGroup> CreateGroupAsync(ScimGroup group, string customerId)
        {
            // Set customer ID
            group.CustomerId = customerId;

            // SCIM compliance: Check for duplicate externalId for this customer
            if (!string.IsNullOrEmpty(group.ExternalId))
            {
                var existing = await _context.Groups
                    .FirstOrDefaultAsync(g => g.ExternalId == group.ExternalId && g.CustomerId == customerId);
                if (existing != null)
                {
                    throw new InvalidOperationException($"Group with externalId '{group.ExternalId}' already exists for this customer.");
                }
            }

            group.Id = Guid.NewGuid().ToString();
            group.Created = DateTime.UtcNow;
            group.LastModified = DateTime.UtcNow;
            group.Meta.Created = group.Created;
            group.Meta.LastModified = group.LastModified;
            group.Meta.ResourceType = "Group";

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            return group;
        }

        public async Task<ScimGroup?> UpdateGroupAsync(string id, ScimGroup group, string customerId)
        {
            var existingGroup = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);
                
            if (existingGroup == null)
                return null;

            existingGroup.DisplayName = group.DisplayName;
            existingGroup.Members = group.Members;
            existingGroup.LastModified = DateTime.UtcNow;
            existingGroup.Meta.LastModified = existingGroup.LastModified;

            await _context.SaveChangesAsync();
            return existingGroup;
        }

        public async Task<ScimGroup?> PatchGroupAsync(string id, ScimPatchRequest patchRequest, string customerId)
        {
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);
            if (group == null)
                return null;

            foreach (var operation in patchRequest.Operations)
            {
                ApplyPatchOperation(group, operation);
            }

            group.LastModified = DateTime.UtcNow;
            group.Meta.LastModified = group.LastModified;

            // Ensure proper schemas are set after patching
            group.Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:Group" };

            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<bool> DeleteGroupAsync(string id, string customerId)
        {
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);
                
            if (group == null)
                return false;

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return true;
        }

        private IQueryable<ScimGroup> ApplyFilter(IQueryable<ScimGroup> query, string filter)
        {
            // Simple filter implementation for displayName eq "value"
            var displayNameMatch = Regex.Match(filter, @"displayName\s+eq\s+""([^""]+)""", RegexOptions.IgnoreCase);
            if (displayNameMatch.Success)
            {
                var displayName = displayNameMatch.Groups[1].Value;
                // Case-insensitive comparison for SCIM compliance
                query = query.Where(g => g.DisplayName.ToLower() == displayName.ToLower());
            }

            return query;
        }

        private void ApplyPatchOperation(ScimGroup group, PatchOperation operation)
        {
            switch (operation.Op.ToLower())
            {
                case "replace":
                    if (operation.Path?.ToLower() == "displayname")
                    {
                        group.DisplayName = operation.Value?.ToString() ?? string.Empty;
                    }
                    else if (operation.Path?.ToLower().StartsWith("members[") == true && operation.Path.Contains("]."))
                    {
                        // Handle member attribute replacement like members[value eq "user-2"].display
                        var memberFilterMatch = Regex.Match(operation.Path, @"members\[value\s+eq\s+""([^""]+)""\]\.(\w+)", RegexOptions.IgnoreCase);
                        if (memberFilterMatch.Success)
                        {
                            var memberId = memberFilterMatch.Groups[1].Value;
                            var attributeName = memberFilterMatch.Groups[2].Value.ToLower();
                            
                            // Ensure Members is initialized
                            if (group.Members == null)
                                group.Members = new List<GroupMember>();
                            
                            // Find the member to update
                            var memberToUpdate = group.Members.FirstOrDefault(m => m.Value == memberId);
                            if (memberToUpdate != null)
                            {
                                switch (attributeName)
                                {
                                    case "display":
                                        memberToUpdate.Display = operation.Value?.ToString() ?? string.Empty;
                                        break;
                                    case "type":
                                        memberToUpdate.Type = operation.Value?.ToString() ?? "User";
                                        break;
                                }
                            }
                        }
                    }
                    break;
                case "add":
                    if (operation.Path?.ToLower() == "members")
                    {
                        // Ensure Members is initialized
                        if (group.Members == null)
                            group.Members = new List<GroupMember>();
                        // Add member to group
                        if (operation.Value is GroupMember member)
                        {
                            group.Members.Add(member);
                        }
                        else if (operation.Value != null)
                        {
                            // Try to parse as JSON object or dictionary
                            try
                            {
                                var valueStr = operation.Value.ToString();
                                if (valueStr != null)
                                {
                                    var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(valueStr);
                                    var newMember = new GroupMember();
                                    if (jsonElement.TryGetProperty("value", out var valueProperty))
                                        newMember.Value = valueProperty.GetString() ?? string.Empty;
                                    if (jsonElement.TryGetProperty("display", out var displayProperty))
                                        newMember.Display = displayProperty.GetString() ?? string.Empty;
                                    if (jsonElement.TryGetProperty("type", out var typeProperty))
                                        newMember.Type = typeProperty.GetString() ?? "User";
                                    if (!string.IsNullOrEmpty(newMember.Value))
                                    {
                                        group.Members.Add(newMember);
                                    }
                                }
                            }
                            catch
                            {
                                // If JSON parsing fails, try reflection or other approaches
                                var valueType = operation.Value.GetType();
                                var valueProp = valueType.GetProperty("value");
                                var displayProp = valueType.GetProperty("display");
                                var typeProp = valueType.GetProperty("type");
                                if (valueProp != null)
                                {
                                    var newMember = new GroupMember
                                    {
                                        Value = valueProp.GetValue(operation.Value)?.ToString() ?? string.Empty,
                                        Display = displayProp?.GetValue(operation.Value)?.ToString() ?? string.Empty,
                                        Type = typeProp?.GetValue(operation.Value)?.ToString() ?? "User"
                                    };
                                    if (!string.IsNullOrEmpty(newMember.Value))
                                    {
                                        group.Members.Add(newMember);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case "remove":
                    if (operation.Path?.ToLower().StartsWith("members") == true)
                    {
                        // Ensure Members is initialized
                        if (group.Members == null)
                            group.Members = new List<GroupMember>();
                        // Remove member from group
                        var memberMatch = Regex.Match(operation.Path, @"members\[value\s+eq\s+""([^""]+)""\]");
                        if (memberMatch.Success)
                        {
                            var memberId = memberMatch.Groups[1].Value;
                            group.Members.RemoveAll(m => m.Value == memberId);
                        }
                    }
                    break;
            }
        }
    }
}
