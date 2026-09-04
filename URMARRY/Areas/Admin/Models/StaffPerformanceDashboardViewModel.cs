using System.Collections.Generic;

namespace URMARRY.Areas.Admin.Models
{
    public class StaffPerformanceDashboardViewModel
    {
        // Summary metrics
        public int TotalStaff { get; set; }
        public int TotalAssignedProfiles { get; set; }
        public int VerifiedProfiles { get; set; }
        public int PremiumConverted { get; set; }
        public int RenewalConverted { get; set; }
        public int FollowupsCompleted { get; set; }
        public int AvgConversionRate { get; set; }
        public int AvgRenewalRate { get; set; }
        
        // Detailed row per staff
        public List<StaffPerformanceRowViewModel> StaffPerformanceList { get; set; } = new List<StaffPerformanceRowViewModel>();
    }

    public class StaffPerformanceRowViewModel
    {
        public long StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string RegistrationId { get; set; } = string.Empty;
        public int Assigned { get; set; }
        public int Pending { get; set; }
        public int Verified { get; set; }
        public int Followups { get; set; }
        public int PremiumInterested { get; set; }
        public int PremiumConverted { get; set; }
        public int ExpiredPremium { get; set; }
        public int RenewalFollowups { get; set; }
        public int RenewalConverted { get; set; }
        public int ConvRate { get; set; }
        public int RenewalRate { get; set; }
        public bool IsActive { get; set; }
        public string Status => IsActive ? "Active" : "Inactive";
    }
}
