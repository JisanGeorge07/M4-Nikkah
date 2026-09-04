using System.ComponentModel.DataAnnotations;

namespace Application.Models.Identity.Role;

public class GetRoleResponse
{
    [Required] public RoleDto? Role { get; set; }

    [Required] public List<string?> AllPermissions { get; set; } = new();
}