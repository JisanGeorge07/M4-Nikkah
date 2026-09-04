using Domain.Common;

namespace Domain
{
    public class Contact : BaseEntity
    {
        public string? BannerTitle { get; set; }
        public string? BannerBody { get; set; }
        public string? BannerImagePath { get; set; }
        public string? FormTitle { get; set; }
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? GoogleMapUrl { get; set; }
    }
}
