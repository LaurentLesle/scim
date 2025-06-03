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

        public UserService(ScimDbContext context)
        {
            _context = context;
        }

        public async Task<ScimUser?> GetUserAsync(string id, string customerId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);
        }

        public async Task<ScimUser?> GetUserByUsernameAsync(string username, string customerId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username && u.CustomerId == customerId);
        }

        public async Task<ScimListResponse<ScimUser>> GetUsersAsync(string customerId, int startIndex = 1, int count = 10, string? filter = null)
        {
            var query = _context.Users
                .Where(u => u.CustomerId == customerId)
                .AsQueryable();

            // Apply filter if provided
            if (!string.IsNullOrEmpty(filter))
            {
                query = ApplyFilter(query, filter);
            }

            var totalResults = await query.CountAsync();
            var users = await query
                .Skip(startIndex - 1)
                .Take(count)
                .ToListAsync();

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
                        ApplyReplaceOperation(user, operation);
                        break;
                    case "add":
                        ApplyAddOperation(user, operation);
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
            }

            // Add more filter implementations as needed
            return query;
        }

        private void ApplyPatchOperation(ScimUser user, PatchOperation operation)
        {
            switch (operation.Op.ToLower())
            {
                case "replace":
                    ApplyReplaceOperation(user, operation);
                    break;
                case "add":
                    ApplyAddOperation(user, operation);
                    break;
                case "remove":
                    ApplyRemoveOperation(user, operation);
                    break;
            }
        }

        private void ApplyAddOperation(ScimUser user, PatchOperation operation)
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
                                case "manager": user.EnterpriseUser.Manager = DeserializeManager(kvp.Value); break;
                            }
                        }
                        else
                        {
                            var subOp = new PatchOperation { Op = "add", Path = kvp.Key, Value = kvp.Value };
                            ApplyAddOperation(user, subOp);
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
                    case "manager": user.EnterpriseUser.Manager = DeserializeManager(operation.Value); break;
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

        private void ApplyReplaceOperation(ScimUser user, PatchOperation operation)
        {
            // If path is null or empty, treat as full object replace (RFC 7644 3.5.2.2)
            if (string.IsNullOrEmpty(operation.Path))
            {
                if (operation.Value is Newtonsoft.Json.Linq.JObject jObj)
                {
                    foreach (var prop in jObj.Properties())
                    {
                        var subOp = new PatchOperation { Op = "replace", Path = prop.Name, Value = prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.Null ? null : prop.Value.ToObject<object>() };
                        ApplyReplaceOperation(user, subOp);
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
                        ApplyReplaceOperation(user, subOp);
                    }
                }
                else if (operation.Value is Dictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        var subOp = new PatchOperation { Op = "replace", Path = kvp.Key, Value = kvp.Value };
                        ApplyReplaceOperation(user, subOp);
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
                    case "manager": user.EnterpriseUser.Manager = DeserializeManager(operation.Value); break;
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
    }
}
