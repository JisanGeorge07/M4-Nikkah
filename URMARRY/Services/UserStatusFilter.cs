using Application.Constants;
using Application.Interfaces.Persistence;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace URMARRY.Services;

public class UserStatusFilter : IAsyncActionFilter
{
    private readonly CookieHelper _cookieHelper;
    private readonly IRepository<Registration> _registrationRepo;

    public UserStatusFilter(CookieHelper cookieHelper, IRepository<Registration> registrationRepo)
    {
        _cookieHelper = cookieHelper;
        _registrationRepo = registrationRepo;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var routeData = context.RouteData.Values;
        var area = routeData["area"]?.ToString();
        var controller = routeData["controller"]?.ToString();
        var action = routeData["action"]?.ToString();

        // 1. Bypass Admin area actions
        if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Bypass all API requests
        var path = context.HttpContext.Request.Path.Value;
        if (path != null && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // 2. Bypass public/anonymous routes (HomeController except dashboard-related views)
        if (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(action, "Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // 3. Exclude login/logout, OTP, and suspended actions to prevent redirect loops
        if (string.Equals(controller, "User", StringComparison.OrdinalIgnoreCase))
        {
            if (action == "Login" || action == "AutoLogin" || action == "SignUp" || action == "Signout" || 
                action == "VerifyOtp" || action == "SendOtp" || action == "Suspended" || action == "HelpSupport" || action == "SubmitSupport")
            {
                await next();
                return;
            }
        }

        // 4. Check if current user session is suspended (ReportedViolation) or deactivated/deleted/recycled
        var userIdValue = _cookieHelper.GetUserIdFromCookie(context.HttpContext);
        if (userIdValue.HasValue)
        {
            var user = await _registrationRepo.Get(userIdValue.Value);
            
            // If the user does not exist, or has been deactivated/deleted/recycled, clear their cookie and redirect to login
            if (user == null || !user.IsActive || user.IsDeleted || user.DisabledReason == DisabledReason.Recycled)
            {
                _cookieHelper.ClearSecureCookie(context.HttpContext);
                context.Result = new RedirectToActionResult("Login", "User", null);
                return;
            }

            if (user.DisabledReason == DisabledReason.ReportedViolation)
            {
                context.Result = new RedirectToActionResult("Suspended", "User", null);
                return;
            }
        }

        await next();
    }
}
