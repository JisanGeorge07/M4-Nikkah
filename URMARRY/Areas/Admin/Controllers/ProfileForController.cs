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
    public class ProfileForController : Controller
    {
        private readonly IRepository<ProfileFor> _repo;
        private readonly IMapper _mapper;

        public ProfileForController(IRepository<ProfileFor> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("/admin/profile-for")]
        public async Task<IActionResult> GetAll()
        {
            return View(_mapper.Map<List<ProfileForDto>>(await _repo.GetAll()));
        }

        [HttpGet("/admin/profile-for/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            return View(id > 0
                ? _mapper.Map<ProfileForDto>(await _repo.Get(id))
                : new ProfileForDto
                {
                    IsActive = true,
                    Id = 0
                });
        }

        [HttpPost("/admin/profile-for")]
        public async Task<IActionResult> Post(ProfileForDto model)
        {
            ProfileFor entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<ProfileFor>(model);
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

        [HttpPost("/admin/profile-for/delete/{id:long}")]
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
