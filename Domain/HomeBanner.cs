using Domain.Common;

namespace Domain
{
	public class HomeBanner : OrderableBaseEntity
	{
		public string? Title { get; set; }
		public string? Body { get; set; }
		public string? ImagePath { get; set; }
		public string? ImageAlt { get; set; }
		public bool IsPlaystoreButtonEnabled { get; set; }   
		public bool IsAppstoreButtonEnabled { get; set; } 
		public string? PlaystoreButtonLink { get; set; } 
		public string? AppstoreButtonLink { get; set; } 
	}
}
