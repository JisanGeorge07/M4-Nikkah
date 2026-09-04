using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using URMARRY.Models;
using Microsoft.AspNetCore.Mvc;

namespace URMARRY.ViewComponents;

public class HeaderViewComponent : ViewComponent
{
    private readonly IMapper _mapper;
    private readonly IRepository<PageSettings> _pageRepo;
    private readonly IRepository<SocialMedia> _socialMediaRepo;

    public HeaderViewComponent(
        IMapper mapper,
        IRepository<PageSettings> pageRepo,
        IRepository<SocialMedia> socialMediaRepo)
    {
        _mapper = mapper;
        _pageRepo = pageRepo;
        _socialMediaRepo = socialMediaRepo;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        return View("Default", new HeaderViewModel
        {
            //Pages = _mapper.Map<List<PageSettingsDto>>(await _pageRepo.GetAllActive()),
            //SocialMedia = _mapper.Map<List<SocialMediaDto>>(await _socialMediaRepo.GetAllActive())
        });
    }
}