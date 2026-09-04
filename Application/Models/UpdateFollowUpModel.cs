using Domain;
using System;

namespace Application.Models
{
    public class UpdateFollowUpModel
    {
        public long FollowUpId { get; set; }
        public ContactType ContactType { get; set; }
        public CallStatus CallStatus { get; set; }
        public PremiumInterestStatus? PremiumInterestStatus { get; set; }
        public ProfileVerificationStatus? ProfileVerificationStatus { get; set; }
        public RenewalInterestStatus? RenewalInterestStatus { get; set; }
        public string? Remarks { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public long? StaffId { get; set; }
        public bool? DocumentVerificationComplete { get; set; }

        // Payment Handling fields (when PremiumInterestStatus=Converted or RenewalInterestStatus=Renewed)
        public string? PaymentMode { get; set; } // "Online" or "Offline"
        public OfflinePaymentMethod? OfflinePaymentType { get; set; }
        public string? TransactionId { get; set; }
        public decimal? PaymentAmount { get; set; }

        // Online delivery channels
        public bool SendEmail { get; set; }
        public bool SendMessage { get; set; }
        public bool SendWhatsApp { get; set; }
    }
}
