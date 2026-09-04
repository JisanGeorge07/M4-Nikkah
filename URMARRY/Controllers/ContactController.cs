using Application.Constants;
using Application.Interfaces.Infrastructure;
using Application.Interfaces.Infrastructure.Email;
using Application.Interfaces.Persistence;
using Application.Models.Framework;
using Application.Models;
using AspNetCore.ReCaptcha;
using AutoMapper;
using Domain;
using Domain.Framework;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Razor.Templating.Core;
using URMARRY.Models;

namespace URMARRY.Controllers
{
	public class ContactController : Controller
	{
        private readonly IRepository<Contact> _repo;
        private readonly IMapper _mapper;
        private readonly IRepository<Email> _emailRepo;
        private readonly IEmailService _emailService;
        private readonly IRepository<ContactEnquiry> _enquiryRepo;

        public ContactController(IRepository<Contact> repo,
            IMapper mapper, IRepository<Email> emailRepo,
            IEmailService emailService,
            IRepository<ContactEnquiry> enquiryRepo)
        {
            _repo = repo;
            _mapper = mapper;
            _emailRepo = emailRepo;
            _emailService = emailService;
            _enquiryRepo = enquiryRepo;
        }

        [HttpGet("/contact")]
        public async Task<IActionResult> Index()
        {
            if (TempData["ValidationErrors"] is string)
                ModelState.AddModelError("ValidationErrors", TempData["ValidationErrors"] as string ?? string.Empty);
            return View(
             new HomeViewModel
             {
                 Contact = _mapper.Map<ContactDto>(await _repo.First()),
                 ContactEnquiry = new ContactEnquiryDto(),
             });
        }

        [ValidateReCaptcha]
        [HttpPost("/contact")]
        public async Task<IActionResult> PostEnquiry(ContactEnquiryDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                TempData["ValidationErrors"] = string.Join("\n", errors);
                return RedirectToAction(nameof(Index));
            }
            model.Email = model.Email!.ToLower();
            model.Purpose = EnquiryTypes.General;
            var entity = _mapper.Map<ContactEnquiry>(model);
            await _enquiryRepo.Add(entity);
            await _enquiryRepo.SaveChanges();
            var email = _mapper.Map<EmailDto>(await _emailRepo.FirstOrDefault(x => x.Purpose == EnquiryTypes.General));
            if (!string.IsNullOrWhiteSpace(email.EmailId))
            {
                model.CreatedOn = DateTime.Now;
                var emailModel = new HomeViewModel
                {
                    Contact = _mapper.Map<ContactDto>(await _repo.First()),
                    ContactEnquiry = model,
                    SenderEmail = email.EmailId
                };

                if (!string.IsNullOrWhiteSpace(email.Recipients))
                {
                    var enquiryHtml =
                        await RazorTemplateEngine.RenderAsync("/Templates/Mail/Contact/AdminNotification.cshtml",
                            emailModel);
                    await _emailService.Send(new Message
                    {
                        From = email,
                        To = email.Recipients?.Split(",").Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => new MailboxAddress("M4NIKAH", x)).ToList(),
                        Subject = "New contact enquiry from " + model.Name,
                        Content = enquiryHtml
                    });
                }

                //Confirmation email
                if (!string.IsNullOrWhiteSpace(model.Email))
                {
                    var enquiryConfirmationHtml =
                        await RazorTemplateEngine.RenderAsync("/Templates/Mail/Contact/SenderConfirmation.cshtml",
                            emailModel);
                    await _emailService.Send(new Message
                    {
                        From = email,
                        To = new List<MailboxAddress> { new(model.Name, model.Email) },
                        Subject = "Thank you for contacting M4NIKAH",
                        Content = enquiryConfirmationHtml
                    });
                }
            }

            return RedirectToAction("Result", "Home");
        }
    }
}
