using System.ComponentModel.DataAnnotations;

namespace URMARRY.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Please provide a valid email address")]
        public string? Email { get; set; }
    }
}
