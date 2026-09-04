using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using URMARRY.Models;
using URMARRY.Services;

namespace URMARRY.ViewComponents
{
	public class UserProfileViewComponent : ViewComponent
	{
		private readonly IRepository<Registration> _registrationRepository;
		private readonly IMapper _mapper;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly CookieHelper _cookieHelper;

		public UserProfileViewComponent(IRepository<Registration> registrationRepository,
			IHttpContextAccessor contextAccessor,
			IMapper mapper,
			CookieHelper cookieHelper)
		{
			_registrationRepository = registrationRepository;
			_httpContextAccessor = contextAccessor;
			_mapper = mapper;
			_cookieHelper = cookieHelper;
		}

		public async Task<IViewComponentResult> InvokeAsync()
		{
			var userIdVal = _cookieHelper.GetUserIdFromCookie(HttpContext);
			if (!userIdVal.HasValue)
			{
				return View("Default", new RegistrationDto());
			}
			return View("Default", _mapper.Map<RegistrationDto>(await _registrationRepository.Get(userIdVal.Value)));
		}
	}
}
