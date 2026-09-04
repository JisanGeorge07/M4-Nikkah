using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace URMARRY.Areas.Admin.Controllers;

[Authorize]
[Area("Admin")]
public class SocialMediaController : Controller
{
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;
    private readonly IRepository<SocialMedia> _socialMediaRepo;

    public SocialMediaController(
        IRepository<SocialMedia> socialMediaRepo,
        IMapper mapper,
        IFileService fileService)
    {
        _socialMediaRepo = socialMediaRepo;
        _mapper = mapper;
        _fileService = fileService;
    }


    #region social-media

    [HttpGet("/admin/social-media")]
    public async Task<IActionResult> GetAll()
    {
        return View(_mapper.Map<List<SocialMediaDto>>(await _socialMediaRepo.GetAll()));
    }

    [HttpGet("/admin/social-media/{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        return View(id > 0
            ? _mapper.Map<SocialMediaDto>(await _socialMediaRepo.Get(id))
            : new SocialMediaDto
            {
                IsActive = true,
                Id = 0
            });
    }

    [HttpPost("/admin/social-media")]
    public async Task<IActionResult> Post(SocialMediaDto model)
    {
        SocialMedia entity;

        if (model.Id <= 0)
        {
            entity = _mapper.Map<SocialMedia>(model);
            await _fileService.SaveAllFiles(entity, model, "Uploads/SocialMedia");
            var socialMedia = await _socialMediaRepo.GetAll();
            entity.DisplayOrder = socialMedia.Any() ? socialMedia.Max(x => x.DisplayOrder) + 1 : 1;
            await _socialMediaRepo.Add(entity);
        }
        else
        {
            entity = (await _socialMediaRepo.Get(model.Id))!;
            await _fileService.SaveAllFiles(entity, model, "Uploads/SocialMedia");
            _mapper.Map(model, entity);
            await _socialMediaRepo.Update(entity);
        }

        await _socialMediaRepo.SaveChanges();

        return RedirectToAction(nameof(Get), new { id = entity.Id });
    }

    [HttpPost("/admin/social-media/delete/{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var entity = await _socialMediaRepo.Get(id);
        if (entity != null)
        {
            await _fileService.DeleteAllFiles(entity);
            await _socialMediaRepo.SoftDelete(entity);
            await _socialMediaRepo.SaveChanges();
        }

        return RedirectToAction(nameof(GetAll));
    }

    #endregion
}