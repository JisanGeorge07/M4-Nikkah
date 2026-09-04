using Application.Models.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Models
{
    public class ImagesDto : BaseDto
    {
        public long UserId { get; set; }
        public string? Image1Path { get; set; }
        public IFormFile? Image1 { get; set; }
        public string? Image2Path { get; set; }
        public IFormFile? Image2 { get; set; }
        public string? Image3Path { get; set; }
        public IFormFile? Image3 { get; set; }
        public string? Image4Path { get; set; }
        public IFormFile? Image4 { get; set; }
        public string? Image5Path { get; set; }
        public IFormFile? Image5 { get; set; }

    }
}
