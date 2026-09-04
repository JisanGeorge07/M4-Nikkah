using Domain.Common;

namespace Domain
{
    public class StaffProfileAssignment : BaseEntity
    {
        public long StaffId { get; set; }
        public long ProfileId { get; set; }

        // Navigation property to customer profile
        public virtual Registration? Profile { get; set; }
    }
}
