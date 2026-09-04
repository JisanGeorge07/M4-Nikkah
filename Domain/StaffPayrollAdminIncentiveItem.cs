using Domain.Common;

namespace Domain
{
    public class StaffPayrollAdminIncentiveItem : BaseEntity
    {
        public long StaffPayrollId { get; set; }
        public decimal Amount { get; set; }
        public string? Label { get; set; }
    }
}
