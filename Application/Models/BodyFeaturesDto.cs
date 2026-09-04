using Application.Models.Common;

namespace Application.Models
{
    public class BodyFeaturesDto : OrderableDto
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
    }
}
