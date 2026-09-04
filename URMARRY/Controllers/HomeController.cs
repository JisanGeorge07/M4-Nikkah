using Application.Constants;
using Application.Helpers;
using Application.Interfaces.Infrastructure;
using Application.Interfaces.Infrastructure.Email;
using Application.Interfaces.Persistence;
using Application.Models;
using Application.Models.Framework;
using AutoMapper;
using Domain;
using Domain.Framework;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Persistence.Repositories;
using Persistence.Services;
using Razor.Templating.Core;
using Serilog;
using System.Diagnostics;
using System.Net.Mail;
using System.Security.Claims;
using URMARRY.Models;
using URMARRY.Services;

namespace URMARRY.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepository<HomeContent> _homeContentRepo;
        private readonly IRepository<ImageGallery> _imageGalleryRepo;
        private readonly IRepository<State> _stateRepo;
        private readonly IProfileService _profileService;
        private readonly IRepository<ProfileFor> _profileForRepo;
        private readonly IRepository<Nationality> _nationalityRepo;
        private readonly IRepository<MaritalStatus> _maritalStatusRepo;
        private readonly IRepository<BodyFeatures> _bodyFeaturesRepo;
        private readonly IRepository<Profession> _professionRepo;
        private readonly IRepository<MotherTongue> _motherTongueRepo;
        private readonly IRepository<ReligionCaste> _religionCasteRepo;
        private readonly IRepository<Religiousness> _religiousnessRepo;
        private readonly IRepository<Community> _communityRepo;
        private readonly IRepository<FinancialStatus> _financialStatus;
        private readonly IMapper _mapper;
        private readonly IRepository<SocialMedia> _socialMediaRepo;
        private readonly IFileService _fileService;
        private readonly IRepository<Email> _emailRepo;
        private readonly IEmailService _emailService;
        private readonly EmailNotificationHelper _emailNotificationHelper;
        private readonly IRepository<Registration> _registrationRepo;
        private readonly IRepository<State> _stateRepository;
        private readonly IRepository<District> _districtRepository;
		private readonly IRepository<City> _cityRepository;
		private readonly CookieHelper _cookieHelper;

		internal Guid key = new Guid("F7AD797A-416C-4C56-8749-7017FBC90427");
        public IHttpContextAccessor ContextAccessor { get; }
        private readonly IRepository<HomeBanner> _homeBannerRepo;
        private readonly IRepository<About> _aboutRepo;
        private readonly IRepository<SuccessStory> _successStoryRepo;

        public HomeController(ILogger<HomeController> logger,
            IRepository<HomeContent> homeContentRepo, IProfileService profileService,
            IRepository<ImageGallery> imageGalleryRepo,
            IRepository<ProfileFor> profileForRepo,
            IRepository<State> stateRepo,
            IRepository<Nationality> nationalityRepo,
            IRepository<MaritalStatus> maritalStatusRepo,
            IRepository<BodyFeatures> bodyFeaturesRepo,
            IRepository<Profession> professionRepo,
            IRepository<MotherTongue> motherTongueRepo,
            IRepository<ReligionCaste> religionCasteRepo,
            IRepository<Religiousness> religiousnessRepo,
            EmailNotificationHelper emailNotificationHelper,
            IRepository<Community> communityRepo,
            IRepository<FinancialStatus> financialStatus,
            IMapper mapper,
            IRepository<SocialMedia> socialMediaRepo,
            IFileService fileService,
            IRepository<Email> emailRepo,
            IEmailService emailService,
            IRepository<Registration> registrationRepo,
            IHttpContextAccessor contextAccessor,
            IRepository<HomeBanner> homeBannerRepo,
            IRepository<About> aboutRepo,
            IRepository<State> stateRepository,
            IRepository<District> districtRepository,
			IRepository<City> cityRepository,
            IRepository<SuccessStory> successStoryRepo,
			CookieHelper cookieHelper)
        {
            _logger = logger;
            _homeContentRepo = homeContentRepo;
            _imageGalleryRepo = imageGalleryRepo;
            _profileForRepo = profileForRepo;
            _nationalityRepo = nationalityRepo;
            _maritalStatusRepo = maritalStatusRepo;
            _bodyFeaturesRepo = bodyFeaturesRepo;
            _professionRepo = professionRepo;
            _stateRepo = stateRepo;
            _motherTongueRepo = motherTongueRepo;
            _religionCasteRepo = religionCasteRepo;
            _profileService = profileService;
            _emailNotificationHelper = emailNotificationHelper;
            _religiousnessRepo = religiousnessRepo;
            _communityRepo = communityRepo;
            _financialStatus = financialStatus;
            _mapper = mapper;
            _socialMediaRepo = socialMediaRepo;
            _emailRepo = emailRepo;
            _emailService = emailService;
            _fileService = fileService;
            _registrationRepo = registrationRepo;
            ContextAccessor = contextAccessor;
            _homeBannerRepo = homeBannerRepo;
            _aboutRepo = aboutRepo;
            _stateRepository = stateRepository;
            _districtRepository = districtRepository;
			_cityRepository = cityRepository;
            _successStoryRepo = successStoryRepo;
			_cookieHelper = cookieHelper;
		}

        [HttpGet("/")]
        public async Task<IActionResult> Index()
        {
            var userId = _cookieHelper.GetUserIdFromCookie(HttpContext);
            Registration? incompleteUser = null;
            if (userId.HasValue)
            {
                var user = await _registrationRepo.Get(userId.Value);
                if (user != null)
                {
                    if (user.IsComplete)
                    {
                        return RedirectToAction("Dashboard", "User");
                    }
                    incompleteUser = user;
                }
            }

            var rawStories = await _successStoryRepo.WhereActive(x => x.Status == SuccessStoryStatus.Published);
            var mappedStories = _mapper.Map<List<SuccessStoryDto>>(rawStories)
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.CreatedOn)
                .ToList();

            return View(new HomeViewModel
            {
                HomeBanners = _mapper.Map<List<HomeBannerDto>>(await _homeBannerRepo.GetAllActive()),
                About = _mapper.Map<AboutDto>(await _aboutRepo.First()),
                HomeContent = _mapper.Map<HomeContentDto>(await _homeContentRepo.First()),
                Registrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive(x => x.ShowOnHomePage)).Take(20).ToList(),
                ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive()),
                Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive()).OrderBy( x => x.Title).ToList(),
                MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
                BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
                Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive()),
                MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive()),
                Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0)),
                Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId != 0)),
                Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
                Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive()),
                FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive()),
                States = _mapper.Map<List<StateDto>>(await _stateRepository.GetAllActive()),
                Districts = _mapper.Map<List<DistrictDto>>(await _districtRepository.GetAllActive()),
                Cities = _mapper.Map<List<CityDto>>(await _cityRepository.GetAllActive()),
				Registration = incompleteUser != null ? _mapper.Map<RegistrationDto>(incompleteUser) : new RegistrationDto(),
                SuccessStories = mappedStories,
            });
        }

        //[ValidateReCaptcha]
        [HttpPost("/registration")]
        public async Task<IActionResult> PostEnquiry(RegistrationDto model)
        {
            //if (!ModelState.IsValid)
            //{
            //    var errors = ModelState.Values.SelectMany(v => v.Errors)
            //        .Select(e => e.ErrorMessage);
            //    TempData["ValidationErrors"] = string.Join("\n", errors);
            //    return RedirectToAction(nameof(Index));
            //}  model.EntityName = career.TitleEnglish;
            //model.Email = model.Email!.ToLower();
            RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
            //var States = _mapper.Map<List<StateDto>>(await _stateRepo.GetAllActive());
            var ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive());
            var Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive());
            var MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive());
            var BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive());
            var Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive());
            var MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive());
            var Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0));
            var Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId != 0));
            var Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive());
            var Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive());
            var FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive());
            model.DOB = model.Day + "/" + model.Month + "/" + model.Year;
			//model.State = !string.IsNullOrEmpty(model.State) ? States.FirstOrDefault(x => x.Name == model.State)?.Name : null;
			model.ProfileFor = model.ProfileForId > 0 ? ProfileFor.FirstOrDefault(x => x.Id == model.ProfileForId).Title : "";
            model.Nationality = model.NationalityId > 0 ? Nationalities.FirstOrDefault(x => x.Id == model.NationalityId).Title : "";
            model.MaritalStatus = model.MaritalStatusId > 0 ? MaritalStatuses.FirstOrDefault(x => x.Id == model.MaritalStatusId).Title : "";
            model.Height = model.HeightId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.HeightId).Title : "";
            model.Weight = model.WeightId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.WeightId).Title : "";
            model.Complexion = model.ComplexionId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.ComplexionId).Title : "";
            model.BodyType = model.BodyTypeId > 0 ? BodyFeatures.FirstOrDefault(x => x.Id == model.BodyTypeId).Title : "";
            model.MotherTongue = model.MotherTongueId > 0 ? MotherTongues.FirstOrDefault(x => x.Id == model.MotherTongueId).Title : "";
            model.Religion = model.ReligionId > 0 ? Religions.FirstOrDefault(x => x.Id == model.ReligionId).Title : "";
            model.Caste = model.CasteId > 0 ? Castes.FirstOrDefault(x => x.Id == model.CasteId).Title : "";
            model.Community = model.CommunityId > 0 ? Communities.FirstOrDefault(x => x.Id == model.CommunityId).Title : "";
            model.Religiousness = model.ReligiousnessId > 0 ? Religiousnesses.FirstOrDefault(x => x.Id == model.ReligiousnessId).Title : "";
            model.FinancialStatus = model.FinancialStatusId > 0 ? FinancialStatuses.FirstOrDefault(x => x.Id == model.FinancialStatusId).Title : "";

            //new changes
            Random rdm = new Random();
            string pin = rdm.Next(111111, 999999).ToString();
            model.VerificationCode = pin;
            model.RegisterNumber = "M4N" + Guid.NewGuid().ToString("N").Substring(0, 7);
            model.IsVerified = false;
            model.IsPremiumMember = false;
            model.Source = "Website";
            //model.PasswordHash = encryption.CreateSalt();
            //model.Password = encryption.EncryptRijndael(model.Password, model.PasswordHash);

            var entity = _mapper.Map<Registration>(model);
            await _fileService.SaveAllFiles(entity, model, "Uploads/Registration");
            await _registrationRepo.Add(entity);
            await _registrationRepo.SaveChanges();

            // Set secure JWT cookie upon successful registration
            _cookieHelper.SetSecureJwtCookie(HttpContext, entity.Id, entity.Email);

            var claims = new List<Claim>
                        {
                           new Claim(ClaimTypes.NameIdentifier, entity.Id.ToString()),
                           new Claim(ClaimTypes.UserData, model.IsVerified.ToString())
                        };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(
                "Cookies",
                principal);

            var email = _mapper.Map<EmailDto>(await _emailRepo.FirstOrDefault(x => x.Purpose == EnquiryTypes.Registration));
            if (!string.IsNullOrWhiteSpace(email.EmailId))
            {
                model.CreatedOn = DateTime.Now;
                var emailModel = new HomeViewModel
                {
                    HomeContent = _mapper.Map<HomeContentDto>(await _homeContentRepo.First()),
                    Enquiry = model,
                    SenderEmail = email.EmailId
                };

                if (!string.IsNullOrWhiteSpace(email.Recipients))
                {
                    var enquiryHtml =
                        await RazorTemplateEngine.RenderAsync("/Templates/Mail/CareerApplication/AdminNotification.cshtml",
                            emailModel);
                    await _emailService.Send(new Message
                    {
                        From = email,
                        To = email.Recipients?.Split(",").Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => new MailboxAddress("M4 NIKAH", x)).ToList(),
                        Subject = "New enquiry from " + model.Name,
                        Content = enquiryHtml,
                        AttachmentPaths = new List<string?> { model.ImagePath }
                    });
                }

                //Confirmation email
                if (!string.IsNullOrWhiteSpace(model.Email))
                {
                    var enquiryConfirmationHtml =
                        await RazorTemplateEngine.RenderAsync("/Templates/Mail/CareerApplication/SenderConfirmation.cshtml",
                            emailModel);
                    await _emailService.Send(new Message
                    {
                        From = email,
                        To = new List<MailboxAddress> { new(model.Name, model.Email) },
                        Subject = "Account verification",
                        Content = enquiryConfirmationHtml,
                        //AttachmentPaths = new List<string?> { model.AttachedFilePath }
                    });
                }
            }

            return RedirectToAction(nameof(Result));
        }

        public async Task<JsonResult> Save(RegistrationDto model)
        {
            try
            {
                _logger.LogWarning("Save function starts: {Id}", model.Id);
                RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
                Registration entity;


                if (model.Id <= 0)
                {
                    _logger.LogWarning("Adding a new registration for email: {Email}", model.Email);
                    if (string.IsNullOrEmpty(model.Email))
                    {
                        return Json(new { status = 0, message = "Please provide an email address", id = 0, code = 0 });
                    }
                    var emailValidation = await _registrationRepo.WhereActive(x => x.Email == model.Email && x.Email != null && x.IsVerified);
                    if (emailValidation.Count() > 0)
                    {
                        return Json(new { status = 0, message = "Email address already exists", id = 0, code = 0 });
                    }
                    var phoneValidation = await _registrationRepo.WhereActive(x => x.Phone == model.Phone && x.Phone != null && x.IsVerified);
                    if (phoneValidation.Count() > 0)
                    {
                        return Json(new { status = 0, message = "Phone number already exists", id = 0, code = 0 });
                    }
                    model.DOB = !string.IsNullOrEmpty(model.DOB) ? model.Day + "/" + model.Month + "/" + model.Year : "01/01/1990";
                    model.BodyTypeId = 0;
                    model.CasteId = 0;
                    model.CommunityId = 0;
                    model.ComplexionId = 0;
                    model.FinancialStatusId = 0;
                    model.HeightId = 0;
                    model.MaritalStatusId = 0;
                    model.MotherTongueId = 0;
                    model.NationalityId = 0;
                    model.ProfessionId = 0;
                    model.ReligionId = 0;
                    model.IsPhysicallyChallenged = false;
                    model.IsComplete = false;
                    model.IsSpecialRequest = false;
                    model.IsActive = true;

                    model.IsVerified = false;
                    model.IsPremiumMember = false;
                    model.ShowOnHomePage = false;
                    model.Source = "Website";

                    // Generate verification OTP
                    Random rdm = new Random();
                    string pin = rdm.Next(111111, 999999).ToString();
                    model.VerificationCode = pin;
                    model.OtpGeneratedAt = DateTime.Now;
                    model.OtpResendCount = 0;

                    // Hashing password
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        model.PasswordHash = encryption.CreateSalt();
                        model.Password = encryption.EncryptRijndael(model.Password, model.PasswordHash);
                    }

                    entity = _mapper.Map<Registration>(model);
                    await _fileService.SaveAllFiles(entity, model, "Uploads/Registration");
                    await _registrationRepo.Add(entity);
                    await _registrationRepo.SaveChanges();

                    // Send OTP via SMS
                    await _emailService.SendSmsAsync(model.VerificationCode, model.Phone);
                    _logger.LogWarning("Sent OTP via SMS to phone number: {Phone}", model.Phone);

                    // Send OTP via Email
                    try
                    {
                        var htmlContent = $"Dear Customer, <br/><br/>{pin} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
                        var htmlContentView = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                        _logger.LogWarning("sent OTP Via Email Starts for ID: {Id}", entity.Id);
                        _emailNotificationHelper.SendEmail(model.Email, htmlContentView, "Your OTP for M4nikah");
                        _logger.LogWarning("sent OTP Via Email Ends for ID: {Id}", entity.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        _logger.LogError("OTP send via Email failed due to error message");
                    }

                    return Json(new { status = 1, id = entity.Id, code = "" });
                }
                else
                {
                    entity = (await _registrationRepo.Get(model.Id))!;

                    // Only check email and phone uniqueness if on Step-1 (where these fields are entered/modified) or if completed step is not specified.
                    if (model.CompletedStep == "Step-1" || string.IsNullOrEmpty(model.CompletedStep))
                    {
                        var existing = await _registrationRepo.WhereActive(x => x.Email == model.Email && x.Id != entity.Id && x.IsVerified);
                        if (existing.Count() > 0)
                        {
                            return Json(new { status = 0, message = "Email address already exists", id = 0, code = 0 });
                        }
                        var existingPhone = await _registrationRepo.WhereActive(x => x.Phone == model.Phone && x.Id != entity.Id && x.IsVerified);
                        if (existingPhone.Count() > 0)
                        {
                            return Json(new { status = 0, message = "Phone number already exists", id = 0, code = 0 });
                        }
                    }

                    if (model.CompletedStep == "Step-1")
                    {
                        _logger.LogWarning("Re-entering Step-1 for user with ID: {Id}", model.Id);
                        Random rdm = new Random();
                        string pin = rdm.Next(111111, 999999).ToString();
                        model.VerificationCode = pin;
                        model.OtpGeneratedAt = DateTime.Now;
                        model.OtpResendCount = 0;
                        _logger.LogWarning("Generated verification code: {Code} for email: {Email}", pin, model.Email);

                        // Send OTP via SMS
                        await _emailService.SendSmsAsync(model.VerificationCode, model.Phone);
                        _logger.LogWarning("Sent OTP via SMS to phone number: {Phone}", model.Phone);

                        // Send OTP via Email
                        try
                        {
                            var htmlContent = $"Dear Customer, <br/><br/>{pin} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
                            var htmlContentView = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                            _logger.LogWarning("sent OTP Via Email Starts for ID: {Id}", entity.Id);
                            _emailNotificationHelper.SendEmail(model.Email, htmlContentView, "Your OTP for M4nikah");
                            _logger.LogWarning("sent OTP Via Email Ends for ID: {Id}", entity.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                            _logger.LogError("OTP send via Email failed due to error message");
                        }

                        // Re-hash Password if changed
                        if (!string.IsNullOrEmpty(model.Password) && model.Password != entity.Password)
                        {
                            model.PasswordHash = encryption.CreateSalt();
                            model.Password = encryption.EncryptRijndael(model.Password, model.PasswordHash);
                        }
                        else
                        {
                            model.Password = entity.Password;
                            model.PasswordHash = entity.PasswordHash;
                        }
                    }
                    else
                    {
                        model.VerificationCode = entity.VerificationCode;
                        model.OtpGeneratedAt = entity.OtpGeneratedAt;
                        model.OtpResendCount = entity.OtpResendCount;
                        model.Password = entity.Password;
                        model.PasswordHash = entity.PasswordHash;
                    }

                    _logger.LogWarning("verification code and password updated successfully for ID: {Id}", entity.Id);
                    model.Phone = string.IsNullOrEmpty(model.Phone) ? entity.Phone : model.Phone;
                    model.Email = string.IsNullOrEmpty(model.Email) ? entity.Email : model.Email;
                    model.Name = string.IsNullOrEmpty(model.Name) ? entity.Name : model.Name;
                    model.Gender = string.IsNullOrEmpty(model.Gender) ? entity.Gender : model.Gender;
                    model.ProfileForId = model.ProfileForId <= 0 ? entity.ProfileForId : model.ProfileForId;
                    model.DOB = (!string.IsNullOrEmpty(model.Day) ? model.Day : "01") + "/" + (!string.IsNullOrEmpty(model.Month) ? model.Month : "01") + "/" + (!string.IsNullOrEmpty(model.Year) ? model.Year : "1990");
                    model.NationalityId = model.NationalityId <= 0 ? entity.NationalityId : model.NationalityId;
                    model.MaritalStatusId = model.MaritalStatusId <= 0 ? entity.MaritalStatusId : model.MaritalStatusId;
                    model.HeightId = model.HeightId <= 0 ? entity.HeightId : model.HeightId;
                    model.WeightId = model.WeightId <= 0 ? entity.WeightId : model.WeightId;
                    model.ComplexionId = model.ComplexionId <= 0 ? entity.ComplexionId : model.ComplexionId;
                    model.BodyTypeId = model.BodyTypeId <= 0 ? entity.BodyTypeId : model.BodyTypeId;
                    model.ProfessionId = model.ProfessionId <= 0 ? entity.ProfessionId : model.ProfessionId;
                    model.MotherTongueId = model.MotherTongueId <= 0 ? entity.MotherTongueId : model.MotherTongueId;
                    model.ReligionId = model.ReligionId <= 0 ? entity.ReligionId : model.ReligionId;
                    model.CasteId = model.CasteId <= 0 ? entity.CasteId : model.CasteId;
                    model.CommunityId = model.CommunityId <= 0 ? entity.CommunityId : model.CommunityId;
                    model.ReligiousnessId = model.ReligiousnessId <= 0 ? entity.ReligiousnessId : model.ReligiousnessId;
                    model.FinancialStatusId = model.FinancialStatusId <= 0 ? entity.FinancialStatusId : model.FinancialStatusId;

                    model.RegisterNumber = entity.RegisterNumber;
                    model.Source = string.IsNullOrEmpty(entity.Source) ? "Website" : entity.Source;
                    model.IsActive = true;
                    model.IsPremiumMember = false;
                    model.ShowOnHomePage = false;

                    // Set verify status based on current completed step
                    model.IsVerified = (model.CompletedStep == "Step-OTP-Verify") || (model.CompletedStep == "Step-5") || entity.IsVerified;
                    model.IsComplete = (model.CompletedStep == "Step-5") || entity.IsComplete;

                    if (model.Id.ToString().Length > 0)
                    {
                        if (6 - model.Id.ToString().Length == 5)
                        {
                            model.RegisterNumber = "M400000" + (model.Id).ToString();
                        }
                        else if (6 - model.Id.ToString().Length == 4)
                        {
                            model.RegisterNumber = "M40000" + (model.Id).ToString();
                        }
                        else if (6 - model.Id.ToString().Length == 3)
                        {
                            model.RegisterNumber = "M4000" + (model.Id).ToString();
                        }
                        else if (6 - model.Id.ToString().Length == 2)
                        {
                            model.RegisterNumber = "M400" + (model.Id).ToString();
                        }
                        else if (6 - model.Id.ToString().Length == 1)
                        {
                            model.RegisterNumber = "M40" + (model.Id).ToString();
                        }
                        else if (6 - model.Id.ToString().Length == 0)
                        {
                            model.RegisterNumber = "M4" + (model.Id).ToString();
                        }
                    }

                    _mapper.Map(model, entity);
                    await _fileService.SaveAllFiles(entity, model, "Uploads/Registration");
                    await _registrationRepo.Update(entity);
                    await _registrationRepo.SaveChanges();

                    if ((model.CompletedStep == "Step-5") && (model.IsVerified == true))
                    {
                        try
                        {
                            _logger.LogWarning("step 5 modal validation success, sending registration success email to {Email}", entity.Email);
                            await SendRegistrationSuccessEmail(model.Email, model.Name, model.RegisterNumber);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("registration success email sending failed due to " + ex.Message);
                        }
                    }
                    if ((model.CompletedStep == "Step-5") && (model.IsVerified == false))
                    {
                        try
                        {
                            _logger.LogWarning("step 5 modal validation failed, sending unsuccessful registration email to {Email}", entity.Email);
                            await SendRegistrationUnsuccessfulEmail(model.Email, model.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("registration unsuccess email sending failed due to " + ex.Message);
                        }
                    }

                    _logger.LogWarning("Registration step {Step} completed successfully for ID: {Id}", model.CompletedStep, entity.Id);
                    return Json(new { status = 1, id = entity.Id, code = "", RegistrationNo = entity.RegisterNumber });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                _logger.LogError(ex, "save failed: {ErrorMessage}", ex.Message);
                //await SendRegistrationUnsuccessfulEmail(model.Email, model.Name);
               
                return Json(new { status = 0, message = "An error occurred while processing your registration.", error = ex.Message });
                
            }
        }

		[HttpGet]
		public async Task<JsonResult> CheckEmail(RegistrationDto model)
	    {
			
			var emailExists = await _registrationRepo.WhereActive(x => x.Email == model.Email && x.Email != null && x.IsVerified);
			return Json(emailExists == null || !emailExists.Any());// Return true if email does not exist, false otherwise
		}

		[HttpGet]
		public async Task<JsonResult> CheckPhone(RegistrationDto model)
		{
			var phoneExists = await _registrationRepo.WhereActive(x => x.Phone == model.Phone && x.Phone != null && x.IsVerified);
			return Json(phoneExists == null || !phoneExists.Any());// Return true if phone does not exist, false otherwise
		}


		private async Task SendRegistrationSuccessEmail(string email, string name, string registerNumber)
        {
            string templatePath = "Templates/Mail/template-registrationsuccessful.html";
            string emailContent = System.IO.File.ReadAllText(templatePath);
            emailContent = emailContent.Replace("[User's Name]", name);
            emailContent = emailContent.Replace("[Profile ID]", registerNumber);


            var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
            _emailNotificationHelper.SendEmail(email, htmlView, "Welcome to M4Nikah - Your Profile Registration is Complete!");
        }

        private Task SendRegistrationUnsuccessfulEmail(string email, string name)
        {
            string templatePath = "Templates/Mail/template-registrationunsuccessful.html";
            string emailContent = System.IO.File.ReadAllText(templatePath);
            emailContent = emailContent.Replace("[User's Name]", name);
            emailContent = emailContent.Replace("[Support Email]", "support@m4nikkah.com");
            emailContent = emailContent.Replace("[Support Phone Number]", "123-456-7890");

            var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
            _emailNotificationHelper.SendEmail(email, htmlView, "M4Nikkah Registration Unsuccessful");
            return Task.CompletedTask;
        }
        public async Task<IActionResult> Result()
        {
            var registerId = ContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.UserData);
            if (string.IsNullOrEmpty(registerId)) return RedirectToAction(nameof(Index));
            var registration = await _registrationRepo.Get(Convert.ToInt64(registerId));
            return View(new HomeViewModel
            {
                Registration = _mapper.Map<RegistrationDto>(registration),
                HomeBanners = _mapper.Map<List<HomeBannerDto>>(await _homeBannerRepo.GetAllActive()),
                About = _mapper.Map<AboutDto>(await _aboutRepo.First()),
                HomeContent = _mapper.Map<HomeContentDto>(await _homeContentRepo.First()),
                Registrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive(x => x.ShowOnHomePage)),
            });
        }

        [HttpGet("/handle-error/{code:int}")]
        public IActionResult HandleError(int code)
        {
            return code switch
            {
                404 => RedirectToAction(nameof(Index)),
                _ => RedirectToAction(nameof(Index))
            };
        }

        [HttpGet("/page-not-found")]
        public IActionResult PageNotFound()
        {
            return View("Error", new ErrorViewModel
            {
                //Pages = _mapper.Map<List<PageSettingsDto>>(await _pageSettingsRepo.GetAll()),
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorCode = "404",
                Message = "Oops! This page doesn't exist."
            });
        }

        [HttpGet("/server-error")]
        public IActionResult ServerError()
        {
            return View("Error", new ErrorViewModel
            {
                //Pages = _mapper.Map<List<PageSettingsDto>>(await _pageSettingsRepo.GetAll()),
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorCode = "500",
                Message = "Oops! Something went wrong."
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> ChangeNumber()
        {
            var users = await _registrationRepo.GetAll();

            foreach (var item in users)
            {
                if (item.Id.ToString().Length > 0)
                {
                    if (6 - item.Id.ToString().Length == 5)
                    {
                        item.RegisterNumber = "M400000" + (item.Id).ToString();
                    }
                    else if (6 - item.Id.ToString().Length == 4)
                    {
                        item.RegisterNumber = "M40000" + (item.Id).ToString();
                    }
                    else if (6 - item.Id.ToString().Length == 3)
                    {
                        item.RegisterNumber = "M4000" + (item.Id).ToString();
                    }
                    else if (6 - item.Id.ToString().Length == 2)
                    {
                        item.RegisterNumber = "M400" + (item.Id).ToString();
                    }
                    else if (6 - item.Id.ToString().Length == 1)
                    {
                        item.RegisterNumber = "M40" + (item.Id).ToString();
                    }
                    else if (6 - item.Id.ToString().Length == 0)
                    {
                        item.RegisterNumber = "M4" + (item.Id).ToString();
                    }
                    await _registrationRepo.Update(item);
                    await _registrationRepo.SaveChanges();
                }
            }

            return View();
        }

        [HttpPost("{id}/image")]
        public async Task<IActionResult> AddProfilePic([FromRoute] long id, [FromForm] IFormFile? image)
        {
            try
            {
                string imagePath = await _profileService.AddProfilePicAsync(id, image);
                return Ok(new { ImagePath = imagePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding profile image.");
                return StatusCode(500, "Internal server error");
            }
        }
        public ActionResult TermsConditions()
        {
            return View();
        }

        public ActionResult Privacy()
        {
            return View();
        }
	
 	public ActionResult ChildSafetyPolicy()
	{
	return View();
	}

        [HttpGet]
        public async Task<JsonResult> GetUserDetails(string id)
        {
            try
            {
                var _registeredUser = await _registrationRepo.Get(Convert.ToInt64(id));
                if (_registeredUser != null)
                {
                    // Set secure JWT cookie instead of raw 'id' cookie
                    _cookieHelper.SetSecureJwtCookie(HttpContext, _registeredUser.Id, _registeredUser.Email);
                    return Json(new { success = true, message = _registeredUser.RegisterNumber });
                }
                else
                {
                    return Json(new { success = false, message = "User not found" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("api/getcountries")]
        public async Task<IActionResult> GetCountries()
        {
            try
            {

                var countries = await _stateRepository.GetCountriesAsync();

                return Ok(countries);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching {CountryId}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("api/getstates")]
        public async Task<IActionResult> GetStates(long countryId)
        {
            try
            {

                var states = await _stateRepository.GetStatesByCountryIdAsync(countryId);

                return Ok(states);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching districts for state_id {CountryId}", countryId);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("api/getdistricts")]
        public async Task<IActionResult> GetDistricts(long stateid)
        {
            try
            {

                var districts = await _stateRepository.GetDistrictsByStateIdAsync(stateid);

                return Ok(districts);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "an error occurred while fetching districts for state_id {stateid}", stateid);
                return StatusCode(500, "an error occurred while processing your request.");
            }
        }


        [HttpGet("api/cities")]
        public async Task<IActionResult> GetCities([FromQuery] long district_id)
        {
            try
            {
                var cities = await _stateRepository.GetCityByDistrictIdAsync(district_id);

                return Ok(cities);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching cities for district_id {DistrictId}", district_id);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDistrictsByStateName(string stateName)
        {
            var District = await _districtRepository.GetDistrictByStateNameAsync(stateName);
            if (District != null)
            {
                return Json(new { success = true, message = District });
            }
            else
            {
                return Json(new { success = false, message = "District Data Unavailable" });
            }

        }

        [HttpGet]
        public async Task<JsonResult> GetCityByDistictName(string districtName)
        {
            var District = await _districtRepository.GetCityByDistictNameAsync(districtName);
            if (District != null)
            {
                return Json(new { success = true, message = District });
            }
            else
            {
                return Json(new { success = false, message = "District Data Unavailable" });
            }

        }

        [HttpPost]
        public async Task<JsonResult> VerifyOtp(long id, string otp)
        {
            try
            {
                if (id <= 0 || string.IsNullOrWhiteSpace(otp))
                {
                    return Json(new { success = false, message = "Invalid parameters." });
                }

                var entity = await _registrationRepo.Get(id);
                if (entity == null)
                {
                    return Json(new { success = false, message = "Registration not found." });
                }

                if (entity.OtpGeneratedAt.HasValue)
                {
                    var expiryTime = entity.OtpGeneratedAt.Value.AddMinutes(3);
                    if (DateTime.Now > expiryTime)
                    {
                        return Json(new { success = false, message = "OTP has expired. Please request a new code." });
                    }
                }

                if (entity.VerificationCode == otp.Trim())
                {
                    entity.IsVerified = true;
                    entity.CompletedStep = "Step-OTP-Verify";
                    await _registrationRepo.Update(entity);
                    await _registrationRepo.SaveChanges();

                    if (entity.StaffId.HasValue)
                    {
                        try
                        {
                            var dbContext = HttpContext.RequestServices.GetService<Persistence.AppDbContext>();
                            if (dbContext != null)
                            {
                                var existingAssignment = await dbContext.StaffProfileAssignments
                                    .FirstOrDefaultAsync(a => a.ProfileId == entity.Id && a.StaffId == entity.StaffId.Value && !a.IsDeleted);
                                if (existingAssignment == null)
                                {
                                    dbContext.StaffProfileAssignments.Add(new StaffProfileAssignment
                                    {
                                        StaffId = entity.StaffId.Value,
                                        ProfileId = entity.Id
                                    });
                                    await dbContext.SaveChangesAsync();
                                }

                                var existingVerifFollowUp = await dbContext.FollowUps
                                    .FirstOrDefaultAsync(f => f.ProfileId == entity.Id && f.FollowUpType == FollowUpType.ProfileVerification && !f.IsDeleted);
                                if (existingVerifFollowUp == null)
                                {
                                    dbContext.FollowUps.Add(new FollowUp
                                    {
                                        ProfileId = entity.Id,
                                        FollowUpType = FollowUpType.ProfileVerification,
                                        LatestProfileVerificationStatus = ProfileVerificationStatus.Pending,
                                        LatestRemarks = "Auto-generated on OTP verification",
                                        AssignedStaffId = entity.StaffId.Value,
                                        IsActive = true
                                    });
                                }

                                var existingPremFollowUp = await dbContext.FollowUps
                                    .FirstOrDefaultAsync(f => f.ProfileId == entity.Id && f.FollowUpType == FollowUpType.PremiumFollowUp && !f.IsDeleted);
                                if (existingPremFollowUp == null)
                                {
                                    dbContext.FollowUps.Add(new FollowUp
                                    {
                                        ProfileId = entity.Id,
                                        FollowUpType = FollowUpType.PremiumFollowUp,
                                        LatestRemarks = "Auto-generated on OTP verification",
                                        AssignedStaffId = entity.StaffId.Value,
                                        IsActive = true
                                    });
                                }

                                await dbContext.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating staff assignment and follow-ups on OTP verification for profile ID: {Id}", entity.Id);
                        }
                    }

                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid verification code. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifyOtp for ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred during verification." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ResendOtp(long id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Registration not found." });
                }

                var entity = await _registrationRepo.Get(id);
                if (entity == null)
                {
                    return Json(new { success = false, message = "Registration session not found. Please try again." });
                }

                if (entity.OtpResendCount >= 3)
                {
                    return Json(new { success = false, message = "Maximum resend attempts (3) exceeded. Please try again later or contact support." });
                }

                Random rdm = new Random();
                string pin = rdm.Next(111111, 999999).ToString();
                
                entity.VerificationCode = pin;
                entity.OtpGeneratedAt = DateTime.Now;
                entity.OtpResendCount++;

                await _registrationRepo.Update(entity);
                await _registrationRepo.SaveChanges();

                // Send OTP via SMS
                try
                {
                    await _emailService.SendSmsAsync(entity.VerificationCode, entity.Phone);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SMS to {Phone}", entity.Phone);
                }

                // Send OTP via Email
                try
                {
                    var htmlContent = $"Dear Customer, <br/><br/>{pin} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
                    var htmlContentView = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                    _emailNotificationHelper.SendEmail(entity.Email, htmlContentView, "Your Resent OTP for M4nikah");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send Email to {Email}", entity.Email);
                }

                return Json(new { success = true, message = "OTP has been resent successfully.", resendCount = entity.OtpResendCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResendOtp for ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while resending OTP." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdatePhoneAndResendOtp(long id, string phone, string? countryCode = null)
        {
            try
            {
                if (id <= 0 || string.IsNullOrWhiteSpace(phone))
                {
                    return Json(new { success = false, message = "Invalid parameters." });
                }

                if (phone.Length < 8 || phone.Length > 10 || !phone.All(char.IsDigit))
                {
                    return Json(new { success = false, message = "Please provide a valid Phone Number" });
                }

                var entity = await _registrationRepo.Get(id);
                if (entity == null)
                {
                    return Json(new { success = false, message = "Registration session not found. Please try again." });
                }

                var phoneExists = await _registrationRepo.WhereActive(x => x.Phone == phone && x.Id != id && x.IsVerified);
                if (phoneExists.Any())
                {
                    return Json(new { success = false, message = "Phone number already exists!" });
                }

                if (entity.OtpResendCount >= 3)
                {
                    return Json(new { success = false, message = "Maximum resend attempts (3) exceeded. Please try again later or contact support." });
                }

                entity.Phone = phone;
                if (!string.IsNullOrEmpty(countryCode))
                {
                    entity.CountryCode = countryCode;
                }

                Random rdm = new Random();
                string pin = rdm.Next(111111, 999999).ToString();
                
                entity.VerificationCode = pin;
                entity.OtpGeneratedAt = DateTime.Now;
                entity.OtpResendCount++;

                await _registrationRepo.Update(entity);
                await _registrationRepo.SaveChanges();

                // Send OTP via SMS
                try
                {
                    await _emailService.SendSmsAsync(entity.VerificationCode, entity.Phone);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SMS to {Phone}", entity.Phone);
                }

                // Send OTP via Email
                try
                {
                    var htmlContent = $"Dear Customer, <br/><br/>{pin} is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
                    var htmlContentView = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                    _emailNotificationHelper.SendEmail(entity.Email, htmlContentView, "Your Resent OTP for M4nikah");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send Email to {Email}", entity.Email);
                }

                return Json(new { success = true, message = "Phone number updated and OTP resent successfully.", phone = entity.Phone, resendCount = entity.OtpResendCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdatePhoneAndResendOtp for ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while updating phone number and resending OTP." });
            }
        }

    }
}
