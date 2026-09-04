using Application.Interfaces.Persistence;
using Application.Models;
using Application.ViewModels;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Mvc;
using URMARRY.Models;
using URMARRY.Services;

namespace URMARRY.ViewComponents
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotifcationRepository _notificationRepo;
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IRepository<UserFavouriteProfile> _userFavouriteProfileRepo;
        private readonly IRepository<MatchStatusUpdate> _matchStatusRepo;
        private readonly IRepository<SuccessStory> _successStoryRepo;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly CookieHelper _cookieHelper;

        public NotificationsViewComponent(IHttpContextAccessor contextAccessor,
            INotifcationRepository notificationRepo,
            IRepository<Registration> registrationRepo,
            IRepository<UserFavouriteProfile> userFavouriteProfileRepo,
            IRepository<MatchStatusUpdate> matchStatusRepo,
            IRepository<SuccessStory> successStoryRepo,
            IMapper mapper,
            IUserService userService,
            CookieHelper cookieHelper)
        {
            _httpContextAccessor = contextAccessor;
            _notificationRepo = notificationRepo;
            _registrationRepo = registrationRepo;
            _userFavouriteProfileRepo = userFavouriteProfileRepo;
            _matchStatusRepo = matchStatusRepo;
            _successStoryRepo = successStoryRepo;
            _mapper = mapper;
            _userService = userService;
            _cookieHelper = cookieHelper;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdVal = _cookieHelper.GetUserIdFromCookie(HttpContext);
            if (!userIdVal.HasValue)
            {
                return View("_Notifications", new NotificationViewModel
                {
                    notificationDto = new List<NotificationDto>(),
                    Registration = new RegistrationDto(),
                    AllRegistrations = new List<RegistrationDto>()
                });
            }

            var userId = userIdVal.Value;
            var loggedInUserprofile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userId));

            loggedInUserprofile.IsPremiumMember = await _userService.IsPremiumUser(loggedInUserprofile.Id);

            var notifications = await _notificationRepo.GetNotificationsBasedOnNotifyId(userId);
            var profileIds = notifications.Select(m => m.UserId).Distinct().ToList();
            var UserProfiles = await _registrationRepo.GetAllByIds(profileIds);
            var allRegistrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive(x => x.IsActive));
            var _notificationViewModel = new NotificationViewModel();
            // Map notifications to NotificationDto
            var userFavourites = await _userFavouriteProfileRepo.WhereActive(x => x.UserId == loggedInUserprofile.Id || x.LikedId == loggedInUserprofile.Id);
            var notificationList = new List<NotificationDto>();
            foreach (var notification in notifications)
            {
                var userProfile = UserProfiles.FirstOrDefault(profile => profile.Id == notification.UserId);
                var favToUs = userFavourites.FirstOrDefault(x => x.UserId == notification.UserId && x.LikedId == loggedInUserprofile.Id);
                var favFromUs = userFavourites.FirstOrDefault(x => x.UserId == loggedInUserprofile.Id && x.LikedId == notification.UserId);
                
                string? interestStatus = null;
                long? interestRecordId = null;
                
                if (notification.Notify_Message != null && notification.Notify_Message.Contains("viewed your profile"))
                {
                    // No status badge for locked photo view notifications
                }
                else if (notification.Notify_Message != null && notification.Notify_Message.Contains("success story", StringComparison.OrdinalIgnoreCase))
                {
                    // No status badge for success story notifications
                }
                else if (favToUs != null)
                {
                    interestStatus = favToUs.Status.ToString();
                    interestRecordId = favToUs.Id;
                }
                else if (favFromUs != null)
                {
                    interestStatus = favFromUs.Status.ToString();
                    interestRecordId = favFromUs.Id;
                }

                var dto = new NotificationDto
                {
                    Id = notification.Id,
                    Notify_Message = notification.Notify_Message,
                    UserId = notification.UserId,
                    Notify_Id = notification.Notify_Id,
                    Is_Read = notification.Is_Read,
                    CreatedOn = notification.CreatedOn,
                    profileImageUrl = userProfile?.ImagePath,
                    likedByUserName = userProfile?.Name,
                    Gender = userProfile?.Gender,
                    InterestStatus = interestStatus,
                    InterestRecordId = interestRecordId
                };

                // Enrich status update prompt notifications
                if (notification.Notify_Message != null &&
                    notification.Notify_Message.StartsWith(RelationshipStatusNotificationService.StatusUpdatePromptPrefix))
                {
                    dto.IsStatusUpdatePrompt = true;
                    dto.StatusUpdatePartnerId = notification.UserId; // partner whose avatar is shown
                    // Strip the prefix from the displayed message
                    dto.Notify_Message = notification.Notify_Message
                        .Replace(RelationshipStatusNotificationService.StatusUpdatePromptPrefix + " ", "")
                        .Replace(RelationshipStatusNotificationService.StatusUpdatePromptPrefix, "");

                    // Find the UserFavouriteProfile record
                    var favRecord = userFavourites.FirstOrDefault(x =>
                        (x.UserId == userId && x.LikedId == notification.UserId) ||
                        (x.UserId == notification.UserId && x.LikedId == userId));
                    if (favRecord != null)
                    {
                        dto.StatusUpdateFavId = favRecord.Id;
                        // Check current match status
                        var matchStatus = (await _matchStatusRepo.WhereActive(x =>
                            x.UserId == userId && x.UserFavouriteProfileId == favRecord.Id)).FirstOrDefault();
                        if (matchStatus != null)
                            dto.CurrentMatchStatus = (int)matchStatus.Status;
                    }

                    // Check if success story already submitted by either user
                    var existingStory = (await _successStoryRepo.WhereActive(x =>
                        (x.SubmittedByUserId == userId && x.PartnerUserId == notification.UserId) ||
                        (x.SubmittedByUserId == notification.UserId && x.PartnerUserId == userId))).FirstOrDefault();
                    dto.HasSubmittedSuccessStory = existingStory != null;
                }

                notificationList.Add(dto);
            }
            _notificationViewModel.notificationDto = notificationList;
            _notificationViewModel.Registration = loggedInUserprofile;
            _notificationViewModel.AllRegistrations = allRegistrations;

            return View("_Notifications", _notificationViewModel);
        }
    }
}
