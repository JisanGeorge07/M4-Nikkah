namespace Application.Models.Identity.Authentication;

public class AuthenticationResponse
{
    public long Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Token { get; set; }
    public List<string>? Permissions { get; set; }
}