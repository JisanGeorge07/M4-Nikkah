using System;
using Application.Models;

namespace URMARRY.Models;

public class UserExploreViewModel
{
    public RegistrationDto? Registration { get; set; }
    public List<RegistrationDto>? RegistrationList { get; set; }
    public List<BodyFeaturesDto>? BodyFeatures { get; set; }
    public List<ReligionCasteDto>? ReligionCaste { get; set; }
    public List<MaritalStatusDto>? MaritalStatus { get; set; }
    public List<ProfessionDto>? Profession { get; set; }
    public List<CommunityDto>? Community { get; set; }
    public List<DistrictDto>? Districts { get; set; }
    public List<UserFavouriteProfileDto>? UserFavouriteProfile { get; set; }
    public List<UserStarProfileDto>? UserStarProfile { get; set; }
    public bool HasReachedStarLimit { get; set; }
    public SearchViewModel? SearchViewModel { get; set; }
}
