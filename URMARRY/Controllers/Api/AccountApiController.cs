using Application.Helpers;
using Application.Interfaces.Persistence;
using Application.Models;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace URMARRY.Controllers.Api
{
    [ApiController]
    [Route("api/account")]
    public class AccountApiController : ControllerBase
    {
        private readonly IRepository<Registration> _registrationRepo;
        private readonly ILogger<AccountApiController> _logger;
        private readonly Guid key = new Guid("F7AD797A-416C-4C56-8749-7017FBC90427");

        public AccountApiController(
            IRepository<Registration> registrationRepo,
            ILogger<AccountApiController> logger)
        {
            _registrationRepo = registrationRepo;
            _logger = logger;
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ApiChangePasswordRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input details." });
                }

                var user = await _registrationRepo.FirstOrDefaultActive(x => x.Id == model.UserId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);

                // Verify the old password matches the current decrypted password
                string decryptedStoredPassword = encryption.DecryptRijndael(user.Password, user.PasswordHash);
                if (model.OldPassword != decryptedStoredPassword)
                {
                    return BadRequest(new { success = false, message = "Incorrect old password." });
                }

                // Verify the new password matches the confirm password
                if (model.NewPassword != model.ConfirmPassword)
                {
                    return BadRequest(new { success = false, message = "Password and confirm password do not match." });
                }

                // Hash and encrypt the new password
                user.PasswordHash = encryption.CreateSalt();
                user.Password = encryption.EncryptRijndael(model.ConfirmPassword, user.PasswordHash);

                await _registrationRepo.Update(user);
                await _registrationRepo.SaveChanges();

                _logger.LogInformation("Password updated successfully via API for user ID: {UserId}", model.UserId);
                return Ok(new { success = true, message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password via API for user ID: {UserId}", model.UserId);
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request." });
            }
        }
    }
}
