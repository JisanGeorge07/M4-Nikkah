using Domain.Common;

namespace Domain
{
    public class Community : OrderableBaseEntity
    {
        public long? Type { get; set; }
        public string? Title { get; set; }
    }
}
