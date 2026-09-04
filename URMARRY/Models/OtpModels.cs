using System.ComponentModel.DataAnnotations;

namespace URMARRY.Models
{
    public class OtpSendRequest
    {
        [Required]
        public string Type { get; set; } = null!; // "email" or "mobile"

        [Required]
        public string Identifier { get; set; } = null!; // email address or phone number
    }

    public class OtpVerifyRequest
    {
        [Required]
        public string Type { get; set; } = null!; // "email" or "mobile"

        [Required]
        public string Identifier { get; set; } = null!; // email address or phone number

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
        public string Otp { get; set; } = null!;
    }
}
