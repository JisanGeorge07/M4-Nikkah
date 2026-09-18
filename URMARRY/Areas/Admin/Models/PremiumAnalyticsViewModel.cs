using Application.Models;
using Domain;
using System;
using System.Collections.Generic;

namespace URMARRY.Areas.Admin.Models
{
    public class PremiumAnalyticsViewModel
    {
        public RegistrationDto User { get; set; }
        public PlanPurchase? LatestPlan { get; set; }
        public ImagesDto? UserImages { get; set; }
        public List<ActivityDetail> ContactViews { get; set; }
        public List<ActivityDetail> Shortlisted { get; set; }
        public List<ActivityDetail> Liked { get; set; }
        public List<ActivityDetail> LikedBy { get; set; } = new();
        public List<ActivityDetail> NotLiked { get; set; }
        public List<ActivityDetail> Reported { get; set; }
        public List<MatchingProfileActivityDetail> MatchingProfiles { get; set; } = new();
    }

    public class ActivityDetail
    {
        public long ProfileId { get; set; }
        public string ProfileName { get; set; }
        public string RegisterNumber { get; set; }
        public DateTime ActionDate { get; set; }
        public string? Details { get; set; }
        public InterestStatus? InterestStatus { get; set; }
        public long? UserFavouriteProfileId { get; set; }
        public bool HasSubmittedSuccessStory { get; set; }
    }

    public class MatchingProfileActivityDetail
    {
        public long ProfileId { get; set; }
        public string ProfileName { get; set; } = string.Empty;
        public string RegisterNumber { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public int Age { get; set; }
        public string? Height { get; set; }
        public string? Education { get; set; }
        public string? Profession { get; set; }
        public string? MaritalStatus { get; set; }
        public string? District { get; set; }
        public string? State { get; set; }
        public int TotalMatchingScore { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsLiked { get; set; }
        public bool IsStarred { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
