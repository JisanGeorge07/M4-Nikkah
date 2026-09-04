using Domain.Common;
using System.Collections.Generic;

namespace Domain
{
    public class StaffDailyTargetConfig : BaseEntity
    {
        public long StaffId { get; set; }
        public string? TargetBasis { get; set; } // "Conversion Count", "Premium Value", "Combined", "Separate"
        public string? SlabType { get; set; } // "Exact", "Highest Applicable", "Progressive"

        public virtual ICollection<StaffTargetSlab> Slabs { get; set; } = new List<StaffTargetSlab>();
    }
}
