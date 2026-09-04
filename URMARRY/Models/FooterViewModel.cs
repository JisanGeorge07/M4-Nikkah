using Application.Models;

namespace URMARRY.Models
{
    public class FooterViewModel
    {
        public ContactDto? Contact { get; set; }
        public List<SocialMediaDto>? SocialMedia { get; set; }
        public AboutDto? About { get; set; }
        public RegistrationDto? Registration { get; set; }
        public UserFavouriteProfileDto? UserFavouriteProfile { get; set; }
    }
}
