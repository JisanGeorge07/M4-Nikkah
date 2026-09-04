namespace Application.Models
{
    public class UpdatePhoneOrEmailOtpRequest
    {
        public long Id { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? CountryCode { get; set; }
    }
}
