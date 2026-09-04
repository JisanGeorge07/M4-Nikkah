using Application.Models.Common;

namespace Application.Models
{
    public class TestimonialSettingDto : BaseDto
    {
        public int FirstPromptDays { get; set; }
        public int FollowUpPromptDays { get; set; }
    }
}
