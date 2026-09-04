using Domain.Common;

namespace Domain
{
    public class VerificationDocument : BaseEntity
    {
        public long ProfileId { get; set; }
        public string? DocumentUrl { get; set; }
        public string? DocumentType { get; set; } // "IdFront", "IdBack", "Other"
        public string? OriginalFileName { get; set; }
        public int DisplayOrder { get; set; }

        // Navigation property
        public virtual Registration? Profile { get; set; }
    }
}
