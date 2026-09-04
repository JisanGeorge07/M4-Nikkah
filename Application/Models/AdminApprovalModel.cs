namespace Application.Models
{
    public class AdminApprovalModel
    {
        public long FollowUpId { get; set; }
        public bool IsApproved { get; set; }
        public string? Remarks { get; set; }
        public string? PaymentMode { get; set; } // "Online" or "Offline"
        public bool SendEmail { get; set; }
        public bool SendMessage { get; set; }
        public bool SendWhatsApp { get; set; }
    }
}
