using Application.Helpers;
using Application.Interfaces.Infrastructure;
using Application.Interfaces.Persistence;
using Application.Models;
using Application.ViewModels;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace URMARRY.Controllers;

public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly AppDbContext _dbContext;
    private readonly AdminAuthSettings _adminAuthSettings;

    #region Constructor

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        AppDbContext dbContext,
        IOptions<AdminAuthSettings> adminAuthOptions)
    {
        _signInManager = signInManager;
        _logger = logger;
        _userManager = userManager;
        _emailService = emailService;
        _dbContext = dbContext;
        _adminAuthSettings = adminAuthOptions.Value ?? new AdminAuthSettings();
    }

    #endregion

    #region Admin Login & 2FA Flow

    [AllowAnonymous]
    [HttpGet("/account/login")]
    public async Task<IActionResult> Index([FromQuery] string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out or landing on login page");

        var model = new AdminLoginStateViewModel
        {
            Step = AdminLoginStep.Credentials,
            ReturnUrl = returnUrl
        };
        return View(model);
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [HttpPost("/account/login")]
    public async Task<IActionResult> Index(AdminLoginStateViewModel model, [FromQuery] string? returnUrl = null)
    {
        model.Step = AdminLoginStep.Credentials;
        model.ReturnUrl ??= returnUrl;

        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError("error", "Please provide both email and password.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user == null)
        {
            _logger.LogWarning("Admin login attempt with non-existent email: {Email}", model.Email);
            ModelState.AddModelError("error", "The provided username or password is incorrect.");
            return View(model);
        }

        // Check if user is locked out
        if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Admin login attempt for locked account: {Email}", model.Email);
            ModelState.AddModelError("error", "Account is temporarily locked. Please try again later.");
            return View(model);
        }

        // Verify password without issuing auth cookie yet
        var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!passwordValid)
        {
            _logger.LogWarning("Admin login failed password check for email: {Email}", model.Email);
            ModelState.AddModelError("error", "The provided username or password is incorrect.");
            return View(model);
        }

        // Determine destination phone number for OTP from user record
        string targetPhone = string.Empty;
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin") || user.Id == 1;

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            targetPhone = user.PhoneNumber.Trim();
        }

        if (string.IsNullOrWhiteSpace(targetPhone))
        {
            _logger.LogError("Login OTP dispatch failed: No phone number configured for user {Email}", model.Email);
            ModelState.AddModelError("error", "No mobile number configured for OTP verification for this account. Please contact system administrator.");
            return View(model);
        }

        // Generate 6-digit OTP and session TempToken
        var otp = Random.Shared.Next(100000, 999999).ToString();
        var tempToken = Guid.NewGuid().ToString("N");
        var expirationMinutes = _adminAuthSettings.OtpExpirationMinutes > 0 ? _adminAuthSettings.OtpExpirationMinutes : 5;
        var expiresAt = DateTime.Now.AddMinutes(expirationMinutes);

        // Save OTP record to database
        var otpRecord = new LoginOtpVerification
        {
            UserId = user.Id,
            Email = user.Email ?? model.Email.Trim(),
            PhoneNumber = targetPhone,
            OtpCode = otp,
            Purpose = isAdmin ? "AdminLogin" : "StaffLogin",
            TempToken = tempToken,
            ExpiresAt = expiresAt,
            IsUsed = false,
            AttemptCount = 0,
            ResendCount = 0,
            CreatedOn = DateTime.Now,
            CreatedBy = "System",
            ModifiedOn = DateTime.Now,
            ModifiedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        try
        {
            await _dbContext.LoginOtpVerifications.AddAsync(otpRecord);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save LoginOtpVerification record for user {Email}", model.Email);
            ModelState.AddModelError("error", "Failed to initiate verification. Please try again.");
            return View(model);
        }

        // Send OTP via SMS
        try
        {
            var smsSent = await _emailService.SendSmsAsync(otp, targetPhone);
            if (!smsSent)
            {
                _logger.LogWarning("SMS dispatch returned false for {PhoneMasked}", MaskPhoneNumber(targetPhone));
            }
            else
            {
                _logger.LogInformation("Login OTP sent successfully to {PhoneMasked} for user {Email}", MaskPhoneNumber(targetPhone), model.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending login OTP SMS to {PhoneMasked}", MaskPhoneNumber(targetPhone));
        }

        // Switch to OTP Verification Step
        var stateModel = new AdminLoginStateViewModel
        {
            Email = user.Email ?? model.Email.Trim(),
            Step = AdminLoginStep.OtpVerification,
            TempToken = tempToken,
            MaskedPhone = MaskPhoneNumber(targetPhone),
            ReturnUrl = model.ReturnUrl,
            CooldownSeconds = _adminAuthSettings.ResendCooldownSeconds > 0 ? _adminAuthSettings.ResendCooldownSeconds : 60,
            SuccessMessage = $"A 6-digit verification code has been sent to {MaskPhoneNumber(targetPhone)}."
        };

        return View(stateModel);
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [HttpPost("/account/verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyAdminOtpRequest request)
    {
        var model = new AdminLoginStateViewModel
        {
            Step = AdminLoginStep.OtpVerification,
            TempToken = request.TempToken,
            ReturnUrl = request.ReturnUrl,
            CooldownSeconds = _adminAuthSettings.ResendCooldownSeconds > 0 ? _adminAuthSettings.ResendCooldownSeconds : 60
        };

        if (string.IsNullOrWhiteSpace(request.TempToken))
        {
            model.Step = AdminLoginStep.Credentials;
            model.ErrorMessage = "Session expired. Please log in again.";
            return View("Index", model);
        }

        var otpRecord = await _dbContext.LoginOtpVerifications
            .Where(x => x.TempToken == request.TempToken && !x.IsDeleted && !x.IsUsed)
            .OrderByDescending(x => x.CreatedOn)
            .FirstOrDefaultAsync();

        if (otpRecord == null)
        {
            model.Step = AdminLoginStep.Credentials;
            model.ErrorMessage = "Invalid or expired session. Please log in again.";
            return View("Index", model);
        }

        model.Email = otpRecord.Email;
        model.MaskedPhone = MaskPhoneNumber(otpRecord.PhoneNumber);

        // Check if OTP is expired
        if (DateTime.Now > otpRecord.ExpiresAt)
        {
            model.ErrorMessage = "The verification code has expired. Please click 'Resend OTP' to get a new code.";
            return View("Index", model);
        }

        // Check max failed attempts
        var maxAttempts = _adminAuthSettings.MaxFailedAttempts > 0 ? _adminAuthSettings.MaxFailedAttempts : 5;
        if (otpRecord.AttemptCount >= maxAttempts)
        {
            model.ErrorMessage = "Maximum verification attempts exceeded. Please click 'Resend OTP' to request a new code.";
            return View("Index", model);
        }

        if (string.IsNullOrWhiteSpace(request.Otp) || request.Otp.Trim() != otpRecord.OtpCode.Trim())
        {
            otpRecord.AttemptCount++;
            otpRecord.ModifiedOn = DateTime.Now;
            await _dbContext.SaveChangesAsync();

            var remaining = Math.Max(0, maxAttempts - otpRecord.AttemptCount);
            model.ErrorMessage = remaining > 0
                ? $"Invalid verification code. {remaining} attempt(s) remaining."
                : "Invalid verification code. Maximum attempts reached. Please request a new OTP.";
            return View("Index", model);
        }

        // OTP is valid - Mark as used
        otpRecord.IsUsed = true;
        otpRecord.ModifiedOn = DateTime.Now;
        await _dbContext.SaveChangesAsync();

        // Sign in user
        var user = await _userManager.FindByIdAsync(otpRecord.UserId.ToString());
        if (user == null)
        {
            model.Step = AdminLoginStep.Credentials;
            model.ErrorMessage = "User account not found. Please contact administrator.";
            return View("Index", model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("Admin user {Email} successfully authenticated and signed in via OTP verification", user.Email);

        if (!string.IsNullOrEmpty(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
        {
            return LocalRedirect(request.ReturnUrl);
        }

        return RedirectToAction("Index", "Admin");
    }

    [AllowAnonymous]
    [HttpPost("/account/resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendAdminOtpRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TempToken))
        {
            return BadRequest(new { success = false, message = "Invalid session token." });
        }

        var otpRecord = await _dbContext.LoginOtpVerifications
            .Where(x => x.TempToken == request.TempToken && !x.IsDeleted && !x.IsUsed)
            .OrderByDescending(x => x.CreatedOn)
            .FirstOrDefaultAsync();

        if (otpRecord == null)
        {
            return BadRequest(new { success = false, message = "Session expired or not found. Please log in again." });
        }

        // Check cooldown
        var cooldown = _adminAuthSettings.ResendCooldownSeconds > 0 ? _adminAuthSettings.ResendCooldownSeconds : 60;
        var elapsed = (DateTime.Now - otpRecord.ModifiedOn).TotalSeconds;
        if (elapsed < cooldown)
        {
            var remaining = (int)Math.Ceiling(cooldown - elapsed);
            return BadRequest(new { success = false, message = $"Please wait {remaining} seconds before requesting another OTP.", remainingSeconds = remaining });
        }

        // Check max resends
        var maxResends = _adminAuthSettings.MaxResends > 0 ? _adminAuthSettings.MaxResends : 5;
        if (otpRecord.ResendCount >= maxResends)
        {
            return BadRequest(new { success = false, message = "Maximum resend limit reached. Please restart the login process." });
        }

        // Generate new OTP and update record
        var newOtp = Random.Shared.Next(100000, 999999).ToString();
        var expMinutes = _adminAuthSettings.OtpExpirationMinutes > 0 ? _adminAuthSettings.OtpExpirationMinutes : 5;

        otpRecord.OtpCode = newOtp;
        otpRecord.ExpiresAt = DateTime.Now.AddMinutes(expMinutes);
        otpRecord.ResendCount++;
        otpRecord.AttemptCount = 0; // Reset attempts for newly generated OTP
        otpRecord.ModifiedOn = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        // Dispatch SMS
        try
        {
            await _emailService.SendSmsAsync(newOtp, otpRecord.PhoneNumber);
            _logger.LogInformation("Resent login OTP to {PhoneMasked} for user {Email}", MaskPhoneNumber(otpRecord.PhoneNumber), otpRecord.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send resent login OTP SMS to {PhoneMasked}", MaskPhoneNumber(otpRecord.PhoneNumber));
        }

        return Ok(new
        {
            success = true,
            message = $"A new verification code has been sent to {MaskPhoneNumber(otpRecord.PhoneNumber)}.",
            cooldownSeconds = cooldown
        });
    }

    [Authorize]
    [HttpGet("/account/check")]
    public async Task<string> Check()
    {
        return (await _userManager.GetUserAsync(User)).Id.ToString();
    }

    [HttpGet("/account/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index");
    }

    #endregion

    #region Helpers

    private static string MaskPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "******";
        var clean = phone.Trim();
        if (clean.Length <= 4) return clean;
        var last4 = clean.Substring(clean.Length - 4);
        var prefix = clean.Length > 6 ? clean.Substring(0, Math.Min(3, clean.Length - 4)) : "";
        return string.IsNullOrEmpty(prefix) ? $"******{last4}" : $"{prefix}******{last4}";
    }

    #endregion
}