using Application.Models.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Models
{
    public class HomeContentDto : BaseDto
    {
		public string? Tagline { get; set; }
		public string? Title1 { get; set; }
		public string? Body1 { get; set; }
		public string? Title2 { get; set; }
		public string? Body2 { get; set; }
		public string? Title3 { get; set; }
		public string? Body3 { get; set; }
		public string? ListTitle { get; set; }
	}
}
