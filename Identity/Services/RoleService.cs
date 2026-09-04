using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Application.Constants;
using Application.Interfaces.Identity;
using Application.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace Identity.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole<long>> _roleManager;

    public RoleService(RoleManager<IdentityRole<long>> roleManager)
    {
        _roleManager = roleManager;
    }

    public List<RoleDto> GetAllRoles()
    {
        var roles = _roleManager.Roles.ToList();
        return roles.Select(role => new RoleDto { Id = role.Id, Name = role.Name }).ToList();
    }

    public async Task<List<PermissionDto>> GetAllPermissions()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var permissionsData = await File.ReadAllTextAsync(path + @"/Data/permissions.json");

        return JsonSerializer.Deserialize<List<PermissionDto>>(permissionsData,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PermissionDto>()
            .OrderBy(x => x.Id).ToList();
    }

    public RoleDto GetRole(long id)
    {
        var role = _roleManager.Roles.First(x => x.Id == id);
        var permissions = _roleManager.GetClaimsAsync(role).Result.Where(x => x.Type == CustomClaimTypes.Permission)
            .Select(x => x.Value);
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Permissions = permissions.ToList()
        };
    }

    public async Task<long> UpdateRole(RoleDto roleDto)
    {
        if (roleDto.Id is null) throw new Exception("Role ID cannot be null.");

        var role = await _roleManager.FindByIdAsync(roleDto.Id.Value.ToString()) ??
                   throw new Exception("Role with ID " + roleDto.Id + " does not exist.");

        role.Name = roleDto.Name;
        await _roleManager.UpdateAsync(role);

        var permissions = _roleManager.GetClaimsAsync(role).Result.Where(x => x.Type == CustomClaimTypes.Permission);
        foreach (var permission in permissions) await _roleManager.RemoveClaimAsync(role, permission);

        if (roleDto.Permissions != null)
            foreach (var permission in roleDto.Permissions)
                await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));

        return role.Id;
    }

    public async Task<long> CreateRole(RoleDto roleDto)
    {
        var role = new IdentityRole<long>
        {
            Name = roleDto.Name
        };

        await _roleManager.CreateAsync(role);

        if (roleDto.Permissions != null)
            foreach (var permission in roleDto.Permissions)
                await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));

        return role.Id;
    }

    public async Task<RoleDto> DeleteRole(long id)
    {
        var roleDto = GetRole(id);
        await _roleManager.DeleteAsync(await _roleManager.FindByIdAsync(id.ToString()) ??
                                       throw new Exception("Role with ID " + id + " does not exist."));
        return roleDto;
    }
}