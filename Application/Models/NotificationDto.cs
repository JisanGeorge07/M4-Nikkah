using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public class NotificationDto: Notification
    {
        public string profileImageUrl {  get; set; }
        public string likedByUserName {  get; set; }
        public string Gender { get; set; }
        /// <summary>Populated for interest-related notifications: "Pending", "Accepted", "Declined", or null.</summary>
        public string? InterestStatus { get; set; }
        /// <summary>The UserFavouriteProfile Id associated with this notification, if any.</summary>
        public long? InterestRecordId { get; set; }

        // --- Relationship Status Update Prompt fields ---
        /// <summary>True if this is a relationship status update prompt notification.</summary>
        public bool IsStatusUpdatePrompt { get; set; }
        /// <summary>The UserFavouriteProfile Id for status update prompt notifications.</summary>
        public long? StatusUpdateFavId { get; set; }
        /// <summary>The partner's Id for status update prompt notifications.</summary>
        public long? StatusUpdatePartnerId { get; set; }
        /// <summary>The current match status value (if any existing update exists).</summary>
        public int? CurrentMatchStatus { get; set; }
        /// <summary>True if a success story has already been submitted for this match.</summary>
        public bool HasSubmittedSuccessStory { get; set; }
    }
}
