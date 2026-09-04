using Domain.Common;

namespace Domain
{
    public class ReligionCaste : OrderableBaseEntity
    {
        public long ParentId { get; set; }
        public string? Title { get; set; }
    }
}
