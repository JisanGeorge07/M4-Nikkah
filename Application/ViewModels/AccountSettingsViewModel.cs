namespace Application.ViewModels;

public class AccountSettingsViewModel
{
    public long UserId { get; set; }
    public string? Email { get; set; }
    public string? OldPassword { get; set; }
    public string? Password { get; set; }
    public string? CPassword { get; set; }
}