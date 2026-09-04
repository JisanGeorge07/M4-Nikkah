using Domain.Common;

namespace Domain
{
    public class About : BaseEntity
    {
        public string? BannerTitle { get; set; }
        public string? BannerBody { get; set; }
        public string? BannerImagePath { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? ButtonTitle { get; set; }
        public string? ButtonUrl { get; set; }
        public string? HomeTitle { get; set; }
        public string? HomeBody { get; set; }
        public string? HomeButtonTitle { get; set; }
        public string? HomeButtonUrl { get; set; }
        public string? Home1ImagePath { get;set;}
        public string? Home1ImageAlt { get; set; }
        public string? Home2ImagePath { get;set; }
        public string? Home2ImageAlt { get; set; }
        public string? Home3ImagePath { get; set; }
        public string? Home3ImageAlt { get; set; }
        public string? FooterBody { get; set; }
    }
}
