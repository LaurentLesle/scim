using ScimServiceProvider.Models;

namespace ScimServiceProvider.Services
{
    public interface IUserService
    {
        Task<ScimUser?> GetUserAsync(string id, string customerId);
        Task<ScimUser?> GetUserByUsernameAsync(string username, string customerId);
        Task<ScimListResponse<ScimUser>> GetUsersAsync(string customerId, int startIndex = 1, int count = 10, string? filter = null, string? attributes = null, string? excludedAttributes = null, string? sortBy = null, string? sortOrder = null);
        Task<ScimUser> CreateUserAsync(ScimUser user, string customerId);
        Task<ScimUser?> UpdateUserAsync(string id, ScimUser user, string customerId);
        Task<ScimUser?> PatchUserAsync(string id, ScimPatchRequest patchRequest, string customerId);
        Task<bool> DeleteUserAsync(string id, string customerId);
    }
}
