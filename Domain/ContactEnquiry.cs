using Domain.Common;

namespace Domain
{
    public class ContactEnquiry : BaseEntity
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public string? Purpose { get; set; }
    }
}
