namespace Application.Models.Identity;

public class RoleDto
{
    public long? Id { get; set; }
    public string? Name { get; set; }

    public List<string>? Permissions { get; set; }
}