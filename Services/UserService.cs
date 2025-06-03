using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Data;
using ScimServiceProvider.Models;
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

        public async Task<ScimUser?> PatchUserAsync(string id, ScimPatchRequest patchRequest, string customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);
                
            if (user == null)
                return null;

            foreach (var operation in patchRequest.Operations)
            {
                ApplyPatchOperation(user, operation);
            }

            user.LastModified = DateTime.UtcNow;
            user.Meta.LastModified = user.LastModified;

            // Ensure proper schemas are set after patching
            user.Schemas = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" };
            if (user.EnterpriseUser != null)
            {
                if (!user.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                {
                    user.Schemas.Add("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
                }
            }

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
                        user.Emails = updatedUser.Emails;
                        user.PhoneNumbers = updatedUser.PhoneNumbers;
                        user.Addresses = updatedUser.Addresses;
                        user.Roles = updatedUser.Roles;
                        user.EnterpriseUser = updatedUser.EnterpriseUser;
                    }
                }
                else if (operation.Value is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var updatedUser = System.Text.Json.JsonSerializer.Deserialize<ScimUser>(elem.GetRawText());
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
                        user.Emails = updatedUser.Emails;
                        user.PhoneNumbers = updatedUser.PhoneNumbers;
                        user.Addresses = updatedUser.Addresses;
                        user.Roles = updatedUser.Roles;
                        user.EnterpriseUser = updatedUser.EnterpriseUser;
                    }
                }
                else if (operation.Value is Dictionary<string, object> dict)
                {
                    // For each key-value, treat as a separate add operation
                    foreach (var kvp in dict)
                    {
                        var subOp = new PatchOperation { Op = "add", Path = kvp.Key, Value = kvp.Value };
                        ApplyAddOperation(user, subOp);
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
                    if (user.Emails == null) user.Emails = new List<Email>();
                    var email = user.Emails.FirstOrDefault(e => (filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase)));
                    if (email == null)
                    {
                        email = new Email { Type = filterAttr == "type" ? filterValue : "work" };
                        user.Emails.Add(email);
                    }
                    if (attr == "value") email.Value = operation.Value?.ToString() ?? string.Empty;
                    else if (attr == "primary") email.Primary = operation.Value is bool b ? b : bool.TryParse(operation.Value?.ToString(), out var pb) && pb;
                }
                else if (collection == "phonenumbers")
                {
                    if (user.PhoneNumbers == null) user.PhoneNumbers = new List<PhoneNumber>();
                    var phone = user.PhoneNumbers.FirstOrDefault(e => (filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase)));
                    if (phone == null)
                    {
                        phone = new PhoneNumber { Type = filterAttr == "type" ? filterValue : "work" };
                        user.PhoneNumbers.Add(phone);
                    }
                    if (attr == "value") phone.Value = operation.Value?.ToString() ?? string.Empty;
                    else if (attr == "primary") phone.Primary = operation.Value is bool b ? b : bool.TryParse(operation.Value?.ToString(), out var pb) && pb;
                }
                else if (collection == "addresses")
                {
                    if (user.Addresses == null) user.Addresses = new List<Address>();
                    var addr = user.Addresses.FirstOrDefault(e => (filterAttr == "type" && e.Type.Equals(filterValue, StringComparison.OrdinalIgnoreCase)));
                    if (addr == null)
                    {
                        addr = new Address { Type = filterAttr == "type" ? filterValue : "work" };
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
                    // Support filter on any attribute, not just primary
                    Role? role = null;
                    foreach (var r in user.Roles)
                    {
                        var prop = typeof(Role).GetProperty(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filterAttr));
                        if (prop != null && (prop.GetValue(r)?.ToString() ?? "") == filterValue)
                        {
                            role = r;
                            break;
                        }
                    }
                    if (role == null)
                    {
                        role = new Role();
                        // Set the filter attribute
                        var prop = typeof(Role).GetProperty(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filterAttr));
                        if (prop != null)
                        {
                            prop.SetValue(role, filterValue);
                        }
                        // Set the target attribute
                        var attrProp = typeof(Role).GetProperty(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(attr));
                        if (attrProp != null)
                        {
                            attrProp.SetValue(role, operation.Value?.ToString());
                        }
                        user.Roles.Add(role);
                    }
                    else
                    {
                        var attrProp = typeof(Role).GetProperty(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(attr));
                        if (attrProp != null)
                        {
                            attrProp.SetValue(role, operation.Value?.ToString());
                        }
                    }
                }
                return;
            }
            // Handle enterprise extension fields
            var entPrefix = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:user:";
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
                    case "manager": user.EnterpriseUser.Manager = operation.Value?.ToString(); break;
                }
                return;
            }
            // Flat attributes
            switch (path.ToLower())
            {
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
            }
            // Add more remove operations as needed
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
                        var role = user.Roles.FirstOrDefault(r =>
                            {
                                var prop = typeof(Role).GetProperty(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filterAttr));
                                return prop != null && (prop.GetValue(r)?.ToString() ?? "") == filterValue;
                            });
                        if (role != null)
                        {
                            var attrProp = typeof(Role).GetProperty(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(attr));
                            if (attrProp != null)
                            {
                                attrProp.SetValue(role, operation.Value?.ToString());
                            }
                        }
                    }
                }
                return;
            }
            // Handle enterprise extension fields
            var entPrefix = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:user:";
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
                    case "manager": user.EnterpriseUser.Manager = operation.Value?.ToString(); break;
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
    }
}
