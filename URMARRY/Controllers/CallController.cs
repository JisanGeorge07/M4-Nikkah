using Application.Interfaces.Persistence;
using Application.Models;
using Application.Models.Call;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using URMARRY.Models;
using URMARRY.Services;

namespace URMARRY.Controllers
{
    public class CallController : Controller
    {
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly ICallService _callService;
        private readonly CookieHelper _cookieHelper;

        public CallController(
            IRepository<Registration> registrationRepo,
            IMapper mapper,
            IUserService userService,
            ICallService callService,
            CookieHelper cookieHelper)
        {
            _registrationRepo = registrationRepo;
            _mapper = mapper;
            _userService = userService;
            _callService = callService;
            _cookieHelper = cookieHelper;
        }

        [HttpGet("/call/voice/{targetUserId:long}")]
        [HttpGet("/call/audio/{targetUserId:long}")]
        public async Task<IActionResult> Voice(long targetUserId, [FromQuery] bool isIncoming = false, [FromQuery] long? callLogId = null, [FromQuery] string? roomId = null)
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            if (!userId.HasValue || userId.Value <= 0)
                return RedirectToAction("Login", "Account");

            bool allowed = true;
            string? reason = null;
            bool interestRequired = false;
            bool requiresUpgrade = false;

            if (!isIncoming)
            {
                var perm = await _callService.CheckCallPermissionAsync(
                    userId.Value, targetUserId, UserCallType.Voice);
                allowed = perm.Allowed;
                reason = perm.Reason;
                interestRequired = perm.InterestRequired;
                requiresUpgrade = perm.RequiresUpgrade;
            }

            ViewBag.CurrentUserId = userId.Value;
            ViewBag.TargetUserId = targetUserId;
            ViewBag.CallType = "Voice";
            ViewBag.CallAllowed = allowed;
            ViewBag.CallDeniedReason = reason;
            ViewBag.InterestRequired = interestRequired;
            ViewBag.RequiresUpgrade = requiresUpgrade;
            ViewBag.IsIncoming = isIncoming;
            ViewBag.CallLogId = callLogId ?? 0;
            ViewBag.RoomId = roomId ?? string.Empty;

            var viewModel = await GetUserDashboardViewModelAsync();
            return View("Normal", viewModel);
        }

        [HttpGet("/call/video/{targetUserId:long}")]
        public async Task<IActionResult> Video(long targetUserId, [FromQuery] bool isIncoming = false, [FromQuery] long? callLogId = null, [FromQuery] string? roomId = null)
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            if (!userId.HasValue || userId.Value <= 0)
                return RedirectToAction("Login", "Account");

            bool allowed = true;
            string? reason = null;
            bool interestRequired = false;
            bool requiresUpgrade = false;

            if (!isIncoming)
            {
                var perm = await _callService.CheckCallPermissionAsync(
                    userId.Value, targetUserId, UserCallType.Video);
                allowed = perm.Allowed;
                reason = perm.Reason;
                interestRequired = perm.InterestRequired;
                requiresUpgrade = perm.RequiresUpgrade;
            }

            ViewBag.CurrentUserId = userId.Value;
            ViewBag.TargetUserId = targetUserId;
            ViewBag.CallType = "Video";
            ViewBag.CallAllowed = allowed;
            ViewBag.CallDeniedReason = reason;
            ViewBag.InterestRequired = interestRequired;
            ViewBag.RequiresUpgrade = requiresUpgrade;
            ViewBag.IsIncoming = isIncoming;
            ViewBag.CallLogId = callLogId ?? 0;
            ViewBag.RoomId = roomId ?? string.Empty;

            var viewModel = await GetUserDashboardViewModelAsync();
            return View("Video", viewModel);
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
            var profile = userEntity != null 
                ? _mapper.Map<RegistrationDto>(userEntity) 
                : new RegistrationDto();
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