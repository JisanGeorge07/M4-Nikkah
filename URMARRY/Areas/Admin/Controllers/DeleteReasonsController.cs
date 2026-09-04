using Application.Interfaces.Persistence;
using Application.Models;
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
    public class DeleteReasonsController : Controller
    {
        private readonly IRepository<DeleteReason> _repo;
        private readonly IMapper _mapper;

        public DeleteReasonsController(IRepository<DeleteReason> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("/admin/delete-reasons")]
        public async Task<IActionResult> GetAll()
        {
            var data = (await _repo.GetAll()).OrderBy(x => x.DisplayOrder).ToList();
            return View(_mapper.Map<List<DeleteReasonDto>>(data));
        }

        [HttpGet("/admin/delete-reasons/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            return View(id > 0
                ? _mapper.Map<DeleteReasonDto>(await _repo.Get(id))
                : new DeleteReasonDto
                {
                    IsActive = true,
                    Id = 0
                });
        }

        [HttpPost("/admin/delete-reasons")]
        public async Task<IActionResult> Post(DeleteReasonDto model)
        {
            DeleteReason entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<DeleteReason>(model);
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

        [HttpPost("/admin/delete-reasons/delete/{id:long}")]
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
