using Application.Constants;
using Application.Helpers;
using Application.Interfaces.Persistence;
using Application.Models;
using Application.Models.Framework;
using Application.Models.Transactions;
using Application.ViewModels;
using AutoMapper;
using Domain;
using Domain.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using URMARRY.Areas.Admin.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
using Serilog;

using Microsoft.AspNetCore.Identity;
using Identity.Models;
using Persistence;

namespace URMARRY.Areas.Admin.Controllers;

[Authorize]
[Area("Admin")]
public class EnquiryController : Controller
{
    private readonly IRepository<Email> _emailRepo;
    private readonly IRepository<Registration> _enquiryRepo;
    private readonly IMapper _mapper;
    private readonly IRepository<ProfileFor> _profileForRepo;
    private readonly IRepository<Nationality> _nationalityRepo;
    private readonly IRepository<State> _stateRepository;
    private readonly IRepository<District> _districtRepository;
    private readonly IRepository<City> _cityRepository;
    private readonly IRepository<MaritalStatus> _maritalStatusRepo;
    private readonly IRepository<BodyFeatures> _bodyFeaturesRepo;
    private readonly IRepository<Profession> _professionRepo;
    private readonly IRepository<MotherTongue> _motherTongueRepo;
    private readonly IRepository<ReligionCaste> _religionCasteRepo;
    private readonly IRepository<Religiousness> _religiousnessRepo;
    private readonly IRepository<Community> _communityRepo;
    private readonly IRepository<FinancialStatus> _financialStatusRepo;
    private readonly IRepository<HomeContent> _homeContentRepo;
    private readonly IRepository<Contact> _contactRepo;
    private readonly IRepository<MatchingProfiles> _matchingProfilesRepo;
    private readonly IFileService _fileService;
    private readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment _hostingEnvironment;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRepository<PlanPurchase> _planPurchaseRepository;
    private readonly IRepository<UserContactView> _userContactViewRepo;
    private readonly IRepository<UserFavouriteProfile> _userFavouriteProfileRepo;
    private readonly IRepository<UserStarProfile> _userStarProfileRepo;
    private readonly IRepository<UserNotLikeProfile> _userNotLikeProfileRepo;
    private readonly IRepository<UserReport> _userReportRepo;
    private readonly IRepository<Images> _imagesRepo;
    private readonly IRepository<SuccessStory> _successStoryRepo;
    private readonly Persistence.AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    internal Guid key = new Guid("F7AD797A-416C-4C56-8749-7017FBC90427");

    public EnquiryController(
        IRepository<Email> emailRepo,
        IRepository<Registration> enquiryRepo,
        IMapper mapper,
        IRepository<ProfileFor> profileForRepo,
        IRepository<Nationality> nationalityRepo,
        IRepository<State> stateRepository,
        IRepository<District> districtRepository,
        IRepository<City> cityRepository,
        IRepository<MaritalStatus> maritalStatusRepo,
        IRepository<BodyFeatures> bodyFeaturesRepo,
        IRepository<Profession> professionRepo,
        IRepository<MotherTongue> motherTongueRepo,
        IRepository<ReligionCaste> religionCasteRepo,
        IRepository<Religiousness> religiousnessRepo,
        IRepository<Community> communityRepo,
        IRepository<FinancialStatus> financialStatusRepo,
        IRepository<HomeContent> homeContentRepo,
        IRepository<Contact> contactRepo,
        IRepository<MatchingProfiles> matchingProfilesRepo,
        IFileService fileService,
        ITransactionRepository transactionRepository,
        Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment,
        IRepository<PlanPurchase> planPurchaseRepository,
        IRepository<UserContactView> userContactViewRepo,
        IRepository<UserFavouriteProfile> userFavouriteProfileRepo,
        IRepository<UserStarProfile> userStarProfileRepo,
        IRepository<UserNotLikeProfile> userNotLikeProfileRepo,
        IRepository<UserReport> userReportRepo,
        IRepository<Images> imagesRepo,
        IRepository<SuccessStory> successStoryRepo,
        Persistence.AppDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _emailRepo = emailRepo;
        _enquiryRepo = enquiryRepo;
        _mapper = mapper;
        _maritalStatusRepo = maritalStatusRepo;
        _bodyFeaturesRepo = bodyFeaturesRepo;
        _professionRepo = professionRepo;
        _motherTongueRepo = motherTongueRepo;
        _religionCasteRepo = religionCasteRepo;
        _religiousnessRepo = religiousnessRepo;
        _communityRepo = communityRepo;
        _financialStatusRepo = financialStatusRepo;
        _profileForRepo = profileForRepo;
        _nationalityRepo = nationalityRepo;
        _stateRepository = stateRepository;
        _districtRepository = districtRepository;
        _cityRepository = cityRepository;
        _homeContentRepo = homeContentRepo;
        _contactRepo = contactRepo;
        _matchingProfilesRepo = matchingProfilesRepo;
        _fileService = fileService;
        _hostingEnvironment = hostingEnvironment;
        _planPurchaseRepository = planPurchaseRepository;
        _transactionRepository = transactionRepository;
        _userContactViewRepo = userContactViewRepo;
        _userFavouriteProfileRepo = userFavouriteProfileRepo;
        _userStarProfileRepo = userStarProfileRepo;
        _userNotLikeProfileRepo = userNotLikeProfileRepo;
        _userReportRepo = userReportRepo;
        _imagesRepo = imagesRepo;
        _successStoryRepo = successStoryRepo;
        _dbContext = dbContext;
        _userManager = userManager;
    }

    #region Email

    [HttpGet("/admin/enquiry")]
    public async Task<IActionResult> GetAll()
    {
        return View(new EnquiryViewModel
        {
            Email = _mapper.Map<EmailDto>(await _emailRepo.First(x => x.Purpose == EnquiryTypes.Registration)),
            Enquiries = _mapper.Map<List<RegistrationDto>>(await _enquiryRepo.WhereActive(x => x.IsComplete))
                .OrderByDescending(x => x.CreatedOn)
                .ToList()
        });
    }


    [HttpPost("/admin/email")]
    public async Task<IActionResult> PostEmail(EmailDto model)
    {
        var entity = await _emailRepo.First(x => x.Purpose == model.Purpose);

        _mapper.Map(model, entity);

        await _emailRepo.Update(entity);
        await _emailRepo.SaveChanges();


        return RedirectToAction(nameof(GetAll), new { type = model.Purpose });
    }

    #endregion

    #region Enquiry

