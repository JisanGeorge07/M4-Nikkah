using Domain.Common;

namespace Domain
{
    public class DeleteReason : OrderableBaseEntity
    {
        public string Reason { get; set; } = string.Empty;
        public bool ShowTestimonialPrompt { get; set; } = false;
    }
}
