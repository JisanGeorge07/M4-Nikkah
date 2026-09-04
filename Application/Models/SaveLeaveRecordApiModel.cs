using System;

namespace Application.Models
{
    public class SaveLeaveRecordApiModel
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public DateTime LeaveDate { get; set; }
        public string LeaveType { get; set; } = "FullDay"; // "FullDay", "HalfDay", "Late"
        public string? Reason { get; set; }
        public bool IsPaid { get; set; }
        public bool IsApproved { get; set; } = true;
    }
}
