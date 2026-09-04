using Application.Models.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Models;

public class SocialMediaDto : OrderableDto
{
    public string? Name { get; set; }
    public string? ImagePath { get; set; }
    public IFormFile? Image { get; set; }
    public string? Url { get; set; }
}