using System;

namespace Application.Models
{
    public class SaveComplaintRecordApiModel
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public DateTime ComplaintDate { get; set; }
        public string? ComplaintDescription { get; set; }
        public string? ComplaintLevel { get; set; } = "Level1"; // "Level1", "Level2", "Level3"
        public decimal DeductionAmount { get; set; }
        public bool IsApproved { get; set; } = true;
        public string? Resolution { get; set; }
    }
}
