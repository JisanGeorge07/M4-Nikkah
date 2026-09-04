namespace Application.Models.Identity.ChangePassword;

public class ChangePasswordResponse
{
    public bool Succeeded { get; set; }
    public List<string>? Errors { get; set; }
}