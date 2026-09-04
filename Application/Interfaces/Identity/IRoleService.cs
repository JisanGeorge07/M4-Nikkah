using Application.Models.Identity;

namespace Application.Interfaces.Identity;

public interface IRoleService
{
    public Task<List<PermissionDto>> GetAllPermissions();
    public List<RoleDto> GetAllRoles();
    public RoleDto GetRole(long id);
    public Task<long> UpdateRole(RoleDto roleDto);
    public Task<long> CreateRole(RoleDto roleDto);
    public Task<RoleDto> DeleteRole(long id);
}