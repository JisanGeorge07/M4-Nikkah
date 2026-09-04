using Domain.Common;

namespace Domain
{
    public class MatchingProfiles : BaseEntity
    {
        public long UserId { get; set; }
        public long PartnerId { get; set; }
    }
}
