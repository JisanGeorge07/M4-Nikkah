using Application.ViewModels;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace URMARRY.Areas.Admin.Controllers;

[Authorize]
[Area("Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }


    [HttpGet("/admin")]
    public IActionResult Index()
    {
        return View();
    }


    [HttpGet("/admin/account")]
    public async Task<IActionResult> Get()
    {
        var model = new AccountSettingsViewModel();
        var user = await _userManager.GetUserAsync(User);
        //model.UserId = user.Id;
        model.Email = user!.Email;
        return View(model);
    }

    [HttpPost("/admin/account")]
    public async Task<IActionResult> Post(AccountSettingsViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (string.IsNullOrEmpty(model.Email))
        {
            ModelState.AddModelError("invalid", "Please provide an email address.");
            return View("Get", model);
        }

        user!.Email = model.Email;
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(model.OldPassword) && !string.IsNullOrWhiteSpace(model.Password) &&
            !string.IsNullOrWhiteSpace(model.CPassword))
        {
            if (model.Password != model.CPassword)
            {
                ModelState.AddModelError("invalid", "Passwords do not match.");
            }
            else
            {
                if (await _userManager.CheckPasswordAsync(user, model.OldPassword))
                {
                    var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.Password);
                    ModelState.AddModelError("invalid", "Password changed successfully.");
                }
                else
                {
                    ModelState.AddModelError("invalid", "Current password is incorrect. Please try again.");
                }
            }
        }
        else
        {
            ModelState.AddModelError("invalid", "Please enter data to update.");
        }

        var entity = new AccountSettingsViewModel
        {
            Email = user.Email,
            OldPassword = string.Empty,
            Password = string.Empty,
            CPassword = string.Empty
        };
        return View("Get", entity);
    }

    [HttpGet("/admin/change-phone")]
    public async Task<IActionResult> ChangePhone()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Index", "Account");
        }

        var model = new ChangeAdminPhoneViewModel
        {
            CurrentPhone = user.PhoneNumber
        };
        return View(model);
    }

    [HttpPost("/admin/change-phone")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePhone(ChangeAdminPhoneViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Index", "Account");
        }

        model.CurrentPhone = user.PhoneNumber;

        if (string.IsNullOrWhiteSpace(model.NewPhoneNumber))
        {
            ModelState.AddModelError("NewPhoneNumber", "Please provide a new phone number.");
            return View(model);
        }

        var cleanPhone = model.NewPhoneNumber.Trim();
        if (cleanPhone.Length < 8 || cleanPhone.Length > 15 || !cleanPhone.All(char.IsDigit))
        {
            ModelState.AddModelError("NewPhoneNumber", "Please provide a valid phone number (8-15 digits).");
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError("Password", "Please enter your password to confirm this change.");
            return View(model);
        }

        // Verify that the password is correct
        var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!isPasswordCorrect)
        {
            ModelState.AddModelError("Password", "Current password is incorrect. Phone number was not updated.");
            return View(model);
        }

        // Update phone number
        user.PhoneNumber = cleanPhone;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            ViewBag.SuccessMessage = "Phone number updated successfully.";
            model.CurrentPhone = cleanPhone;
            model.NewPhoneNumber = string.Empty;
            model.Password = string.Empty;
            ModelState.Clear();
            return View(model);
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("error", error.Description);
            }
            return View(model);
        }
    }
}