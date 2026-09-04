using System;
using System.Collections.Generic;
using Domain;

namespace URMARRY.Areas.Admin.Models
{
    public class MemberCrmDetailsViewModel
    {
        public Registration User { get; set; } = null!;
        public string? ProfileForTitle { get; set; }
        public string? ReligionTitle { get; set; }
        public string? CasteTitle { get; set; }
        public string? CommunityTitle { get; set; }
        public string? NationalityTitle { get; set; }
        public string? MaritalStatusTitle { get; set; }
        public string? HeightTitle { get; set; }
        public string? WeightTitle { get; set; }
        public string? ComplexionTitle { get; set; }
        public string? BodyTypeTitle { get; set; }
        public string? ProfessionTitle { get; set; }
        public string? MotherTongueTitle { get; set; }
        public string? ReligiousnessTitle { get; set; }
        public string? FinancialStatusTitle { get; set; }
        public Images? UserImages { get; set; }
        public PlanPurchase? ActivePlan { get; set; }
        public PlanPurchase? LatestPlan { get; set; }
        public int ActiveCredits { get; set; }
        public string ExpiryDateText { get; set; } = "N/A";
        public string? AssignedStaffName { get; set; }
        public long? AssignedStaffId { get; set; }
        public List<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public List<VerificationDocument> VerificationDocuments { get; set; } = new List<VerificationDocument>();
        public int Age { get; set; }
    }
}
