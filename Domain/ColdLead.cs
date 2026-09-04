using Domain.Common;

namespace Domain;

public class ColdLead : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public long AssignedStaffId { get; set; }
    public ColdLeadStatus Status { get; set; } = ColdLeadStatus.Pending;
    public string? Remarks { get; set; }
}
