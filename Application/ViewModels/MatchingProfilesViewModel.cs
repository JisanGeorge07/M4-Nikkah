using Application.Models;

namespace Application.ViewModels
{
    public class MatchingProfilesViewModel
    {
        public RegistrationDto? Registration { get; set; }
        public List<RegistrationDto>? RegistrationList { get; set; }
        public List<MatchingProfilesDto>? MatchingProfiles { get; set; }
        public List<CommunityDto>? Communities { get; set; }
        public List<MaritalStatusDto>? MaritalStatuses { get; set; }
        public long UserId { get; set; }
        public List<Match>? Matches { get; set; }
    }
    public class Match
    {
        public long PartnerId { get; set; }
        public bool IsSelected { get; set; }
    }
}
