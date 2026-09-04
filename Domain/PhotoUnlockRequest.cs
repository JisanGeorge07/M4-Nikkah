using Domain.Common;

namespace Domain
{
    public class PhotoUnlockRequest : BaseEntity
    {
        public long RequesterId { get; set; }
        public long OwnerId { get; set; }
        public PhotoUnlockStatus Status { get; set; } = PhotoUnlockStatus.Pending;
    }
}
