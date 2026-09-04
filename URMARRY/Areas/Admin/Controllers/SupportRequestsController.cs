using Application.Constants;
using Application.Helpers;
using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace URMARRY.Areas.Admin.Controllers;

[Authorize]
[Area("Admin")]
public class SupportRequestsController : Controller
{
    private readonly IRepository<SupportRequest> _supportRequestRepo;
    private readonly IRepository<Registration> _userRepository;
    private readonly IRepository<UserReport> _userReportRepo;
    private readonly EmailNotificationHelper _emailNotificationHelper;
    private readonly IMapper _mapper;

    public SupportRequestsController(
        IRepository<SupportRequest> supportRequestRepo,
        IRepository<Registration> userRepository,
        IRepository<UserReport> userReportRepo,
        EmailNotificationHelper emailNotificationHelper,
        IMapper mapper)
    {
        _supportRequestRepo = supportRequestRepo;
        _userRepository = userRepository;
        _userReportRepo = userReportRepo;
        _emailNotificationHelper = emailNotificationHelper;
        _mapper = mapper;
    }

    [HttpGet("/admin/support-requests")]
    public async Task<IActionResult> GetAll()
    {
        var requests = await _supportRequestRepo.GetAll();
        var users = await _userRepository.GetAll();
        var userMap = users.ToDictionary(x => x.Id, x => x);

        var dtos = requests.OrderByDescending(x => x.CreatedOn).Select(x => {
            var dto = _mapper.Map<SupportRequestDto>(x);
            if (userMap.TryGetValue(x.UserId, out var u))
            {
                dto.UserRegisterNumber = u.RegisterNumber;
                dto.UserName = u.Name;
                dto.UserEmail = u.Email;
                dto.UserPhone = u.Phone;
            }
            return dto;
        }).ToList();

        return View(dtos);
    }

    [HttpGet("/admin/support-requests/{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var request = await _supportRequestRepo.Get(id);
        if (request == null) return RedirectToAction(nameof(GetAll));

        var dto = _mapper.Map<SupportRequestDto>(request);
        var user = await _userRepository.Get(request.UserId);
        if (user != null)
        {
            dto.UserRegisterNumber = user.RegisterNumber;
            dto.UserName = user.Name;
            dto.UserEmail = user.Email;
            dto.UserPhone = user.Phone;
        }

        return View(dto);
    }

    [HttpPost("/admin/support-requests/reactivate/{id:long}")]
    public async Task<IActionResult> Reactivate(long id, string? adminNotes)
    {
        var request = await _supportRequestRepo.Get(id);
        if (request != null)
        {
            request.Status = "Resolved";
            request.AdminNotes = adminNotes ?? "Approved and reactivated by Admin.";
            request.ResolvedOn = DateTime.UtcNow;
            await _supportRequestRepo.Update(request);
            await _supportRequestRepo.SaveChanges();

            // Reactivate user
            var user = await _userRepository.Get(request.UserId);
            if (user != null)
            {
                user.DisabledReason = 0; // DisabledReason.Deactivated (which is 0)
                user.IsVisible = true;
                await _userRepository.Update(user);
                await _userRepository.SaveChanges();

                // Clear policy violation reports
                var reports = await _userReportRepo.Where(x => x.ReportedUserId == user.Id);
                foreach (var report in reports)
                {
                    await _userReportRepo.SoftDelete(report);
                }
                await _userReportRepo.SaveChanges();

                // Send email notification
                await SendResolutionEmail(request, user, "Resolved", "Based on our investigation and the details provided in your appeal, we have reactivated your profile. You can now access all features.");
            }
        }
        return RedirectToAction(nameof(GetAll));
    }

    [HttpPost("/admin/support-requests/reject/{id:long}")]
    public async Task<IActionResult> Reject(long id, string? adminNotes)
    {
        var request = await _supportRequestRepo.Get(id);
        if (request != null)
        {
            request.Status = "Rejected";
            request.AdminNotes = adminNotes ?? "Appeal rejected by Admin.";
            request.ResolvedOn = DateTime.UtcNow;
            await _supportRequestRepo.Update(request);
            await _supportRequestRepo.SaveChanges();

            // Deactivate and recycle user account
            var user = await _userRepository.Get(request.UserId);
            if (user != null)
            {
                user.DisabledReason = DisabledReason.Recycled;
                user.IsActive = false;
                await _userRepository.Update(user);
                await _userRepository.SaveChanges();

                // Send email notification
                await SendResolutionEmail(request, user, "Rejected", "Based on our investigation, we have found that your profile violated our community guidelines. Unfortunately, your appeal has been rejected and your account remains deactivated.");
            }
        }
        return RedirectToAction(nameof(GetAll));
    }

    private async Task SendResolutionEmail(SupportRequest request, Registration user, string status, string actionDetails)
    {
        try
        {
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates/Mail/Support/Resolution.html");
            if (System.IO.File.Exists(templatePath))
            {
                string emailContent = await System.IO.File.ReadAllTextAsync(templatePath);
                emailContent = emailContent.Replace("[UserName]", user.Name)
                                         .Replace("[UserRegisterNumber]", user.RegisterNumber)
                                         .Replace("[Status]", status)
                                         .Replace("[StatusClass]", status == "Resolved" ? "status-resolved" : "status-rejected")
                                         .Replace("[AdminNotes]", request.AdminNotes ?? "No additional notes provided.")
                                         .Replace("[ActionDetails]", actionDetails);

                var subject = $"Support Appeal Update: {status} - M4Nikah";
                var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                _emailNotificationHelper.SendEmail(user.Email!, htmlView, subject);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't break the flow
            Console.WriteLine($"Error sending support appeal resolution email: {ex.Message}");
        }
    }
}
