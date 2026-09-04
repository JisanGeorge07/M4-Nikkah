using Application.Models.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Models
{
	public class HomeBannerDto : OrderableDto
	{
		public string? Title { get; set; }
		public string? Body { get; set; }
		public string? ImagePath { get; set; }
		public IFormFile? Image { get; set; }
		public string? ImageAlt { get; set; }
		public bool IsPlaystoreButtonEnabled { get; set; }
		public bool IsAppstoreButtonEnabled { get; set; }
		public string? PlaystoreButtonLink { get; set; }
		public string? AppstoreButtonLink { get; set; }
	}
}
