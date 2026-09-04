using Application.Models.Common;
using Domain;

namespace Application.Models
{
    public class ColdLeadDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public long AssignedStaffId { get; set; }
        public string? AssignedStaffName { get; set; }
        public ColdLeadStatus Status { get; set; } = ColdLeadStatus.Pending;
        public string StatusName => Status.ToString();
        public string? Remarks { get; set; }
    }
}
