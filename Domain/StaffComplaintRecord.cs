using Domain.Common;
using System;

namespace Domain
{
    public class StaffComplaintRecord : BaseEntity
    {
        public long StaffId { get; set; }
        public DateTime ComplaintDate { get; set; }
        public string? ComplaintDescription { get; set; }
        public string? ComplaintLevel { get; set; } // "Level1", "Level2", "Level3"
        public decimal DeductionAmount { get; set; }
        public bool IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Resolution { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
    }
}
