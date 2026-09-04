using Application.Constants;
using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMARRY.Areas.Admin.Models;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class BodyFeaturesController : Controller
    {
        private readonly IRepository<BodyFeatures> _repo;
        private readonly IMapper _mapper;

        public BodyFeaturesController(IRepository<BodyFeatures> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("/admin/body-features")]
        public async Task<IActionResult> GetAll(string type = BodyFeature.Height)
        {
            return View(new BodyFeaturesViewModel
            {
                BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _repo.Where(x => x.Type == type)),
                Type = type
            });
        }

        [HttpGet("/admin/body-features/{id:long}")]
        public async Task<IActionResult> Get(long id, string type)
        {
            return View(id > 0
                ? _mapper.Map<BodyFeaturesDto>(await _repo.Get(id))
                : new BodyFeaturesDto
                {
                    IsActive = true,
                    Id = 0,
                    Type = type
                });
        }

        [HttpPost("/admin/body-features")]
        public async Task<IActionResult> Post(BodyFeaturesDto model)
        {
            BodyFeatures entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<BodyFeatures>(model);
                var data = await _repo.Where(x => x.Type == model.Type);
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

        [HttpPost("/admin/body-features/delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _repo.Get(id);
            if (entity != null)
            {
                await _repo.SoftDelete(entity);
                await _repo.SaveChanges();
            }

            return RedirectToAction(nameof(GetAll), new { type = entity.Type });
        }
    }
}
