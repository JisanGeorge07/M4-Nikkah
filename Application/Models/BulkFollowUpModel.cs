using System.Collections.Generic;
using Domain;

namespace Application.Models
{
    public class BulkFollowUpModel
    {
        public List<long> ProfileIds { get; set; } = new List<long>();
        public FollowUpType? FollowUpType { get; set; }
        public List<FollowUpType>? FollowUpTypes { get; set; }
        public string Remarks { get; set; } = null!;
    }
}
