using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using URMARRY.Models;
using Microsoft.AspNetCore.Mvc;

namespace URMARRY.ViewComponents;

public class FooterViewComponent : ViewComponent
{
    private readonly IMapper _mapper;
    private readonly IRepository<SocialMedia> _socialMediaRepo;
    private readonly IRepository<Contact> _contactRepo;
    private readonly IRepository<About> _aboutRepo;

    public FooterViewComponent(
        IMapper mapper,
        IRepository<SocialMedia> socialMediaRepo,
        IRepository<Contact> contactRepo,
        IRepository<About> aboutRepo)
    {
        _mapper = mapper;
        _socialMediaRepo = socialMediaRepo;
        _contactRepo = contactRepo;
        _aboutRepo = aboutRepo;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        return View("Default", new FooterViewModel
        {
            Contact = _mapper.Map<ContactDto>(await _contactRepo.First()),
            SocialMedia = _mapper.Map<List<SocialMediaDto>>(await _socialMediaRepo.GetAllActive()),
            About = _mapper.Map<AboutDto>(await _aboutRepo.First()),
        });
    }
}