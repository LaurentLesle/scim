using ScimServiceProvider.Models;

namespace ScimServiceProvider.Services
{
    public interface IUserService
    {
        Task<ScimUser?> GetUserAsync(string id);
        Task<ScimUser?> GetUserByUsernameAsync(string username);
        Task<ScimListResponse<ScimUser>> GetUsersAsync(int startIndex = 1, int count = 10, string? filter = null);
        Task<ScimUser> CreateUserAsync(ScimUser user);
        Task<ScimUser?> UpdateUserAsync(string id, ScimUser user);
        Task<ScimUser?> PatchUserAsync(string id, ScimPatchRequest patchRequest);
        Task<bool> DeleteUserAsync(string id);
    }
}
