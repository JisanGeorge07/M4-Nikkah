using Application.Interfaces.Persistence;
using Application.Models.Call;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class CallReportReasonsController : Controller
    {
        private readonly IRepository<CallReportReason> _repo;
        private readonly IMapper _mapper;

        public CallReportReasonsController(IRepository<CallReportReason> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("/admin/call-report-reasons")]
        public async Task<IActionResult> Index()
        {
            var reasons = (await _repo.GetAll()).OrderBy(x => x.DisplayOrder).ToList();
            return View(_mapper.Map<List<CallReportReasonDto>>(reasons));
        }

        [HttpGet("/admin/call-report-reasons/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            CallReportReasonDto? dto = id > 0
                ? _mapper.Map<CallReportReasonDto>(await _repo.Get(id))
                : new CallReportReasonDto
                {
                    IsActive = true,
                    Id = 0
                };

            return View(dto);
        }

        [HttpPost("/admin/call-report-reasons/post")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(CallReportReasonDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError("Reason", "Reason is required.");
                return View("Get", model);
            }

            CallReportReason entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<CallReportReason>(model);
                var allReasons = await _repo.GetAll();
                entity.DisplayOrder = allReasons.Any() ? allReasons.Max(x => x.DisplayOrder) + 1 : 1;
                await _repo.Add(entity);
            }
            else
            {
                entity = (await _repo.Get(model.Id))!;
                if (entity == null)
                {
                    return NotFound();
                }
                _mapper.Map(model, entity);
                await _repo.Update(entity);
            }

            await _repo.SaveChanges();
            TempData["Success"] = "Call complaint reason saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/call-report-reasons/delete/{id:long}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _repo.Get(id);
            if (entity != null)
            {
                await _repo.SoftDelete(entity);
                await _repo.SaveChanges();
                TempData["Success"] = "Reason deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
