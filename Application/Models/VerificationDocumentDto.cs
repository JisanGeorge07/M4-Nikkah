using Application.Models.Common;

namespace Application.Models
{
    public class VerificationDocumentDto : BaseDto
    {
        public long ProfileId { get; set; }
        public string? DocumentUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public int DisplayOrder { get; set; }
    }
}
