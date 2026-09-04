using Application.Models;
using System.ComponentModel.DataAnnotations;

namespace URMARRY.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Old Password is required")]
        public string? OldPassword { get; set; }
        [Required(ErrorMessage = "New Password is required")]
        public string? NewPassword { get; set; }
        [Required(ErrorMessage = "Confirm Password is required")]
        public string? ConfirmPassword { get; set;}
		public RegistrationDto? Registration { get; set; }
	}
}
