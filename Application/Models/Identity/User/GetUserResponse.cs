using System.ComponentModel.DataAnnotations;

namespace Application.Models.Identity.User;

public class GetUserResponse
{
    [Required] public ApplicationUserDto? User { get; set; }

    [Required] public List<RoleDto>? AllRoles { get; set; }
}