namespace Application.Models.Identity.ChangePassword;

public class ChangePasswordRequest
{
    public string? UserId { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}