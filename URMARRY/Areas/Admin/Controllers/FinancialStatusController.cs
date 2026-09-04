using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class FinancialStatusController : Controller
    {
        private readonly IRepository<FinancialStatus> _repo;
        private readonly IMapper _mapper;

        public FinancialStatusController(IRepository<FinancialStatus> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("/admin/financial-status")]
        public async Task<IActionResult> GetAll()
        {
            return View(_mapper.Map<List<FinancialStatusDto>>(await _repo.GetAll()));
        }

        [HttpGet("/admin/financial-status/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            return View(id > 0
                ? _mapper.Map<FinancialStatusDto>(await _repo.Get(id))
                : new FinancialStatusDto
                {
                    IsActive = true,
                    Id = 0
                });
        }

        [HttpPost("/admin/financial-status")]
        public async Task<IActionResult> Post(FinancialStatusDto model)
        {
            FinancialStatus entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<FinancialStatus>(model);
                var data = await _repo.GetAll();
                entity.DisplayOrder = data.Any() ? data.Max(x => x.DisplayOrder) + 1 : 1;
                await _repo.Add(entity);
            }
            else
            {
                entity = (await _repo.Get(model.Id))!;
                _mapper.Map(model, entity);
                await _repo.Update(entity);
            }

            await _repo.SaveChanges();

            return RedirectToAction(nameof(Get), new { id = entity.Id });
        }

        [HttpPost("/admin/financial-status/delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _repo.Get(id);
            if (entity != null)
            {
                await _repo.SoftDelete(entity);
                await _repo.SaveChanges();
            }

            return RedirectToAction(nameof(GetAll));
        }
    }
}
