using Application.Models;
using Application.Models.Framework;

namespace URMARRY.Areas.Admin.Models
{
    public class ContactEnquiryViewModel
    {
        public EmailDto? Email { get; set; }
        public ContactEnquiryDto? Enquiry { get; set; }
        public List<ContactEnquiryDto>? Enquiries { get; set; }
    }
}
