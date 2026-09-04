using Application.Models.Common;

namespace Application.Models
{
    public class ContactEnquiryDto : BaseDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public string? Purpose { get; set; }
    }
}
