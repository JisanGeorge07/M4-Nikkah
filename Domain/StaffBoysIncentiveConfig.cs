using Domain.Common;
using System;

namespace Domain
{
    public class StaffBoysIncentiveConfig : BaseEntity
    {
        public long StaffId { get; set; }
        public string? IncentiveType { get; set; } // "Fixed" or "Percentage"
        public decimal IncentiveValue { get; set; }
        public string? ApplicablePremiumPackages { get; set; } // JSON array or comma separated Package IDs
        public decimal MinimumPremiumAmount { get; set; }
        public decimal? MaximumIncentiveLimit { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }
}
