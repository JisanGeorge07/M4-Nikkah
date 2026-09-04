using Application.Models.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Models
{
    public class ImageGalleryDto : OrderableDto
    {
        public string? ImagePath { get; set; }
        public IFormFile? Image { get; set; }
        public string? ImageAlt { get; set; }
    }
}
