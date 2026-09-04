using Application.Models.Common;

namespace Application.Models
{
    public class CommunityDto : OrderableDto
    {
        public long? Type { get; set; }
        public string? Title { get; set; }
    }
}
