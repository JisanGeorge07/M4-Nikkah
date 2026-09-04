using Application.Models;
using Application.Models.Identity;
using Application.Models.Transactions;
using Newtonsoft.Json;

namespace URMARRY.Models
{
    public class HomeViewModel
    {
        //public List<StateDto>? States { get; set; }
        public List<CityDto> Citys { get; set; }
        public HomeContentDto? HomeContent { get; set; }
        public RegistrationDto? Enquiry { get; set; }
        public ContactEnquiryDto? ContactEnquiry { get; set; }
        public List<ImageGalleryDto>? ImageGalleries { get; set; }
        public List<RegistrationDto>? Enquiries { get; set; }
        public List<ProfileForDto>? ProfileFor { get; set; }
        public List<NationalityDto>? Nationalities { get; set; }
        public List<MaritalStatusDto>? MaritalStatuses { get; set; }
        public List<BodyFeaturesDto>? BodyFeatures { get; set; }
        public List<ProfessionDto>? Professions { get; set; }
        public List<MotherTongueDto>? MotherTongues { get; set; }
        public List<ReligionCasteDto>? Religions { get; set; }
        public List<ReligionCasteDto>? Castes { get; set; }
        public List<ReligiousnessDto>? Religiousnesses { get; set; }
        public List<CommunityDto>? Communities { get; set; }
        public List<FinancialStatusDto>? FinancialStatuses { get; set; }
        public List<SocialMediaDto>? SocialMedias { get; set; }
        public RegistrationDto? Registration { get; set; }
        public RegistrationDto? MatchedUserProfile { get; set; }
        public bool ContactDetailsUnlocked = false; 
        public Transaction Transaction { get; set; }
        public List<HomeBannerDto>? HomeBanners { get; set; }
        public List<RegistrationDto>? Registrations { get; set; }
        public ImagesDto? Images { get; set; }
        public AboutDto? About { get; set; }
        public ContactDto? Contact { get; set; }
        public UserFavouriteProfileDto? UserFavourite { get; set; }
        public Domain.PlanPurchase? ActivePlan { get; set; }
		public List<StateDto>? States { get; set; }
		public List<DistrictDto>? Districts { get; set; }
		public List<CityDto>? Cities { get; set; }
		public string? SenderEmail { get; set; }
        public string? Message { get; set; }
        [JsonProperty("success")] public bool Success { get; set; }
        [JsonProperty("error-codes")] public List<string>? ErrorMessage { get; set; }
        public List<UserReportReasonDto>? UserReportReasons { get; set; }
        /// <summary>True when the viewed profile has already sent an interest (liked) to the currently logged-in user.</summary>
        public bool ProfileLikedUs { get; set; } = false;
        /// <summary>Status of OUR interest sent TO the viewed profile: -1=None, 0=Pending, 1=Accepted, 2=Declined.</summary>
        public int InterestStatusFromUs { get; set; } = -1;
        /// <summary>Status of THEIR interest sent TO US: -1=None, 0=Pending, 1=Accepted, 2=Declined.</summary>
        public int InterestStatusToUs { get; set; } = -1;
        /// <summary>Whether the viewing user can see this profile's photos (auto-resolved or manually approved).</summary>
        public bool PhotosUnlocked { get; set; } = true;
        /// <summary>Status of photo unlock request from viewer to owner: -1=None, 0=Pending, 1=Approved, 2=Rejected.</summary>
        public int PhotoUnlockRequestStatus { get; set; } = -1;
        /// <summary>Published success stories for home page testimonials section.</summary>
        public List<SuccessStoryDto>? SuccessStories { get; set; }
        /// <summary>True when the currently logged-in user has already reported this profile.</summary>
        public bool AlreadyReported { get; set; } = false;
        /// <summary>Whether the viewed profile is currently online.</summary>
        public bool IsProfileOnline { get; set; } = false;
        /// <summary>When the viewed profile was last seen online.</summary>
        public DateTime? ProfileLastSeenAt { get; set; }
    }
}
