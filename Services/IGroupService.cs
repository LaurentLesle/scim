using ScimServiceProvider.Models;

namespace ScimServiceProvider.Services
{
    public interface IGroupService
    {
        Task<ScimGroup?> GetGroupAsync(string id);
        Task<ScimListResponse<ScimGroup>> GetGroupsAsync(int startIndex = 1, int count = 10, string? filter = null);
        Task<ScimGroup> CreateGroupAsync(ScimGroup group);
        Task<ScimGroup?> UpdateGroupAsync(string id, ScimGroup group);
        Task<ScimGroup?> PatchGroupAsync(string id, ScimPatchRequest patchRequest);
        Task<bool> DeleteGroupAsync(string id);
    }
}
