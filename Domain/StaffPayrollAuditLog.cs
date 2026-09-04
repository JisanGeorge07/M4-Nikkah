using Domain.Common;
using System;

namespace Domain
{
    public class StaffPayrollAuditLog : BaseEntity
    {
        public long StaffPayrollId { get; set; }
        public string Action { get; set; } = string.Empty; // "StatusChanged", "AmountEdited", "Reopened", "Locked", "Approved"
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public decimal? PreviousAmount { get; set; }
        public decimal? NewAmount { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }

        // Navigation property
        public virtual StaffPayroll? StaffPayroll { get; set; }
    }
}
