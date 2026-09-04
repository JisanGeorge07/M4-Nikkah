using Application.Models.Common;
using Domain;

namespace Application.Models
{
    public class MatchStatusUpdateDto : BaseDto
    {
        public long UserId { get; set; }
        public long PartnerId { get; set; }
        public long UserFavouriteProfileId { get; set; }
        public MatchRelationshipStatus Status { get; set; }
        public DateTime? StatusUpdatedOn { get; set; }
    }
}
