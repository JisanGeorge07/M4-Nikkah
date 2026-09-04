using Domain.Common;

namespace Domain
{
    public class BodyFeatures : OrderableBaseEntity
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
    }
}
