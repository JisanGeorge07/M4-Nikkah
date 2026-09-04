using System.ComponentModel.DataAnnotations;

namespace Application.Models
{
    public class StaffVerifyOtpRequest
    {
        [Required(ErrorMessage = "Session token is required.")]
        public string TempToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP must contain only digits.")]
        public string Otp { get; set; } = string.Empty;
    }

    public class StaffResendOtpRequest
    {
        [Required(ErrorMessage = "Session token is required.")]
        public string TempToken { get; set; } = string.Empty;
    }
}
