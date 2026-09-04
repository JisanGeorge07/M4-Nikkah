using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels;

public enum AdminLoginStep
{
    Credentials = 1,
    OtpVerification = 2
}

public class AdminLoginStateViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [DataType(DataType.EmailAddress, ErrorMessage = "Please provide a valid email address")]
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }

    public bool RememberMe { get; set; }

    public AdminLoginStep Step { get; set; } = AdminLoginStep.Credentials;

    public string? TempToken { get; set; }

    public string? MaskedPhone { get; set; }

    public string? Otp { get; set; }

    public int CooldownSeconds { get; set; } = 60;

    public string? ErrorMessage { get; set; }

    public string? SuccessMessage { get; set; }

    public string? ReturnUrl { get; set; }
}

public class VerifyAdminOtpRequest
{
    [Required(ErrorMessage = "Session token is missing")]
    public string TempToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP must contain only digits")]
    public string Otp { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ResendAdminOtpRequest
{
    [Required(ErrorMessage = "Session token is missing")]
    public string TempToken { get; set; } = string.Empty;
}
