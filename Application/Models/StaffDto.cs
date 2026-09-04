using Application.Models.Common;

namespace Application.Models
{
    public class StaffDto : OrderableDto
    {
        public string? StaffName { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? PasswordHash { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public System.DateTime? JoiningDate { get; set; }
        public long StaffDetailId { get; set; }
    }
}

 