using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Mvc;
using URMARRY.Models;
using URMARRY.Services;

namespace URMARRY.Controllers
{
    public class ChatController : Controller
    {
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IChatService _chatService;
        private readonly CookieHelper _cookieHelper;

        public ChatController(
            IRepository<Registration> registrationRepo,
            IMapper mapper,
            IUserService userService,
            IChatService chatService,
            CookieHelper cookieHelper)
        {
            _registrationRepo = registrationRepo;
            _mapper = mapper;
            _userService = userService;
            _chatService = chatService;
            _cookieHelper = cookieHelper;
        }

        [HttpGet("/chat")]
        public async Task<IActionResult> Index()
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            var viewModel = await GetUserDashboardViewModelAsync();
            ViewBag.CurrentUserId = userId ?? 0;
            return View("ChatList", viewModel);
        }

        [HttpGet("/chat/with/{targetUserId}")]
        public async Task<IActionResult> With(long targetUserId)
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            if (!userId.HasValue || userId.Value <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var perm = await _chatService.CheckChatPermissionAsync(userId.Value, targetUserId);
            if (perm.MessageLimitReached || (perm.RequiresUpgrade && !perm.Allowed))
            {
                TempData["LimitMessage"] = perm.Reason;
                return RedirectToAction("MyPremium");
            }

            var viewModel = await GetUserDashboardViewModelAsync();
            ViewBag.CurrentUserId = userId.Value;
            ViewBag.TargetUserId = targetUserId;
            ViewBag.IsSupportChat = false;
            return View("Chat", viewModel);
        }

        [HttpGet("/chat/support")]
        public async Task<IActionResult> Support()
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            var viewModel = await GetUserDashboardViewModelAsync();
            ViewBag.CurrentUserId = userId ?? 0;
            ViewBag.TargetUserId = 0;
            ViewBag.IsSupportChat = true;
            return View("Chat", viewModel);
        }

        [HttpGet("/chat/online-list")]
        public async Task<IActionResult> OnlineList()
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            var viewModel = await GetUserDashboardViewModelAsync();
            ViewBag.CurrentUserId = userId ?? 0;
            return View("OnlineList", viewModel);
        }

        [HttpGet("/chat/my-premium")]
        public async Task<IActionResult> MyPremium()
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            var viewModel = await GetUserDashboardViewModelAsync();
            ViewBag.CurrentUserId = userId ?? 0;

            if (userId.HasValue && userId.Value > 0)
            {
                var activePlan = await _userService.GetActivePlanPurchase(userId.Value);
                ViewBag.ViewCreditsUsed = activePlan?.ViewCreditsUsed ?? 0;
                ViewBag.ViewCreditsPurchased = activePlan?.ViewCreditsPurchased ?? 50;
                ViewBag.MessageCreditsUsed = activePlan?.MessageCreditsUsed ?? 0;
                ViewBag.MessageCreditsPurchased = activePlan?.MessageCreditsPurchased ?? 50;
                ViewBag.AudioCallContactsUsed = activePlan?.AudioCallContactsUsed ?? 0;
                ViewBag.AudioCallContactsPurchased = activePlan?.AudioCallContactsPurchased ?? 50;
                ViewBag.VideoCallMinutesUsed = activePlan?.VideoCallMinutesUsed ?? 0;
                ViewBag.VideoCallMinutesPurchased = activePlan?.VideoCallMinutesPurchased ?? 60;
                ViewBag.PlanExpiresAt = activePlan?.ExpiresAt;
            }
            else
            {
                ViewBag.ViewCreditsUsed = 0;
                ViewBag.ViewCreditsPurchased = 50;
                ViewBag.MessageCreditsUsed = 0;
                ViewBag.MessageCreditsPurchased = 50;
                ViewBag.AudioCallContactsUsed = 0;
                ViewBag.AudioCallContactsPurchased = 50;
                ViewBag.VideoCallMinutesUsed = 0;
                ViewBag.VideoCallMinutesPurchased = 60;
                ViewBag.PlanExpiresAt = null;
            }

            return View("MyPremium", viewModel);
        }

        private async Task<UserDashboardViewModel> GetUserDashboardViewModelAsync()
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            if (!userId.HasValue || userId.Value <= 0)
            {
                return new UserDashboardViewModel
                {
                    Registration = new RegistrationDto()
                };
            }

            var userEntity = await _registrationRepo.Get(userId.Value);
            var profile = userEntity != null ? _mapper.Map<RegistrationDto>(userEntity) : new RegistrationDto();
            if (profile != null && !profile.IsPremiumMember && profile.Id > 0)
            {
                profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
            }

            return new UserDashboardViewModel
            {
                Registration = profile ?? new RegistrationDto(),
                RegistrationList = new List<RegistrationDto>()
            };
        }
    }
}
