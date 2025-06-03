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
                query = query.Where(u => u.UserName == userName);
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
            if (string.IsNullOrEmpty(operation.Path))
                return;

            var path = operation.Path.ToLower();
            
            // Handle enterprise extension manager
            if (path == "urn:ietf:params:scim:schemas:extension:enterprise:2.0:user:manager")
            {
                // Ensure enterprise extension is initialized
                if (user.EnterpriseUser == null)
                {
                    user.EnterpriseUser = new EnterpriseUser();
                }
                user.EnterpriseUser.Manager = operation.Value?.ToString();
            }
            else if (path == "active")
            {
                if (operation.Value is bool b)
                {
                    user.Active = b;
                }
                else if (operation.Value is string s)
                {
                    user.Active = bool.TryParse(s, out var result) && result;
                }
                else if (operation.Value != null && bool.TryParse(operation.Value.ToString(), out var result2))
                {
                    user.Active = result2;
                }
            }
            else if (path == "displayname")
            {
                user.DisplayName = operation.Value?.ToString();
            }
            // Add more add operations as needed
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
            // Debug log for troubleshooting
            Console.WriteLine($"[DEBUG] Replace op: path={operation.Path}, value={operation.Value}");
            if (string.IsNullOrEmpty(operation.Path))
                return;

            var path = operation.Path.ToLower();
            
            if (path == "active")
            {
                // Handle bool, string, and boxed values
                if (operation.Value is bool b)
                {
                    user.Active = b;
                }
                else if (operation.Value is string s)
                {
                    user.Active = bool.TryParse(s, out var result) && result;
                }
                else if (operation.Value != null && bool.TryParse(operation.Value.ToString(), out var result2))
                {
                    user.Active = result2;
                }
            }
            else if (path == "displayname")
            {
                user.DisplayName = operation.Value?.ToString();
            }
            else if (path == "urn:ietf:params:scim:schemas:extension:enterprise:2.0:user:manager")
            {
                // Ensure enterprise extension is initialized
                if (user.EnterpriseUser == null)
                {
                    user.EnterpriseUser = new EnterpriseUser();
                }
                user.EnterpriseUser.Manager = operation.Value?.ToString();
                Console.WriteLine($"[DEBUG] Set manager to: {user.EnterpriseUser.Manager}");
            }
            // Add more replace operations as needed
        }
    }
}
