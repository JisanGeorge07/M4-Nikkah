using Domain.Common;

namespace Domain
{
    public class StaffTargetSlab : BaseEntity
    {
        public long StaffDailyTargetConfigId { get; set; }
        public decimal MinThreshold { get; set; }
        public decimal? MaxThreshold { get; set; }
        public decimal IncentiveAmount { get; set; }

        public virtual StaffDailyTargetConfig? TargetConfig { get; set; }
    }
}
