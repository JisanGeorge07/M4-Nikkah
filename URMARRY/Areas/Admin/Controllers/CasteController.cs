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
    public class CasteController : Controller
    {
        private readonly IRepository<ReligionCaste> _repo;
        private readonly IMapper _mapper;

        public CasteController(IRepository<ReligionCaste> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("/admin/caste")]
        public async Task<IActionResult> GetAll()
        {
            return View(_mapper.Map<List<ReligionCasteDto>>(await _repo.Where(x => x.ParentId != 0)));
        }

        [HttpGet("/admin/caste/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            return View(id > 0
                ? _mapper.Map<ReligionCasteDto>(await _repo.Get(id))
                : new ReligionCasteDto
                {
                    IsActive = true,
                    Id = 0
                });
        }

        [HttpPost("/admin/caste")]
        public async Task<IActionResult> Post(ReligionCasteDto model)
        {
            ReligionCaste entity;

            if (model.Id <= 0)
            {
                model.ParentId = 1;
                entity = _mapper.Map<ReligionCaste>(model);
                var data = await _repo.Where(x => x.ParentId != 0);
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

        [HttpPost("/admin/caste/delete/{id:long}")]
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
