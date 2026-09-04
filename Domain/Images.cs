using Domain.Common;

namespace Domain
{
    public class Images : BaseEntity
    {
        public long UserId { get; set; }
        public string? Image1Path { get; set; }
        public string? Image2Path { get; set; }
        public string? Image3Path { get; set; }
        public string? Image4Path { get; set; }
        public string? Image5Path { get; set; }
    }
}
