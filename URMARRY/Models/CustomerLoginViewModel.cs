using System.ComponentModel.DataAnnotations;

namespace URMARRY.Models
{
	public class CustomerLoginViewModel
	{
		[Required(ErrorMessage = "Email is required")]
		[DataType(DataType.EmailAddress, ErrorMessage = "Please provide a valid email address")]
		public string LoginEmail { get; set; } = null!;

		[Required(ErrorMessage = "Password is required")]
		public string LoginPassword { get; set; } = null!;

		public bool RememberMe { get; set; }
	}
}
