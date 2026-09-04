using Application.Interfaces.Infrastructure;
using Application.Models;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace URMARRY.Controllers.Api.Staff
{
    [ApiController]
    [Route("api/staff")]
    public class StaffAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StaffAuthController> _logger;
        private readonly IEmailService _emailService;
        private readonly AppDbContext _dbContext;
        private readonly AdminAuthSettings _authSettings;

        private const int TokenExpiryDays = 7;

        public StaffAuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<StaffAuthController> logger,
            IEmailService emailService,
            AppDbContext dbContext,
            IOptions<AdminAuthSettings> authSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
            _dbContext = dbContext;
            _authSettings = authSettings.Value ?? new AdminAuthSettings();
        }

        /// <summary>
        /// Staff login initiation endpoint. Authenticates credentials and sends OTP to the staff's registered phone number.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] StaffLoginRequest model)
        {
            try
            {
                // 1. Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Email and password are required."
                    });
                }

                // 2. Find user by email
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid email or password."
                    });
                }

                // 3. Verify user is in "Staff" role
                var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
                if (!isStaff)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Access denied. Not a staff member."
                    });
                }

                // 4. Check if account is locked/disabled
                bool isLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;
                if (isLocked)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Account is disabled. Contact administrator."
                    });
                }

                // 5. Verify password
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!result.Succeeded)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid email or password."
                    });
                }

                // 6. Verify phone number exists for staff
                string targetPhone = user.PhoneNumber?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(targetPhone))
                {
                    _logger.LogWarning("Staff user {Email} (ID: {UserId}) has no registered phone number for OTP verification.", user.Email, user.Id);
                    return BadRequest(new
                    {
                        success = false,
                        message = "No mobile number is registered with your staff account. Please contact administrator."
                    });
                }

                // 7. Generate 6-digit OTP and session tempToken
                var otp = Random.Shared.Next(100000, 999999).ToString();
                var tempToken = Guid.NewGuid().ToString("N");
                var expMinutes = _authSettings.OtpExpirationMinutes > 0 ? _authSettings.OtpExpirationMinutes : 5;
                var cooldownSeconds = _authSettings.ResendCooldownSeconds > 0 ? _authSettings.ResendCooldownSeconds : 60;
                var expiresAt = DateTime.Now.AddMinutes(expMinutes);

                // 8. Save OTP record to database
                var otpRecord = new LoginOtpVerification
                {
                    UserId = user.Id,
                    Email = user.Email ?? model.Email.Trim(),
                    PhoneNumber = targetPhone,
                    OtpCode = otp,
                    Purpose = "StaffLogin",
                    TempToken = tempToken,
                    ExpiresAt = expiresAt,
                    IsUsed = false,
                    AttemptCount = 0,
                    ResendCount = 0,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "StaffAuthApi",
                    ModifiedOn = DateTime.Now,
                    ModifiedBy = "StaffAuthApi",
                    IsActive = true,
                    IsDeleted = false
                };

                await _dbContext.LoginOtpVerifications.AddAsync(otpRecord);
                await _dbContext.SaveChangesAsync();

                // 9. Dispatch SMS
                try
                {
                    var smsSent = await _emailService.SendSmsAsync(otp, targetPhone);
                    if (!smsSent)
                    {
                        _logger.LogWarning("Staff OTP SMS dispatch returned false for {PhoneMasked}", MaskPhoneNumber(targetPhone));
                    }
                    else
                    {
                        _logger.LogInformation("Staff OTP sent successfully to {PhoneMasked} for user {Email}", MaskPhoneNumber(targetPhone), user.Email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception while sending staff login OTP SMS to {PhoneMasked}", MaskPhoneNumber(targetPhone));
                }

                // 10. Return response containing existing response fields and OTP metadata
                return Ok(new
                {
                    success = true,
                    requiresOtp = true,
                    data = new
                    {
                        Id = user.Id.ToString(),
                        name = user.NormalizedUserName ?? user.UserName,
                        email = user.Email ?? string.Empty,
                        role = "Staff",
                        tempToken = tempToken,
                        maskedPhone = MaskPhoneNumber(targetPhone),
                        cooldownSeconds = cooldownSeconds,
                        expiresInMinutes = expMinutes
                    },
                    message = $"OTP has been sent to your registered mobile number {MaskPhoneNumber(targetPhone)}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during staff login initiation for email: {Email}", model.Email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        /// <summary>
        /// Staff OTP verification endpoint. Validates OTP code and issues JWT token upon successful verification.
        /// </summary>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] StaffVerifyOtpRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Session token and 6-digit OTP are required."
                    });
                }

                // 1. Find active OTP record
                var otpRecord = await _dbContext.LoginOtpVerifications
                    .Where(x => x.TempToken == model.TempToken && x.Purpose == "StaffLogin" && !x.IsDeleted && !x.IsUsed)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid or expired session. Please login again."
                    });
                }

                // 2. Check expiration
                if (DateTime.Now > otpRecord.ExpiresAt)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "The verification code has expired. Please request a new OTP."
                    });
                }

                // 3. Check max failed attempts
                var maxAttempts = _authSettings.MaxFailedAttempts > 0 ? _authSettings.MaxFailedAttempts : 5;
                if (otpRecord.AttemptCount >= maxAttempts)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Maximum verification attempts exceeded. Please request a new OTP."
                    });
                }

                // 4. Validate OTP code
                if (string.IsNullOrWhiteSpace(model.Otp) || model.Otp.Trim() != otpRecord.OtpCode.Trim())
                {
                    otpRecord.AttemptCount++;
                    otpRecord.ModifiedOn = DateTime.Now;
                    await _dbContext.SaveChangesAsync();

                    var remaining = Math.Max(0, maxAttempts - otpRecord.AttemptCount);
                    return BadRequest(new
                    {
                        success = false,
                        message = remaining > 0
                            ? $"Invalid verification code. {remaining} attempt(s) remaining."
                            : "Invalid verification code. Maximum attempts reached. Please request a new OTP."
                    });
                }

                // 5. Mark OTP as used
                otpRecord.IsUsed = true;
                otpRecord.ModifiedOn = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                // 6. Find user and verify status
                var user = await _userManager.FindByIdAsync(otpRecord.UserId.ToString());
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Staff account not found."
                    });
                }

                bool isLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;
                if (isLocked)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Account is disabled. Contact administrator."
                    });
                }

                // 7. Generate JWT token
                var token = GenerateJwtToken(user);
                _logger.LogInformation("Staff OTP verified and login successful for user: {UserName} (ID: {UserId})", user.UserName, user.Id);

                // 8. Return response matching existing data structure
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Id = user.Id.ToString(),
                        name = user.NormalizedUserName ?? user.UserName,
                        email = user.Email ?? string.Empty,
                        role = "Staff",
                        token = token
                    },
                    message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during staff OTP verification.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        /// <summary>
        /// Staff resend OTP endpoint. Resends OTP to the staff's registered phone number enforcing cooldown.
        /// </summary>
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] StaffResendOtpRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Session token is required."
                    });
                }

                // 1. Find active OTP record
                var otpRecord = await _dbContext.LoginOtpVerifications
                    .Where(x => x.TempToken == model.TempToken && x.Purpose == "StaffLogin" && !x.IsDeleted && !x.IsUsed)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid or expired session. Please start the login process again."
                    });
                }

                // 2. Check cooldown
                var cooldown = _authSettings.ResendCooldownSeconds > 0 ? _authSettings.ResendCooldownSeconds : 60;
                var elapsed = (DateTime.Now - otpRecord.ModifiedOn).TotalSeconds;
                if (elapsed < cooldown)
                {
                    var remaining = (int)Math.Ceiling(cooldown - elapsed);
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Please wait {remaining} seconds before requesting another OTP.",
                        remainingSeconds = remaining
                    });
                }

                // 3. Check max resends
                var maxResends = _authSettings.MaxResends > 0 ? _authSettings.MaxResends : 5;
                if (otpRecord.ResendCount >= maxResends)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Maximum resend limit reached. Please restart the login process."
                    });
                }

                // 4. Generate new OTP and update record
                var newOtp = Random.Shared.Next(100000, 999999).ToString();
                var expMinutes = _authSettings.OtpExpirationMinutes > 0 ? _authSettings.OtpExpirationMinutes : 5;

                otpRecord.OtpCode = newOtp;
                otpRecord.ExpiresAt = DateTime.Now.AddMinutes(expMinutes);
                otpRecord.ResendCount++;
                otpRecord.AttemptCount = 0; // Reset attempts for fresh OTP
                otpRecord.ModifiedOn = DateTime.Now;

                await _dbContext.SaveChangesAsync();

                // 5. Dispatch SMS
                try
                {
                    await _emailService.SendSmsAsync(newOtp, otpRecord.PhoneNumber);
                    _logger.LogInformation("Resent staff login OTP to {PhoneMasked} for user {Email}", MaskPhoneNumber(otpRecord.PhoneNumber), otpRecord.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send resent staff login OTP SMS to {PhoneMasked}", MaskPhoneNumber(otpRecord.PhoneNumber));
                }

                return Ok(new
                {
                    success = true,
                    message = $"A new verification code has been sent to {MaskPhoneNumber(otpRecord.PhoneNumber)}.",
                    data = new
                    {
                        maskedPhone = MaskPhoneNumber(otpRecord.PhoneNumber),
                        cooldownSeconds = cooldown
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during staff OTP resend.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        /// <summary>
        /// Generates a JWT token for the authenticated staff member.
        /// Uses JwtSettings:Secret from appsettings.json (same key as CookieHelper).
        /// </summary>
        private string GenerateJwtToken(ApplicationUser user)
        {
            var secret = _configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("JWT secret is not configured in appsettings.json under JwtSettings:Secret");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(ClaimTypes.Role, "Staff")
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(TokenExpiryDays),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string MaskPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "******";
            var clean = phone.Trim();
            if (clean.Length <= 4) return clean;
            var last4 = clean.Substring(clean.Length - 4);
            var prefix = clean.Length > 6 ? clean.Substring(0, Math.Min(3, clean.Length - 4)) : "";
            return string.IsNullOrEmpty(prefix) ? $"******{last4}" : $"{prefix}******{last4}";
        }
    }
}
