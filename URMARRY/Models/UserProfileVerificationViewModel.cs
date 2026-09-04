using Application.Models;
using Domain;
using System.Collections.Generic;

namespace URMARRY.Models
{
    public class UserProfileVerificationViewModel
    {
        public long? UserId { get; set; }
        public RegistrationDto? Registration { get; set; }
        public List<VerificationDocument> Documents { get; set; } = new List<VerificationDocument>();
        public bool DocumentVerificationEnabled { get; set; }
        public bool DocumentVerificationComplete { get; set; }
        public bool DocumentVerificationRejected { get; set; }
        public bool DocumentVerificationFollowupApproved { get; set; }
        public string? FollowupStatus { get; set; }
        public string? LatestRemarks { get; set; }
    }
}
