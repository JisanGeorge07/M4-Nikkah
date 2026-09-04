namespace Application.Models
{
    public class VerifyRegistrationOtpRequest
    {
        public long ProfileId { get; set; }
        public string Otp { get; set; } = string.Empty;
    }
}
