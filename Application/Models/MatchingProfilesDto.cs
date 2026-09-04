using Application.Models.Common;

namespace Application.Models
{
    public class MatchingProfilesDto : BaseDto
    {
        public long UserId { get; set; }
        public long PartnerId { get; set; }
    }
}
