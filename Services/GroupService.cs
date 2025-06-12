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
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);
            if (group != null)
                CleanupEmptyCollections(group);
            return group;
        }

        public async Task<ScimListResponse<ScimGroup>> GetGroupsAsync(string customerId, int startIndex = 1, int count = 10, string? filter = null, string? attributes = null, string? excludedAttributes = null, string? sortBy = null, string? sortOrder = null)
        {
            var query = _context.Groups
                .Where(g => g.CustomerId == customerId)
                .AsQueryable();

            // Apply filter if provided
            if (!string.IsNullOrEmpty(filter))
            {
                query = ApplyFilter(query, filter);
            }

            // Apply sorting if provided
            if (!string.IsNullOrEmpty(sortBy))
            {
                query = ApplySorting(query, sortBy, sortOrder);
            }

            var totalResults = await query.CountAsync();
            var groups = await query
                .Skip(startIndex - 1)
                .Take(count)
                .ToListAsync();

            // Clean up empty collections for all groups
            foreach (var group in groups)
            {
                CleanupEmptyCollections(group);
                // Apply attribute selection if specified
                if (!string.IsNullOrEmpty(attributes) || !string.IsNullOrEmpty(excludedAttributes))
                {
                    ApplyAttributeSelection(group, attributes, excludedAttributes);
                }
            }

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

            // Validate Group member schema compliance
            ValidateGroupMembers(group);

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

            // Populate $ref fields for members
            PopulateMemberReferences(group);

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            CleanupEmptyCollections(group);
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

            // Populate $ref fields for members
            PopulateMemberReferences(existingGroup);

            await _context.SaveChangesAsync();
            CleanupEmptyCollections(existingGroup);
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
            
            // Populate $ref fields for members
            PopulateMemberReferences(group);
            
            CleanupEmptyCollections(group);
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
                return query;
            }

            // Filter by Id eq "value"
            var idMatch = Regex.Match(filter, @"\bId\s+eq\s+""([^""]+)""", RegexOptions.IgnoreCase);
            if (idMatch.Success)
            {
                var id = idMatch.Groups[1].Value;
                query = query.Where(g => g.Id == id);
                return query;
            }

            // Filter by id eq "value" (lowercase)
            var lowerIdMatch = Regex.Match(filter, @"\bid\s+eq\s+""([^""]+)""", RegexOptions.IgnoreCase);
            if (lowerIdMatch.Success)
            {
                var id = lowerIdMatch.Groups[1].Value;
                query = query.Where(g => g.Id == id);
                return query;
            }

            // Filter by externalId eq "value"
            var externalIdMatch = Regex.Match(filter, @"externalId\s+eq\s+""([^""]+)""", RegexOptions.IgnoreCase);
            if (externalIdMatch.Success)
            {
                var externalId = externalIdMatch.Groups[1].Value;
                query = query.Where(g => g.ExternalId == externalId);
                return query;
            }

            // Check for unsupported member filter patterns (these are only valid in PATCH operations, not GET filters)
            if (filter.Contains("members[") && filter.Contains("type eq"))
            {
                throw new InvalidOperationException($"The filter '{filter}' contains unsupported member type filtering. Group member filtering by type is not supported in GET operations. Please use supported filters like 'Id eq \"value\"', 'displayName eq \"value\"', or 'externalId eq \"value\"'.");
            }

            // If no patterns matched but we have a filter, it's unsupported
            // For now, we'll just return the unfiltered query to maintain compatibility
            // In a strict SCIM implementation, you might want to throw an error here
            return query;
        }

        private void ApplyPatchOperation(ScimGroup group, PatchOperation operation)
        {
            switch (operation.Op.ToLower())
            {
                case "replace":
                    if (string.IsNullOrEmpty(operation.Path))
                    {
                        // No path specified - replace the entire resource with the value
                        // Handle when value is a complex object containing group attributes
                        if (operation.Value != null)
                        {
                            try
                            {
                                // Handle JsonElement (most common case)
                                if (operation.Value is System.Text.Json.JsonElement jsonElement)
                                {
                                    // Update displayName if present
                                    if (jsonElement.TryGetProperty("displayName", out var displayNameProperty))
                                    {
                                        group.DisplayName = displayNameProperty.GetString() ?? string.Empty;
                                    }
                                    
                                    // Update externalId if present
                                    if (jsonElement.TryGetProperty("externalId", out var externalIdProperty))
                                    {
                                        group.ExternalId = externalIdProperty.GetString();
                                    }

                                    // Update members if present
                                    if (jsonElement.TryGetProperty("members", out var membersProperty) && membersProperty.ValueKind == System.Text.Json.JsonValueKind.Array)
                                    {
                                        var members = new List<GroupMember>();
                                        foreach (var memberElement in membersProperty.EnumerateArray())
                                        {
                                            var member = new GroupMember();
                                            if (memberElement.TryGetProperty("value", out var valueProperty))
                                                member.Value = valueProperty.GetString() ?? string.Empty;
                                            if (memberElement.TryGetProperty("display", out var displayProperty))
                                                member.Display = displayProperty.GetString();
                                            members.Add(member);
                                        }
                                        group.Members = members;
                                    }
                                }
                                // Handle ScimGroup object directly
                                else if (operation.Value is ScimGroup scimGroup)
                                {
                                    group.DisplayName = scimGroup.DisplayName;
                                    if (!string.IsNullOrEmpty(scimGroup.ExternalId))
                                        group.ExternalId = scimGroup.ExternalId;
                                    if (scimGroup.Members != null)
                                        group.Members = scimGroup.Members;
                                }
                                // Handle anonymous objects or other types
                                else
                                {
                                    // Serialize and deserialize to handle anonymous objects
                                    var json = System.Text.Json.JsonSerializer.Serialize(operation.Value);
                                    var jsonElement2 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                                    
                                    // Update displayName if present
                                    if (jsonElement2.TryGetProperty("displayName", out var displayNameProperty))
                                    {
                                        group.DisplayName = displayNameProperty.GetString() ?? string.Empty;
                                    }
                                    
                                    // Update externalId if present
                                    if (jsonElement2.TryGetProperty("externalId", out var externalIdProperty))
                                    {
                                        group.ExternalId = externalIdProperty.GetString();
                                    }
                                    
                                    // Update members if present
                                    if (jsonElement2.TryGetProperty("members", out var membersProperty) && membersProperty.ValueKind == System.Text.Json.JsonValueKind.Array)
                                    {
                                        var members = new List<GroupMember>();
                                        foreach (var memberElement in membersProperty.EnumerateArray())
                                        {
                                            var member = new GroupMember();
                                            if (memberElement.TryGetProperty("value", out var valueProperty))
                                                member.Value = valueProperty.GetString() ?? string.Empty;
                                            if (memberElement.TryGetProperty("display", out var displayProperty))
                                                member.Display = displayProperty.GetString() ?? string.Empty;
                                            members.Add(member);
                                        }
                                        group.Members = members;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException($"Failed to process replace operation without path: {ex.Message}", ex);
                            }
                        }
                    }
                    else if (operation.Path?.ToLower() == "displayname")
                    {
                        group.DisplayName = operation.Value?.ToString() ?? string.Empty;
                    }
                    else if (operation.Path?.ToLower() == "externalid")
                    {
                        group.ExternalId = operation.Value?.ToString();
                    }
                    else if (operation.Path?.ToLower().StartsWith("members[") == true && operation.Path.Contains("]."))
                    {
                        // Check for unsupported attributes in member filter paths
                        if (operation.Path.Contains("type eq"))
                        {
                            // SCIM Groups don't support member type filtering
                            // According to SCIM RFC 7643, Group members only have value, display, and $ref
                            throw new InvalidOperationException($"The attribute members[type eq \"untyped\"].value for Group is not supported by the SCIM protocol. According to SCIM RFC 7643 Section 4.2, Group members only support 'value', 'display', and '$ref' attributes, not 'type'. Please refer to the SCIM RFC for correct Group member schema.");
                        }
                        
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
                                if (valueProp != null)
                                {
                                    var newMember = new GroupMember
                                    {
                                        Value = valueProp.GetValue(operation.Value)?.ToString() ?? string.Empty,
                                        Display = displayProp?.GetValue(operation.Value)?.ToString() ?? string.Empty
                                    };
                                    if (!string.IsNullOrEmpty(newMember.Value))
                                    {
                                        group.Members.Add(newMember);
                                    }
                                }
                            }
                        }
                    }
                    else if (operation.Path?.ToLower().StartsWith("members[") == true && operation.Path.Contains("]."))
                    {
                        // Check for unsupported attributes in member filter paths for add operations
                        if (operation.Path.Contains("type eq"))
                        {
                            // SCIM Groups don't support member type filtering
                            // According to SCIM RFC 7643, Group members only have value, display, and $ref
                            throw new InvalidOperationException($"Invalid path '{operation.Path}'. Group members do not support 'type' attribute filtering. Supported member attributes are 'value', 'display', and '$ref'. Please refer to SCIM RFC 7643 Section 4.2.");
                        }
                        
                        // Handle member attribute addition like members[value eq "user-2"].display
                        // For now, we don't support adding to specific members via filtered paths
                        // This would require more complex logic to find existing members and update them
                        throw new InvalidOperationException($"Invalid path '{operation.Path}'. Adding to specific member attributes via filtered paths is not supported. Use 'members' path to add complete member objects.");
                    }
                    break;
                case "remove":
                    if (operation.Path?.ToLower() == "externalid")
                    {
                        group.ExternalId = null;
                    }
                    else if (operation.Path?.ToLower() == "members")
                    {
                        // Remove all members
                        group.Members = null;
                    }
                    else if (operation.Path?.ToLower().StartsWith("members[") == true)
                    {
                        // Check for unsupported attributes in member filter paths
                        if (operation.Path.Contains("type eq"))
                        {
                            // SCIM Groups don't support member type filtering
                            // According to SCIM RFC 7643, Group members only have value, display, and $ref
                            throw new InvalidOperationException($"The attribute members[type eq \"untyped\"].value for Group is not supported by the SCIM protocol. According to SCIM RFC 7643 Section 4.2, Group members only support 'value', 'display', and '$ref' attributes, not 'type'. Please refer to the SCIM RFC for correct Group member schema.");
                        }
                        
                        // Ensure Members is initialized
                        if (group.Members == null)
                            group.Members = new List<GroupMember>();
                        // Remove specific member from group
                        var memberMatch = Regex.Match(operation.Path, @"members\[value\s+eq\s+""([^""]+)""\]");
                        if (memberMatch.Success)
                        {
                            var memberId = memberMatch.Groups[1].Value;
                            group.Members.RemoveAll(m => m.Value == memberId);
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported PATCH operation: {operation.Op}");
            }
        }

        private void CleanupEmptyCollections(ScimGroup group)
        {
            if (group.Members?.Count == 0) group.Members = null;
        }

        private IQueryable<ScimGroup> ApplySorting(IQueryable<ScimGroup> query, string sortBy, string? sortOrder = null)
        {
            var ascending = string.IsNullOrEmpty(sortOrder) || 
                           string.Equals(sortOrder, "ascending", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "displayname" => ascending ? query.OrderBy(g => g.DisplayName) : query.OrderByDescending(g => g.DisplayName),
                "created" => ascending ? query.OrderBy(g => g.Created) : query.OrderByDescending(g => g.Created),
                "lastmodified" => ascending ? query.OrderBy(g => g.LastModified) : query.OrderByDescending(g => g.LastModified),
                _ => query // Default: no sorting if sortBy is not recognized
            };
        }

        private void ApplyAttributeSelection(ScimGroup group, string? attributes, string? excludedAttributes)
        {
            var includedAttrs = string.IsNullOrEmpty(attributes) ? null : 
                attributes.Split(',').Select(a => a.Trim().ToLower()).ToHashSet();
            var excludedAttrs = string.IsNullOrEmpty(excludedAttributes) ? null : 
                excludedAttributes.Split(',').Select(a => a.Trim().ToLower()).ToHashSet();

            // Always include core SCIM attributes
            var alwaysInclude = new HashSet<string> { "id", "schemas", "meta" };

            // Helper function to check if an attribute should be included
            bool ShouldInclude(string attrName)
            {
                var lowerAttr = attrName.ToLower();
                
                // Always include core attributes
                if (alwaysInclude.Contains(lowerAttr)) return true;
                
                // If excluded attributes are specified and this attribute is excluded, don't include
                if (excludedAttrs != null && excludedAttrs.Contains(lowerAttr)) return false;
                
                // If included attributes are specified, only include if it's in the list
                if (includedAttrs != null) return includedAttrs.Contains(lowerAttr);
                
                // Default: include if no specific inclusion/exclusion rules apply
                return true;
            }

            // Apply attribute filtering
            if (!ShouldInclude("displayname")) group.DisplayName = null!;
            if (!ShouldInclude("externalid")) group.ExternalId = null;
            if (!ShouldInclude("members")) group.Members = null;
        }

        private void PopulateMemberReferences(ScimGroup group, HttpContext? httpContext = null)
        {
            if (group.Members != null)
            {
                foreach (var member in group.Members)
                {
                    if (!string.IsNullOrEmpty(member.Value) && string.IsNullOrEmpty(member.Ref))
                    {
                        // For consistency with SCIM RFC and other services, use relative URLs
                        // This ensures consistent behavior across unit tests and API calls
                        member.Ref = $"../Users/{member.Value}";
                    }
                }
            }
        }

        private void ValidateGroupMembers(ScimGroup group)
        {
            if (group.Members == null || !group.Members.Any())
                return;

            // For now, we'll implement a simpler validation approach
            // The main issue is that JSON deserialization ignores extra properties
            // So we need to validate at the JSON level before deserialization
            // This method serves as a placeholder for basic validation
            
            foreach (var member in group.Members)
            {
                if (string.IsNullOrEmpty(member.Value))
                {
                    throw new InvalidOperationException("Group member 'value' attribute is required and cannot be empty.");
                }
            }
        }
    }
}
