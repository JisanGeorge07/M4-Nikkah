using Domain.Common;

namespace Domain
{
    public class Religiousness : OrderableBaseEntity
    {
        public long? Type { get; set; }
        public string? Title { get; set; }
    }
}
