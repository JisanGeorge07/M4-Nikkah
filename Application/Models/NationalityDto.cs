using Application.Models.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Models
{
    public class NationalityDto : OrderableDto
    {
        public string? Title { get; set; }
        public string? CountryCode { get; set; }
        public string? FlagImagePath { get; set; }
        public IFormFile? FlagImage { get; set; }
    }
}
