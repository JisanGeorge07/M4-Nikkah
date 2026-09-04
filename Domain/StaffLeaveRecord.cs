using Domain.Common;
using System;

namespace Domain
{
    public class StaffLeaveRecord : BaseEntity
    {
        public long StaffId { get; set; }
        public DateTime LeaveDate { get; set; }
        public string LeaveType { get; set; } = string.Empty; // "FullDay", "HalfDay", "Late"
        public string? Reason { get; set; }
        public bool IsPaid { get; set; }
        public bool IsApproved { get; set; } = true;
        public string? ApprovedBy { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
    }
}
