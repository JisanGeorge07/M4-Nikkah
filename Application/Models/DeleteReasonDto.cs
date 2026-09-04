using Application.Models.Common;

namespace Application.Models
{
    public class DeleteReasonDto : OrderableDto
    {
        public string Reason { get; set; } = string.Empty;
        public bool ShowTestimonialPrompt { get; set; } = false;
    }
}
