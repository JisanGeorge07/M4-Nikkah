using Domain;

namespace Application.Models
{
    public class SaveFollowUpModel
    {
        public long ProfileId { get; set; }
        public FollowUpType? FollowUpType { get; set; }
        public List<FollowUpType>? FollowUpTypes { get; set; }
        public string Remarks { get; set; } = null!;
    }
}