    [HttpGet("/admin/enquiry/{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var enquiry = _mapper.Map<RegistrationDto>(await _enquiryRepo.Get(id));
        RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);

        // Check if enquiry.Password is not null or empty before decrypting
        if (!string.IsNullOrEmpty(enquiry?.Password))
        {
            enquiry.Password = encryption.DecryptRijndael(enquiry.Password, enquiry.PasswordHash);
        }
        else
        {
            enquiry.Password = ""; // Optional: Set a default value if password is null
        }
        //enquiry.Password = encryption.DecryptRijndael(enquiry.Password, enquiry.PasswordHash);
        return View(new EnquiryViewModel
        {
            Enquiry = enquiry,
            ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAll()),
            Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAll()).OrderBy(x => x.Title).ToList(),
            States = _mapper.Map<List<StateDto>>(await _stateRepository.GetAllActive()),
            Districts = _mapper.Map<List<DistrictDto>>(await _districtRepository.GetAllActive()),
            Cities = _mapper.Map<List<CityDto>>(await _cityRepository.GetAllActive()),
            MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAll()),
            BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAll()),
            Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAll()),
            MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAll()),
            ReligionCastes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.GetAll()),
            Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAll()),
            Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAll()),
            FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatusRepo.GetAll()),
        });
    }

    [HttpPost("/admin/enquiry")]
    public async Task<IActionResult> Post(RegistrationDto model)
    {
        var user = await _enquiryRepo.FirstOrDefaultActive(x => x.Id == model.Id);
        if (user == null)
        {
            return RedirectToAction(nameof(GetAll));
        }
        if (model.Image != null)
        {
            var uploadPath = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads/Registration");
            var extension = Path.GetExtension(model.Image.FileName);
            var fileNameWithExtension = Guid.NewGuid() + extension;
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            var filePath = Path.Combine(uploadPath, fileNameWithExtension);
            user.ImagePath = "Uploads/Registration/" + fileNameWithExtension;
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                fileStream.Position = 0;
                await model.Image.CopyToAsync(fileStream);
            }
        }
        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            user.Name = model.Name.Trim();
        }
        user.Email = model.Email;
        if (string.IsNullOrEmpty(user.RegisterNumber))
        {
            user.RegisterNumber = "M4N" + Guid.NewGuid().ToString("N").Substring(0, 7);
        }
        user.ProfileForId = model.ProfileForId;
        user.Gender = model.Gender;
        if (!string.IsNullOrWhiteSpace(model.DOB))
        {
            if (DateTime.TryParse(model.DOB, out var parsedDate))
            {
                user.DOB = parsedDate.ToString("dd/MM/yyyy");
            }
            else
            {
                user.DOB = model.DOB.Trim();
            }
        }
        user.NationalityId = model.NationalityId;
        user.CountryCode = !string.IsNullOrWhiteSpace(model.CountryCode) ? model.CountryCode.Trim() : null;
        user.Phone = model.Phone;
        user.MaritalStatusId = model.MaritalStatusId;
        user.NumberOfChildrens = (model.MaritalStatusId == 1) ? null : model.NumberOfChildrens;
        user.HeightId = model.HeightId;
        user.WeightId = model.WeightId;
        user.ComplexionId = model.ComplexionId;
        user.BodyTypeId = model.BodyTypeId;
        user.IsPhysicallyChallenged = model.IsPhysicallyChallenged;
        user.PhysicallyChallengedDetail = model.IsPhysicallyChallenged ? model.PhysicallyChallengedDetail : null;
        user.HighestEducation = model.HighestEducation;
        user.EducationType = model.EducationType;
        user.ProfessionId = model.ProfessionId;
        user.ProfessionType = model.ProfessionType;
        user.MotherTongueId = model.MotherTongueId;
        user.ReligionId = model.ReligionId;
        user.CommunityId = model.CommunityId;
        user.ReligiousnessId = model.ReligiousnessId;
        user.FinancialStatusId = model.FinancialStatusId;
        user.FamilyName = model.FamilyName;
        user.FatherName = model.FatherName;
        user.Post = model.Post;
        user.Village = model.Village;
        user.PinCode = model.PinCode;
        user.Country = model.Country;
        user.State = model.State;
        user.District = model.District;
        user.PresentCountry = model.PresentCountry;
        user.PresentState = model.PresentState;
        user.PresentDistrict = model.PresentDistrict;
        user.PresentCity = model.PresentCity;
        user.About = model.About;
        // user.IsPremiumMember = model.IsPremiumMember;
        user.IsSpecialRequest = model.IsSpecialRequest;
        user.ShowOnHomePage = model.ShowOnHomePage;
        user.IsActive = model.IsActive;
        user.IsVerified = model.IsVerified;
        user.IsComplete = model.IsComplete;
        user.IsVisible = model.IsVisible;
        user.LandlineNumber = model.LandlineNumber;
        user.SecondaryCountryCode = !string.IsNullOrWhiteSpace(model.SecondaryCountryCode) ? model.SecondaryCountryCode.Trim() : null;
        user.PhotoVisibleToAll = model.PhotoVisibleToAll;
        user.PhotoVisibleToPremium = model.PhotoVisibleToPremium;
        user.PhotoVisibleToAccepted = model.PhotoVisibleToAccepted;
        if (!isIndia(model.Country))
        {
            user.State = "";
            user.District = "";
            user.Village = "";
        }
        else
        {
            user.State = model.State;
            user.District = model.District;
            if (!isKerala(model.State))
            {
                user.Village = "";
            }
            else
            {
                user.Village = model.Village;
            }
        }
        if (!string.IsNullOrEmpty(model.Password))
        {
            RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
            bool passwordChanged = true;
            if (!string.IsNullOrEmpty(user.Password) && !string.IsNullOrEmpty(user.PasswordHash))
            {
                try
                {
                    string existingDecrypted = encryption.DecryptRijndael(user.Password, user.PasswordHash);
                    if (existingDecrypted == model.Password)
                    {
                        passwordChanged = false;
                    }
                }
                catch
                {
                    passwordChanged = true;
                }
            }

            if (passwordChanged)
            {
                user.PasswordHash = encryption.CreateSalt();
                user.Password = encryption.EncryptRijndael(model.Password, user.PasswordHash);
            }
        }
        await _enquiryRepo.Update(user);
        await _enquiryRepo.SaveChanges();
        return RedirectToAction(nameof(Get), new { id = model.Id });
    }

    [HttpGet("/admin/print/{id:long}")]
    public async Task<IActionResult> Print(long id)
    {
        var enquiry = _mapper.Map<RegistrationDto>(await _enquiryRepo.Get(id));
        return View(new EnquiryViewModel
        {
            Enquiry = enquiry,
            ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAll()),
            Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAll()),
            MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAll()),
            BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAll()),
            Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAll()),
            MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAll()),
            ReligionCastes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.GetAll()),
            Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAll()),
            Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAll()),
            FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatusRepo.GetAll()),
            HomeContent = _mapper.Map<HomeContentDto>(await _homeContentRepo.First()),
            Contact = _mapper.Map<ContactDto>(await _contactRepo.First())
        });
    }

    [HttpGet("/admin/enquiry/matching/{id:long}")]
    public async Task<IActionResult> Matching(long id)
    {
        var user = _mapper.Map<RegistrationDto>(await _enquiryRepo.Get(id));
        if (user == null)
        {
            return RedirectToAction(nameof(GetAll));
        }
        var partners = _mapper.Map<List<RegistrationDto>>(await _enquiryRepo.WhereActive(x => x.IsVerified
        && x.IsComplete && x.Gender != user.Gender));
        var list = await _matchingProfilesRepo.GetAllActive();
        var matchings = _mapper.Map<List<MatchingProfilesDto>>(await _matchingProfilesRepo.WhereActive(x => x.UserId == user.Id));
        return View(new MatchingProfilesViewModel
        {
            Registration = user,
            MatchingProfiles = matchings,
            RegistrationList = partners,
            Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAll()),
            Matches = new List<Match>(),
            MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAll()),
        });
    }

    [HttpPost("/admin/enquiry/save-match")]
    public async Task<IActionResult> SaveMatch(MatchingProfilesViewModel model)
    {
        var matches = await _matchingProfilesRepo.WhereActive(x => x.UserId == model.UserId);
        if (matches.Count() > 0)
        {
            await _matchingProfilesRepo.RemoveRange(matches);
            await _matchingProfilesRepo.SaveChanges();
        }
        var list = new List<MatchingProfiles>();
        foreach (var match in model.Matches.Where(x => x.IsSelected))
        {
            var item = new MatchingProfiles
            {
                UserId = model.UserId,
                PartnerId = match.PartnerId,
            };
            list.Add(item);
        }
        if (list.Count() > 0)
        {
            await _matchingProfilesRepo.AddRange(list);
            await _matchingProfilesRepo.SaveChanges();
        }
        return RedirectToAction(nameof(Matching), new { @id = model.UserId });
    }
    //public ActionResult Test()
    //{
    //    return View();
    //}

    //[HttpPost("/admin/enquiry/delete/{id:long}")]
    //public async Task<IActionResult> Delete(long id)
    //{
    //    var entity = await _enquiryRepo.Get(id);
    //    if (entity != null)
    //    {
    //        await _enquiryRepo.SoftDelete(entity);
    //        await _enquiryRepo.SaveChanges();
    //    }

    //    return RedirectToAction(nameof(GetAll));
    //}
    [HttpPost("/admin/delete-pending-enquiry/{id:long}")]
    public async Task<IActionResult> DeletePending(long id)
    {
        var entity = await _enquiryRepo.Get(id);
        if (entity != null)
        {
            await _enquiryRepo.SoftDelete(entity);
            await _enquiryRepo.SaveChanges();
        }

        return RedirectToAction("GetAllPending", "Enquiry");
    }

    [HttpPost("/admin/enquiry/recycle/{id:long}")]
    public async Task<IActionResult> RecycleEnquiry(long id, bool restore = false)
    {
        var entity = await _enquiryRepo.Get(id);
        if (entity != null)
        {
            entity.IsActive = restore;
            if (restore)
            {
                entity.DisabledReason = DisabledReason.Deactivated;
                entity.IsVisible = true; // Ensure restored profiles are visible in active lists
            }
            else
            {
                entity.DisabledReason = DisabledReason.Recycled;
                entity.IsVisible = false; // Hide recycled profiles from active lists
            }

            await _enquiryRepo.Update(entity);
            await _enquiryRepo.SaveChanges();
        }
        return Json(new { success = true });
    }

    [HttpPost("/admin/enquiry/delete/{id:long}")]
    public async Task<IActionResult> DeleteEnquiry(long id)
    {
        var entity = await _enquiryRepo.Get(id);
        if (entity != null)
        {
            // Remove reports associated with this profile
            var reports = await _userReportRepo.Where(x => x.ReportedUserId == id);
            foreach (var report in reports)
            {
                await _userReportRepo.SoftDelete(report);
            }
            await _userReportRepo.SaveChanges();

            await _enquiryRepo.SoftDelete(entity);
            await _enquiryRepo.SaveChanges();
        }
        return Json(new { success = true });
        //return RedirectToAction("memberslist", "Enquiry");
    }
    #endregion

    #region PendingList
    [HttpGet("/admin/enquiry/pending-enquiry")]
    public async Task<IActionResult> GetAllPending()
    {
        return View(new EnquiryViewModel
        {
            Enquiries = _mapper.Map<List<RegistrationDto>>(await _enquiryRepo.WhereActive(x => !x.IsComplete))
                .OrderByDescending(x => x.CreatedOn)
                .ToList()
        });
    }
    [HttpGet("/admin/enquiry/pending-enquiry/{id:long}")]
    public async Task<IActionResult> GetPending(long id)
    {
        var enquiry = _mapper.Map<RegistrationDto>(await _enquiryRepo.Get(id));
        RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
        enquiry.Password = encryption.DecryptRijndael(enquiry.Password, enquiry.PasswordHash);
        return View(new EnquiryViewModel
        {
            Enquiry = enquiry,
            ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAll()),
            Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAll()),
            MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAll()),
            BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAll()),
            Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAll()),
            MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAll()),
            ReligionCastes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.GetAll()),
            Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAll()),
            Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAll()),
            FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatusRepo.GetAll()),
        });
    }

    [HttpPost("/admin/enquiry/pending-enquiry")]
    public async Task<IActionResult> PostPending(RegistrationDto model)
    {
        var user = await _enquiryRepo.FirstOrDefaultActive(x => x.Id == Convert.ToInt64(model.Id));
        if (user == null)
        {
            return RedirectToAction(nameof(GetAll));
        }
        user.Name = model.Name;
        user.Email = model.Email;
        if (string.IsNullOrEmpty(user.RegisterNumber))
        {
            user.RegisterNumber = "M4N" + Guid.NewGuid().ToString("N").Substring(0, 7);
        }
        if (model.Image != null)
        {
            var uploadPath = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads/Registration");
            var extension = Path.GetExtension(model.Image.FileName);
            var fileNameWithExtension = Guid.NewGuid() + extension;
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            var filePath = Path.Combine(uploadPath, fileNameWithExtension);
            user.ImagePath = "Uploads/Registration/" + fileNameWithExtension;
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                fileStream.Position = 0;
                await model.Image.CopyToAsync(fileStream);
            }
        }
        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            user.Name = model.Name.Trim();
        }
        user.ProfileForId = model.ProfileForId;
        user.Gender = model.Gender;
        if (!string.IsNullOrWhiteSpace(model.DOB))
        {
            if (DateTime.TryParse(model.DOB, out var parsedDob))
            {
                user.DOB = parsedDob.ToString("dd/MM/yyyy");
            }
            else
            {
                user.DOB = model.DOB.Trim();
            }
        }
        user.NationalityId = model.NationalityId;
        user.CountryCode = !string.IsNullOrWhiteSpace(model.CountryCode) ? model.CountryCode.Trim() : null;
        user.Phone = model.Phone;
        user.MaritalStatusId = model.MaritalStatusId;
        user.HeightId = model.HeightId;
        user.WeightId = model.WeightId;
        user.ComplexionId = model.ComplexionId;
        user.BodyTypeId = model.BodyTypeId;
        user.IsPhysicallyChallenged = model.IsPhysicallyChallenged;
        user.HighestEducation = model.HighestEducation;
        user.EducationType = model.EducationType;
        user.ProfessionId = model.ProfessionId;
        user.ProfessionType = model.ProfessionType;
        user.MotherTongueId = model.MotherTongueId;
        user.ReligionId = model.ReligionId;
        user.CommunityId = model.CommunityId;
        user.ReligiousnessId = model.ReligiousnessId;
        user.FinancialStatusId = model.FinancialStatusId;
        user.FamilyName = model.FamilyName;
        user.FatherName = model.FatherName;
        user.Post = model.Post;
        user.Village = model.Village;
        user.PinCode = model.PinCode;
        user.Country = model.Country;
        user.State = model.State;
        user.District = model.District;
        user.PresentCountry = model.PresentCountry;
        user.PresentState = model.PresentState;
        user.PresentDistrict = model.PresentDistrict;
        user.PresentCity = model.PresentCity;
        user.LandlineNumber = model.LandlineNumber;
        user.SecondaryCountryCode = !string.IsNullOrWhiteSpace(model.SecondaryCountryCode) ? model.SecondaryCountryCode.Trim() : null;
        user.PhotoVisibleToAll = model.PhotoVisibleToAll;
        user.PhotoVisibleToPremium = model.PhotoVisibleToPremium;
        user.PhotoVisibleToAccepted = model.PhotoVisibleToAccepted;
        user.About = model.About;
        user.IsComplete = model.IsComplete;
        // user.IsPremiumMember = model.IsPremiumMember;
        user.IsSpecialRequest = model.IsSpecialRequest;
        user.ShowOnHomePage = model.ShowOnHomePage;
        user.IsActive = model.IsActive;
        user.IsVerified = model.IsVerified;
        if (model.IsComplete)
        {
            user.CompletedStep = "Step-6";
        }
        if (string.IsNullOrEmpty(user.Password))
        {
            RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
            user.PasswordHash = encryption.CreateSalt();
            user.Password = encryption.EncryptRijndael(model.Password, user.PasswordHash);
        }
        await _enquiryRepo.Update(user);
        await _enquiryRepo.SaveChanges();
        return RedirectToAction(nameof(GetPending), new { id = model.Id });
    }

    //[HttpGet("/admin/enquiry/pending-enquiry/delete/{id:long}")]
    //public async Task<IActionResult> DeletePending(long id)
    //{
    //    var entity = await _enquiryRepo.Get(id);
    //    if (entity != null)
    //    {
    //        await _enquiryRepo.SoftDelete(entity);
    //        await _enquiryRepo.SaveChanges();
    //    }

    //    return RedirectToAction(nameof(GetAllPending));
    //}
    [HttpGet("/admin/enquiry/memberslist")]
    public async Task<IActionResult> MemberList(string status, string name)
    {
        return View(new EnquiryViewModel
        {
            SelectedStatus = status,
            NameFilter = name,
            Statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Premium", Text = "Premium" },
                new SelectListItem { Value = "Active", Text = "Active" },
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "PremiumExpiring", Text = "Premium Expiring" }
            },
            Enquiries = new List<RegistrationDto>(),
            Email = null,
            Nationalities = new List<NationalityDto>()
        });
    }

    [HttpGet("/admin/enquiry/memberslist-data")]
    public async Task<IActionResult> MemberListData(string status, string name)
    {
        int draw = Convert.ToInt32(Request.Query["draw"]);
        int start = Convert.ToInt32(Request.Query["start"]);
        int length = Convert.ToInt32(Request.Query["length"]);
        if (length <= 0) length = 100;

        string searchValue = Request.Query["search[value]"];
        string sortColumnIndex = Request.Query["order[0][column]"];
        string sortDirection = Request.Query["order[0][dir]"];

        string sortColumn = sortColumnIndex switch
        {
            "0" => "RegisterNumber",
            "1" => "Name",
            "2" => "Phone",
            "3" => "CreatedOn",
            _ => "CreatedOn"
        };
        bool isAscending = sortDirection == "asc";

        var query = _enquiryRepo.GetQueryable();

        switch (status)
        {
            case "Premium":
                var activePlanUsers = await _planPurchaseRepository.WhereActive(x => x.ExpiresAt > DateTime.UtcNow);
                var activePlanUserIds = activePlanUsers.Select(p => p.UserId).Distinct().ToList();
                query = query.Where(x => x.IsActive && x.IsVisible && (x.IsPremiumMember || activePlanUserIds.Contains(x.Id)));
                break;

            case "Active":
                query = query.Where(x => x.IsActive && x.IsComplete && x.IsVisible);
                break;

            case "Pending":
                query = query.Where(x => x.IsActive && !x.IsComplete && x.IsVerified);
                break;

            case "NewRegistration":
                query = query.Where(x => x.IsActive && !x.IsVisible && x.IsVerified);
                break;

            case "Recycled":
                query = query.Where(x => (x.DisabledReason == DisabledReason.Recycled && !x.IsActive) || x.DisabledReason == DisabledReason.ReportedViolation);
                break;

            case "PremiumExpiring":
                var now = DateTime.UtcNow;
                var tenDaysLater = now.AddDays(10);
                var allActivePlans = await _planPurchaseRepository.WhereActive(x => x.ExpiresAt >= now);

                var expiringOrLowCreditUserIds = allActivePlans
                    .GroupBy(p => p.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        LatestExpiry = g.Max(p => p.ExpiresAt),
                        TotalRemaining = g.Sum(p => (int)p.ViewCreditsPurchased - p.ViewCreditsUsed)
                    })
                    .Where(x => x.LatestExpiry <= tenDaysLater || x.TotalRemaining <= 5)
                    .Select(x => x.UserId)
                    .ToList();

                query = query.Where(x => x.IsActive && expiringOrLowCreditUserIds.Contains(x.Id) && x.IsPremiumMember);
                break;

            default:
                query = query.Where(x => x.IsActive && x.IsComplete);
                break;
        }

        int recordsTotal = await query.CountAsync();

        string search = !string.IsNullOrEmpty(searchValue) ? searchValue : name;
        if (!string.IsNullOrEmpty(search))
        {
            string lowerCaseSearch = search.ToLower();
            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(lowerCaseSearch)) ||
                (x.Phone != null && x.Phone.Contains(search)) ||
                (x.RegisterNumber != null && x.RegisterNumber.Contains(search))
            );
        }

        int recordsFiltered = await query.CountAsync();

        if (sortColumn == "RegisterNumber")
        {
            query = isAscending ? query.OrderBy(x => x.RegisterNumber) : query.OrderByDescending(x => x.RegisterNumber);
        }
        else if (sortColumn == "Name")
        {
            query = isAscending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name);
        }
        else if (sortColumn == "Phone")
        {
            query = isAscending ? query.OrderBy(x => x.Phone) : query.OrderByDescending(x => x.Phone);
        }
        else
        {
            query = isAscending ? query.OrderBy(x => x.CreatedOn) : query.OrderByDescending(x => x.CreatedOn);
        }

        var dbList = await query.Skip(start).Take(length).ToListAsync();
        var enquiries = _mapper.Map<List<RegistrationDto>>(dbList);

        var profileIds = dbList.Select(x => x.Id).ToList();

        // 1. Get assignments from StaffProfileAssignments for these profileIds
        var assignments = await _dbContext.StaffProfileAssignments
            .Where(a => profileIds.Contains(a.ProfileId) && !a.IsDeleted)
            .ToListAsync();

        var profileStaffMap = assignments
            .GroupBy(a => a.ProfileId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ModifiedOn != default ? a.ModifiedOn : a.CreatedOn).First().StaffId);

        // 2. Fallback: check active followups for any profile not yet in profileStaffMap
        var unmappedProfileIds = profileIds.Where(id => !profileStaffMap.ContainsKey(id)).ToList();
        if (unmappedProfileIds.Any())
        {
            var followUps = await _dbContext.FollowUps
                .Where(f => unmappedProfileIds.Contains(f.ProfileId) && !f.IsDeleted && f.AssignedStaffId.HasValue && f.AssignedStaffId.Value > 0)
                .Select(f => new { f.ProfileId, StaffId = f.AssignedStaffId.Value, f.CreatedOn })
                .ToListAsync();

            foreach (var group in followUps.GroupBy(f => f.ProfileId))
            {
                var latestStaffId = group.OrderByDescending(f => f.CreatedOn).First().StaffId;
                profileStaffMap[group.Key] = latestStaffId;
            }
        }

        // 3. Get staff users dictionary
        var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
        var staffDict = staffUsers.ToDictionary(u => u.Id, u => u.NormalizedUserName ?? u.UserName ?? "Staff");

        var missingStaffIds = profileStaffMap.Values.Distinct().Where(id => !staffDict.ContainsKey(id)).ToList();
        if (missingStaffIds.Any())
        {
            var extraUsers = await _userManager.Users.Where(u => missingStaffIds.Contains(u.Id)).ToListAsync();
            foreach (var u in extraUsers)
            {
                staffDict[u.Id] = u.NormalizedUserName ?? u.UserName ?? "Staff";
            }
        }

        var activePlanProfiles = await _planPurchaseRepository.WhereActive(x => profileIds.Contains(x.UserId) && x.ExpiresAt > DateTime.UtcNow);
        var activePlanUserSet = new HashSet<long>(activePlanProfiles.Select(p => p.UserId));

        var data = enquiries.Select(x =>
        {
            string staffName = "Unassigned";
            if (profileStaffMap.TryGetValue(x.Id, out var staffId))
            {
                if (staffDict.TryGetValue(staffId, out var sName))
                {
                    staffName = sName;
                }
            }

            return new
            {
                x.Id,
                x.RegisterNumber,
                x.Name,
                x.Phone,
                x.Country,
                CountryCode = CountryCodeHelper.GetCountryCode(x.CountryCode ?? x.Country),
                CreatedOn = x.CreatedOn?.ToString("yyyy-MM-ddTHH:mm:ss"),
                StaffName = staffName,
                x.IsActive,
                x.DisabledReason,
                IsPremiumMember = x.IsPremiumMember || activePlanUserSet.Contains(x.Id),
                x.IsVerified,
                x.IsComplete
            };
        }).ToList();

        return Json(new
        {
            draw = draw,
            recordsTotal = recordsTotal,
            recordsFiltered = recordsFiltered,
            data = data
        });
    }

    [HttpGet("/admin/enquiry/crm-details/{id:long}")]
    public async Task<IActionResult> CrmDetails(long id)
    {
        var user = await _dbContext.Registration.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return RedirectToAction(nameof(MemberList));
        }

        // Assigned Staff
        var assignment = await _dbContext.StaffProfileAssignments
            .FirstOrDefaultAsync(a => a.ProfileId == id && !a.IsDeleted);
        string assignedStaffName = "Unassigned";
        long? assignedStaffId = null;
        if (assignment != null && assignment.StaffId > 0)
        {
            assignedStaffId = assignment.StaffId;
            var staffUser = await _userManager.FindByIdAsync(assignment.StaffId.ToString());
            if (staffUser != null)
            {
                assignedStaffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Staff";
            }
        }

        // Plans
        var planPurchases = await _dbContext.PlanPurchases
            .Where(p => p.UserId == id && !p.IsDeleted)
            .ToListAsync();

        var activePlan = planPurchases
            .Where(p => p.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(p => p.ExpiresAt)
            .FirstOrDefault();

        var latestPlan = planPurchases
            .OrderByDescending(p => p.ExpiresAt)
            .FirstOrDefault();

        int activeCredits = planPurchases
            .Where(p => p.ExpiresAt > DateTime.UtcNow)
            .Sum(p => Math.Max(0, p.ViewCreditsPurchased - p.ViewCreditsUsed));

        string expiryDate = activePlan != null
            ? activePlan.ExpiresAt.ToString("dd MMM yyyy")
            : (latestPlan != null ? latestPlan.ExpiresAt.ToString("dd MMM yyyy") + " (Expired)" : "N/A");

        // Followups with timelines
        var followUps = await _dbContext.FollowUps
            .Where(f => f.ProfileId == id && !f.IsDeleted)
            .Include(f => f.Timelines.Where(t => !t.IsDeleted))
            .OrderByDescending(f => f.CreatedOn)
            .ToListAsync();

        // Photos
        var userImages = await _imagesRepo.FirstOrDefault(x => x.UserId == id);

        // Lookups
        var profileFor = await _profileForRepo.FirstOrDefault(x => x.Id == user.ProfileForId);
        var religion = await _religionCasteRepo.FirstOrDefault(x => x.Id == user.ReligionId);
        var nationality = await _nationalityRepo.FirstOrDefault(x => x.Id == user.NationalityId);
        var maritalStatus = await _maritalStatusRepo.FirstOrDefault(x => x.Id == user.MaritalStatusId);
        var height = await _bodyFeaturesRepo.FirstOrDefault(x => x.Id == user.HeightId);
        var weight = await _bodyFeaturesRepo.FirstOrDefault(x => x.Id == user.WeightId);
        var complexion = await _bodyFeaturesRepo.FirstOrDefault(x => x.Id == user.ComplexionId);
        var bodyType = await _bodyFeaturesRepo.FirstOrDefault(x => x.Id == user.BodyTypeId);
        var profession = await _professionRepo.FirstOrDefault(x => x.Id == user.ProfessionId);
        var motherTongue = await _motherTongueRepo.FirstOrDefault(x => x.Id == user.MotherTongueId);
        var community = await _communityRepo.FirstOrDefault(x => x.Id == user.CommunityId);
        var religiousness = await _religiousnessRepo.FirstOrDefault(x => x.Id == user.ReligiousnessId);
        var financialStatus = await _financialStatusRepo.FirstOrDefault(x => x.Id == user.FinancialStatusId);

        var verificationDocs = await _dbContext.VerificationDocuments
            .Where(d => d.ProfileId == id && !d.IsDeleted)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.CreatedOn)
            .ToListAsync();

        var viewModel = new MemberCrmDetailsViewModel
        {
            User = user,
            ProfileForTitle = profileFor?.Title,
            ReligionTitle = religion?.Title,
            CasteTitle = religion?.Title,
            CommunityTitle = community?.Title,
            NationalityTitle = nationality?.Title,
            MaritalStatusTitle = maritalStatus?.Title,
            HeightTitle = height?.Title,
            WeightTitle = weight?.Title,
            ComplexionTitle = complexion?.Title,
            BodyTypeTitle = bodyType?.Title,
            ProfessionTitle = profession?.Title,
            MotherTongueTitle = motherTongue?.Title,
            ReligiousnessTitle = religiousness?.Title,
            FinancialStatusTitle = financialStatus?.Title,
            UserImages = userImages,
            ActivePlan = activePlan,
            LatestPlan = latestPlan,
            ActiveCredits = activeCredits,
            ExpiryDateText = expiryDate,
            AssignedStaffName = assignedStaffName,
            AssignedStaffId = assignedStaffId,
            FollowUps = followUps,
            VerificationDocuments = verificationDocs,
            Age = CalculateAge(user.DOB)
        };

        return View("CrmDetails", viewModel);
    }

    private int CalculateAge(string? dobString)
    {
        if (string.IsNullOrEmpty(dobString)) return 0;
        if (DateTime.TryParse(dobString, out DateTime dob))
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }
        return 0;
    }

    [HttpGet("/admin/enquiry/get-details/{id:long}")]
    public async Task<IActionResult> GetDetails(long id)
    {
        var user = await _enquiryRepo.Get(id);
        if (user == null) return RedirectToAction(nameof(MemberList));

        var latestPlan = (await _planPurchaseRepository.WhereActive(x => x.UserId == id))
            .OrderByDescending(x => x.CreatedOn)
            .FirstOrDefault();

        var userImages = await _imagesRepo.FirstOrDefault(x => x.UserId == id);

        var contactViews = await _userContactViewRepo.WhereActive(x => x.ViewerUserId == id);
        var shortlists = await _userStarProfileRepo.WhereActive(x => x.UserId == id);
        var likes = await _userFavouriteProfileRepo.WhereActive(x => x.UserId == id);
        var notLikes = await _userNotLikeProfileRepo.WhereActive(x => x.UserId == id);
        var reports = await _userReportRepo.WhereActive(x => x.ReporterUserId == id);

        var successStories = await _successStoryRepo.WhereActive(x => x.SubmittedByUserId == id || x.PartnerUserId == id);
        var successStoryPartnerIds = successStories
            .Select(x => x.SubmittedByUserId == id ? x.PartnerUserId : x.SubmittedByUserId)
            .ToHashSet();

        var allViewedIds = contactViews.Select(x => x.ViewedUserId)
            .Union(shortlists.Select(x => x.StarId))
            .Union(likes.Select(x => x.LikedId))
            .Union(notLikes.Select(x => x.NotLikedId))
            .Union(reports.Select(x => x.ReportedUserId))
            .Distinct()
            .ToList();

        var profiles = await _enquiryRepo.WhereActive(x => allViewedIds.Contains(x.Id));
        var profileDict = profiles.ToDictionary(x => x.Id, x => x);

        bool isPremium = user.IsPremiumMember || (latestPlan != null && latestPlan.ExpiresAt > DateTime.UtcNow);
        var userDto = _mapper.Map<RegistrationDto>(user);
        userDto.IsPremiumMember = isPremium;

        // Auto-heal database flag if out of sync
        if (!user.IsPremiumMember && isPremium)
        {
            try
            {
                user.IsPremiumMember = true;
                await _enquiryRepo.Update(user);
                await _enquiryRepo.SaveChanges();
            }
            catch { }
        }

        var viewModel = new PremiumAnalyticsViewModel
        {
            User = userDto,
            LatestPlan = latestPlan,
            UserImages = _mapper.Map<ImagesDto>(userImages),
            ContactViews = contactViews.OrderByDescending(x => x.CreatedOn).Select(x => new ActivityDetail
            {
                ProfileId = x.ViewedUserId,
                ProfileName = profileDict.ContainsKey(x.ViewedUserId) ? profileDict[x.ViewedUserId].Name : "Unknown",
                RegisterNumber = profileDict.ContainsKey(x.ViewedUserId) ? profileDict[x.ViewedUserId].RegisterNumber : "N/A",
                ActionDate = x.CreatedOn
            }).ToList(),
            Shortlisted = shortlists.OrderByDescending(x => x.CreatedOn).Select(x => new ActivityDetail
            {
                ProfileId = x.StarId,
                ProfileName = profileDict.ContainsKey(x.StarId) ? profileDict[x.StarId].Name : "Unknown",
                RegisterNumber = profileDict.ContainsKey(x.StarId) ? profileDict[x.StarId].RegisterNumber : "N/A",
                ActionDate = x.CreatedOn
            }).ToList(),
            Liked = likes.OrderByDescending(x => x.CreatedOn).Select(x => new ActivityDetail
            {
                ProfileId = x.LikedId,
                ProfileName = profileDict.ContainsKey(x.LikedId) ? profileDict[x.LikedId].Name : "Unknown",
                RegisterNumber = profileDict.ContainsKey(x.LikedId) ? profileDict[x.LikedId].RegisterNumber : "N/A",
                ActionDate = x.CreatedOn,
                InterestStatus = x.Status,
                UserFavouriteProfileId = x.Id,
                HasSubmittedSuccessStory = successStoryPartnerIds.Contains(x.LikedId)
            }).ToList(),
            NotLiked = notLikes.OrderByDescending(x => x.CreatedOn).Select(x => new ActivityDetail
            {
                ProfileId = x.NotLikedId,
                ProfileName = profileDict.ContainsKey(x.NotLikedId) ? profileDict[x.NotLikedId].Name : "Unknown",
                RegisterNumber = profileDict.ContainsKey(x.NotLikedId) ? profileDict[x.NotLikedId].RegisterNumber : "N/A",
                ActionDate = x.CreatedOn
            }).ToList(),
            Reported = reports.OrderByDescending(x => x.CreatedOn).Select(x => new ActivityDetail
            {
                ProfileId = x.ReportedUserId,
                ProfileName = profileDict.ContainsKey(x.ReportedUserId) ? profileDict[x.ReportedUserId].Name : "Unknown",
                RegisterNumber = profileDict.ContainsKey(x.ReportedUserId) ? profileDict[x.ReportedUserId].RegisterNumber : "N/A",
                ActionDate = x.CreatedOn,
                Details = x.Reason
            }).ToList()
        };

        return View(viewModel);
    }
    //[HttpGet("/admin/pending-enquiry/delete/{id:long}")]
    //public async Task<JsonResult> DeletePending(long id)
    //{
    //    var entity = await _enquiryRepo.Get(id);
    //    if (entity != null)
    //    {
    //        await _enquiryRepo.SoftDelete(entity);
    //        await _enquiryRepo.SaveChanges();
    //        return Json(true);
    //    }
    //    return Json(false);
    //}
    //[HttpPost("/admin/pending-enquiry-delete/{id:long}")]
    public async Task<IActionResult> Del(long id)
    {
        return RedirectToAction(nameof(GetAllPending));
    }


    #endregion

    #region PremiumList
    [HttpGet("/admin/enquiry/premium-enquiry")]
    public async Task<IActionResult> GetAllPremium()
    {
        return View(new EnquiryViewModel
        {
            Enquiries = _mapper.Map<List<RegistrationDto>>((await _planPurchaseRepository.WhereActive(x => x.ExpiresAt > DateTime.UtcNow)).Select(x => x.User))
        });
    }
    #endregion
    #region TransactionDetails
    //[HttpGet("/admin/enquiry/Transaction-enquiry")]
    //public async Task<IActionResult> GetAllTransaction()
    //{
    //    var allTransactionList = await _transactionRepository.GetAllTransactionsAsync();
    //    var transactionViewModel = _mapper.Map<List<Transaction>>(allTransactionList);
    //    return View(transactionViewModel);
    //}
    [HttpGet("/admin/enquiry/Transaction-enquiry")]
    public async Task<IActionResult> GetAllTransaction(DateTime? fromDate, DateTime? toDate, string name, string paymentType)
    {
        var newName = name?.Trim();
        var transactionList = await _transactionRepository.GetTransactionsBasedOnNameDateAsync(fromDate, toDate, newName, paymentType);

        List<RegistrationDto> enquiries = new List<RegistrationDto>();

        enquiries = _mapper.Map<List<RegistrationDto>>(await _enquiryRepo.WhereActive(x => x.IsComplete))
                                    .OrderByDescending(x => x.CreatedOn)
                                    .ToList();

        var transactionViewModel = _mapper.Map<List<Transaction>>(transactionList);
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.Name = newName;
        ViewBag.PaymentType = paymentType;
        ViewBag.Enquiries = enquiries;
        return View(transactionViewModel);
    }

    [HttpGet("/admin/enquiry/Transaction-enquiry/exportpdf")]
    public async Task<IActionResult> ExportToPdf(DateTime? fromDate, DateTime? toDate)
    {
        var allTransactionList = await _transactionRepository.GetTransactionsBasedOnDateAsync(fromDate, toDate);
        var transactionViewModel = _mapper.Map<List<Transaction>>(allTransactionList);
        // Create a new PDF document
        var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PageSize.A3;
        page.Orientation = PageOrientation.Landscape;
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Verdana", 12, XFontStyleEx.Regular);
        var boldFont = new XFont("Verdana", 12, XFontStyleEx.Bold);

        // Define table layout
        double margin = 30;
        double yPoint = 50;
        double col1Width = 150; // Transaction Id
        double col2Width = 150; // Name
        double col3Width = 150; // Sent on
        double col4Width = 100; // Status
        double col5Width = 100; // Amount
        double col6Width = 150; // Phone
        double col7Width = 300; // Email

        // Draw header
        gfx.DrawString("Transactions", boldFont, XBrushes.Black, new XRect(0, yPoint, page.Width, 20), XStringFormats.TopCenter);
        yPoint += 30;

        // Draw table headers
        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, new XRect(margin, yPoint, col1Width, 20));
        gfx.DrawString("Sent on", boldFont, XBrushes.Black, new XRect(margin, yPoint, col1Width, 20), XStringFormats.Center);

        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, new XRect(margin + col1Width, yPoint, col2Width, 20));
        gfx.DrawString("Transaction Id", boldFont, XBrushes.Black, new XRect(margin + col1Width, yPoint, col2Width, 20), XStringFormats.Center);

        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, new XRect(margin + col1Width + col2Width, yPoint, col3Width, 20));
        gfx.DrawString("Name", boldFont, XBrushes.Black, new XRect(margin + col1Width + col2Width, yPoint, col3Width, 20), XStringFormats.Center);

        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, new XRect(margin + col1Width + col2Width + col3Width, yPoint, col4Width, 20));
        gfx.DrawString("Status", boldFont, XBrushes.Black, new XRect(margin + col1Width + col2Width + col3Width, yPoint, col4Width, 20), XStringFormats.Center);

        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, new XRect(margin + col1Width + col2Width + col3Width + col4Width, yPoint, col5Width, 20));
        gfx.DrawString("Amount", boldFont, XBrushes.Black, new XRect(margin + col1Width + col2Width + col3Width + col4Width, yPoint, col5Width, 20), XStringFormats.Center);

        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, new XRect(margin + col1Width + col2Width + col3Width + col4Width + col5Width, yPoint, col6Width, 20));
        gfx.DrawString("Phone", boldFont, XBrushes.Black, new XRect(margin + col1Width + col2Width + col3Width + col4Width + col5Width, yPoint, col6Width, 20), XStringFormats.Center);

        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, new XRect(margin + col1Width + col2Width + col3Width + col4Width + col5Width + col6Width, yPoint, col7Width, 20));
        gfx.DrawString("Email", boldFont, XBrushes.Black, new XRect(margin + col1Width + col2Width + col3Width + col4Width + col5Width + col6Width, yPoint, col7Width, 20), XStringFormats.Center);

        yPoint += 20;

        // Draw table rows
        foreach (var item in transactionViewModel)
        {
            gfx.DrawRectangle(XPens.Black, XBrushes.White, new XRect(margin, yPoint, col1Width, 20));
            gfx.DrawString(item.CreatedOn.ToShortDateString(), font, XBrushes.Black, new XRect(margin, yPoint, col1Width, 20), XStringFormats.Center);

            gfx.DrawRectangle(XPens.Black, XBrushes.White, new XRect(margin + col1Width, yPoint, col2Width, 20));
            gfx.DrawString(item.TxnId, font, XBrushes.Black, new XRect(margin + col1Width, yPoint, col2Width, 20), XStringFormats.Center);

            gfx.DrawRectangle(XPens.Black, XBrushes.White, new XRect(margin + col1Width + col2Width, yPoint, col3Width, 20));
            gfx.DrawString(item.FirstName, font, XBrushes.Black, new XRect(margin + col1Width + col2Width, yPoint, col3Width, 20), XStringFormats.Center);

            gfx.DrawRectangle(XPens.Black, XBrushes.White, new XRect(margin + col1Width + col2Width + col3Width, yPoint, col4Width, 20));
            gfx.DrawString(item.Status, font, XBrushes.Black, new XRect(margin + col1Width + col2Width + col3Width, yPoint, col4Width, 20), XStringFormats.Center);

            gfx.DrawRectangle(XPens.Black, XBrushes.White, new XRect(margin + col1Width + col2Width + col3Width + col4Width, yPoint, col5Width, 20));
            gfx.DrawString(item.Amount.ToString(), font, XBrushes.Black, new XRect(margin + col1Width + col2Width + col3Width + col4Width, yPoint, col5Width, 20), XStringFormats.Center);

            gfx.DrawRectangle(XPens.Black, XBrushes.White, new XRect(margin + col1Width + col2Width + col3Width + col4Width + col5Width, yPoint, col6Width, 20));
            gfx.DrawString(item.Phone, font, XBrushes.Black, new XRect(margin + col1Width + col2Width + col3Width + col4Width + col5Width, yPoint, col6Width, 20), XStringFormats.Center);

            gfx.DrawRectangle(XPens.Black, XBrushes.White, new XRect(margin + col1Width + col2Width + col3Width + col4Width + col5Width + col6Width, yPoint, col7Width, 20));
            gfx.DrawString(item.Email, font, XBrushes.Black, new XRect(margin + col1Width + col2Width + col3Width + col4Width + col5Width + col6Width, yPoint, col7Width, 20), XStringFormats.Center);

            yPoint += 20;

            // If yPoint exceeds the page height, add a new page
            if (yPoint > page.Height - 50)
            {
                page = document.AddPage();
                page.Size = PageSize.A3;
                page.Orientation = PageOrientation.Landscape;
                gfx = XGraphics.FromPdfPage(page);
                yPoint = 50;
            }
        }
        // Save the document
        using (var stream = new MemoryStream())
        {
            document.Save(stream, false);
            stream.Position = 0;
            if (fromDate.HasValue && toDate.HasValue)
            {
                return File(stream.ToArray(), "application/pdf", "Transactions from " + fromDate.Value.Date + " to " + toDate.Value.Date + ".pdf");

            }
            else
            {
                return File(stream.ToArray(), "application/pdf", "Transactions.pdf");
            }
        }

    }

    [HttpGet("/admin/enquiry/get-user-by-regid/{regId}")]
    public async Task<IActionResult> GetUserByRegId(string regId)
    {
        var user = await _enquiryRepo.FirstOrDefaultActive(x => x.RegisterNumber == regId);
        if (user == null) return Json(null);
        return Json(new { name = user.Name, phone = user.Phone, email = user.Email, id = user.Id });
    }

    [HttpGet("/admin/enquiry/add-subscription")]
    public IActionResult AddSubscription()
    {
        return View();
    }

    [HttpGet("/admin/enquiry/check-txnid-availability")]
    public async Task<IActionResult> CheckTxnIdAvailability([FromQuery] string txnId)
    {
        if (string.IsNullOrWhiteSpace(txnId))
        {
            return Json(new { available = false, message = "Transaction ID cannot be empty." });
        }

        var cleanTxnId = txnId.Trim();
        bool isUsedInTxn = await _transactionRepository.IsTransactionIdExistsAsync(cleanTxnId);
        if (isUsedInTxn)
        {
            return Json(new { available = false, message = "This Transaction ID is already used for another transaction." });
        }

        bool isUsedInFollowUp = await _dbContext.FollowUps.AnyAsync(f => !f.IsDeleted && f.TransactionId != null && f.TransactionId.ToLower() == cleanTxnId.ToLower());
        if (isUsedInFollowUp)
        {
            return Json(new { available = false, message = "This Transaction ID is already submitted in a staff follow-up payment." });
        }

        return Json(new { available = true, message = "Transaction ID is available." });
    }

    [HttpPost("/admin/enquiry/add-subscription")]
    public async Task<IActionResult> AddManualSubscription(string regId, string txnId, string amount, OfflinePaymentMethod offlinePaymentType)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers["Accept"].ToString().Contains("application/json"));
        try
        {
            if (string.IsNullOrWhiteSpace(txnId))
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Transaction ID is required." });
                }
                ModelState.AddModelError("txnId", "Transaction ID is required.");
                return View("AddSubscription");
            }

            var cleanTxnId = txnId.Trim();
            bool isUsedInTxn = await _transactionRepository.IsTransactionIdExistsAsync(cleanTxnId);
            bool isUsedInFollowUp = await _dbContext.FollowUps.AnyAsync(f => !f.IsDeleted && f.TransactionId != null && f.TransactionId.ToLower() == cleanTxnId.ToLower());

            if (isUsedInTxn || isUsedInFollowUp)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Transaction ID is already used for another transaction. Please enter a unique Transaction ID." });
                }
                ModelState.AddModelError("txnId", "Transaction ID is already used for another transaction. Please enter a unique Transaction ID.");
                return View("AddSubscription");
            }

            var user = await _enquiryRepo.FirstOrDefaultActive(x => x.RegisterNumber == regId);
            if (user == null)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "User not found with this Registration ID" });
                }
                ModelState.AddModelError("", "User not found with this Registration ID");
                return View("AddSubscription");
            }

            var transaction = new Transaction
            {
                userId = user.Id,
                TxnId = cleanTxnId,
                Amount = amount,
                Status = "success",
                PaymentType = "Offline",
                OfflinePaymentType = offlinePaymentType,
                PaymentGateway = "Manual",
                Key = "",
                Udf1 = user.Id.ToString(),
                ProductInfo = "Manual Subscription",
                Hash = "",
                FirstName = user.Name ?? "",
                Email = user.Email ?? "",
                Phone = user.Phone ?? "",
                CreatedBy = User.Identity?.Name ?? "Admin",
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now,
                ModifiedBy = User.Identity?.Name ?? "Admin",
                Source = "Admin"
            };

            await _transactionRepository.AddPaymentResultAsync(transaction);

            var planPurchase = new PlanPurchase
            {
                UserId = user.Id,
                ViewCreditsPurchased = 50
            };
            await _planPurchaseRepository.Add(planPurchase);
            await _planPurchaseRepository.SaveChanges();

            user.IsPremiumMember = true;
            await _enquiryRepo.Update(user);
            await _enquiryRepo.SaveChanges();

            // Update follow-up and timeline so assigned staff receives incentive & conversion attribution
            try
            {
                var userFollowUps = await _dbContext.FollowUps
                    .Where(f => f.ProfileId == user.Id && !f.IsDeleted && (f.FollowUpType == FollowUpType.PremiumFollowUp || f.FollowUpType == FollowUpType.RenewalFollowUp))
                    .ToListAsync();

                var activeFollowUp = userFollowUps.OrderByDescending(f => f.CreatedOn).FirstOrDefault();

                // If no follow-up entity exists but a staff is assigned, auto-create the follow-up
                if (activeFollowUp == null)
                {
                    var assignment = await _dbContext.StaffProfileAssignments
                        .FirstOrDefaultAsync(a => a.ProfileId == user.Id && !a.IsDeleted && a.IsActive);

                    if (assignment != null)
                    {
                        activeFollowUp = new FollowUp
                        {
                            ProfileId = user.Id,
                            FollowUpType = FollowUpType.PremiumFollowUp,
                            AssignedStaffId = assignment.StaffId,
                            IsActive = true,
                            CreatedOn = DateTime.Now
                        };
                        await _dbContext.FollowUps.AddAsync(activeFollowUp);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                if (activeFollowUp != null)
                {
                    activeFollowUp.PaymentCompleted = true;
                    activeFollowUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;
                    activeFollowUp.PaymentMode = "Offline";
                    activeFollowUp.OfflinePaymentType = offlinePaymentType;
                    activeFollowUp.TransactionId = txnId;
                    if (decimal.TryParse(amount, out decimal parsedAmt))
                    {
                        activeFollowUp.PaymentAmount = parsedAmt;
                    }
                    activeFollowUp.ModifiedOn = DateTime.Now;

                    if (activeFollowUp.FollowUpType == FollowUpType.PremiumFollowUp)
                    {
                        activeFollowUp.LatestInterestStatus = PremiumInterestStatus.Converted;
                    }
                    else if (activeFollowUp.FollowUpType == FollowUpType.RenewalFollowUp)
                    {
                        activeFollowUp.LatestRenewalInterestStatus = RenewalInterestStatus.Renewed;
                    }

                    _dbContext.FollowUps.Update(activeFollowUp);
                    await _dbContext.SaveChangesAsync();

                    var adminName = User.Identity?.Name ?? "Admin";
                    var timeline = new FollowUpTimeline
                    {
                        FollowUpId = activeFollowUp.Id,
                        StaffId = activeFollowUp.AssignedStaffId ?? 0,
                        StaffName = $"Admin ({adminName}) - Manual Subscription",
                        ContactType = activeFollowUp.LatestContactType,
                        CallStatus = activeFollowUp.LatestCallStatus,
                        Remarks = $"Manual subscription added by Admin. TxnId: {txnId}, Amount: {amount}, Payment Method: {offlinePaymentType}. Membership activated.",
                        NextFollowUpDate = null,
                        InterestStatus = activeFollowUp.LatestInterestStatus,
                        RenewalInterestStatus = activeFollowUp.LatestRenewalInterestStatus,
                        ProfileVerificationStatus = activeFollowUp.LatestProfileVerificationStatus,
                        CreatedOn = DateTime.Now,
                        ModifiedOn = DateTime.Now,
                        IsActive = true
                    };
                    await _dbContext.FollowUpTimelines.AddAsync(timeline);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception fuEx)
            {
                Log.Error(fuEx, "Error updating follow-up on manual subscription for UserId={UserId}", user.Id);
            }

            if (isAjax)
            {
                return Json(new { success = true, message = "Manual subscription added successfully." });
            }

            return RedirectToAction(nameof(GetAllTransaction));
        }
        catch (Exception ex)
        {
            if (isAjax)
            {
                return Json(new { success = false, message = ex.Message });
            }
            ModelState.AddModelError("", ex.Message);
            return View("AddSubscription");
        }
    }
    #endregion

    bool isKerala(string stateName)
    {
        if (stateName.Trim().ToUpper() == "KERALA")
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool isIndia(string countryName)
    {
        if (countryName.Trim().ToUpper() == "INDIA")
        {
            return true;
        }
        else
        {
            return false;
        }
    }


}