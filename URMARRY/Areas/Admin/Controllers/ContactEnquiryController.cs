using Application.Constants;
using Application.Interfaces.Persistence;
using Application.Models;
using Application.Models.Framework;
using AutoMapper;
using Domain;
using Domain.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMARRY.Areas.Admin.Models;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class ContactEnquiryController : Controller
    {
        private readonly IRepository<Email> _emailRepo;
        private readonly IRepository<ContactEnquiry> _enquiryRepo;
        private readonly IMapper _mapper;

        public ContactEnquiryController(
            IRepository<Email> emailRepo,
            IRepository<ContactEnquiry> enquiryRepo,
            IMapper mapper)
        {
            _emailRepo = emailRepo;
            _enquiryRepo = enquiryRepo;
            _mapper = mapper;
        }

        #region Email

        [HttpGet("/admin/contact-enquiry")]
        public async Task<IActionResult> GetAll(string type = EnquiryTypes.General)
        {
            return View(new ContactEnquiryViewModel
            {
                Email = _mapper.Map<EmailDto>(await _emailRepo.First(x => x.Purpose == type)),
                Enquiries = _mapper.Map<List<ContactEnquiryDto>>(await _enquiryRepo.WhereActive(x => x.Purpose == type))
                    .OrderByDescending(x => x.CreatedOn)
                    .ToList()
            });
        }

        [HttpPost("/admin/contact-email")]
        public async Task<IActionResult> PostEmail(EmailDto model)
        {
            var entity = await _emailRepo.First(x => x.Purpose == model.Purpose);

            _mapper.Map(model, entity);

            await _emailRepo.Update(entity);
            await _emailRepo.SaveChanges();


            return RedirectToAction(nameof(GetAll), new { type = model.Purpose });
        }

        #endregion

        #region Enquiry

        [HttpGet("/admin/contact-enquiry/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            return View(_mapper.Map<ContactEnquiryDto>(await _enquiryRepo.Get(id)));
        }

        [HttpPost("/admin/contact-enquiry/delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _enquiryRepo.Get(id);
            if (entity != null)
            {
                await _enquiryRepo.SoftDelete(entity);
                await _enquiryRepo.SaveChanges();
            }

            return RedirectToAction(nameof(GetAll), new { type = entity.Purpose });
        }

        #endregion
    }
}
