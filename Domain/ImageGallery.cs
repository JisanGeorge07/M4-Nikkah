using Domain.Common;

namespace Domain
{
    public class ImageGallery : OrderableBaseEntity
    {
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
    }
}
