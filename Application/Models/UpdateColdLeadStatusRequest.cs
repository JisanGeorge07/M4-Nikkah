using Domain;

namespace Application.Models
{
    public class UpdateColdLeadStatusRequest
    {
        public long Id { get; set; }
        public ColdLeadStatus Status { get; set; }
        public string? Remarks { get; set; }
    }
}
