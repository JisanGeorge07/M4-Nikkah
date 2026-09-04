using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using URMARRY.Models;
using URMARRY.Services;

namespace URMARRY.ViewComponents
{
    public class UserViewComponent : ViewComponent
    {
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly CookieHelper _cookieHelper;

		public UserViewComponent(IHttpContextAccessor contextAccessor, CookieHelper cookieHelper)
        {
			_httpContextAccessor = contextAccessor;
			_cookieHelper = cookieHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdVal = _cookieHelper.GetUserIdFromCookie(HttpContext);
            return View("Default", new HeaderViewModel
            {
                IsLogin = userIdVal.HasValue,
            });
        }
    }
}
