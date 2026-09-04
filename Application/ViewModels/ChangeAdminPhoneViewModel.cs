using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels;

public class ChangeAdminPhoneViewModel
{
    public string? CurrentPhone { get; set; }

    [Required(ErrorMessage = "Please enter a new phone number.")]
    [RegularExpression(@"^[0-9]{8,15}$", ErrorMessage = "Please enter a valid phone number (8 to 15 digits).")]
    public string NewPhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password to confirm.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
