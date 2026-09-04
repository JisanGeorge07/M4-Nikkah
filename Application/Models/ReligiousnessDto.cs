using Application.Models.Common;

namespace Application.Models
{
    public class ReligiousnessDto : OrderableDto
    {
        public long? Type { get; set; }
        public string? Title { get; set; }
    }
}
