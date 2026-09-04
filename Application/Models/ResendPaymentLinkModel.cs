namespace Application.Models
{
    public class ResendPaymentLinkModel
    {
        public long FollowUpId { get; set; }
        public bool SendEmail { get; set; } = true;
        public bool SendMessage { get; set; } = true;
        public bool SendWhatsApp { get; set; } = false;
    }
}
