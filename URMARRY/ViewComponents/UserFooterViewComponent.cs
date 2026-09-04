using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Mvc;
using URMARRY.Models;

namespace URMARRY.ViewComponents
{
	public class UserFooterViewComponent : ViewComponent
	{
        private readonly IMapper _mapper;
        private readonly IRepository<SocialMedia> _socialMediaRepo;
        private readonly IRepository<Contact> _contactRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IRepository<UserFavouriteProfile> _userFavouriteProfileRepo;
        public UserFooterViewComponent(
            IHttpContextAccessor contextAccessor,
            IRepository<Registration> registrationRepo,
            IRepository<UserFavouriteProfile> userFavouriteProfileRepo,
            IMapper mapper,
            IRepository<SocialMedia> socialMediaRepo,
            IRepository<Contact> contactRepo)
        {
            _mapper = mapper;
            _registrationRepo = registrationRepo;
            _userFavouriteProfileRepo = userFavouriteProfileRepo;
            _httpContextAccessor = contextAccessor;
            _socialMediaRepo = socialMediaRepo;
            _contactRepo = contactRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            //var userId = _httpContextAccessor.HttpContext.Request.Cookies["id"];
            //var loggedInUserprofile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
            //var starredProfile = await _userFavouriteProfileRepo.GetAllActive(x => x.UserId == userId);
            //var likedIdslist = starredProfile.Select(x => x.likeId).ToList();
            // Fetch the registration details for the liked profiles
            //var likedProfiles = await _registrationRepo.GetAllActive(x => likedIdslist.Contains(x.Id));
            //var likedProfilesDto = _mapper.Map<List<RegistrationDto>>(likedProfiles);
            return View("Default", new FooterViewModel
            {
                Contact = _mapper.Map<ContactDto>(await _contactRepo.First()),
                SocialMedia = _mapper.Map<List<SocialMediaDto>>(await _socialMediaRepo.GetAllActive())
                //Registration = likedProfilesDto
            });
        }
    }
}
