using Domain.Common;

namespace Domain;

public class TestimonialSetting : BaseEntity
{
    public int FirstPromptDays { get; set; } = 7;
    public int FollowUpPromptDays { get; set; } = 30;
}
