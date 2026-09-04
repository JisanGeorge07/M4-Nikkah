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
    public class MotherTongueController : Controller
    {
        private readonly IRepository<MotherTongue> _repo;
        private readonly IMapper _mapper;

        public MotherTongueController(IRepository<MotherTongue> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("/admin/mother-tongue")]
        public async Task<IActionResult> GetAll()
        {
            return View(_mapper.Map<List<MotherTongueDto>>(await _repo.GetAll()));
        }

        [HttpGet("/admin/mother-tongue/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            return View(id > 0
                ? _mapper.Map<MotherTongueDto>(await _repo.Get(id))
                : new MotherTongueDto
                {
                    IsActive = true,
                    Id = 0
                });
        }

        [HttpPost("/admin/mother-tongue")]
        public async Task<IActionResult> Post(MotherTongueDto model)
        {
            MotherTongue entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<MotherTongue>(model);
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

        [HttpPost("/admin/mother-tongue/delete/{id:long}")]
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
