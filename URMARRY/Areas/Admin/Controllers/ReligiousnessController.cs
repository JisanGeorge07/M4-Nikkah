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
    public class ReligiousnessController : Controller
    {
        private readonly IRepository<Religiousness> _repo;
        private readonly IMapper _mapper;

        public ReligiousnessController(IRepository<Religiousness> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("/admin/religiousness")]
        public async Task<IActionResult> GetAll()
        {
            return View(_mapper.Map<List<ReligiousnessDto>>(await _repo.GetAll()));
        }

        [HttpGet("/admin/religiousness/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            return View(id > 0
                ? _mapper.Map<ReligiousnessDto>(await _repo.Get(id))
                : new ReligiousnessDto
                {
                    IsActive = true,
                    Id = 0
                });
        }

        [HttpPost("/admin/religiousness")]
        public async Task<IActionResult> Post(ReligiousnessDto model)
        {
            Religiousness entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<Religiousness>(model);
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

        [HttpPost("/admin/religiousness/delete/{id:long}")]
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
