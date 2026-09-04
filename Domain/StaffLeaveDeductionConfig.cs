using Domain.Common;

namespace Domain
{
    public class StaffLeaveDeductionConfig : BaseEntity
    {
        public long StaffId { get; set; }
        public int PaidLeaveLimit { get; set; }
        public string? UnpaidLeaveDeductionRule { get; set; }
        public decimal HalfDayDeduction { get; set; }
        public decimal AbsentDayDeduction { get; set; }
        public string? LateAttendanceDeduction { get; set; }
        public string? LeaveDeductionFormula { get; set; }
        public bool ManualDeductionPermission { get; set; }
    }
}
