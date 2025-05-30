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

        public async Task<ScimGroup?> GetGroupAsync(string id)
        {
            return await _context.Groups.FindAsync(id);
        }

        public async Task<ScimListResponse<ScimGroup>> GetGroupsAsync(int startIndex = 1, int count = 10, string? filter = null)
        {
            var query = _context.Groups.AsQueryable();

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

        public async Task<ScimGroup> CreateGroupAsync(ScimGroup group)
        {
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

        public async Task<ScimGroup?> UpdateGroupAsync(string id, ScimGroup group)
        {
            var existingGroup = await _context.Groups.FindAsync(id);
            if (existingGroup == null)
                return null;

            existingGroup.DisplayName = group.DisplayName;
            existingGroup.Members = group.Members;
            existingGroup.LastModified = DateTime.UtcNow;
            existingGroup.Meta.LastModified = existingGroup.LastModified;

            await _context.SaveChangesAsync();
            return existingGroup;
        }

        public async Task<ScimGroup?> PatchGroupAsync(string id, ScimPatchRequest patchRequest)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return null;

            foreach (var operation in patchRequest.Operations)
            {
                ApplyPatchOperation(group, operation);
            }

            group.LastModified = DateTime.UtcNow;
            group.Meta.LastModified = group.LastModified;

            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<bool> DeleteGroupAsync(string id)
        {
            var group = await _context.Groups.FindAsync(id);
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
                query = query.Where(g => g.DisplayName == displayName);
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
                    break;
                case "add":
                    if (operation.Path?.ToLower() == "members")
                    {
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
