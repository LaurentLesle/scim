using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Data;
using ScimServiceProvider.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScimServiceProvider.Services
{
    public class UserService : IUserService
    {
        private readonly ScimDbContext _context;
        
        // Configuration option to disable manager validation for unit testing
        public bool EnableStrictManagerValidation { get; set; } = false;
        
        public UserService(ScimDbContext context)
        {
            _context = context;
        }

        public async Task<ScimUser?> GetUserAsync(string id, string customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);
            if (user != null)
                CleanupEmptyCollections(user);
            return user;
        }

        public async Task<ScimUser?> GetUserByUsernameAsync(string username, string customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username && u.CustomerId == customerId);
            if (user != null)
                CleanupEmptyCollections(user);
            return user;
        }

        public async Task<ScimListResponse<ScimUser>> GetUsersAsync(string customerId, int startIndex = 1, int count = 10, string? filter = null, string? attributes = null, string? excludedAttributes = null, string? sortBy = null, string? sortOrder = null)
        {
            var query = _context.Users
                .Where(u => u.CustomerId == customerId)
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
            var users = await query
                .Skip(startIndex - 1)
                .Take(count)
                .ToListAsync();

            // Clean up empty collections for all users
            foreach (var user in users)
            {
                CleanupEmptyCollections(user);
                // Apply attribute selection if specified
                if (!string.IsNullOrEmpty(attributes) || !string.IsNullOrEmpty(excludedAttributes))
                {
                    ApplyAttributeSelection(user, attributes, excludedAttributes);
                }
            }

            return new ScimListResponse<ScimUser>
            {
                TotalResults = totalResults,
                StartIndex = startIndex,
                ItemsPerPage = Math.Min(count, users.Count),
                Resources = users
            };
        }

        public async Task<ScimUser> CreateUserAsync(ScimUser user, string customerId)
        {
            // Set customer ID
            user.CustomerId = customerId;

            // SCIM compliance: Check for duplicate externalId for this customer
            if (!string.IsNullOrEmpty(user.ExternalId))
            {
                var existing = await _context.Users
                    .FirstOrDefaultAsync(u => u.ExternalId == user.ExternalId && u.CustomerId == customerId);
                if (existing != null)
                {
                    throw new InvalidOperationException($"User with externalId '{user.ExternalId}' already exists for this customer.");
                }
            }

            user.Id = Guid.NewGuid().ToString();
            user.Created = DateTime.UtcNow;
            user.LastModified = DateTime.UtcNow;
            user.Meta.Created = user.Created;
            user.Meta.LastModified = user.LastModified;
            user.Meta.ResourceType = "User";

            // Ensure proper schemas are set
            user.Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" };
            if (user.EnterpriseUser != null)
            {
                if (!user.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                {
                    user.Schemas.Add("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
                }
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            CleanupEmptyCollections(user);
            return user;
        }

        public async Task<ScimUser?> UpdateUserAsync(string id, ScimUser user, string customerId)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);
                
            if (existingUser == null)
                return null;

            // Update properties
            existingUser.UserName = user.UserName;
            existingUser.Name = user.Name;
            existingUser.DisplayName = user.DisplayName;
            existingUser.NickName = user.NickName;
            existingUser.ProfileUrl = user.ProfileUrl;
            existingUser.Title = user.Title;
            existingUser.UserType = user.UserType;
            existingUser.PreferredLanguage = user.PreferredLanguage;
            existingUser.Locale = user.Locale;
            existingUser.Timezone = user.Timezone;
            existingUser.Active = user.Active;
            existingUser.Emails = user.Emails;
            existingUser.PhoneNumbers = user.PhoneNumbers;
            existingUser.Addresses = user.Addresses;
            existingUser.Groups = user.Groups;
            existingUser.Roles = user.Roles;
            existingUser.EnterpriseUser = user.EnterpriseUser;
            existingUser.LastModified = DateTime.UtcNow;
            existingUser.Meta.LastModified = existingUser.LastModified;

            // Ensure proper schemas are set
            existingUser.Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" };
            if (existingUser.EnterpriseUser != null)
            {
                if (!existingUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                {
                    existingUser.Schemas.Add("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
                }
            }

            await _context.SaveChangesAsync();
            CleanupEmptyCollections(existingUser);
            return existingUser;
        }

        // Ensure all collections and objects are initialized before returning the user
        private void EnsureUserCollectionsInitialized(ScimUser user)
        {
            if (user.Emails == null) user.Emails = new List<Email>();
            if (user.PhoneNumbers == null) user.PhoneNumbers = new List<PhoneNumber>();
            if (user.Addresses == null) user.Addresses = new List<Address>();
            if (user.Groups == null) user.Groups = new List<GroupMembership>();
            if (user.Roles == null) user.Roles = new List<Role>();
            if (user.Name == null) user.Name = new Name();
            if (user.EnterpriseUser == null) user.EnterpriseUser = new EnterpriseUser();
            if (user.Meta == null) user.Meta = new ScimMeta();
        }

        private void CleanupEmptyCollections(ScimUser user)
        {
            if (user.Emails?.Count == 0) user.Emails = null;
            if (user.PhoneNumbers?.Count == 0) user.PhoneNumbers = null;
            if (user.Addresses?.Count == 0) user.Addresses = null;
            if (user.Groups?.Count == 0) user.Groups = null;
            if (user.Roles?.Count == 0) user.Roles = null;
        }

        public async Task<ScimUser?> PatchUserAsync(string id, ScimPatchRequest patchRequest, string customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);
                
            if (user == null)
                return null;

            foreach (var operation in patchRequest.Operations)
            {
                switch (operation.Op.ToLower())
                {
                    case "replace":
                        await ApplyReplaceOperationAsync(user, operation, customerId);
                        break;
                    case "add":
                        await ApplyAddOperationAsync(user, operation, customerId);
                        break;
                    case "remove":
                        ApplyRemoveOperation(user, operation);
                        break;
                }
            }

            user.LastModified = DateTime.UtcNow;
            user.Meta.LastModified = user.LastModified;
            
            // Ensure proper schemas are set
            user.Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" };
            if (user.EnterpriseUser != null)
            {
                if (!user.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                {
                    user.Schemas.Add("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
                }
            }

            EnsureUserCollectionsInitialized(user);

            await _context.SaveChangesAsync();
            CleanupEmptyCollections(user);
            return user;
        }

        public async Task<bool> DeleteUserAsync(string id, string customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);
                
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        private IQueryable<ScimUser> ApplyFilter(IQueryable<ScimUser> query, string filter)
        {
            // Simple filter implementation for userName eq "value"
            var userNameMatch = Regex.Match(filter, @"userName\s+eq\s+""([^""]+)""", RegexOptions.IgnoreCase);
            if (userNameMatch.Success)
            {
                var userName = userNameMatch.Groups[1].Value;
                // Case-insensitive comparison for SCIM compliance
                query = query.Where(u => u.UserName.ToLower() == userName.ToLower());
                return query;
            }

            // Filter by active status: active eq true/false
            var activeMatch = Regex.Match(filter, @"active\s+eq\s+(true|false)", RegexOptions.IgnoreCase);
            if (activeMatch.Success)
            {
                var activeValue = bool.Parse(activeMatch.Groups[1].Value.ToLower());
                query = query.Where(u => u.Active == activeValue);
                return query;
            }

            // Add more filter implementations as needed
            return query;
        }


        private async Task ApplyAddOperationAsync(ScimUser user, PatchOperation operation, string customerId)
        {
            // If path is null or empty, treat as full object add/merge
            if (string.IsNullOrEmpty(operation.Path))
            {
                if (operation.Value is Newtonsoft.Json.Linq.JObject jObj)
                {
                    var userJson = Newtonsoft.Json.Linq.JObject.FromObject(user);
                    userJson.Merge(jObj, new Newtonsoft.Json.Linq.JsonMergeSettings { MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Merge });
                    var updatedUser = userJson.ToObject<ScimUser>();
                    if (updatedUser != null)
                    {
                        user.DisplayName = updatedUser.DisplayName;
                        user.Title = updatedUser.Title;
                        user.PreferredLanguage = updatedUser.PreferredLanguage;
                        user.Name = updatedUser.Name;
                        user.NickName = updatedUser.NickName;
                        user.Locale = updatedUser.Locale;
                        user.Timezone = updatedUser.Timezone;
                        user.ProfileUrl = updatedUser.ProfileUrl;
                        user.UserType = updatedUser.UserType;
                        // Only update collections if they are provided and not empty
                        if (updatedUser.Emails != null && updatedUser.Emails.Any()) user.Emails = updatedUser.Emails;
                        if (updatedUser.PhoneNumbers != null && updatedUser.PhoneNumbers.Any()) user.PhoneNumbers = updatedUser.PhoneNumbers;
                        if (updatedUser.Addresses != null && updatedUser.Addresses.Any()) user.Addresses = updatedUser.Addresses;
                        if (updatedUser.Roles != null && updatedUser.Roles.Any()) user.Roles = updatedUser.Roles;
                        user.EnterpriseUser = updatedUser.EnterpriseUser;
                    }
                }
                else if (operation.Value is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // Handle dot-notated keys for name fields and enterprise extension attributes
                    foreach (var prop in elem.EnumerateObject())
                    {
                        if (prop.Name.StartsWith("name.", StringComparison.OrdinalIgnoreCase))
                        {
                            if (user.Name == null) user.Name = new Name();
                            var sub = prop.Name.Substring(5).ToLower();
                            switch (sub)
                            {
                                case "givenname": user.Name.GivenName = prop.Value.GetString(); break;
                                case "familyname": user.Name.FamilyName = prop.Value.GetString(); break;
                                case "formatted": user.Name.Formatted = prop.Value.GetString(); break;
                                case "middlename": user.Name.MiddleName = prop.Value.GetString(); break;
                                case "honorificprefix": user.Name.HonorificPrefix = prop.Value.GetString(); break;
                                case "honorificsuffix": user.Name.HonorificSuffix = prop.Value.GetString(); break;
                            }
                        }
                        else if (prop.Name.StartsWith("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (user.EnterpriseUser == null) user.EnterpriseUser = new EnterpriseUser();
                            var enterpriseAttr = prop.Name.Substring("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:".Length).ToLower();
                            switch (enterpriseAttr)
                            {
                                case "employeenumber": user.EnterpriseUser.EmployeeNumber = prop.Value.GetString(); break;
                                case "department": user.EnterpriseUser.Department = prop.Value.GetString(); break;
                                case "costcenter": user.EnterpriseUser.CostCenter = prop.Value.GetString(); break;
                                case "organization": user.EnterpriseUser.Organization = prop.Value.GetString(); break;
                                case "division": user.EnterpriseUser.Division = prop.Value.GetString(); break;
                                case "manager": user.EnterpriseUser.Manager = DeserializeManager(prop.Value.GetString()); break;
                            }
                        }
                    }
                    var updatedUser = System.Text.Json.JsonSerializer.Deserialize<ScimUser>(elem.GetRawText());
                    if (updatedUser != null)
                    {
                        user.DisplayName = updatedUser.DisplayName;
                        user.Title = updatedUser.Title;
                        user.PreferredLanguage = updatedUser.PreferredLanguage;
                        // Do not overwrite user.Name here, as we just set it above
                        user.NickName = updatedUser.NickName;
                        user.Locale = updatedUser.Locale;
                        user.Timezone = updatedUser.Timezone;
                        user.ProfileUrl = updatedUser.ProfileUrl;
                        user.UserType = updatedUser.UserType;
                        // Only update collections if they are provided and not empty
                        if (updatedUser.Emails != null && updatedUser.Emails.Any()) user.Emails = updatedUser.Emails;
                        if (updatedUser.PhoneNumbers != null && updatedUser.PhoneNumbers.Any()) user.PhoneNumbers = updatedUser.PhoneNumbers;
                        if (updatedUser.Addresses != null && updatedUser.Addresses.Any()) user.Addresses = updatedUser.Addresses;
                        if (updatedUser.Roles != null && updatedUser.Roles.Any()) user.Roles = updatedUser.Roles;
                        // Do not overwrite EnterpriseUser here, as we just set it above
                    }
                }
                else if (operation.Value is Dictionary<string, object> dict)
                {
                    // For each key-value, treat as a separate add operation
                    foreach (var kvp in dict)
                    {
                        // Special handling for name.* keys
                        if (kvp.Key.StartsWith("name.", StringComparison.OrdinalIgnoreCase))
                        {
                            if (user.Name == null) user.Name = new Name();
                            var sub = kvp.Key.Substring(5).ToLower();
                            switch (sub)
                            {
                                case "givenname": user.Name.GivenName = kvp.Value?.ToString(); break;
                                case "familyname": user.Name.FamilyName = kvp.Value?.ToString(); break;
                                case "formatted": user.Name.Formatted = kvp.Value?.ToString(); break;
                                case "middlename": user.Name.MiddleName = kvp.Value?.ToString(); break;
                                case "honorificprefix": user.Name.HonorificPrefix = kvp.Value?.ToString(); break;
                                case "honorificsuffix": user.Name.HonorificSuffix = kvp.Value?.ToString(); break;
                            }
                        }
                        else if (kvp.Key.StartsWith("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (user.EnterpriseUser == null) user.EnterpriseUser = new EnterpriseUser();
                            var enterpriseAttr = kvp.Key.Substring("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:".Length).ToLower();
                            switch (enterpriseAttr)
                            {
                                case "employeenumber": user.EnterpriseUser.EmployeeNumber = kvp.Value?.ToString(); break;
                                case "department": user.EnterpriseUser.Department = kvp.Value?.ToString(); break;
                                case "costcenter": user.EnterpriseUser.CostCenter = kvp.Value?.ToString(); break;
                                case "organization": user.EnterpriseUser.Organization = kvp.Value?.ToString(); break;
                                case "division": user.EnterpriseUser.Division = kvp.Value?.ToString(); break;
                                case "manager": 
                                    var managerAddAlt = await DeserializeManagerAsync(kvp.Value, customerId);
                                    if (managerAddAlt != null && !string.IsNullOrEmpty(managerAddAlt.Value))
                                    {
                                        // Validate that the manager exists
                                        if (!await ValidateManagerExistsAsync(managerAddAlt.Value, customerId))
                                        {
                                            throw new InvalidOperationException($"Manager with ID '{managerAddAlt.Value}' does not exist.");
                                        }
                                    }
                                    user.EnterpriseUser.Manager = managerAddAlt; 
                                    break;
                            }
                        }
                        else
                        {
                            var subOp = new PatchOperation { Op = "add", Path = kvp.Key, Value = kvp.Value };
                            await ApplyAddOperationAsync(user, subOp, customerId);
                        }
                    }
                    return;
                }
                return;
            }

            var path = operation.Path;
            
            // Handle enterprise extension as a whole object
            if (path.Equals("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", StringComparison.OrdinalIgnoreCase))
            {
                if (user.EnterpriseUser == null) user.EnterpriseUser = new EnterpriseUser();
                
                // Handle the value as an anonymous object containing enterprise user fields
                if (operation.Value != null)
                {
                    if (operation.Value is Dictionary<string, object> dict)
                    {
                        foreach (var kvp in dict)
                        {
                            var fieldName = kvp.Key.ToLower();
                            switch (fieldName)
                            {
                                case "employeenumber": user.EnterpriseUser.EmployeeNumber = kvp.Value?.ToString(); break;
                                case "department": user.EnterpriseUser.Department = kvp.Value?.ToString(); break;
                                case "costcenter": user.EnterpriseUser.CostCenter = kvp.Value?.ToString(); break;
                                case "organization": user.EnterpriseUser.Organization = kvp.Value?.ToString(); break;
                                case "division": user.EnterpriseUser.Division = kvp.Value?.ToString(); break;
                                case "manager": user.EnterpriseUser.Manager = DeserializeManager(kvp.Value); break;
                            }
                        }
                    }
                    else if (operation.Value is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var prop in elem.EnumerateObject())
                        {
                            var fieldName = prop.Name.ToLower();
                            switch (fieldName)
                            {
                                case "employeenumber": user.EnterpriseUser.EmployeeNumber = prop.Value.GetString(); break;
                                case "department": user.EnterpriseUser.Department = prop.Value.GetString(); break;
                                case "costcenter": user.EnterpriseUser.CostCenter = prop.Value.GetString(); break;
                                case "organization": user.EnterpriseUser.Organization = prop.Value.GetString(); break;
                                case "division": user.EnterpriseUser.Division = prop.Value.GetString(); break;
                                case "manager": user.EnterpriseUser.Manager = DeserializeManager(prop.Value.GetString()); break;
                            }
                        }
                    }
                    else
                    {
                        // Handle anonymous objects by using reflection to get property values
                        var valueType = operation.Value.GetType();
                        var properties = valueType.GetProperties();
                        
                        foreach (var prop in properties)
                        {
                            var fieldName = prop.Name.ToLower();
                            var propValue = prop.GetValue(operation.Value);
                            
                            switch (fieldName)
                            {
                                case "employeenumber": user.EnterpriseUser.EmployeeNumber = propValue?.ToString(); break;
                                case "department": user.EnterpriseUser.Department = propValue?.ToString(); break;
                                case "costcenter": user.EnterpriseUser.CostCenter = propValue?.ToString(); break;
                                case "organization": user.EnterpriseUser.Organization = propValue?.ToString(); break;
                                case "division": user.EnterpriseUser.Division = propValue?.ToString(); break;
                                case "manager": user.EnterpriseUser.Manager = DeserializeManager(propValue); break;
                            }
                        }
                    }
                }
                return;
            }
            
            // Handle dot notation for nested attributes
            if (path.StartsWith("name."))
            {
                if (user.Name == null) user.Name = new Name();
                var sub = path.Substring(5).ToLower();
                switch (sub)
                {
                    case "givenname": user.Name.GivenName = operation.Value?.ToString(); break;
                    case "familyname": user.Name.FamilyName = operation.Value?.ToString(); break;
                    case "formatted": user.Name.Formatted = operation.Value?.ToString(); break;
                    case "middlename": user.Name.MiddleName = operation.Value?.ToString(); break;
                    case "honorificprefix": user.Name.HonorificPrefix = operation.Value?.ToString(); break;
                    case "honorificsuffix": user.Name.HonorificSuffix = operation.Value?.ToString(); break;
                }
                return;
            }
            // Handle multi-valued attributes with filter, e.g. emails[type eq "work"].value, roles[primary eq "True"].display
            var mvMatch = System.Text.RegularExpressions.Regex.Match(path, @"^(\w+)\[(\w+) eq ""([\w ]+)""\]\.(\w+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (mvMatch.Success)
            {
                var collection = mvMatch.Groups[1].Value.ToLower();
                var filterAttr = mvMatch.Groups[2].Value.ToLower();
                var filterValue = mvMatch.Groups[3].Value;
                var attr = mvMatch.Groups[4].Value.ToLower();
                if (collection == "emails")
                {
                    if (user.Emails == null) user.Emails = new List<Email>();
                    var email = user.Emails.FirstOrDefault(e =>
                        filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase));
                    if (email == null)
                    {
                        // Create new email with filter criteria
                        email = new Email();
                        if (filterAttr == "type") email.Type = filterValue;
                        user.Emails.Add(email);
                    }
                    if (attr == "value") email.Value = operation.Value?.ToString() ?? string.Empty;
                    else if (attr == "primary") email.Primary = operation.Value is bool b ? b : bool.TryParse(operation.Value?.ToString(), out var pb) && pb;
                }
                else if (collection == "phonenumbers")
                {
                    if (user.PhoneNumbers == null) user.PhoneNumbers = new List<PhoneNumber>();
                    var phone = user.PhoneNumbers.FirstOrDefault(e =>
                        filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase));
                    if (phone == null)
                    {
                        // Create new phone number with filter criteria
                        phone = new PhoneNumber();
                        if (filterAttr == "type") phone.Type = filterValue;
                        user.PhoneNumbers.Add(phone);
                    }
                    if (attr == "value") phone.Value = operation.Value?.ToString() ?? string.Empty;
                    else if (attr == "primary") phone.Primary = operation.Value is bool b ? b : bool.TryParse(operation.Value?.ToString(), out var pb) && pb;
                }
                else if (collection == "addresses")
                {
                    if (user.Addresses == null) user.Addresses = new List<Address>();
                    var addr = user.Addresses.FirstOrDefault(e =>
                        filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase));
                    if (addr == null)
                    {
                        // Create new address with filter criteria
                        addr = new Address();
                        if (filterAttr == "type") addr.Type = filterValue;
                        user.Addresses.Add(addr);
                    }
                    if (attr == "formatted") addr.Formatted = operation.Value?.ToString();
                    else if (attr == "streetaddress") addr.StreetAddress = operation.Value?.ToString();
                    else if (attr == "locality") addr.Locality = operation.Value?.ToString();
                    else if (attr == "region") addr.Region = operation.Value?.ToString();
                    else if (attr == "postalcode") addr.PostalCode = operation.Value?.ToString();
                    else if (attr == "country") addr.Country = operation.Value?.ToString();
                    else if (attr == "primary") addr.Primary = operation.Value is bool b ? b : bool.TryParse(operation.Value?.ToString(), out var pb) && pb;
                }
                else if (collection == "roles")
                {
                    if (user.Roles == null) user.Roles = new List<Role>();
                    
                    // Use explicit property matching instead of reflection for consistency
                    Role? role = null;
                    if (filterAttr.Equals("primary", StringComparison.OrdinalIgnoreCase))
                    {
                        role = user.Roles.FirstOrDefault(r => string.Equals(r.Primary, filterValue, StringComparison.OrdinalIgnoreCase));
                    }
                    else if (filterAttr.Equals("type", StringComparison.OrdinalIgnoreCase))
                    {
                        role = user.Roles.FirstOrDefault(r => string.Equals(r.Type, filterValue, StringComparison.OrdinalIgnoreCase));
                    }
                    else if (filterAttr.Equals("value", StringComparison.OrdinalIgnoreCase))
                    {
                        role = user.Roles.FirstOrDefault(r => string.Equals(r.Value, filterValue, StringComparison.OrdinalIgnoreCase));
                    }
                    else if (filterAttr.Equals("display", StringComparison.OrdinalIgnoreCase))
                    {
                        role = user.Roles.FirstOrDefault(r => string.Equals(r.Display, filterValue, StringComparison.OrdinalIgnoreCase));
                    }
                    
                    if (role == null)
                    {
                        // Create new role with filter criteria
                        role = new Role();
                        if (filterAttr.Equals("primary", StringComparison.OrdinalIgnoreCase)) role.Primary = filterValue;
                        else if (filterAttr.Equals("type", StringComparison.OrdinalIgnoreCase)) role.Type = filterValue;
                        else if (filterAttr.Equals("value", StringComparison.OrdinalIgnoreCase)) role.Value = filterValue;
                        else if (filterAttr.Equals("display", StringComparison.OrdinalIgnoreCase)) role.Display = filterValue;
                        user.Roles.Add(role);
                    }
                    
                    if (attr.Equals("display", StringComparison.OrdinalIgnoreCase))
                        role.Display = operation.Value?.ToString();
                    else if (attr.Equals("value", StringComparison.OrdinalIgnoreCase))
                        role.Value = operation.Value?.ToString() ?? string.Empty;
                    else if (attr.Equals("type", StringComparison.OrdinalIgnoreCase))
                        role.Type = operation.Value?.ToString() ?? string.Empty;
                    else if (attr.Equals("primary", StringComparison.OrdinalIgnoreCase))
                        role.Primary = operation.Value?.ToString();
                }
                return;
            }
            // Handle enterprise extension fields (case-insensitive)
            var entPrefix = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:";
            if (path.StartsWith(entPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (user.EnterpriseUser == null) user.EnterpriseUser = new EnterpriseUser();
                var sub = path.Substring(entPrefix.Length).ToLower();
                switch (sub)
                {
                    case "employeenumber": user.EnterpriseUser.EmployeeNumber = operation.Value?.ToString(); break;
                    case "department": user.EnterpriseUser.Department = operation.Value?.ToString(); break;
                    case "costcenter": user.EnterpriseUser.CostCenter = operation.Value?.ToString(); break;
                    case "organization": user.EnterpriseUser.Organization = operation.Value?.ToString(); break;
                    case "division": user.EnterpriseUser.Division = operation.Value?.ToString(); break;
                    case "manager": 
                        var managerAdd = await DeserializeManagerAsync(operation.Value, customerId);
                        if (EnableStrictManagerValidation && managerAdd != null && !string.IsNullOrEmpty(managerAdd.Value))
                        {
                            // Validate that the manager exists
                            if (!await ValidateManagerExistsAsync(managerAdd.Value, customerId))
                            {
                                throw new InvalidOperationException($"Manager with ID '{managerAdd.Value}' does not exist.");
                            }
                        }
                        user.EnterpriseUser.Manager = managerAdd; 
                        break;
                }
                return;
            }
            // Flat attributes
            switch (path.ToLower())
            {
                case "username": user.UserName = operation.Value?.ToString() ?? string.Empty; break;
                case "active":
                    if (operation.Value is bool b) user.Active = b;
                    else if (operation.Value is string s) user.Active = bool.TryParse(s, out var result) && result;
                    else if (operation.Value != null && bool.TryParse(operation.Value.ToString(), out var result2)) user.Active = result2;
                    break;
                case "displayname": user.DisplayName = operation.Value?.ToString(); break;
                case "title": user.Title = operation.Value?.ToString(); break;
                case "preferredlanguage": user.PreferredLanguage = operation.Value?.ToString(); break;
                case "usertype": user.UserType = operation.Value?.ToString(); break;
                case "nickname": user.NickName = operation.Value?.ToString(); break;
                case "locale": user.Locale = operation.Value?.ToString(); break;
                case "timezone": user.Timezone = operation.Value?.ToString(); break;
                case "profileurl": user.ProfileUrl = operation.Value?.ToString(); break;
                case "roles":
                    // Handle roles addition for multi-valued attribute
                    if (user.Roles == null) user.Roles = new List<Role>();
                    
                    if (operation.Value != null)
                    {
                        // Handle different value types
                        if (operation.Value is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var roleJson = elem.GetRawText();
                            var newRole = System.Text.Json.JsonSerializer.Deserialize<Role>(roleJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (newRole != null) user.Roles.Add(newRole);
                        }
                        else if (operation.Value is string jsonString)
                        {
                            // Handle JSON string - could be single object or array
                            try
                            {
                                using var document = JsonDocument.Parse(jsonString);
                                if (document.RootElement.ValueKind == JsonValueKind.Array)
                                {
                                    // Handle array of roles
                                    var roles = JsonSerializer.Deserialize<Role[]>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (roles != null)
                                    {
                                        foreach (var role in roles)
                                        {
                                            if (role != null) user.Roles.Add(role);
                                        }
                                    }
                                }
                                else if (document.RootElement.ValueKind == JsonValueKind.Object)
                                {
                                    // Handle single role object
                                    var newRole = JsonSerializer.Deserialize<Role>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (newRole != null) user.Roles.Add(newRole);
                                }
                            }
                            catch (JsonException)
                            {
                                // If JSON parsing fails, treat as simple string value
                                user.Roles.Add(new Role { Value = jsonString });
                            }
                        }
                        else if (operation.Value is Dictionary<string, object> dict)
                        {
                            var newRole = new Role();
                            if (dict.ContainsKey("display")) newRole.Display = dict["display"]?.ToString();
                            if (dict.ContainsKey("value")) newRole.Value = dict["value"]?.ToString() ?? string.Empty;
                            if (dict.ContainsKey("type")) newRole.Type = dict["type"]?.ToString() ?? string.Empty;
                            if (dict.ContainsKey("primary")) newRole.Primary = dict["primary"]?.ToString();
                            user.Roles.Add(newRole);
                        }
                        else
                        {
                            // Handle anonymous objects using reflection
                            var valueType = operation.Value.GetType();
                            var properties = valueType.GetProperties();
                            var newRole = new Role();
                            
                            foreach (var prop in properties)
                            {
                                var propName = prop.Name.ToLower();
                                var propValue = prop.GetValue(operation.Value);
                                
                                switch (propName)
                                {
                                    case "display": newRole.Display = propValue?.ToString(); break;
                                    case "value": newRole.Value = propValue?.ToString() ?? string.Empty; break;
                                    case "type": newRole.Type = propValue?.ToString() ?? string.Empty; break;
                                    case "primary": newRole.Primary = propValue?.ToString(); break;
                                }
                            }
                            
                            user.Roles.Add(newRole);
                        }
                    }
                    break;
            }
        }

        private void ApplyRemoveOperation(ScimUser user, PatchOperation operation)
        {
            if (string.IsNullOrEmpty(operation.Path))
                return;

            var path = operation.Path.ToLower();
            
            // Handle enterprise extension manager removal
            if (path == "urn:ietf:params:scim:schemas:extension:enterprise:2.0:user:manager")
            {
                if (user.EnterpriseUser != null)
                {
                    user.EnterpriseUser.Manager = null;
                }
                return;
            }

            // Handle multi-valued attribute removal for roles (e.g., roles[primary eq "True"])
            var mvRemoveMatch = System.Text.RegularExpressions.Regex.Match(path, @"^(\w+)\s*\[\s*(\w+)\s+eq\s+""([\w ]+)""\s*\]$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (mvRemoveMatch.Success)
            {
                var collection = mvRemoveMatch.Groups[1].Value.ToLower();
                var filterAttr = mvRemoveMatch.Groups[2].Value;
                var filterValue = mvRemoveMatch.Groups[3].Value;
                if (collection == "roles" && user.Roles != null)
                {
                    var toRemove = user.Roles.Where(r => {
                        var prop = typeof(Role).GetProperty(filterAttr, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (prop == null) return false;
                        var rawVal = prop.GetValue(r);
                        var filter = (filterValue ?? string.Empty).Trim().ToLowerInvariant();
                        if (filterAttr.Equals("primary", System.StringComparison.OrdinalIgnoreCase))
                        {
                            // Special case for Primary: compare as string and as bool
                            var valStr = (rawVal?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
                            if (valStr == filter) return true;
                            if ((valStr == "true" && filter == "true") || (valStr == "false" && filter == "false")) return true;
                            if (bool.TryParse(valStr, out var valBool) && bool.TryParse(filter, out var filterBool))
                                return valBool == filterBool;
                        }
                        else
                        {
                            var val = (rawVal?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
                            if (val == filter) return true;
                        }
                        return false;
                    }).ToList();
                    foreach (var role in toRemove)
                    {
                        user.Roles.Remove(role);
                        // If using EF and roles are tracked, also remove from context
                        try { _context.Entry(role).State = EntityState.Deleted; } catch { /* ignore if not tracked */ }
                    }
                }
                // Add similar logic for other collections if needed
                return;
            }
        }

        private async Task ApplyReplaceOperationAsync(ScimUser user, PatchOperation operation, string customerId)
        {
            // If path is null or empty, treat as full object replace (RFC 7644 3.5.2.2)
            if (string.IsNullOrEmpty(operation.Path))
            {
                if (operation.Value is Newtonsoft.Json.Linq.JObject jObj)
                {
                    foreach (var prop in jObj.Properties())
                    {
                        var subOp = new PatchOperation { Op = "replace", Path = prop.Name, Value = prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.Null ? null : prop.Value.ToObject<object>() };
                        await ApplyReplaceOperationAsync(user, subOp, customerId);
                    }
                }
                else if (operation.Value is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in elem.EnumerateObject())
                    {
                        object? value = null;
                        if (prop.Value.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            value = prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object || prop.Value.ValueKind == System.Text.Json.JsonValueKind.Array
                                ? System.Text.Json.JsonSerializer.Deserialize<object>(prop.Value.GetRawText())
                                : prop.Value.ToString();
                        }
                        var subOp = new PatchOperation { Op = "replace", Path = prop.Name, Value = value };
                        await ApplyReplaceOperationAsync(user, subOp, customerId);
                    }
                }
                else if (operation.Value is Dictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        var subOp = new PatchOperation { Op = "replace", Path = kvp.Key, Value = kvp.Value };
                        await ApplyReplaceOperationAsync(user, subOp, customerId);
                    }
                }
                return;
            }

            var path = operation.Path;
            // Handle dot notation for nested attributes
            if (path.StartsWith("name."))
            {
                if (user.Name == null) user.Name = new Name();
                var sub = path.Substring(5).ToLower();
                switch (sub)
                {
                    case "givenname": user.Name.GivenName = operation.Value?.ToString(); break;
                    case "familyname": user.Name.FamilyName = operation.Value?.ToString(); break;
                    case "formatted": user.Name.Formatted = operation.Value?.ToString(); break;
                    case "middlename": user.Name.MiddleName = operation.Value?.ToString(); break;
                    case "honorificprefix": user.Name.HonorificPrefix = operation.Value?.ToString(); break;
                    case "honorificsuffix": user.Name.HonorificSuffix = operation.Value?.ToString(); break;
                }
                return;
            }
            // Handle multi-valued attributes with filter, e.g. emails[type eq "work"].value, roles[primary eq "True"].display
            var mvMatch = System.Text.RegularExpressions.Regex.Match(path, @"^(\w+)\[(\w+) eq ""([\w ]+)""\]\.(\w+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (mvMatch.Success)
            {
                var collection = mvMatch.Groups[1].Value.ToLower();
                var filterAttr = mvMatch.Groups[2].Value.ToLower();
                var filterValue = mvMatch.Groups[3].Value;
                var attr = mvMatch.Groups[4].Value.ToLower();
                if (collection == "emails")
                {
                    var email = user.Emails?.FirstOrDefault(e =>
                        filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase));
                    if (email != null)
                    {
                        if (attr == "value") email.Value = operation.Value?.ToString() ?? string.Empty;
                        else if (attr == "primary") email.Primary = operation.Value is bool b ? b : bool.TryParse(operation.Value?.ToString(), out var pb) && pb;
                    }
                }
                else if (collection == "phonenumbers")
                {
                    var phone = user.PhoneNumbers?.FirstOrDefault(e =>
                        filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase));
                    if (phone != null)
                    {
                        if (attr == "value") phone.Value = operation.Value?.ToString() ?? string.Empty;
                        else if (attr == "primary") phone.Primary = operation.Value is bool b ? b : bool.TryParse(operation.Value?.ToString(), out var pb) && pb;
                    }
                }
                else if (collection == "addresses")
                {
                    var addr = user.Addresses?.FirstOrDefault(e =>
                        filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase));
                    if (addr != null)
                    {
                        if (attr == "formatted") addr.Formatted = operation.Value?.ToString();
                        else if (attr == "streetaddress") addr.StreetAddress = operation.Value?.ToString();
                        else if (attr == "locality") addr.Locality = operation.Value?.ToString();
                        else if (attr == "region") addr.Region = operation.Value?.ToString();
                        else if (attr == "postalcode") addr.PostalCode = operation.Value?.ToString();
                        else if (attr == "country") addr.Country = operation.Value?.ToString();
                        else if (attr == "primary") addr.Primary = operation.Value is bool b ? b : bool.TryParse(operation.Value?.ToString(), out var pb) && pb;
                    }
                }
                else if (collection == "roles")
                {
                    if (user.Roles != null)
                    {
                        // Use explicit property matching instead of reflection for consistency
                        Role? role = null;
                        if (filterAttr.Equals("primary", StringComparison.OrdinalIgnoreCase))
                        {
                            role = user.Roles.FirstOrDefault(r => string.Equals(r.Primary, filterValue, StringComparison.OrdinalIgnoreCase));
                        }
                        else if (filterAttr.Equals("type", StringComparison.OrdinalIgnoreCase))
                        {
                            role = user.Roles.FirstOrDefault(r => string.Equals(r.Type, filterValue, StringComparison.OrdinalIgnoreCase));
                        }
                        else if (filterAttr.Equals("value", StringComparison.OrdinalIgnoreCase))
                        {
                            role = user.Roles.FirstOrDefault(r => string.Equals(r.Value, filterValue, StringComparison.OrdinalIgnoreCase));
                        }
                        else if (filterAttr.Equals("display", StringComparison.OrdinalIgnoreCase))
                        {
                            role = user.Roles.FirstOrDefault(r => string.Equals(r.Display, filterValue, StringComparison.OrdinalIgnoreCase));
                        }
                        
                        if (role != null)
                        {
                            if (attr.Equals("display", StringComparison.OrdinalIgnoreCase))
                                role.Display = operation.Value?.ToString();
                            else if (attr.Equals("value", StringComparison.OrdinalIgnoreCase))
                                role.Value = operation.Value?.ToString() ?? string.Empty;
                            else if (attr.Equals("type", StringComparison.OrdinalIgnoreCase))
                                role.Type = operation.Value?.ToString() ?? string.Empty;
                            else if (attr.Equals("primary", StringComparison.OrdinalIgnoreCase))
                                role.Primary = operation.Value?.ToString();
                        }
                    }
                }
                return;
            }
            // Handle enterprise extension fields (case-insensitive)
            var entPrefix = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:";
            if (path.StartsWith(entPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (user.EnterpriseUser == null) user.EnterpriseUser = new EnterpriseUser();
                var sub = path.Substring(entPrefix.Length).ToLower();
                switch (sub)
                {
                    case "employeenumber": user.EnterpriseUser.EmployeeNumber = operation.Value?.ToString(); break;
                    case "department": user.EnterpriseUser.Department = operation.Value?.ToString(); break;
                    case "costcenter": user.EnterpriseUser.CostCenter = operation.Value?.ToString(); break;
                    case "organization": user.EnterpriseUser.Organization = operation.Value?.ToString(); break;
                    case "division": user.EnterpriseUser.Division = operation.Value?.ToString(); break;
                    case "manager": 
                        var managerReplace = await DeserializeManagerAsync(operation.Value, customerId);
                        if (EnableStrictManagerValidation && managerReplace != null && !string.IsNullOrEmpty(managerReplace.Value))
                        {
                            // Validate that the manager exists
                            if (!await ValidateManagerExistsAsync(managerReplace.Value, customerId))
                            {
                                throw new InvalidOperationException($"Manager with ID '{managerReplace.Value}' does not exist.");
                            }
                        }
                        user.EnterpriseUser.Manager = managerReplace; 
                        break;
                }
                return;
            }
            // Flat attributes
            switch (path.ToLower())
            {
                case "username": user.UserName = operation.Value?.ToString() ?? string.Empty; break;
                case "active":
                    if (operation.Value is bool b) user.Active = b;
                    else if (operation.Value is string s) user.Active = bool.TryParse(s, out var result) && result;
                    else if (operation.Value != null && bool.TryParse(operation.Value.ToString(), out var result2)) user.Active = result2;
                    break;
                case "displayname": user.DisplayName = operation.Value?.ToString(); break;
                case "title": user.Title = operation.Value?.ToString(); break;
                case "preferredlanguage": user.PreferredLanguage = operation.Value?.ToString(); break;
                case "usertype": user.UserType = operation.Value?.ToString(); break;
                case "nickname": user.NickName = operation.Value?.ToString(); break;
                case "locale": user.Locale = operation.Value?.ToString(); break;
                case "timezone": user.Timezone = operation.Value?.ToString(); break;
                case "profileurl": user.ProfileUrl = operation.Value?.ToString(); break;
            }
        }

        private Manager? DeserializeManager(object? value)
        {
            if (value == null) return null;

            try
            {
                // If it's already a Manager object, return it
                if (value is Manager manager) 
                {
                    return manager;
                }

                // If it's a JsonElement, deserialize it
                if (value is System.Text.Json.JsonElement element)
                {
                    if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var elementString = element.GetString();
                        if (elementString != null)
                        {
                            // Check if the string contains JSON (starts with { and ends with })
                            if (elementString.TrimStart().StartsWith("{") && elementString.TrimEnd().EndsWith("}"))
                            {
                                try
                                {
                                    // Use default JsonSerializer options (JsonPropertyName attributes should work)
                                    var result = System.Text.Json.JsonSerializer.Deserialize<Manager>(elementString);
                                    
                                    // Ensure $ref is populated if we have a value but no ref
                                    if (result != null && !string.IsNullOrEmpty(result.Value) && string.IsNullOrEmpty(result.Ref))
                                    {
                                        result.Ref = $"../Users/{result.Value}";
                                    }
                                    
                                    return result;
                                }
                                catch (System.Text.Json.JsonException)
                                {
                                    return new Manager 
                                    { 
                                        Value = elementString,
                                        Ref = $"../Users/{elementString}"
                                    };
                                }
                            }
                            else
                            {
                                // Simple string value
                                return new Manager 
                                { 
                                    Value = elementString,
                                    Ref = $"../Users/{elementString}"
                                };
                            }
                        }
                        return null;
                    }
                    else if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var rawText = element.GetRawText();
                        
                        // Create JsonSerializerOptions to handle property names properly
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        
                        var result = System.Text.Json.JsonSerializer.Deserialize<Manager>(rawText, options);
                        
                        // Ensure $ref is populated if we have a value but no ref
                        if (result != null && !string.IsNullOrEmpty(result.Value) && string.IsNullOrEmpty(result.Ref))
                        {
                            result.Ref = $"../Users/{result.Value}";
                        }
                        
                        return result;
                    }
                    else
                    {
                        var result = System.Text.Json.JsonSerializer.Deserialize<Manager>(element.GetRawText());
                        
                        // Ensure $ref is populated if we have a value but no ref
                        if (result != null && !string.IsNullOrEmpty(result.Value) && string.IsNullOrEmpty(result.Ref))
                        {
                            result.Ref = $"../Users/{result.Value}";
                        }
                        
                        return result;
                    }
                }

                // If it's a string, check if it's JSON or a simple value
                if (value is string stringValue)
                {
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        // Try to parse as JSON first
                        if (stringValue.TrimStart().StartsWith("{") && stringValue.TrimEnd().EndsWith("}"))
                        {
                            try
                            {
                                // Use default JsonSerializer options (JsonPropertyName attributes should work)
                                var result = System.Text.Json.JsonSerializer.Deserialize<Manager>(stringValue);
                                
                                // Ensure $ref is populated if we have a value but no ref
                                if (result != null && !string.IsNullOrEmpty(result.Value) && string.IsNullOrEmpty(result.Ref))
                                {
                                    result.Ref = $"../Users/{result.Value}";
                                }
                                
                                return result;
                            }
                            catch (System.Text.Json.JsonException)
                            {
                                // If JSON parsing fails, treat as a simple string value
                                return new Manager 
                                { 
                                    Value = stringValue,
                                    Ref = $"../Users/{stringValue}"
                                };
                            }
                        }
                        else
                        {
                            // Simple string value - legacy support
                            return new Manager 
                            { 
                                Value = stringValue,
                                Ref = $"../Users/{stringValue}"
                            };
                        }
                    }
                    return null;
                }

                // Handle anonymous objects or complex types by serializing to JSON first then deserializing
                try
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(value);
                    var result = System.Text.Json.JsonSerializer.Deserialize<Manager>(jsonString);
                    
                    // Ensure $ref is populated if we have a value but no ref
                    if (result != null && !string.IsNullOrEmpty(result.Value) && string.IsNullOrEmpty(result.Ref))
                    {
                        result.Ref = $"../Users/{result.Value}";
                    }
                    
                    return result;
                }
                catch (System.Text.Json.JsonException)
                {
                    // If JSON serialization/deserialization fails, treat as string value for backward compatibility
                    var fallbackValue = value.ToString();
                    if (!string.IsNullOrEmpty(fallbackValue))
                    {
                        return new Manager 
                        { 
                            Value = fallbackValue,
                            Ref = $"../Users/{fallbackValue}"
                        };
                    }
                }
            }
            catch (Exception)
            {
                // If anything else fails, treat as string value for backward compatibility
                var fallbackValue = value.ToString();
                if (!string.IsNullOrEmpty(fallbackValue))
                {
                    return new Manager { Value = fallbackValue };
                }
            }

            return null;
        }

        private async Task<bool> ValidateManagerExistsAsync(string managerId, string customerId)
        {
            if (string.IsNullOrEmpty(managerId))
                return false;

            var manager = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == managerId && u.CustomerId == customerId);
            
            return manager != null;
        }

        private async Task<Manager?> DeserializeManagerAsync(object? value, string customerId)
        {
            if (value == null) return null;

            Manager? manager = null;

            try
            {
                // If it's already a Manager object, use it
                if (value is Manager existingManager) 
                {
                    manager = existingManager;
                }
                // Handle JsonElement deserialization
                else if (value is System.Text.Json.JsonElement element)
                {
                    manager = await DeserializeManagerFromJsonElement(element);
                }
                // Handle string values
                else if (value is string stringValue)
                {
                    manager = await DeserializeManagerFromString(stringValue);
                }
                // Handle other object types using reflection
                else
                {
                    manager = DeserializeManagerFromObject(value);
                }

                // Validate that the referenced manager exists (only if strict validation is enabled)
                if (EnableStrictManagerValidation && manager != null && !string.IsNullOrEmpty(manager.Value))
                {
                    var managerExists = await ValidateManagerExistsAsync(manager.Value, customerId);
                    if (!managerExists)
                    {
                        throw new InvalidOperationException($"Referenced manager with ID '{manager.Value}' does not exist or is not accessible.");
                    }
                }

                return manager;
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize manager: {ex.Message}", ex);
            }
        }

        private async Task<Manager?> DeserializeManagerFromJsonElement(System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var elementString = element.GetString();
                return await DeserializeManagerFromString(elementString);
            }
            else if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var rawText = element.GetRawText();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var result = System.Text.Json.JsonSerializer.Deserialize<Manager>(rawText, options);
                
                // Ensure $ref is populated if we have a value but no ref
                if (result != null && !string.IsNullOrEmpty(result.Value) && string.IsNullOrEmpty(result.Ref))
                {
                    result.Ref = $"../Users/{result.Value}";
                }
                
                return result;
            }
            else
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<Manager>(element.GetRawText());
                
                // Ensure $ref is populated if we have a value but no ref
                if (result != null && !string.IsNullOrEmpty(result.Value) && string.IsNullOrEmpty(result.Ref))
                {
                    result.Ref = $"../Users/{result.Value}";
                }
                
                return result;
            }
        }

        private Task<Manager?> DeserializeManagerFromString(string? stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
                return Task.FromResult<Manager?>(null);

            // Try to parse as JSON first
            if (stringValue.TrimStart().StartsWith("{") && stringValue.TrimEnd().EndsWith("}"))
            {
                try
                {
                    var result = System.Text.Json.JsonSerializer.Deserialize<Manager>(stringValue);
                    
                    // Ensure $ref is populated if we have a value but no ref
                    if (result != null && !string.IsNullOrEmpty(result.Value) && string.IsNullOrEmpty(result.Ref))
                    {
                        result.Ref = $"../Users/{result.Value}";
                    }
                    
                    return Task.FromResult<Manager?>(result);
                }
                catch (System.Text.Json.JsonException)
                {
                    // Fall through to simple string handling
                }
            }
            
            // Simple string value - treat as manager ID
            return Task.FromResult<Manager?>(new Manager 
            { 
                Value = stringValue,
                Ref = $"../Users/{stringValue}"
            });
        }

        private Manager? DeserializeManagerFromObject(object value)
        {
            var manager = new Manager();
            var valueType = value.GetType();
            var properties = valueType.GetProperties();
            
            foreach (var prop in properties)
            {
                var fieldName = prop.Name.ToLower();
                var propValue = prop.GetValue(value);
                
                switch (fieldName)
                {
                    case "value": manager.Value = propValue?.ToString(); break;
                    case "$ref":
                    case "ref": manager.Ref = propValue?.ToString(); break;
                    case "displayname": manager.DisplayName = propValue?.ToString(); break;
                }
            }
            
            // Ensure $ref is populated if we have a value but no ref
            if (!string.IsNullOrEmpty(manager.Value) && string.IsNullOrEmpty(manager.Ref))
            {
                manager.Ref = $"../Users/{manager.Value}";
            }
            
            return manager;
        }



        private IQueryable<ScimUser> ApplySorting(IQueryable<ScimUser> query, string sortBy, string? sortOrder = null)
        {
            var ascending = string.IsNullOrEmpty(sortOrder) || 
                           string.Equals(sortOrder, "ascending", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "username" => ascending ? query.OrderBy(u => u.UserName) : query.OrderByDescending(u => u.UserName),
                "displayname" => ascending ? query.OrderBy(u => u.DisplayName) : query.OrderByDescending(u => u.DisplayName),
                "created" => ascending ? query.OrderBy(u => u.Created) : query.OrderByDescending(u => u.Created),
                "lastmodified" => ascending ? query.OrderBy(u => u.LastModified) : query.OrderByDescending(u => u.LastModified),
                "name.givenname" => ascending ? query.OrderBy(u => u.Name!.GivenName) : query.OrderByDescending(u => u.Name!.GivenName),
                "name.familyname" => ascending ? query.OrderBy(u => u.Name!.FamilyName) : query.OrderByDescending(u => u.Name!.FamilyName),
                _ => query // Default: no sorting if sortBy is not recognized
            };
        }

        private void ApplyAttributeSelection(ScimUser user, string? attributes, string? excludedAttributes)
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
            if (!ShouldInclude("username")) user.UserName = null!;
            if (!ShouldInclude("displayname")) user.DisplayName = null;
            if (!ShouldInclude("nickname")) user.NickName = null;
            if (!ShouldInclude("profileurl")) user.ProfileUrl = null;
            if (!ShouldInclude("title")) user.Title = null;
            if (!ShouldInclude("usertype")) user.UserType = null;
            if (!ShouldInclude("preferredlanguage")) user.PreferredLanguage = null;
            if (!ShouldInclude("locale")) user.Locale = null;
            if (!ShouldInclude("timezone")) user.Timezone = null;
            // Note: Active is non-nullable bool, so we can't set it to null for attribute filtering
            if (!ShouldInclude("password")) user.Password = null;
            if (!ShouldInclude("externalid")) user.ExternalId = null;
            
            if (!ShouldInclude("name")) user.Name = null;
            if (!ShouldInclude("emails")) user.Emails = null;
            if (!ShouldInclude("phonenumbers")) user.PhoneNumbers = null;
            if (!ShouldInclude("addresses")) user.Addresses = null;
            if (!ShouldInclude("roles")) user.Roles = null;
            if (!ShouldInclude("groups")) user.Groups = null;
            
            // Handle enterprise extension
            if (!ShouldInclude("urn:ietf:params:scim:schemas:extension:enterprise:2.0:user") && 
                !ShouldInclude("enterpriseuser"))
            {
                user.EnterpriseUser = null;
                // Remove enterprise schema if present
                if (user.Schemas != null)
                {
                    user.Schemas = user.Schemas.Where(s => s != "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User").ToList();
                }
            }
        }
    }
}
