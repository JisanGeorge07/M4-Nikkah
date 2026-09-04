using Application.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace URMARRY.Models
{
	public class SearchViewModel
	{
		public string? ProfileId { get; set; }
		public int? AgeFrom { get; set; }
		public int? AgeTo { get; set; }
		public string? SearchQuery { get; set; }
		public int? HeightFrom { get; set; }
		public int? HeightTo { get; set; }
		public long? MaritalStatusId { get; set; }
		public long[]? SelectedMaritalStatusIds { get; set; } = Array.Empty<long>();
		public long? CommunityId { get; set; }
		public long[] SelectedCommunityIds { get; set; } = Array.Empty<long>();
		public String? HighestEducationTitle { get; set; }

		public string? country { get; set; }
		public string? state { get; set; }
		public string? district { get; set; }
		public List<string>? SelectedDistricts { get; set; } = new List<string>();
		public string? present_country { get; set; }

		public RegistrationDto Registration { get; set; }
		public UserStarProfileDto? UserStarProfile { get; set; }
		public UserFavouriteProfileDto? UserFavouriteProfile { get; set; }
        public bool IsStarred { get; set; } // New property
        public bool IsLiked { get; set; }
        public IEnumerable<MaritalStatusDto>? MaritalStatuses { get; set; }
		public IEnumerable<CommunityDto>? Communities { get; set; }
		public IEnumerable<BodyFeaturesDto>? BodyFeatures { get; set; }
		public List<long>? SelectedBodyFeaturesIds { get; set; } = new List<long>();

		public List<StateDto>? States { get; set; }
		public List<DistrictDto>? Districts { get; set; }
		public List<CityDto>? Cities { get; set; }
		public List<NationalityDto>? Nationalities { get; set; }

	}
}
