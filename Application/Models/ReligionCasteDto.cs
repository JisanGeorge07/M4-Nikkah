using Application.Models.Common;

namespace Application.Models
{
    public class ReligionCasteDto : OrderableDto
    {
        public long ParentId { get; set; }
        public string? Title { get; set; }
    }
}
