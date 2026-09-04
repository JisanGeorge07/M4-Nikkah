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
    public class HomeController : Controller
    {
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;
        private readonly IRepository<HomeContent> _homeContentRepo;
        private readonly IRepository<ImageGallery> _imageGalleryRepo;
        private readonly IRepository<HomeBanner> _homeBannerRepo;
        private readonly IRepository<About> _aboutRepo;
        private readonly IRepository<Contact> _contactRepo;
        private readonly IRepository<Registration> _enquiryRepo;
        private readonly IRepository<TestimonialSetting> _testimonialSettingRepo;

        public HomeController(IFileService fileService,
            IMapper mapper,
            IRepository<HomeContent> homeContentRepo,
            IRepository<ImageGallery> imageGalleryRepo,
            IRepository<HomeBanner> homeBannerRepo,
            IRepository<About> aboutRepo,
            IRepository<Contact> contactRepo,
            IRepository<Registration> enquiryRepo,
            IRepository<TestimonialSetting> testimonialSettingRepo)
        {
            _fileService = fileService;
            _mapper = mapper;
            _homeContentRepo = homeContentRepo;
            _imageGalleryRepo = imageGalleryRepo;
            _homeBannerRepo = homeBannerRepo;
            _aboutRepo = aboutRepo;
            _contactRepo = contactRepo;
            _enquiryRepo = enquiryRepo;
            _testimonialSettingRepo = testimonialSettingRepo;
        }

        #region Banner

        [HttpGet("/admin/home-banner")]
        public async Task<IActionResult> GetAllBanner()
        {
            return View(_mapper.Map<List<HomeBannerDto>>(await _homeBannerRepo.GetAll()));
        }

        [HttpGet("/admin/home-banner/{id:long}")]
        public async Task<IActionResult> GetBanner(long id)
        {
            return View(id > 0
                ? _mapper.Map<HomeBannerDto>(await _homeBannerRepo.Get(id))
                : new HomeBannerDto
                {
                    IsActive = true,
                    Id = 0
                });
        }

        [HttpPost("/admin/home-banner")]
        public async Task<IActionResult> PostBanner(HomeBannerDto model)
        {
            HomeBanner entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<HomeBanner>(model);
                await _fileService.SaveAllFiles(entity, model, "Uploads/Home");
                var socialMedia = await _homeBannerRepo.GetAll();
                entity.DisplayOrder = socialMedia.Any() ? socialMedia.Max(x => x.DisplayOrder) + 1 : 1;
                await _homeBannerRepo.Add(entity);
            }
            else
            {
                entity = (await _homeBannerRepo.Get(model.Id))!;
                await _fileService.SaveAllFiles(entity, model, "Uploads/Home");
                _mapper.Map(model, entity);
                await _homeBannerRepo.Update(entity);
            }

            await _homeBannerRepo.SaveChanges();

            return RedirectToAction(nameof(GetBanner), new { id = entity.Id });
        }

        [HttpPost("/admin/home-banner/delete/{id:long}")]
        public async Task<IActionResult> DeleteBanner(long id)
        {
            var entity = await _homeBannerRepo.Get(id);
            if (entity != null)
            {
                await _fileService.DeleteAllFiles(entity);
                await _homeBannerRepo.SoftDelete(entity);
                await _homeBannerRepo.SaveChanges();
            }

            return RedirectToAction(nameof(GetAllBanner));
        }

        #endregion

        #region Home-Content

        [HttpGet("/admin/home-content")]
        public async Task<IActionResult> GetContent()
        {
            return View(_mapper.Map<HomeContentDto>(await _homeContentRepo.First()));
        }

        [HttpPost("/admin/home-content")]
        public async Task<IActionResult> PostContent(HomeContentDto model)
        {
            var entity = (await _homeContentRepo.First())!;
            await _fileService.SaveAllFiles(entity, model, "Uploads/Home");
            _mapper.Map(model, entity);
            await _homeContentRepo.Update(entity);
            await _homeContentRepo.SaveChanges();
            return RedirectToAction(nameof(GetContent));
        }

        #endregion

        #region Image-Gallery

        [HttpGet("/admin/gallery")]
        public async Task<IActionResult> GetAll()
        {
            return View(_mapper.Map<List<ImageGalleryDto>>(await _imageGalleryRepo.GetAll()));
        }

        [HttpGet("/admin/gallery/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            return View(id > 0
                ? _mapper.Map<ImageGalleryDto>(await _imageGalleryRepo.Get(id))
                : new ImageGalleryDto
                {
                    IsActive = true,
                    Id = 0
                });
        }

        [HttpPost("/admin/gallery")]
        public async Task<IActionResult> Post(ImageGalleryDto model)
        {
            ImageGallery entity;

            if (model.Id <= 0)
            {
                entity = _mapper.Map<ImageGallery>(model);
                await _fileService.SaveAllFiles(entity, model, "Uploads/Home");
                var socialMedia = await _imageGalleryRepo.GetAll();
                entity.DisplayOrder = socialMedia.Any() ? socialMedia.Max(x => x.DisplayOrder) + 1 : 1;
                await _imageGalleryRepo.Add(entity);
            }
            else
            {
                entity = (await _imageGalleryRepo.Get(model.Id))!;
                await _fileService.SaveAllFiles(entity, model, "Uploads/Home");
                _mapper.Map(model, entity);
                await _imageGalleryRepo.Update(entity);
            }

            await _imageGalleryRepo.SaveChanges();

            return RedirectToAction(nameof(Get), new { id = entity.Id });
        }

        [HttpPost("/admin/gallery/delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _imageGalleryRepo.Get(id);
            if (entity != null)
            {
                await _fileService.DeleteAllFiles(entity);
                await _imageGalleryRepo.SoftDelete(entity);
                await _imageGalleryRepo.SaveChanges();
            }

            return RedirectToAction(nameof(GetAll));
        }

        #endregion

        #region About

        [HttpGet("/admin/about")]
        public async Task<IActionResult> GetAbout()
        {
            return View(_mapper.Map<AboutDto>(await _aboutRepo.First()));
        }

        [HttpPost("/admin/about")]
        public async Task<IActionResult> PostAbout(AboutDto model)
        {
            var entity = (await _aboutRepo.First())!;
            await _fileService.SaveAllFiles(entity, model, "Uploads/About");
            _mapper.Map(model, entity);
            await _aboutRepo.Update(entity);
            await _aboutRepo.SaveChanges();
            return RedirectToAction(nameof(GetAbout));
        }

        #endregion

        #region Contact

        [HttpGet("/admin/contact")]
        public async Task<IActionResult> GetContact()
        {
            return View(_mapper.Map<ContactDto>(await _contactRepo.First()));
        }

        [HttpPost("/admin/contact")]
        public async Task<IActionResult> PostContact(ContactDto model)
        {
            var entity = (await _contactRepo.First())!;
            await _fileService.SaveAllFiles(entity, model, "Uploads/Contact");
            _mapper.Map(model, entity);
            await _contactRepo.Update(entity);
            await _contactRepo.SaveChanges();
            return RedirectToAction(nameof(GetContact));
        }

        #endregion

        #region TestimonialSettings

        [HttpGet("/admin/testimonial-settings")]
        public async Task<IActionResult> GetTestimonialSettings()
        {
            var setting = await _testimonialSettingRepo.First();
            if (setting == null)
            {
                setting = new TestimonialSetting { FirstPromptDays = 7, FollowUpPromptDays = 30 };
                await _testimonialSettingRepo.Add(setting);
                await _testimonialSettingRepo.SaveChanges();
            }
            return View(_mapper.Map<TestimonialSettingDto>(setting));
        }

        [HttpPost("/admin/testimonial-settings")]
        public async Task<IActionResult> PostTestimonialSettings(TestimonialSettingDto model)
        {
            var entity = await _testimonialSettingRepo.First();
            if (entity == null)
            {
                entity = new TestimonialSetting();
                _mapper.Map(model, entity);
                await _testimonialSettingRepo.Add(entity);
            }
            else
            {
                _mapper.Map(model, entity);
                await _testimonialSettingRepo.Update(entity);
            }
            await _testimonialSettingRepo.SaveChanges();
            return RedirectToAction(nameof(GetTestimonialSettings));
        }

        #endregion

    }
}
