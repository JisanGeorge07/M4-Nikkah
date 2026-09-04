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
        public List<ActivityDetail> NotLiked { get; set; }
        public List<ActivityDetail> Reported { get; set; }
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
}
