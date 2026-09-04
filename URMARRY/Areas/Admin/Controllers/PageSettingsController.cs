using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hayatt.Areas.Admin.Controllers;

[Authorize]
[Area("Admin")]
public class PageSettingsController : Controller
{
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;
    private readonly IRepository<PageSettings> _pageSettingsRepo;


    public PageSettingsController(
        IRepository<PageSettings> pageSettingsRepo,
        IMapper mapper, IFileService fileService)
    {
        _pageSettingsRepo = pageSettingsRepo;
        _mapper = mapper;
        _fileService = fileService;
    }


    #region Page Settings

    [HttpGet("/admin/page-settings")]
    public async Task<IActionResult> GetAll()
    {
        return View(_mapper.Map<List<PageSettingsDto>>(await _pageSettingsRepo.GetAll()));
    }

    [HttpGet("/admin/page-settings/{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        return View(_mapper.Map<PageSettingsDto>(await _pageSettingsRepo.Get(id)));
    }

    [HttpGet("/admin/page-settings/by-url")]
    public async Task<IActionResult> GetByUrl(string url)
    {
        url = url.Replace("%2F", "/");
        var indexOfLastSlash = url.LastIndexOf('/');

        var name = indexOfLastSlash > -1 ? url[(indexOfLastSlash + 1)..] : url;
        var parentName = indexOfLastSlash > -1
            ? url[..indexOfLastSlash]
            : name.Equals("index")
                ? null
                : "index";
        if (parentName != null && parentName[0] == '/') parentName = parentName[1..];

        return View("Get", _mapper.Map<PageSettingsDto>(await _pageSettingsRepo
            .First(x =>
                x.ParentName == parentName
                && x.Name == name)));
    }

    [HttpPost("/admin/page-settings")]
    public async Task<IActionResult> Post(PageSettingsDto model)
    {
        var entity = (await _pageSettingsRepo.Get(model.Id))!;
        await _fileService.SaveAllFiles(entity, model, $"Uploads/Page/{entity.Id}");
        _mapper.Map(model, entity);
        await _pageSettingsRepo.Update(entity);


        await _pageSettingsRepo.SaveChanges();

        return RedirectToAction(nameof(Get), new { id = entity.Id });
    }

    #endregion
}