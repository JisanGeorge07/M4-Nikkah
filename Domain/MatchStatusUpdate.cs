using Domain.Common;

namespace Domain
{
    public class MatchStatusUpdate : BaseEntity
    {
        public long UserId { get; set; }                    // The user who submitted the update
        public long PartnerId { get; set; }                 // The matched partner
        public long UserFavouriteProfileId { get; set; }    // FK to the accepted interest record
        public MatchRelationshipStatus Status { get; set; } // Enum: Engaged, Married, etc.
        public DateTime? StatusUpdatedOn { get; set; }
    }
}
