using Application.Constants;
using Application.Interfaces.Persistence;
using Application.Models;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using URMARRY.Services;

namespace URMARRY.Controllers.Api
{
    [ApiController]
    [Route("api/support")]
    public class SupportApiController : ControllerBase
    {
        private readonly IRepository<SupportRequest> _supportRequestRepo;
        private readonly IRepository<Registration> _userRepository;
        private readonly IFileService _fileService;
        private readonly ILogger<SupportApiController> _logger;

        public SupportApiController(
            IRepository<SupportRequest> supportRequestRepo,
            IRepository<Registration> userRepository,
            IFileService fileService,
            ILogger<SupportApiController> logger)
        {
            _supportRequestRepo = supportRequestRepo;
            _userRepository = userRepository;
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Submits a profile suspension appeal request.
        /// Suitable for consumption by both the mobile application and the web interface.
        /// </summary>
        [HttpPost("submit-appeal")]
        public async Task<IActionResult> SubmitAppeal([FromForm] SupportAppealSubmitModel model)
        {
            try
            {
                if (model == null || model.UserId <= 0)
                {
                    return BadRequest(new { success = false, message = "User ID is required." });
                }

                if (string.IsNullOrWhiteSpace(model.Message))
                {
                    return BadRequest(new { success = false, message = "Appeal message cannot be empty." });
                }

                var user = await _userRepository.Get(model.UserId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                if (user.DisabledReason != DisabledReason.ReportedViolation)
                {
                    return BadRequest(new { success = false, message = "User profile is not suspended under policy violations." });
                }

                var hasPending = (await _supportRequestRepo.Where(x => x.UserId == user.Id && x.Status == "Pending")).Any();
                if (hasPending)
                {
                    return BadRequest(new { success = false, message = "You already have a pending appeal under review." });
                }

                string? attachmentPath = null;
                if (model.Attachment != null && model.Attachment.Length > 0)
                {
                    attachmentPath = await _fileService.UploadFile(model.Attachment, "Uploads/Support");
                }

                var supportRequest = new SupportRequest
                {
                    UserId = user.Id,
                    Subject = "Profile Suspension Appeal",
                    Message = model.Message,
                    Status = "Pending",
                    AttachmentPath = attachmentPath,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                await _supportRequestRepo.Add(supportRequest);
                await _supportRequestRepo.SaveChanges();

                _logger.LogInformation("Support appeal submitted successfully via API for user ID: {UserId}", model.UserId);

                return Ok(new { success = true, message = "Your appeal has been submitted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting support appeal via API for user ID: {UserId}", model?.UserId);
                return StatusCode(500, new { success = false, message = "An error occurred while processing your appeal." });
            }
        }
    }
}
