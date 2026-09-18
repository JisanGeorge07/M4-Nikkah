using System;

namespace Application.Models
{
    public class MarkAdminBonusAsReadRequest
    {
        /// <summary>
        /// Specific admin incentive item ID to mark as read.
        /// If null or 0, all unread admin bonus items for the staff member will be marked as read.
        /// </summary>
        public long? ItemId { get; set; }

        /// <summary>
        /// Optional staff ID (in case called on behalf of a specific staff, defaults to authenticated staff).
        /// </summary>
        public long? StaffId { get; set; }
    }
}
