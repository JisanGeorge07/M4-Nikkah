using Domain.Common;

namespace Domain
{
    public class Nationality : OrderableBaseEntity
    {
        public string? Title { get; set; }
        public string? CountryCode { get; set; }
        public string? FlagImagePath { get; set; }
    }
}
