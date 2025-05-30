using ScimServiceProvider.Models;

namespace ScimServiceProvider.Services
{
    public interface IGroupService
    {
        Task<ScimGroup?> GetGroupAsync(string id, string customerId);
        Task<ScimListResponse<ScimGroup>> GetGroupsAsync(string customerId, int startIndex = 1, int count = 10, string? filter = null);
        Task<ScimGroup> CreateGroupAsync(ScimGroup group, string customerId);
        Task<ScimGroup?> UpdateGroupAsync(string id, ScimGroup group, string customerId);
        Task<ScimGroup?> PatchGroupAsync(string id, ScimPatchRequest patchRequest, string customerId);
        Task<bool> DeleteGroupAsync(string id, string customerId);
    }
}
