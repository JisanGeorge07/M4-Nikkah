using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Helpers;
using Application.Interfaces.Infrastructure;
using Application.Interfaces.Persistence;
using Application.Models;
using Application.Models.Transactions;
using Application.ViewModels;
using AutoMapper;
using Domain;
using Domain.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using URMARRY.Hubs;
using URMARRY.Models;
using URMARRY.Services;

namespace URMARRY.Controllers
{
public class UserController : Controller
{
	public class MobileProfileDto
	{
		public long Id { get; set; }

		public long? Profile_For_Id { get; set; }

		public long? ProfileForId { get; set; }

		public long Nationality_Id { get; set; }

		public long NationalityId { get; set; }

		public string? Country_Code { get; set; }

		public string? CountryCode { get; set; }

		public string? Secondary_Country_Code { get; set; }

		public string? SecondaryCountryCode { get; set; }

		public long Marital_Status_Id { get; set; }

		public long MaritalStatusId { get; set; }

		public long Height_Id { get; set; }

		public long HeightId { get; set; }

		public long Weight_Id { get; set; }

		public long WeightId { get; set; }

		public long Complexion_Id { get; set; }

		public long ComplexionId { get; set; }

		public long Body_Type_Id { get; set; }

		public long BodyTypeId { get; set; }

		public long? Is_Physically_Challenged { get; set; }

		public long? IsPhysicallyChallenged { get; set; }

		public string? Physically_Challenged_Detail { get; set; }

		public string? PhysicallyChallengedDetail { get; set; }

		public string? Name { get; set; }

		public string? Gender { get; set; }

		public string? DOB { get; set; }

		public string? Phone { get; set; }

		public string? Landline_Number { get; set; }

		public string? LandlineNumber { get; set; }

		public string? Highest_Education { get; set; }

		public string? HighestEducation { get; set; }

		public long Profession_Id { get; set; }

		public long ProfessionId { get; set; }

		public long Mother_Tongue_Id { get; set; }

		public long MotherTongueId { get; set; }

		public long Religion_Id { get; set; }

		public long ReligionId { get; set; }

		public long Caste_Id { get; set; }

		public long CasteId { get; set; }

		public long Community_Id { get; set; }

		public long CommunityId { get; set; }

		public long Religiousness_Id { get; set; }

		public long ReligiousnessId { get; set; }

		public string? Profession_Type { get; set; }

		public string? ProfessionType { get; set; }

		public long financial_status_id { get; set; }

		public long FinancialStatusId { get; set; }

		public string? Family_Name { get; set; }

		public string? FamilyName { get; set; }

		public string? Father_name { get; set; }

		public string? FatherName { get; set; }

		public string? Post { get; set; }

		public string? Village { get; set; }

		public string? Pin_Code { get; set; }

		public string? PinCode { get; set; }

		public string? Country { get; set; }

		public string? State { get; set; }

		public string? District { get; set; }

		public string? Present_Country { get; set; }

		public string? PresentCountry { get; set; }

		public string? Present_State { get; set; }

		public string? PresentState { get; set; }

		public string? Present_District { get; set; }

		public string? PresentDistrict { get; set; }

		public string? Present_City { get; set; }

		public string? PresentCity { get; set; }

		public string? Image_Path { get; set; }

		public string? ImagePath { get; set; }

		public IFormFile? Image { get; set; }

		public string? About { get; set; }

		public string? Education_Type { get; set; }

		public string? EducationType { get; set; }

		public string? Email { get; set; }

		public string? Password { get; set; }

		public string? Register_Number { get; set; }

		public string? RegisterNumber { get; set; }

		public long? NumberOfChildrens { get; set; }

		public bool? PhotoVisibleToAll { get; set; }

		public bool? Photo_Visible_To_All { get; set; }

		public bool? PhotoVisibleToPremium { get; set; }

		public bool? Photo_Visible_To_Premium { get; set; }

		public bool? PhotoVisibleToAccepted { get; set; }

		public bool? Photo_Visible_To_Accepted { get; set; }

		public string? CompletedStep { get; set; }

		public string? Completed_Step { get; set; }

		public string? Source { get; set; }

		public bool DocumentVerificationEnabled { get; set; }

		public bool? Document_Verification_Enabled { get; set; }

		public string? VerificationDocumentUrl { get; set; }

		public string? Verification_Document_Url { get; set; }

		public bool DocumentVerificationComplete { get; set; }

		public bool? Document_Verification_Complete { get; set; }

		public bool DocumentVerificationRejected { get; set; }

		public bool? Document_Verification_Rejected { get; set; }

		public bool DocumentVerificationFollowupApproved { get; set; }

		public bool? Document_Verification_Followup_Approved { get; set; }

		public List<VerificationDocumentDto>? VerificationDocuments { get; set; }
	}

	public class DeleteDocRequest
	{
		public long DocumentId { get; set; }
	}

	private readonly ILogger<UserController> _logger;

	private readonly IRepository<Registration> _registrationRepo;

	private readonly IRepository<Email> _emailRepo;

	private readonly IEmailService _emailService;

	private readonly IMapper _mapper;

	private readonly EmailNotificationHelper _emailNotificationHelper;

	private readonly IRepository<ProfileFor> _profileForRepo;

	private readonly IRepository<UserStarProfile> _userStarProfileRepo;

	private readonly IRepository<State> _stateRepo;

	private readonly IRepository<UserFavouriteProfile> _userFavouriteProfileRepo;

	private readonly IRepository<Notification> _notificationRepo;

	private readonly INotifcationRepository _notificationPageRepo;

	private readonly IRepository<Nationality> _nationalityRepo;

	private readonly IRepository<MaritalStatus> _maritalStatusRepo;

	private readonly IRepository<BodyFeatures> _bodyFeaturesRepo;

	private readonly IRepository<Profession> _professionRepo;

	private readonly ITransactionRepository _transactionRepository;

	private readonly IRepository<MotherTongue> _motherTongueRepo;

	private readonly IRepository<ReligionCaste> _religionCasteRepo;

	private readonly IRepository<Religiousness> _religiousnessRepo;

	private readonly IRepository<Community> _communityRepo;

	private readonly IRepository<FinancialStatus> _financialStatus;

	private readonly IRepository<MatchingProfiles> _matchingProfilesRepo;

	private readonly IMatchingProfileRepo _matchingProfilesResponseDtoRepo;

	private readonly IRepository<Images> _imagesRepo;

	private readonly IFileService _fileService;

	private readonly IHttpContextAccessor _httpContextAccessor;

	private readonly IRepository<State> _stateRepository;

	private readonly IRepository<District> _districtRepository;

	private readonly IRepository<City> _cityRepository;

	private readonly IRepository<UserReport> _userReportRepository;

	private readonly IUserService _userService;

	private readonly IRepository<UserContactView> _userContactViewRepository;

	private readonly IRepository<UserReportReason> _userReportReasonRepository;

	private readonly IProfileService _profileService;

	private readonly IRepository<PhotoUnlockRequest> _photoUnlockRequestRepo;

	private readonly IRepository<MatchStatusUpdate> _matchStatusUpdateRepo;

	private readonly IRepository<SuccessStory> _successStoryRepo;

	private readonly CookieHelper _cookieHelper;

	private readonly IRepository<VerificationDocument> _verificationDocRepo;

	private readonly IRepository<FollowUp> _followUpRepo;

	private readonly IRepository<FollowUpTimeline> _followUpTimelineRepo;

	private readonly PresenceTracker _presenceTracker;

	private readonly IHubContext<PresenceHub> _hubContext;

	internal Guid key = new Guid("F7AD797A-416C-4C56-8749-7017FBC90427");

	public IHttpContextAccessor ContextAccessor { get; }

	public UserController(ILogger<UserController> logger, IRepository<Registration> registrationRepo, IMapper mapper, IRepository<Email> emailRepo, IEmailService emailService, IRepository<ProfileFor> profileForRepo, IRepository<State> stateRepo, ITransactionRepository transactionRepository, IRepository<Nationality> nationalityRepo, INotifcationRepository notificationPageRepo, EmailNotificationHelper emailNotificationHelper, IRepository<MaritalStatus> maritalStatusRepo, IRepository<BodyFeatures> bodyFeaturesRepo, IRepository<Profession> professionRepo, IMatchingProfileRepo matchingProfileRepo, IRepository<MotherTongue> motherTongueRepo, IRepository<ReligionCaste> religionCasteRepo, IRepository<UserStarProfile> userStarProfileRepo, IRepository<UserFavouriteProfile> userFavouriteProfileRepo, IRepository<Notification> notificationRepo, IRepository<Religiousness> religiousnessRepo, IRepository<Community> communityRepo, IRepository<FinancialStatus> financialStatus, IHttpContextAccessor contextAccessor, IRepository<MatchingProfiles> matchingProfilesRepo, IRepository<Images> imagesRepo, IFileService fileService, IHttpContextAccessor httpContextAccessor, IRepository<State> stateRepository, IRepository<District> districtRepository, IRepository<City> cityRepository, IRepository<UserReport> userReportRepository, IUserService userService, IRepository<UserContactView> userContactViewRepository, IProfileService profileService, IRepository<UserReportReason> userReportReasonRepository, IRepository<PhotoUnlockRequest> photoUnlockRequestRepo, IRepository<MatchStatusUpdate> matchStatusUpdateRepo, IRepository<SuccessStory> successStoryRepo, CookieHelper cookieHelper, IRepository<VerificationDocument> verificationDocRepo, IRepository<FollowUp> followUpRepo, IRepository<FollowUpTimeline> followUpTimelineRepo, PresenceTracker presenceTracker, IHubContext<PresenceHub> hubContext)
	{
		_logger = logger;
		_registrationRepo = registrationRepo;
		_emailRepo = emailRepo;
		_mapper = mapper;
		_emailService = emailService;
		_transactionRepository = transactionRepository;
		_stateRepo = stateRepo;
		_userFavouriteProfileRepo = userFavouriteProfileRepo;
		_profileForRepo = profileForRepo;
		_nationalityRepo = nationalityRepo;
		_userStarProfileRepo = userStarProfileRepo;
		_notificationRepo = notificationRepo;
		_maritalStatusRepo = maritalStatusRepo;
		_bodyFeaturesRepo = bodyFeaturesRepo;
		_professionRepo = professionRepo;
		_motherTongueRepo = motherTongueRepo;
		_religionCasteRepo = religionCasteRepo;
		_religiousnessRepo = religiousnessRepo;
		_communityRepo = communityRepo;
		_emailNotificationHelper = emailNotificationHelper;
		_financialStatus = financialStatus;
		_matchingProfilesResponseDtoRepo = matchingProfileRepo;
		ContextAccessor = contextAccessor;
		_matchingProfilesRepo = matchingProfilesRepo;
		_imagesRepo = imagesRepo;
		_fileService = fileService;
		_httpContextAccessor = httpContextAccessor;
		_stateRepository = stateRepository;
		_districtRepository = districtRepository;
		_cityRepository = cityRepository;
		_userReportRepository = userReportRepository;
		_userService = userService;
		_userContactViewRepository = userContactViewRepository;
		_profileService = profileService;
		_userReportReasonRepository = userReportReasonRepository;
		_notificationPageRepo = notificationPageRepo;
		_photoUnlockRequestRepo = photoUnlockRequestRepo;
		_matchStatusUpdateRepo = matchStatusUpdateRepo;
		_successStoryRepo = successStoryRepo;
		_cookieHelper = cookieHelper;
		_verificationDocRepo = verificationDocRepo;
		_followUpRepo = followUpRepo;
		_followUpTimelineRepo = followUpTimelineRepo;
		_presenceTracker = presenceTracker;
		_hubContext = hubContext;
	}

	private long? GetCurrentUserId()
	{
		return _cookieHelper.GetUserIdFromCookie(HttpContext);
	}

	private IActionResult? CheckRegistrationComplete(RegistrationDto? profile)
	{
		if (profile != null && !profile.IsComplete)
		{
			return RedirectToAction("Index", "Home");
		}
		return null;
	}

	[AllowAnonymous]
	[HttpGet("/user/login")]
	public async Task<IActionResult> Login()
	{
		if (GetCurrentUserId().HasValue)
		{
			return RedirectToAction("Dashboard", "User");
		}
		return View();
	}

	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	[HttpPost("/user/login")]
	public async Task<IActionResult> Login(CustomerLoginViewModel loginViewModel, [FromQuery] string? returnUrl = null)
	{
		if (!ModelState.IsValid)
		{
			return View();
		}
		Registration user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Email == loginViewModel.LoginEmail && x.IsVerified);
		if (user != null)
		{
			RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
			if (loginViewModel.LoginPassword == encryption.DecryptRijndael(user.Password, user.PasswordHash))
			{
				_cookieHelper.SetSecureJwtCookie(HttpContext, user.Id, user.Email);
				_logger.LogInformation("User logged in");
				if (!user.IsComplete)
				{
					return RedirectToAction("Index", "Home");
				}
				if (returnUrl != null)
				{
					return LocalRedirect(returnUrl);
				}
				return RedirectToAction("Dashboard");
			}
			ModelState.AddModelError("error", "Invalid password");
		}
		else
		{
			ModelState.AddModelError("error", "Account not found.");
		}
		return View();
	}

	[AllowAnonymous]
	[HttpPost("/user/autologin")]
	public async Task<IActionResult> AutoLogin([FromForm] CustomerLoginViewModel loginViewModel, [FromQuery] string? returnUrl = null)
	{
		if (Request.ContentType != null && Request.ContentType.Contains("application/json"))
		{
			using StreamReader reader = new StreamReader(Request.Body);
			CustomerLoginViewModel jsonModel = JsonSerializer.Deserialize<CustomerLoginViewModel>(await reader.ReadToEndAsync(), new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});
			if (jsonModel != null)
			{
				loginViewModel = jsonModel;
				ModelState.Clear();
				TryValidateModel(loginViewModel);
			}
		}
		if (!ModelState.IsValid)
		{
			return StatusCode(400, new
			{
				success = false,
				errors = ModelState.Values.SelectMany((ModelStateEntry v) => v.Errors.Select((ModelError e) => e.ErrorMessage))
			});
		}
		Registration user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Email == loginViewModel.LoginEmail && x.IsVerified);
		if (user != null)
		{
			RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
			if (loginViewModel.LoginPassword == encryption.DecryptRijndael(user.Password, user.PasswordHash))
			{
				_cookieHelper.SetSecureJwtCookie(HttpContext, user.Id, user.Email);
				string token = _cookieHelper.GenerateJwtToken(user.Id, user.Email);
				_logger.LogInformation("User logged in");
				RegistrationDto userDto = _mapper.Map<RegistrationDto>(user);
				userDto.Password = null;
				userDto.PasswordHash = null;
				userDto.VerificationCode = null;
				_ = returnUrl;
				string returnUrlTarget = (user.IsComplete ? (returnUrl ?? Url.Action("Dashboard")) : Url.Action("Index", "Home"));
				return Json(new
				{
					success = true,
					token = token,
					userId = user.Id,
					isIncomplete = !user.IsComplete,
					data = new
					{
						token = token,
						id = user.Id,
						email = user.Email,
						name = user.Name,
						profile = userDto
					},
					returnUrl = returnUrlTarget
				});
			}
			return StatusCode(401, new
			{
				success = false,
				error = "Invalid password"
			});
		}
		return StatusCode(404, new
		{
			success = false,
			error = "Account not found"
		});
	}

	[AllowAnonymous]
	[HttpGet("/user/login-email-otp")]
	public async Task<IActionResult> LoginWithEmailOtp()
	{
		if (GetCurrentUserId().HasValue)
		{
			return RedirectToAction("Dashboard", "User");
		}
		return View();
	}

	[AllowAnonymous]
	[HttpGet("/user/login-mobile-otp")]
	public async Task<IActionResult> LoginWithMobileOtp()
	{
		if (GetCurrentUserId().HasValue)
		{
			return RedirectToAction("Dashboard", "User");
		}
		return View();
	}

	[AllowAnonymous]
	[HttpPost("/user/otp/send")]
	public async Task<IActionResult> SendOtp([FromBody] OtpSendRequest request)
	{
		if (Request.ContentType != null && !Request.ContentType.Contains("application/json"))
		{
			try
			{
				string type = Request.Form["Type"].ToString();
				string identifier = Request.Form["Identifier"].ToString();
				request = new OtpSendRequest
				{
					Type = type,
					Identifier = identifier
				};
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Failed to read form data in SendOtp");
			}
		}
		else if (Request.ContentType != null && Request.ContentType.Contains("application/json") && request == null)
		{
			try
			{
				using StreamReader reader = new StreamReader(Request.Body);
				request = JsonSerializer.Deserialize<OtpSendRequest>(await reader.ReadToEndAsync(), new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to deserialize JSON in SendOtp");
			}
		}
		if (request == null || string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Type))
		{
			return BadRequest(new
			{
				success = false,
				error = "Please provide valid request details."
			});
		}
		string typeLower = request.Type.ToLower();
		Registration user = null;
		switch (typeLower)
		{
		case "email":
			user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Email == request.Identifier && x.IsVerified);
			break;
		case "mobile":
		case "phone":
			user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Phone == request.Identifier && x.IsVerified);
			break;
		}
		if (user == null)
		{
			return NotFound(new
			{
				success = false,
				error = "Account not found or not fully registered/verified."
			});
		}
		Random random = new Random();
		string otp = (user.VerificationCode = random.Next(100000, 999999).ToString());
		user.OtpGeneratedAt = DateTime.Now;
		user.OtpResendCount++;
		await _registrationRepo.Update(user);
		await _registrationRepo.SaveChanges();
		if (typeLower == "email")
		{
			try
			{
				string htmlContent = "Dear Customer, <br/><br/>" + otp + " is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
				AlternateView htmlContentView = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
				_emailNotificationHelper.SendEmail(user.Email, htmlContentView, "Your OTP for M4nikah");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send email verification OTP to {Email}", user.Email);
				return StatusCode(500, new
				{
					success = false,
					error = "Failed to dispatch email verification OTP."
				});
			}
		}
		else
		{
			try
			{
				if (!(await _emailService.SendSmsAsync(otp, user.Phone)))
				{
					_logger.LogWarning("SMS dispatch returned false for {Phone}", user.Phone);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send SMS verification OTP to {Phone}", user.Phone);
				return StatusCode(500, new
				{
					success = false,
					error = "Failed to dispatch SMS verification OTP."
				});
			}
		}
		return Ok(new
		{
			success = true,
			message = "OTP code has been sent successfully to your registered " + typeLower + "."
		});
	}

	[AllowAnonymous]
	[HttpPost("/user/otp/login")]
	public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyRequest request, [FromQuery] string? returnUrl = null)
	{
		if (Request.ContentType != null && !Request.ContentType.Contains("application/json"))
		{
			try
			{
				string type = Request.Form["Type"].ToString();
				string identifier = Request.Form["Identifier"].ToString();
				string otp = Request.Form["Otp"].ToString();
				request = new OtpVerifyRequest
				{
					Type = type,
					Identifier = identifier,
					Otp = otp
				};
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Failed to read form data in VerifyOtp");
			}
		}
		else if (Request.ContentType != null && Request.ContentType.Contains("application/json") && request == null)
		{
			try
			{
				using StreamReader reader = new StreamReader(Request.Body);
				request = JsonSerializer.Deserialize<OtpVerifyRequest>(await reader.ReadToEndAsync(), new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to deserialize JSON in VerifyOtp");
			}
		}
		if (request == null || string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Otp))
		{
			return BadRequest(new
			{
				success = false,
				error = "Please provide valid request details."
			});
		}
		string typeLower = request.Type.ToLower();
		Registration user = null;
		switch (typeLower)
		{
		case "email":
			user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Email == request.Identifier && x.IsVerified);
			break;
		case "mobile":
		case "phone":
			user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Phone == request.Identifier && x.IsVerified);
			break;
		}
		if (user == null)
		{
			return NotFound(new
			{
				success = false,
				error = "Account not found or not fully registered/verified."
			});
		}
		if (string.IsNullOrEmpty(user.VerificationCode) || user.VerificationCode != request.Otp)
		{
			return BadRequest(new
			{
				success = false,
				error = "Invalid verification OTP code."
			});
		}
		if (!user.OtpGeneratedAt.HasValue || DateTime.Now.Subtract(user.OtpGeneratedAt.Value).TotalMinutes > 10.0)
		{
			return BadRequest(new
			{
				success = false,
				error = "The OTP code has expired. Please request a new one."
			});
		}
		_cookieHelper.SetSecureJwtCookie(HttpContext, user.Id, user.Email);
		string token = _cookieHelper.GenerateJwtToken(user.Id, user.Email);
		user.VerificationCode = null;
		await _registrationRepo.Update(user);
		await _registrationRepo.SaveChanges();
		_logger.LogInformation("User logged in via OTP");
		RegistrationDto userDto = _mapper.Map<RegistrationDto>(user);
		userDto.Password = null;
		userDto.PasswordHash = null;
		userDto.VerificationCode = null;
		_ = returnUrl;
		string returnUrlTarget = (user.IsComplete ? (returnUrl ?? Url.Action("Dashboard")) : Url.Action("Index", "Home"));
		return Json(new
		{
			success = true,
			token = token,
			userId = user.Id,
			isIncomplete = !user.IsComplete,
			data = new
			{
				token = token,
				id = user.Id,
				email = user.Email,
				name = user.Name,
				profile = userDto
			},
			returnUrl = returnUrlTarget
		});
	}

	[AllowAnonymous]
	[HttpPost("/user/api-register")]
	public async Task<IActionResult> ApiRegister(MobileProfileDto mobileModel)
	{
		_logger.LogWarning("ApiRegister called. Content-Type: {ContentType}", Request.ContentType);
		if (Request.ContentType != null && Request.ContentType.Contains("application/json"))
		{
			try
			{
				using StreamReader reader = new StreamReader(Request.Body);
				string body = await reader.ReadToEndAsync();
				_logger.LogWarning("ApiRegister JSON Body: {Body}", body);
				MobileProfileDto jsonModel = JsonSerializer.Deserialize<MobileProfileDto>(body, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});
				if (jsonModel != null)
				{
					mobileModel = jsonModel;
				}
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Failed to deserialize JSON in ApiRegister");
			}
		}
		else
		{
			try
			{
				string formKeys = string.Join(", ", Request.Form.Keys.Select((string k) => $"{k}={Request.Form[k]}"));
				_logger.LogWarning("ApiRegister Form Data: {FormKeys}", formKeys);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to read form data in ApiRegister");
			}
		}
		if (mobileModel != null)
		{
			_logger.LogWarning("Parsed mobileModel: Id={Id}, Name={Name}, Email={Email}, PresentCountry={PresentCountry}, Present_Country={Present_Country}, LandlineNumber={LandlineNumber}, Landline_Number={Landline_Number}, photoVisibleToAll={PhotoVisibleToAll}, photoVisibleToPremium={PhotoVisibleToPremium}, photoVisibleToAccepted={PhotoVisibleToAccepted}", mobileModel.Id, mobileModel.Name, mobileModel.Email, mobileModel.PresentCountry, mobileModel.Present_Country, mobileModel.LandlineNumber, mobileModel.Landline_Number, mobileModel.PhotoVisibleToAll, mobileModel.PhotoVisibleToPremium, mobileModel.PhotoVisibleToAccepted);
		}
		if (mobileModel == null)
		{
			return Json(new
			{
				success = false,
				error = "Please provide valid registration details."
			});
		}
		if (mobileModel.Id <= 0)
		{
			if (string.IsNullOrEmpty(mobileModel.Email))
			{
				return Json(new
				{
					success = false,
					error = "Please provide valid registration details."
				});
			}
			try
			{
				if ((await _registrationRepo.WhereActive((Registration x) => x.Email == mobileModel.Email && x.Email != null && x.IsVerified)).Count() > 0)
				{
					return Json(new
					{
						success = false,
						error = "Email address already exists"
					});
				}
				if ((await _registrationRepo.WhereActive((Registration x) => x.Phone == mobileModel.Phone && x.Phone != null && x.IsVerified)).Count() > 0)
				{
					return Json(new
					{
						success = false,
						error = "Phone number already exists"
					});
				}
				RegistrationDto model = MapMobileProfileToRegistrationDto(mobileModel);
				model.Source = "Mobile";
				List<ProfileForDto> ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive());
				List<NationalityDto> Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive());
				List<MaritalStatusDto> MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive());
				List<BodyFeaturesDto> BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive());
				_mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive());
				List<MotherTongueDto> MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive());
				List<ReligionCasteDto> Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive((ReligionCaste x) => x.ParentId == 0));
				List<ReligionCasteDto> Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive((ReligionCaste x) => x.ParentId != 0));
				List<CommunityDto> Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive());
				List<ReligiousnessDto> Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive());
				List<FinancialStatusDto> FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive());
				model.ProfileFor = model.ProfileForId <= 0 ? "" : (ProfileFor.FirstOrDefault(x => x.Id == model.ProfileForId)?.Title ?? "");
				model.Nationality = model.NationalityId <= 0 ? "" : (Nationalities.FirstOrDefault(x => x.Id == model.NationalityId)?.Title ?? "");
				model.MaritalStatus = model.MaritalStatusId <= 0 ? "" : (MaritalStatuses.FirstOrDefault(x => x.Id == model.MaritalStatusId)?.Title ?? "");
				model.Height = model.HeightId <= 0 ? "" : (BodyFeatures.FirstOrDefault(x => x.Id == model.HeightId)?.Title ?? "");
				model.Weight = model.WeightId <= 0 ? "" : (BodyFeatures.FirstOrDefault(x => x.Id == model.WeightId)?.Title ?? "");
				model.Complexion = model.ComplexionId <= 0 ? "" : (BodyFeatures.FirstOrDefault(x => x.Id == model.ComplexionId)?.Title ?? "");
				model.BodyType = model.BodyTypeId <= 0 ? "" : (BodyFeatures.FirstOrDefault(x => x.Id == model.BodyTypeId)?.Title ?? "");
				model.MotherTongue = model.MotherTongueId <= 0 ? "" : (MotherTongues.FirstOrDefault(x => x.Id == model.MotherTongueId)?.Title ?? "");
				model.Religion = model.ReligionId <= 0 ? "" : (Religions.FirstOrDefault(x => x.Id == model.ReligionId)?.Title ?? "");
				model.Caste = model.CasteId <= 0 ? "" : (Castes.FirstOrDefault(x => x.Id == model.CasteId)?.Title ?? "");
				model.Community = model.CommunityId <= 0 ? "" : (Communities.FirstOrDefault(x => x.Id == model.CommunityId)?.Title ?? "");
				model.Religiousness = model.ReligiousnessId <= 0 ? "" : (Religiousnesses.FirstOrDefault(x => x.Id == model.ReligiousnessId)?.Title ?? "");
				model.FinancialStatus = model.FinancialStatusId <= 0 ? "" : (FinancialStatuses.FirstOrDefault(x => x.Id == model.FinancialStatusId)?.Title ?? "");
				RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
				model.DOB = ((!string.IsNullOrEmpty(model.DOB)) ? model.DOB : "01/01/1990");
				model.IsPhysicallyChallenged = mobileModel.Is_Physically_Challenged == 1;
				model.IsComplete = false;
				model.IsVerified = false;
				model.IsActive = true;
				model.IsPremiumMember = false;
				model.ShowOnHomePage = false;
				Random rdm = new Random();
				string pin = rdm.Next(111111, 999999).ToString();
				model.VerificationCode = pin;
				model.OtpGeneratedAt = DateTime.Now;
				model.OtpResendCount = 0;
				if (!string.IsNullOrEmpty(model.Password))
				{
					model.PasswordHash = encryption.CreateSalt();
					model.Password = encryption.EncryptRijndael(model.Password, model.PasswordHash);
				}
				Registration entity = _mapper.Map<Registration>(model);
				await _fileService.SaveAllFiles(entity, model, "Uploads/Registration");
				await _registrationRepo.Add(entity);
				await _registrationRepo.SaveChanges();
				if (entity.Id.ToString().Length > 0)
				{
					if (6 - entity.Id.ToString().Length == 5)
					{
						entity.RegisterNumber = "M400000" + entity.Id;
					}
					else if (6 - entity.Id.ToString().Length == 4)
					{
						entity.RegisterNumber = "M40000" + entity.Id;
					}
					else if (6 - entity.Id.ToString().Length == 3)
					{
						entity.RegisterNumber = "M4000" + entity.Id;
					}
					else if (6 - entity.Id.ToString().Length == 2)
					{
						entity.RegisterNumber = "M400" + entity.Id;
					}
					else if (6 - entity.Id.ToString().Length == 1)
					{
						entity.RegisterNumber = "M40" + entity.Id;
					}
					await _registrationRepo.Update(entity);
					await _registrationRepo.SaveChanges();
				}
				await _emailService.SendSmsAsync(entity.VerificationCode, entity.Phone);
				try
				{
					string htmlContent = "Dear Customer, <br/><br/>" + pin + " is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
					AlternateView htmlContentView = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
					_logger.LogWarning("API sent OTP Via Email Starts for ID: {Id}", entity.Id);
					_emailNotificationHelper.SendEmail(entity.Email, htmlContentView, "Your OTP for M4nikah");
					_logger.LogWarning("API sent OTP Via Email Ends for ID: {Id}", entity.Id);
				}
				catch (Exception ex)
				{
					_logger.LogError("API Email OTP send failed: " + ex.Message);
				}
				string token = _cookieHelper.GenerateJwtToken(entity.Id, entity.Email);
				model.Id = entity.Id;
				model.RegisterNumber = entity.RegisterNumber;
				MobileProfileDto savedMobileProfile = MapRegistrationDtoToMobileProfile(model);
				var dataObj = new
				{
					token = token,
					id = entity.Id,
					email = entity.Email,
					name = entity.Name,
					register_Number = entity.RegisterNumber,
					profile = savedMobileProfile
				};
				return Json(new
				{
					success = true,
					status = "Success",
					token = token,
					userId = entity.Id,
					registrationNo = entity.RegisterNumber,
					verificationCode = entity.VerificationCode,
					data = dataObj
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "API registration save failed");
				return Json(new
				{
					success = false,
					error = "An error occurred during registration: " + ex.Message
				});
			}
		}
		try
		{
			Registration entity = await _registrationRepo.Get(mobileModel.Id);
			if (entity == null)
			{
				return Json(new
				{
					success = false,
					error = "Registration session not found."
				});
			}
			if (mobileModel.CompletedStep == "Step-1" || mobileModel.Completed_Step == "Step-1" || string.IsNullOrEmpty(mobileModel.CompletedStep))
			{
				if ((await _registrationRepo.WhereActive((Registration x) => x.Email == mobileModel.Email && x.Email != null && x.Id != entity.Id && x.IsVerified)).Count() > 0)
				{
					return Json(new
					{
						success = false,
						error = "Email address already exists"
					});
				}
				if ((await _registrationRepo.WhereActive((Registration x) => x.Phone == mobileModel.Phone && x.Phone != null && x.Id != entity.Id && x.IsVerified)).Count() > 0)
				{
					return Json(new
					{
						success = false,
						error = "Phone number already exists"
					});
				}
			}
			RegistrationDto model = MapMobileProfileToRegistrationDto(mobileModel);
			model.Source = (string.IsNullOrEmpty(entity.Source) ? "Mobile" : entity.Source);
			RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
			if (model.CompletedStep == "Step-1")
			{
				_logger.LogWarning("API re-entering Step-1 for user with ID: {Id}", model.Id);
				Random rdm = new Random();
				string pin = rdm.Next(111111, 999999).ToString();
				model.VerificationCode = pin;
				model.OtpGeneratedAt = DateTime.Now;
				model.OtpResendCount = 0;
				await _emailService.SendSmsAsync(model.VerificationCode, model.Phone);
				try
				{
					string htmlContent = "Dear Customer, <br/><br/>" + pin + " is your SECRET One Time Password (OTP) to log in to your M4nikah Muslim Matrimony account. Please Do not share it with anyone.";
					AlternateView htmlContentView = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
					_emailNotificationHelper.SendEmail(model.Email, htmlContentView, "Your OTP for M4nikah");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "API OTP send via Email failed");
				}
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
			List<ProfileForDto> ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive());
			List<NationalityDto> Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive());
			List<MaritalStatusDto> MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive());
			List<BodyFeaturesDto> BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive());
			_mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive());
			List<MotherTongueDto> MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive());
			List<ReligionCasteDto> Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive((ReligionCaste x) => x.ParentId == 0));
			List<ReligionCasteDto> Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive((ReligionCaste x) => x.ParentId != 0));
			List<CommunityDto> Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive());
			List<ReligiousnessDto> Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive());
			List<FinancialStatusDto> FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive());
			model.Phone = (string.IsNullOrEmpty(model.Phone) ? entity.Phone : model.Phone);
			model.Email = (string.IsNullOrEmpty(model.Email) ? entity.Email : model.Email);
			model.Name = (string.IsNullOrEmpty(model.Name) ? entity.Name : model.Name);
			model.Gender = (string.IsNullOrEmpty(model.Gender) ? entity.Gender : model.Gender);
			model.ProfileForId = ((model.ProfileForId <= 0) ? entity.ProfileForId : model.ProfileForId);
			model.NationalityId = ((model.NationalityId <= 0) ? entity.NationalityId : model.NationalityId);
			model.MaritalStatusId = ((model.MaritalStatusId <= 0) ? entity.MaritalStatusId : model.MaritalStatusId);
			model.HeightId = ((model.HeightId <= 0) ? entity.HeightId : model.HeightId);
			model.WeightId = ((model.WeightId <= 0) ? entity.WeightId : model.WeightId);
			model.ComplexionId = ((model.ComplexionId <= 0) ? entity.ComplexionId : model.ComplexionId);
			model.BodyTypeId = ((model.BodyTypeId <= 0) ? entity.BodyTypeId : model.BodyTypeId);
			model.ProfessionId = ((model.ProfessionId <= 0) ? entity.ProfessionId : model.ProfessionId);
			model.MotherTongueId = ((model.MotherTongueId <= 0) ? entity.MotherTongueId : model.MotherTongueId);
			model.ReligionId = ((model.ReligionId <= 0) ? entity.ReligionId : model.ReligionId);
			model.CasteId = ((model.CasteId <= 0) ? entity.CasteId : model.CasteId);
			model.CommunityId = ((model.CommunityId <= 0) ? entity.CommunityId : model.CommunityId);
			model.ReligiousnessId = ((model.ReligiousnessId <= 0) ? entity.ReligiousnessId : model.ReligiousnessId);
			model.FinancialStatusId = ((model.FinancialStatusId <= 0) ? entity.FinancialStatusId : model.FinancialStatusId);
			model.Name = ((!string.IsNullOrEmpty(model.Name)) ? model.Name : entity.Name);
			model.Gender = ((!string.IsNullOrEmpty(model.Gender)) ? model.Gender : entity.Gender);
			model.DOB = ((!string.IsNullOrEmpty(mobileModel.DOB)) ? mobileModel.DOB : entity.DOB);
			model.Phone = ((!string.IsNullOrEmpty(model.Phone)) ? model.Phone : entity.Phone);
			model.LandlineNumber = ((!string.IsNullOrEmpty(model.LandlineNumber)) ? model.LandlineNumber : entity.LandlineNumber);
			model.HighestEducation = ((!string.IsNullOrEmpty(model.HighestEducation)) ? model.HighestEducation : entity.HighestEducation);
			model.ProfessionType = ((!string.IsNullOrEmpty(model.ProfessionType)) ? model.ProfessionType : entity.ProfessionType);
			model.FamilyName = ((!string.IsNullOrEmpty(model.FamilyName)) ? model.FamilyName : entity.FamilyName);
			model.FatherName = ((!string.IsNullOrEmpty(model.FatherName)) ? model.FatherName : entity.FatherName);
			model.Post = ((!string.IsNullOrEmpty(model.Post)) ? model.Post : entity.Post);
			model.Village = ((!string.IsNullOrEmpty(model.Village)) ? model.Village : entity.Village);
			model.PinCode = ((!string.IsNullOrEmpty(model.PinCode)) ? model.PinCode : entity.PinCode);
			model.Country = ((!string.IsNullOrEmpty(model.Country)) ? model.Country : entity.Country);
			model.State = ((!string.IsNullOrEmpty(model.State)) ? model.State : entity.State);
			model.District = ((!string.IsNullOrEmpty(model.District)) ? model.District : entity.District);
			model.PresentCountry = ((!string.IsNullOrEmpty(model.PresentCountry)) ? model.PresentCountry : entity.PresentCountry);
			model.PresentState = ((!string.IsNullOrEmpty(model.PresentState)) ? model.PresentState : entity.PresentState);
			model.PresentDistrict = ((!string.IsNullOrEmpty(model.PresentDistrict)) ? model.PresentDistrict : entity.PresentDistrict);
			model.PresentCity = ((!string.IsNullOrEmpty(model.PresentCity)) ? model.PresentCity : entity.PresentCity);
			model.ImagePath = ((!string.IsNullOrEmpty(model.ImagePath)) ? model.ImagePath : entity.ImagePath);
			model.About = ((!string.IsNullOrEmpty(model.About)) ? model.About : entity.About);
			model.EducationType = ((!string.IsNullOrEmpty(model.EducationType)) ? model.EducationType : entity.EducationType);
			model.Email = ((!string.IsNullOrEmpty(model.Email)) ? model.Email : entity.Email);
			model.CompletedStep = ((!string.IsNullOrEmpty(model.CompletedStep)) ? model.CompletedStep : entity.CompletedStep);
			model.CountryCode = ((!string.IsNullOrEmpty(model.CountryCode)) ? model.CountryCode : entity.CountryCode);
			model.SecondaryCountryCode = ((!string.IsNullOrEmpty(model.SecondaryCountryCode)) ? model.SecondaryCountryCode : entity.SecondaryCountryCode);
			model.PhysicallyChallengedDetail = ((!string.IsNullOrEmpty(model.PhysicallyChallengedDetail)) ? model.PhysicallyChallengedDetail : entity.PhysicallyChallengedDetail);
			model.NumberOfChildrens = mobileModel.NumberOfChildrens ?? entity.NumberOfChildrens;
			long? physChallenged = mobileModel.Is_Physically_Challenged ?? mobileModel.IsPhysicallyChallenged;
			if (physChallenged.HasValue)
			{
				model.IsPhysicallyChallenged = physChallenged.Value == 1;
			}
			else
			{
				model.IsPhysicallyChallenged = entity.IsPhysicallyChallenged;
			}
			bool? photoAll = mobileModel.PhotoVisibleToAll ?? mobileModel.Photo_Visible_To_All;
			if (photoAll.HasValue)
			{
				model.PhotoVisibleToAll = photoAll.Value;
			}
			else
			{
				model.PhotoVisibleToAll = entity.PhotoVisibleToAll;
			}
			bool? photoPremium = mobileModel.PhotoVisibleToPremium ?? mobileModel.Photo_Visible_To_Premium;
			if (photoPremium.HasValue)
			{
				model.PhotoVisibleToPremium = photoPremium.Value;
			}
			else
			{
				model.PhotoVisibleToPremium = entity.PhotoVisibleToPremium;
			}
			bool? photoAccepted = mobileModel.PhotoVisibleToAccepted ?? mobileModel.Photo_Visible_To_Accepted;
			if (photoAccepted.HasValue)
			{
				model.PhotoVisibleToAccepted = photoAccepted.Value;
			}
			else
			{
				model.PhotoVisibleToAccepted = entity.PhotoVisibleToAccepted;
			}
			model.ProfileFor = model.ProfileForId <= 0 ? "" : (ProfileFor.FirstOrDefault(x => x.Id == model.ProfileForId)?.Title ?? "");
			model.Nationality = model.NationalityId <= 0 ? "" : (Nationalities.FirstOrDefault(x => x.Id == model.NationalityId)?.Title ?? "");
			model.MaritalStatus = model.MaritalStatusId <= 0 ? "" : (MaritalStatuses.FirstOrDefault(x => x.Id == model.MaritalStatusId)?.Title ?? "");
			model.Height = model.HeightId <= 0 ? "" : (BodyFeatures.FirstOrDefault(x => x.Id == model.HeightId)?.Title ?? "");
			model.Weight = model.WeightId <= 0 ? "" : (BodyFeatures.FirstOrDefault(x => x.Id == model.WeightId)?.Title ?? "");
			model.Complexion = model.ComplexionId <= 0 ? "" : (BodyFeatures.FirstOrDefault(x => x.Id == model.ComplexionId)?.Title ?? "");
			model.BodyType = model.BodyTypeId <= 0 ? "" : (BodyFeatures.FirstOrDefault(x => x.Id == model.BodyTypeId)?.Title ?? "");
			model.MotherTongue = model.MotherTongueId <= 0 ? "" : (MotherTongues.FirstOrDefault(x => x.Id == model.MotherTongueId)?.Title ?? "");
			model.Religion = model.ReligionId <= 0 ? "" : (Castes.FirstOrDefault(x => x.Id == model.ReligionId)?.Title ?? "");
			model.Caste = model.CasteId <= 0 ? "" : (Religions.FirstOrDefault(x => x.Id == model.CasteId)?.Title ?? "");
			model.Community = model.CommunityId <= 0 ? "" : (Communities.FirstOrDefault(x => x.Id == model.CommunityId)?.Title ?? "");
			model.Religiousness = model.ReligiousnessId <= 0 ? "" : (Religiousnesses.FirstOrDefault(x => x.Id == model.ReligiousnessId)?.Title ?? "");
			model.FinancialStatus = model.FinancialStatusId <= 0 ? "" : (FinancialStatuses.FirstOrDefault(x => x.Id == model.FinancialStatusId)?.Title ?? "");
			model.RegisterNumber = entity.RegisterNumber;
			model.IsActive = true;
			model.IsPremiumMember = false;
			model.ShowOnHomePage = false;
			model.IsVerified = model.CompletedStep == "Step-OTP-Verify" || model.CompletedStep == "Step-5" || entity.IsVerified;
			model.IsComplete = model.CompletedStep == "Step-5" || entity.IsComplete;
			_mapper.Map<RegistrationDto, Registration>(model, entity);
			await _fileService.SaveAllFiles(entity, model, "Uploads/Registration");
			await _registrationRepo.Update(entity);
			await _registrationRepo.SaveChanges();
			if (model.CompletedStep == "Step-5" && model.IsVerified)
			{
				try
				{
					await SendRegistrationSuccessEmail(model.Email, model.Name, model.RegisterNumber);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "API registration success email sending failed");
				}
			}
			if (model.CompletedStep == "Step-5" && !model.IsVerified)
			{
				try
				{
					await SendRegistrationUnsuccessfulEmail(model.Email, model.Name);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "API registration unsuccess email sending failed");
				}
			}
			string token = _cookieHelper.GenerateJwtToken(entity.Id, entity.Email);
			model.Id = entity.Id;
			model.RegisterNumber = entity.RegisterNumber;
			MobileProfileDto savedMobileProfile = MapRegistrationDtoToMobileProfile(model);
			var dataObj = new
			{
				token = token,
				id = entity.Id,
				email = entity.Email,
				name = entity.Name,
				register_Number = entity.RegisterNumber,
				profile = savedMobileProfile
			};
			return Json(new
			{
				success = true,
				status = "Success",
				token = token,
				userId = entity.Id,
				registrationNo = entity.RegisterNumber,
				verificationCode = entity.VerificationCode,
				data = dataObj
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "API registration update failed");
			return Json(new
			{
				success = false,
				error = "An error occurred during registration update: " + ex.Message
			});
		}
	}

	private RegistrationDto MapMobileProfileToRegistrationDto(MobileProfileDto mobile)
	{
		return new RegistrationDto
		{
			Id = mobile.Id,
			ProfileForId = (mobile.Profile_For_Id ?? mobile.ProfileForId.GetValueOrDefault()),
			NationalityId = ((mobile.Nationality_Id > 0) ? mobile.Nationality_Id : ((mobile.NationalityId > 0) ? mobile.NationalityId : 0)),
			CountryCode = (mobile.Country_Code ?? mobile.CountryCode),
			SecondaryCountryCode = (mobile.Secondary_Country_Code ?? mobile.SecondaryCountryCode),
			MaritalStatusId = ((mobile.Marital_Status_Id > 0) ? mobile.Marital_Status_Id : ((mobile.MaritalStatusId > 0) ? mobile.MaritalStatusId : 0)),
			HeightId = ((mobile.Height_Id > 0) ? mobile.Height_Id : ((mobile.HeightId > 0) ? mobile.HeightId : 0)),
			WeightId = ((mobile.Weight_Id > 0) ? mobile.Weight_Id : ((mobile.WeightId > 0) ? mobile.WeightId : 0)),
			ComplexionId = ((mobile.Complexion_Id > 0) ? mobile.Complexion_Id : ((mobile.ComplexionId > 0) ? mobile.ComplexionId : 0)),
			BodyTypeId = ((mobile.Body_Type_Id > 0) ? mobile.Body_Type_Id : ((mobile.BodyTypeId > 0) ? mobile.BodyTypeId : 0)),
			IsPhysicallyChallenged = ((mobile.Is_Physically_Challenged ?? mobile.IsPhysicallyChallenged) == 1),
			PhysicallyChallengedDetail = (mobile.Physically_Challenged_Detail ?? mobile.PhysicallyChallengedDetail),
			Name = mobile.Name,
			Gender = mobile.Gender,
			DOB = mobile.DOB,
			Phone = mobile.Phone,
			LandlineNumber = (mobile.Landline_Number ?? mobile.LandlineNumber),
			HighestEducation = (mobile.Highest_Education ?? mobile.HighestEducation),
			ProfessionId = ((mobile.Profession_Id > 0) ? mobile.Profession_Id : ((mobile.ProfessionId > 0) ? mobile.ProfessionId : 0)),
			MotherTongueId = ((mobile.Mother_Tongue_Id > 0) ? mobile.Mother_Tongue_Id : ((mobile.MotherTongueId > 0) ? mobile.MotherTongueId : 0)),
			ReligionId = ((mobile.Religion_Id > 0) ? mobile.Religion_Id : ((mobile.ReligionId > 0) ? mobile.ReligionId : 0)),
			CasteId = ((mobile.Caste_Id > 0) ? mobile.Caste_Id : ((mobile.CasteId > 0) ? mobile.CasteId : 0)),
			CommunityId = ((mobile.Community_Id > 0) ? mobile.Community_Id : ((mobile.CommunityId > 0) ? mobile.CommunityId : 0)),
			ReligiousnessId = ((mobile.Religiousness_Id > 0) ? mobile.Religiousness_Id : ((mobile.ReligiousnessId > 0) ? mobile.ReligiousnessId : 0)),
			ProfessionType = (mobile.Profession_Type ?? mobile.ProfessionType),
			FinancialStatusId = ((mobile.financial_status_id > 0) ? mobile.financial_status_id : ((mobile.FinancialStatusId > 0) ? mobile.FinancialStatusId : 0)),
			FamilyName = (mobile.Family_Name ?? mobile.FamilyName),
			FatherName = (mobile.Father_name ?? mobile.FatherName),
			Post = mobile.Post,
			Village = mobile.Village,
			PinCode = (mobile.Pin_Code ?? mobile.PinCode),
			Country = mobile.Country,
			State = mobile.State,
			District = mobile.District,
			PresentCountry = (mobile.Present_Country ?? mobile.PresentCountry),
			PresentState = (mobile.Present_State ?? mobile.PresentState),
			PresentDistrict = (mobile.Present_District ?? mobile.PresentDistrict),
			PresentCity = (mobile.Present_City ?? mobile.PresentCity),
			ImagePath = (mobile.Image_Path ?? mobile.ImagePath),
			Image = mobile.Image,
			About = mobile.About,
			EducationType = (mobile.Education_Type ?? mobile.EducationType),
			Email = mobile.Email,
			Password = mobile.Password,
			RegisterNumber = (mobile.Register_Number ?? mobile.RegisterNumber),
			NumberOfChildrens = mobile.NumberOfChildrens,
			PhotoVisibleToAll = (mobile.PhotoVisibleToAll ?? mobile.Photo_Visible_To_All ?? true),
			PhotoVisibleToPremium = (mobile.PhotoVisibleToPremium ?? (mobile.Photo_Visible_To_Premium == true)),
			PhotoVisibleToAccepted = (mobile.PhotoVisibleToAccepted ?? (mobile.Photo_Visible_To_Accepted == true)),
			CompletedStep = (mobile.CompletedStep ?? mobile.Completed_Step),
			Source = mobile.Source,
			DocumentVerificationEnabled = (mobile.DocumentVerificationEnabled || mobile.Document_Verification_Enabled == true),
			VerificationDocumentUrl = (mobile.Verification_Document_Url ?? mobile.VerificationDocumentUrl),
			DocumentVerificationComplete = (mobile.DocumentVerificationComplete || mobile.Document_Verification_Complete == true),
			DocumentVerificationRejected = (mobile.DocumentVerificationRejected || mobile.Document_Verification_Rejected == true),
			DocumentVerificationFollowupApproved = (mobile.DocumentVerificationFollowupApproved || mobile.Document_Verification_Followup_Approved == true)
		};
	}

	private MobileProfileDto MapRegistrationDtoToMobileProfile(RegistrationDto reg)
	{
		return new MobileProfileDto
		{
			Id = reg.Id,
			Profile_For_Id = reg.ProfileForId,
			ProfileForId = reg.ProfileForId,
			Nationality_Id = reg.NationalityId,
			NationalityId = reg.NationalityId,
			Country_Code = reg.CountryCode,
			CountryCode = reg.CountryCode,
			Secondary_Country_Code = reg.SecondaryCountryCode,
			SecondaryCountryCode = reg.SecondaryCountryCode,
			Marital_Status_Id = reg.MaritalStatusId,
			MaritalStatusId = reg.MaritalStatusId,
			Height_Id = reg.HeightId,
			HeightId = reg.HeightId,
			Weight_Id = reg.WeightId,
			WeightId = reg.WeightId,
			Complexion_Id = reg.ComplexionId,
			ComplexionId = reg.ComplexionId,
			Body_Type_Id = reg.BodyTypeId,
			BodyTypeId = reg.BodyTypeId,
			Is_Physically_Challenged = (reg.IsPhysicallyChallenged ? 1 : 0),
			IsPhysicallyChallenged = (reg.IsPhysicallyChallenged ? 1 : 0),
			Physically_Challenged_Detail = reg.PhysicallyChallengedDetail,
			PhysicallyChallengedDetail = reg.PhysicallyChallengedDetail,
			Name = reg.Name,
			Gender = reg.Gender,
			DOB = reg.DOB,
			Phone = reg.Phone,
			Landline_Number = reg.LandlineNumber,
			LandlineNumber = reg.LandlineNumber,
			Highest_Education = reg.HighestEducation,
			HighestEducation = reg.HighestEducation,
			Profession_Id = reg.ProfessionId,
			ProfessionId = reg.ProfessionId,
			Mother_Tongue_Id = reg.MotherTongueId,
			MotherTongueId = reg.MotherTongueId,
			Religion_Id = reg.ReligionId,
			ReligionId = reg.ReligionId,
			Caste_Id = reg.CasteId,
			CasteId = reg.CasteId,
			Community_Id = reg.CommunityId,
			CommunityId = reg.CommunityId,
			Religiousness_Id = reg.ReligiousnessId,
			ReligiousnessId = reg.ReligiousnessId,
			Profession_Type = reg.ProfessionType,
			ProfessionType = reg.ProfessionType,
			financial_status_id = reg.FinancialStatusId,
			FinancialStatusId = reg.FinancialStatusId,
			Family_Name = reg.FamilyName,
			FamilyName = reg.FamilyName,
			Father_name = reg.FatherName,
			FatherName = reg.FatherName,
			Post = reg.Post,
			Village = reg.Village,
			Pin_Code = reg.PinCode,
			PinCode = reg.PinCode,
			Country = reg.Country,
			State = reg.State,
			District = reg.District,
			Present_Country = reg.PresentCountry,
			PresentCountry = reg.PresentCountry,
			Present_State = reg.PresentState,
			PresentState = reg.PresentState,
			Present_District = reg.PresentDistrict,
			PresentDistrict = reg.PresentDistrict,
			Present_City = reg.PresentCity,
			PresentCity = reg.PresentCity,
			Image_Path = reg.ImagePath,
			ImagePath = reg.ImagePath,
			About = reg.About,
			Education_Type = reg.EducationType,
			EducationType = reg.EducationType,
			Email = reg.Email,
			Password = reg.Password,
			Register_Number = reg.RegisterNumber,
			RegisterNumber = reg.RegisterNumber,
			NumberOfChildrens = reg.NumberOfChildrens,
			PhotoVisibleToAll = reg.PhotoVisibleToAll,
			Photo_Visible_To_All = reg.PhotoVisibleToAll,
			PhotoVisibleToPremium = reg.PhotoVisibleToPremium,
			Photo_Visible_To_Premium = reg.PhotoVisibleToPremium,
			PhotoVisibleToAccepted = reg.PhotoVisibleToAccepted,
			Photo_Visible_To_Accepted = reg.PhotoVisibleToAccepted,
			CompletedStep = reg.CompletedStep,
			Completed_Step = reg.CompletedStep,
			Source = reg.Source,
			DocumentVerificationEnabled = reg.DocumentVerificationEnabled,
			Document_Verification_Enabled = reg.DocumentVerificationEnabled,
			VerificationDocumentUrl = reg.VerificationDocumentUrl,
			Verification_Document_Url = reg.VerificationDocumentUrl,
			DocumentVerificationComplete = reg.DocumentVerificationComplete,
			Document_Verification_Complete = reg.DocumentVerificationComplete,
			DocumentVerificationRejected = reg.DocumentVerificationRejected,
			Document_Verification_Rejected = reg.DocumentVerificationRejected,
			DocumentVerificationFollowupApproved = reg.DocumentVerificationFollowupApproved,
			Document_Verification_Followup_Approved = reg.DocumentVerificationFollowupApproved,
			VerificationDocuments = reg.VerificationDocuments
		};
	}

	private async Task SendRegistrationSuccessEmail(string email, string name, string registerNumber)
	{
		string templatePath = "Templates/Mail/template-registrationsuccessful.html";
		string emailContent = System.IO.File.ReadAllText(templatePath);
		emailContent = emailContent.Replace("[User's Name]", name);
		emailContent = emailContent.Replace("[Profile ID]", registerNumber);
		AlternateView htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
		_emailNotificationHelper.SendEmail(email, htmlView, "Welcome to M4Nikah - Your Profile Registration is Complete!");
	}

	private async Task SendRegistrationUnsuccessfulEmail(string email, string name)
	{
		string templatePath = "Templates/Mail/template-registrationunsuccessful.html";
		string emailContent = System.IO.File.ReadAllText(templatePath);
		emailContent = emailContent.Replace("[User's Name]", name);
		emailContent = emailContent.Replace("[Support Email]", "support@m4nikkah.com");
		emailContent = emailContent.Replace("[Support Phone Number]", "123-456-7890");
		AlternateView htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
		_emailNotificationHelper.SendEmail(email, htmlView, "M4Nikkah Registration Unsuccessful");
	}

	[HttpGet("/signout")]
	public async Task<IActionResult> Signout()
	{
		long? userId = GetCurrentUserId();
		if (userId.HasValue && userId.Value > 0)
		{
			_presenceTracker.UserLoggedOut(userId.Value);
			await _hubContext.Clients.All.SendAsync("UserOffline", userId.Value);
			_logger.LogInformation("Presence: User {UserId} marked offline on signout", userId.Value);
		}
		_cookieHelper.ClearSecureCookie(HttpContext);
		_logger.LogInformation("User logged out");
		return RedirectToAction("Login");
	}

	[AllowAnonymous]
	[HttpGet("/signup")]
	public async Task<IActionResult> SignUp()
	{
		var homeViewModel = new HomeViewModel
		{
			Registrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive((Registration x) => x.ShowOnHomePage)),
			ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive()),
			Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive()),
			MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
			Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive()),
			MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive()),
			Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive((ReligionCaste x) => x.ParentId == 0)),
			Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive((ReligionCaste x) => x.ParentId != 0)),
			Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
			Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive()),
			FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive()),
			Registration = new RegistrationDto()
		};
		return View(homeViewModel);
	}

	[AllowAnonymous]
	[HttpGet("/forgot-password")]
	public async Task<IActionResult> ForgotPassword()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
		}
		return View();
	}

	[HttpPost("/forgot-password")]
	public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel loginViewModel, [FromQuery] string? returnUrl = null)
	{
		_logger.LogInformation("ForgotPassword method called with email: {Email}", loginViewModel.Email);
		if (!ModelState.IsValid)
		{
			_logger.LogWarning("Model state is invalid for email: {Email}", loginViewModel.Email);
			return View();
		}
		try
		{
			Registration user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Email == loginViewModel.Email);
			if (user == null)
			{
				_logger.LogWarning("No user found with email: {Email}", loginViewModel.Email);
				ModelState.AddModelError("error", "Account not found. The provided email is incorrect.");
				return View();
			}
			_logger.LogInformation("User found with email: {Email}", user.Email);
			RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
			user.Password = encryption.DecryptRijndael(user.Password, user.PasswordHash);
			_logger.LogInformation("Password decrypted for user: {UserId}", user.Id);
			string templatePath = "Templates/Mail/PasswordRecovery/Recovery.html";
			string passwordContent = (await System.IO.File.ReadAllTextAsync(templatePath)).Replace("[Password]", user.Password);
			passwordContent = passwordContent.Replace("[User's Name]", user.Name);
			string subject = "Your M4Nikah Password Recovery";
			AlternateView htmlView = AlternateView.CreateAlternateViewFromString(passwordContent, null, "text/html");
			_emailNotificationHelper.SendEmail(user.Email, htmlView, subject);
			_logger.LogInformation("Password recovery email sent to: {Email}", user.Email);
			ModelState.AddModelError("success", "You have successfully recovered your password. Please check your registered email address.");
			return RedirectToAction("Login");
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "An exception occurred while processing password recovery for email: {Email}", loginViewModel.Email);
			ModelState.AddModelError("error", "An error occurred while processing your request. Please try again later.");
		}
		return View();
	}

	[AllowAnonymous]
	[HttpPost("/user/forgot-password/mobile/recover")]
	public async Task<IActionResult> RecoverPasswordViaMobile([FromBody] OtpSendRequest request)
	{
		if (Request.ContentType != null && !Request.ContentType.Contains("application/json"))
		{
			try
			{
				string type = Request.Form["Type"].ToString();
				string identifier = Request.Form["Identifier"].ToString();
				request = new OtpSendRequest
				{
					Type = type,
					Identifier = identifier
				};
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Failed to read form data in RecoverPasswordViaMobile");
			}
		}
		else if (Request.ContentType != null && Request.ContentType.Contains("application/json") && request == null)
		{
			try
			{
				using StreamReader reader = new StreamReader(Request.Body);
				request = JsonSerializer.Deserialize<OtpSendRequest>(await reader.ReadToEndAsync(), new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to deserialize JSON in RecoverPasswordViaMobile");
			}
		}
		if (request == null || string.IsNullOrEmpty(request.Identifier))
		{
			return BadRequest(new
			{
				success = false,
				error = "Please provide a valid phone number."
			});
		}
		Registration user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Phone == request.Identifier && x.IsVerified && x.IsComplete);
		if (user == null)
		{
			return NotFound(new
			{
				success = false,
				error = "Account not found or not fully registered/verified."
			});
		}
		RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
		string decryptedPassword = encryption.DecryptRijndael(user.Password, user.PasswordHash);
		try
		{
			if (!(await _emailService.SendSmsForgotPasswordAsync(decryptedPassword, user.Phone)))
			{
				_logger.LogWarning("SMS dispatch returned false for forgot password: {Phone}", user.Phone);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send SMS password to {Phone}", user.Phone);
			return StatusCode(500, new
			{
				success = false,
				error = "Failed to send SMS."
			});
		}
		_logger.LogInformation("Password successfully sent via SMS to mobile number: {Phone}", user.Phone);
		return Ok(new
		{
			success = true,
			message = "Your password has been successfully sent to your registered mobile number."
		});
	}

	[HttpGet("/user/billhistory")]
	public async Task<IActionResult> BillHistory()
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			_cookieHelper.ClearSecureCookie(HttpContext);
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdValue.Value));
		if (profile == null)
		{
			_cookieHelper.ClearSecureCookie(HttpContext);
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		if (!profile.IsPremiumMember)
		{
			profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		}
		return View(new BillHistoryViewModel
		{
			Registration = profile
		});
	}

	[HttpGet("/api/user/transactions")]
	public async Task<IActionResult> GetBillHistoryApi(int page = 1, int pageSize = 30)
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			return Unauthorized(new
			{
				success = false,
				message = "User not logged in."
			});
		}
		List<Transaction> transactions = await _transactionRepository.GetPagedTransactionsByUserIdAsync(userIdValue.Value, page, pageSize);
		var data = transactions.Select((Transaction t) => new
		{
			id = t.Id,
			txnId = t.TxnId,
			amount = t.Amount,
			paymentType = ((!(t.PaymentType == "Offline")) ? (t.PaymentType ?? "Online") : ((t.OfflinePaymentType == OfflinePaymentMethod.UPIPayment) ? "Offline (UPI Payment)" : ((t.OfflinePaymentType == OfflinePaymentMethod.BankPayment) ? "Offline (Bank Payment)" : ((t.OfflinePaymentType == OfflinePaymentMethod.CashPayment) ? "Offline (Cash Payment)" : "Offline")))),
			createdOn = t.CreatedOn.ToString("dd-MM-yyyy hh:mm tt"),
			status = t.Status
		}).ToList();
		return Ok(new
		{
			success = true,
			page = page,
			pageSize = pageSize,
			hasMore = (transactions.Count == pageSize),
			data = data
		});
	}

	[HttpGet("/api/user/invoice/{transactionId}")]
	public async Task<IActionResult> GetInvoiceApi(long transactionId)
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			return Unauthorized(new
			{
				success = false,
				message = "User not logged in."
			});
		}
		Transaction transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);
		if (transaction == null || transaction.userId != userIdValue.Value)
		{
			return NotFound(new
			{
				success = false,
				message = "Invoice not found."
			});
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(transaction.Registration);
		if (profile == null)
		{
			profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdValue.Value));
		}
		List<string> addressParts = new List<string>();
		if (!string.IsNullOrEmpty(profile.Village))
		{
			addressParts.Add(profile.Village);
		}
		if (!string.IsNullOrEmpty(profile.Post))
		{
			addressParts.Add(profile.Post);
		}
		if (!string.IsNullOrEmpty(profile.District))
		{
			addressParts.Add(profile.District);
		}
		if (!string.IsNullOrEmpty(profile.State))
		{
			addressParts.Add(profile.State);
		}
		string address = ((addressParts.Count > 0) ? string.Join(", ", addressParts) : "N/A");
		string invoiceNo = $"INV-{transaction.CreatedOn.Year}-{transaction.Id.ToString().PadLeft(4, '0')}";
		string purchaseDateStr = transaction.CreatedOn.ToString("dd-MM-yyyy hh:mm tt");
		string expiryDateStr = transaction.CreatedOn.AddDays(180.0).ToString("dd-MM-yyyy hh:mm tt");
		var data = new
		{
			invoiceNo = invoiceNo,
			transactionId = transaction.Id,
			txnId = transaction.TxnId,
			amount = transaction.Amount,
			paymentType = (transaction.PaymentType ?? "Online"),
			offlinePaymentMethod = transaction.OfflinePaymentType,
			createdOn = purchaseDateStr,
			expiryDate = expiryDateStr,
			status = transaction.Status,
			user = new
			{
				registerNumber = profile.RegisterNumber,
				name = profile.Name,
				email = profile.Email,
				phone = profile.Phone,
				address = address
			},
			package = new
			{
				description = "Premium Package Activation Charge (6 Months)",
				qty = 1,
				rate = transaction.Amount,
				total = transaction.Amount
			},
			bankDetails = new
			{
				accountName = "M4 Nikah Muslim Matrimony",
				bankName = "HDFC Bank - Kottakkal Branch",
				accountNo = "50200090982976",
				ifsc = "HDFC0001594"
			}
		};
		return Ok(new
		{
			success = true,
			data = data
		});
	}

	[HttpGet("/user/invoice/{transactionId}")]
	public async Task<IActionResult> Invoice(long transactionId)
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			_cookieHelper.ClearSecureCookie(HttpContext);
			return RedirectToAction("Login");
		}
		Transaction transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);
		if (transaction == null || transaction.userId != userIdValue.Value)
		{
			return NotFound("Invoice not found.");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(transaction.Registration);
		if (profile == null)
		{
			profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdValue.Value));
		}
		InvoiceViewModel model = new InvoiceViewModel
		{
			Registration = profile,
			Transaction = transaction
		};
		return View(model);
	}

	[HttpGet("/dashboard")]
	public async Task<IActionResult> Dashboard()
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			_cookieHelper.ClearSecureCookie(HttpContext);
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdValue.Value));
		if (profile == null)
		{
			_cookieHelper.ClearSecureCookie(HttpContext);
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		IActionResult redirectResult = CheckRegistrationComplete(profile);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		if (!profile.IsPremiumMember)
		{
			profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		}
		return View(new UserDashboardViewModel
		{
			Registration = profile,
			RegistrationList = new List<RegistrationDto>()
		});
	}

	[HttpGet("/explore")]
	public async Task<IActionResult> Explore()
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			_cookieHelper.ClearSecureCookie(HttpContext);
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdValue.Value));
		if (profile == null)
		{
			_cookieHelper.ClearSecureCookie(HttpContext);
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		IActionResult redirectResult = CheckRegistrationComplete(profile);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		if (!profile.IsPremiumMember)
		{
			profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		}
		return View(new UserExploreViewModel
		{
			Registration = profile
		});
	}

	[HttpPost]
	public async Task<IActionResult> SearchStarProfile([FromForm] UserStarProfile userStarProfile, [FromForm] string Action)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		bool success = false;
		if (Action == "Add")
		{
			if (await _userStarProfileRepo.FirstOrDefaultActive((UserStarProfile x) => x.UserId == userIdLong && x.StarId == userStarProfile.StarId) == null)
			{
				RegistrationDto profiledetails = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
				UserStarProfile user = new UserStarProfile
				{
					UserId = userIdLong,
					StarId = userStarProfile.StarId
				};
				await _userStarProfileRepo.Add(user);
				success = true;
				Notification notification = new Notification
				{
					UserId = userIdLong,
					Notify_Id = userStarProfile.StarId,
					Notify_Message = "Your profile has been shortlisted by " + profiledetails.Name,
					CreatedOn = DateTime.UtcNow,
					ModifiedOn = DateTime.UtcNow
				};
				await _notificationRepo.Add(notification);
			}
		}
		else if (Action == "Remove")
		{
			UserStarProfile existingStar = await _userStarProfileRepo.FirstOrDefaultActive(x => x.UserId == userIdLong && x.StarId == userStarProfile.StarId);
			if (existingStar != null)
			{
				await _userStarProfileRepo.Remove(existingStar);
				success = true;
				foreach (Notification n in await _notificationRepo.Where((Notification x) => x.UserId == userIdLong && x.Notify_Id == userStarProfile.StarId && x.Notify_Message.Contains("shortlisted by")))
				{
					await _notificationRepo.Remove(n);
				}
				await _notificationRepo.SaveChanges();
			}
		}
		await _userStarProfileRepo.SaveChanges();
		return Json(new { success });
	}

	[HttpPost]
	public async Task<IActionResult> UserStarProfile([FromForm] UserStarProfile userStarProfile, [FromForm] string Action)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		bool success = false;
		if (Action == "Add")
		{
			if (await _userStarProfileRepo.FirstOrDefaultActive((UserStarProfile x) => x.UserId == userIdLong && x.StarId == userStarProfile.StarId) == null)
			{
				RegistrationDto profiledetails = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
				UserStarProfile user = new UserStarProfile
				{
					UserId = userIdLong,
					StarId = userStarProfile.StarId
				};
				await _userStarProfileRepo.Add(user);
				success = true;
				Notification notification = new Notification
				{
					UserId = userIdLong,
					Notify_Id = userStarProfile.StarId,
					Notify_Message = "Your profile has been shortlisted by " + profiledetails.Name,
					CreatedOn = DateTime.UtcNow,
					ModifiedOn = DateTime.UtcNow
				};
				await _notificationRepo.Add(notification);
			}
		}
		else if (Action == "Remove")
		{
			UserStarProfile existingStar = await _userStarProfileRepo.FirstOrDefaultActive(x => x.UserId == userIdLong && x.StarId == userStarProfile.StarId);
			if (existingStar != null)
			{
				await _userStarProfileRepo.Remove(existingStar);
				success = true;
				foreach (Notification n in await _notificationRepo.Where((Notification x) => x.UserId == userIdLong && x.Notify_Id == userStarProfile.StarId && x.Notify_Message.Contains("shortlisted by")))
				{
					await _notificationRepo.Remove(n);
				}
				await _notificationRepo.SaveChanges();
			}
		}
		await _userStarProfileRepo.SaveChanges();
		return Json(new { success });
	}

	[HttpPost]
	public async Task<IActionResult> FavouriteIcon([FromForm] UserFavouriteProfile userfavouriteProfile, [FromForm] string Action)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		if (Action == "Add")
		{
			if (await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => x.UserId == userIdLong && x.LikedId == userfavouriteProfile.LikedId) == null)
			{
				UserFavouriteProfile user = new UserFavouriteProfile
				{
					UserId = userIdLong,
					LikedId = userfavouriteProfile.LikedId
				};
				await _userFavouriteProfileRepo.Add(user);
			}
		}
		else if (Action == "Remove")
		{
			UserFavouriteProfile existingFav = await _userFavouriteProfileRepo.FirstOrDefaultActive(x => x.UserId == userIdLong && x.LikedId == userfavouriteProfile.LikedId);
			if (existingFav != null)
			{
				await _userFavouriteProfileRepo.Remove(existingFav);
				foreach (Notification n in await _notificationRepo.Where((Notification x) => x.UserId == userIdLong && x.Notify_Id == userfavouriteProfile.LikedId && x.Notify_Message.Contains("Interest request from")))
				{
					await _notificationRepo.Remove(n);
				}
				foreach (Notification n in await _notificationRepo.Where((Notification x) => x.UserId == userfavouriteProfile.LikedId && x.Notify_Id == userIdLong && (x.Notify_Message.Contains("Interest sent to") || x.Notify_Message.Contains("interest has been accepted") || x.Notify_Message.Contains("interest has been declined"))))
				{
					await _notificationRepo.Remove(n);
				}
				await _notificationRepo.SaveChanges();
			}
		}
		await _userFavouriteProfileRepo.SaveChanges();
		if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
		{
			return Json(new
			{
				success = true
			});
		}
		return RedirectToAction("GetStarredProfiles");
	}

	[HttpPost]
	public async Task<IActionResult> StarredIcon([FromForm] UserStarProfile userStarProfile, [FromForm] string Action)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		bool success = false;
		if (Action == "Add")
		{
			if (await _userStarProfileRepo.FirstOrDefaultActive((UserStarProfile x) => x.UserId == userIdLong && x.StarId == userStarProfile.StarId) == null)
			{
				RegistrationDto profiledetails = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
				UserStarProfile user = new UserStarProfile
				{
					UserId = userIdLong,
					StarId = userStarProfile.StarId
				};
				await _userStarProfileRepo.Add(user);
				success = true;
				Notification notification = new Notification
				{
					UserId = userIdLong,
					Notify_Id = userStarProfile.StarId,
					Notify_Message = "Your profile has been shortlisted by " + profiledetails.Name,
					CreatedOn = DateTime.UtcNow,
					ModifiedOn = DateTime.UtcNow
				};
				await _notificationRepo.Add(notification);
				await _notificationRepo.SaveChanges();
			}
		}
		else if (Action == "Remove")
		{
			UserStarProfile existingStar = await _userStarProfileRepo.FirstOrDefaultActive(x => x.UserId == userIdLong && x.StarId == userStarProfile.StarId);
			if (existingStar != null)
			{
				await _userStarProfileRepo.Remove(existingStar);
				success = true;
				foreach (Notification n in await _notificationRepo.Where((Notification x) => x.UserId == userIdLong && x.Notify_Id == userStarProfile.StarId && x.Notify_Message.Contains("shortlisted by")))
				{
					await _notificationRepo.Remove(n);
				}
				await _notificationRepo.SaveChanges();
			}
		}
		await _userStarProfileRepo.SaveChanges();
		return Json(new { success });
	}

	[HttpPost]
	public async Task<IActionResult> NotLikeProfile([FromForm] long NotLikedId)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found."
			});
		}
		await _profileService.NotLikeProfileAsync(Convert.ToInt64(userId), NotLikedId);
		return Json(new
		{
			success = true
		});
	}

	[HttpPost]
	public async Task<IActionResult> UserFavouriteProfile([FromForm] UserFavouriteProfile userfavouriteProfile, [FromForm] string Action)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		bool success = false;
		if (Action == "Add")
		{
			UserFavouriteProfile user = await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => x.UserId == userIdLong && x.LikedId == userfavouriteProfile.LikedId);
			RegistrationDto profiledetails = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
			RegistrationDto targetProfile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userfavouriteProfile.LikedId));
			if (user == null)
			{
				user = new UserFavouriteProfile
				{
					UserId = userIdLong,
					LikedId = userfavouriteProfile.LikedId,
					Status = InterestStatus.Pending
				};
				await _userFavouriteProfileRepo.Add(user);
				success = true;
			}
			else if (user.Status == InterestStatus.Declined)
			{
				user.Status = InterestStatus.Pending;
				_userFavouriteProfileRepo.Update(user);
				success = true;
			}
			if (success)
			{
				Notification notification = new Notification
				{
					UserId = userIdLong,
					Notify_Id = userfavouriteProfile.LikedId,
					Notify_Message = "Interest request from " + profiledetails.Name,
					CreatedOn = DateTime.UtcNow,
					ModifiedOn = DateTime.UtcNow
				};
				await _notificationRepo.Add(notification);
				Notification senderNotification = new Notification
				{
					UserId = userfavouriteProfile.LikedId,
					Notify_Id = userIdLong,
					Notify_Message = "Interest sent to " + targetProfile.Name + " — Pending",
					CreatedOn = DateTime.UtcNow,
					ModifiedOn = DateTime.UtcNow
				};
				await _notificationRepo.Add(senderNotification);
			}
		}
		else if (Action == "Remove")
		{
			UserFavouriteProfile existingFav = await _userFavouriteProfileRepo.FirstOrDefaultActive(x => x.UserId == userIdLong && x.LikedId == userfavouriteProfile.LikedId);
			if (existingFav != null)
			{
				await _userFavouriteProfileRepo.Remove(existingFav);
				success = true;
				foreach (Notification n in await _notificationRepo.Where((Notification x) => x.UserId == userIdLong && x.Notify_Id == userfavouriteProfile.LikedId && x.Notify_Message.Contains("Interest request from")))
				{
					await _notificationRepo.Remove(n);
				}
				foreach (Notification n in await _notificationRepo.Where((Notification x) => x.UserId == userfavouriteProfile.LikedId && x.Notify_Id == userIdLong && (x.Notify_Message.Contains("Interest sent to") || x.Notify_Message.Contains("interest has been accepted") || x.Notify_Message.Contains("interest has been declined"))))
				{
					await _notificationRepo.Remove(n);
				}
				await _notificationRepo.SaveChanges();
			}
		}
		await _userFavouriteProfileRepo.SaveChanges();
		return Json(new { success });
	}

	[HttpPost]
	public async Task<IActionResult> ViewUserContactDetails([FromForm] UserContactViewDto userContactView)
	{
		long viewedUserId = userContactView.ViewedUserId;
		long viewerUserId = userContactView.ViewerUserId;
		UserContactView existingUserContactView = await _userContactViewRepository.FirstOrDefaultActive((UserContactView x) => x.ViewerUserId == viewerUserId && x.ViewedUserId == viewedUserId);
		RegistrationDto user = _mapper.Map<RegistrationDto>(await _registrationRepo.FirstActive((Registration x) => x.Id == viewedUserId));
		RegistrationDto registrationDto = user;
		registrationDto.Nationality = (await _nationalityRepo.Get(user.NationalityId))?.Title;
		registrationDto = user;
		registrationDto.FinancialStatus = (await _financialStatus.Get(user.FinancialStatusId))?.Title;
		user.CountryCode = CountryCodeHelper.GetCountryCode(user.CountryCode ?? user.Country);
		user.PresentCountryCode = CountryCodeHelper.GetCountryCode(user.SecondaryCountryCode ?? user.PresentCountry);
		if (existingUserContactView != null)
		{
			return Json(new
			{
				success = true,
				message = "Contact details already unlocked.",
				user = user
			});
		}
		if (await _userService.SpendContactViewCredit(viewerUserId))
		{
			await _userContactViewRepository.Add(_mapper.Map<UserContactView>(userContactView));
			await _userContactViewRepository.SaveChanges();
			return Json(new
			{
				success = true,
				message = "Contact details unlocked. One credit spent.",
				user = user
			});
		}
		return Json(new
		{
			success = false,
			message = "Insufficient credits."
		});
	}

	[HttpGet("/profile/{*url}")]
	public async Task<IActionResult> Profile(string url)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		if (string.IsNullOrEmpty(url))
		{
			return RedirectToAction("Dashboard");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.FirstOrDefaultActive((Registration x) => x.RegisterNumber == url));
		RegistrationDto loggedInUser;
		if (profile == null)
		{
			if (await _registrationRepo.FirstOrDefaultWithDeleted((Registration x) => x.RegisterNumber == url) != null)
			{
				loggedInUser = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
				if (!loggedInUser.IsPremiumMember)
				{
					loggedInUser.IsPremiumMember = await _userService.IsPremiumUser(loggedInUser.Id);
				}
				return View("ProfileUnavailable", new HomeViewModel
				{
					Registration = loggedInUser
				});
			}
			return RedirectToAction("Dashboard");
		}
		long userIdLong = Convert.ToInt64(userId);
		if (profile.Id != userIdLong && !profile.IsVisible)
		{
			loggedInUser = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
			if (!loggedInUser.IsPremiumMember)
			{
				loggedInUser.IsPremiumMember = await _userService.IsPremiumUser(loggedInUser.Id);
			}
			return View("ProfileUnavailable", new HomeViewModel
			{
				Registration = loggedInUser
			});
		}
		profile = await MapStarredProfiles(profile);
		profile = await MapLikedProfiles(profile);
		profile.CountryCode = CountryCodeHelper.GetCountryCode(profile.CountryCode ?? profile.Country);
		profile.PresentCountryCode = CountryCodeHelper.GetCountryCode(profile.SecondaryCountryCode ?? profile.PresentCountry);
		ImagesDto image = _mapper.Map<ImagesDto>(await _imagesRepo.FirstOrDefaultActive((Images x) => x.UserId == profile.Id));
		RegistrationDto loggedInUserDetails = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		loggedInUser = loggedInUserDetails;
		if (!loggedInUserDetails.IsPremiumMember)
		{
			loggedInUserDetails.IsPremiumMember = await _userService.IsPremiumUser(loggedInUserDetails.Id);
		}
		UserFavouriteProfile profileLikedUs = await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => x.UserId == profile.Id && x.LikedId == loggedInUserDetails.Id);
		UserFavouriteProfile profileWeLiked = await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => x.UserId == loggedInUserDetails.Id && x.LikedId == profile.Id);
		UserReport existingReport = await _userReportRepository.FirstOrDefaultActive((UserReport x) => x.ReporterUserId == loggedInUserDetails.Id && x.ReportedUserId == profile.Id);
		bool photosUnlocked = false;
		long viewerId = loggedInUserDetails.Id;
		long ownerId = profile.Id;
		if (viewerId == ownerId)
		{
			photosUnlocked = true;
		}
		else if (profile.PhotoVisibleToAll)
		{
			photosUnlocked = true;
		}
		else
		{
			if (profile.PhotoVisibleToPremium && (await _userService.GetActivePlanPurchase(viewerId) != null || loggedInUserDetails.IsPremiumMember))
			{
				photosUnlocked = true;
			}
			if (!photosUnlocked && profile.PhotoVisibleToAccepted && await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => ((x.UserId == viewerId && x.LikedId == ownerId) || (x.UserId == ownerId && x.LikedId == viewerId)) && (int)x.Status == 1) != null)
			{
				photosUnlocked = true;
			}
			if (!photosUnlocked && await _photoUnlockRequestRepo.FirstOrDefaultActive((PhotoUnlockRequest x) => x.RequesterId == viewerId && x.OwnerId == ownerId && (int)x.Status == 1) != null)
			{
				photosUnlocked = true;
			}
		}
		if (!photosUnlocked && !string.IsNullOrEmpty(profile.ImagePath))
		{
			string message = loggedInUserDetails.Name + " viewed your profile";
			Notification existingNotif = await _notificationRepo.FirstOrDefaultActive((Notification x) => x.UserId == viewerId && x.Notify_Id == ownerId && x.Notify_Message == message);
			if (existingNotif == null)
			{
				Notification notification = new Notification
				{
					UserId = viewerId,
					Notify_Id = ownerId,
					Notify_Message = message,
					CreatedOn = DateTime.UtcNow,
					ModifiedOn = DateTime.UtcNow
				};
				await _notificationRepo.Add(notification);
			}
			else
			{
				existingNotif.CreatedOn = DateTime.UtcNow;
				existingNotif.ModifiedOn = DateTime.UtcNow;
				await _notificationRepo.Update(existingNotif);
			}
			await _notificationRepo.SaveChanges();
		}
		int photoUnlockRequestStatus = -1;
		PhotoUnlockRequest request = await _photoUnlockRequestRepo.FirstOrDefaultActive((PhotoUnlockRequest x) => x.RequesterId == viewerId && x.OwnerId == ownerId);
		if (request != null)
		{
			photoUnlockRequestStatus = (int)request.Status;
		}
		HomeViewModel homeViewModel = new HomeViewModel
		{
			Registrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive(x => x.ShowOnHomePage)),
			ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive()),
			Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive()),
			MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
			Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive()),
			MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive()),
			Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0)),
			Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId != 0)),
			Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
			Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive()),
			FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive()),
			Registration = loggedInUserDetails,
			MatchedUserProfile = profile,
			ContactDetailsUnlocked = await _userService.AreUserContactDetailsUnlocked(loggedInUserDetails.Id, profile.Id),
			ActivePlan = await _userService.GetActivePlanPurchase(loggedInUserDetails.Id),
			Images = image ?? new ImagesDto(),
			UserReportReasons = _mapper.Map<List<UserReportReasonDto>>(await _userReportReasonRepository.GetAllActive()),
			ProfileLikedUs = profileLikedUs != null,
			InterestStatusFromUs = (int)(profileWeLiked?.Status ?? ((InterestStatus)(-1))),
			InterestStatusToUs = (int)(profileLikedUs?.Status ?? ((InterestStatus)(-1))),
			PhotosUnlocked = photosUnlocked,
			PhotoUnlockRequestStatus = photoUnlockRequestStatus,
			AlreadyReported = existingReport != null
		};
		return View(homeViewModel);
	}

	[HttpGet("/GetStarredProfiles")]
	public async Task<IActionResult> GetStarredProfiles()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		DateTime sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
		List<MatchingProfilesResponseDto> matchingProfiles = (await _matchingProfilesResponseDtoRepo.GetMatchingUsersWithPercentage(Convert.ToInt32(userId))).Where((MatchingProfilesResponseDto mp) => mp.Starred_Created_On >= sixMonthsAgo).ToList();
		Dictionary<long, MatchingProfilesResponseDto> matchingProfilesDict = (from mp in matchingProfiles
			group mp by mp.Id).ToDictionary((IGrouping<long, MatchingProfilesResponseDto> g) => g.Key, (IGrouping<long, MatchingProfilesResponseDto> g) => g.Last());
		List<long> userIds = matchingProfiles.Select((MatchingProfilesResponseDto m) => m.Id).Distinct().ToList();
		List<Registration> matchedUserProfiles = await _registrationRepo.GetAllByIds(userIds);
		List<RegistrationDto> mappedMatchedUserProfiles = _mapper.Map<List<RegistrationDto>>(matchedUserProfiles);
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		if (!profile.IsPremiumMember)
		{
			profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		}
		List<RegistrationDto> list = new List<RegistrationDto>();
		foreach (RegistrationDto item in mappedMatchedUserProfiles)
		{
			if (item != null && matchingProfilesDict.TryGetValue(item.Id, out var matchingProfile))
			{
				item.IsStarred = matchingProfile.IsStarred;
				item.IsLiked = matchingProfile.IsLiked;
				item.total_matching_score = matchingProfile.total_matching_score;
				if (item.IsStarred)
				{
					list.Add(item);
				}
			}
		}
		list = list.OrderByDescending(x => x.total_matching_score).Take(50).ToList();
		UserDashboardViewModel userDashboardViewModel = new UserDashboardViewModel
		{
			Registration = profile,
			RegistrationList = list,
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
			ReligionCaste = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0)),
			MaritalStatus = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			Profession = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive()),
			Community = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive())
		};
		return View(userDashboardViewModel);
	}

	[HttpPost("/ExpressInterestProfiles")]
	public async Task<IActionResult> ExpressInterest([FromForm] UserFavouriteProfile userfavouriteProfile)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		UserFavouriteProfile existingFavorite = await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => x.UserId == userIdLong && x.LikedId == userfavouriteProfile.LikedId);
		RegistrationDto profiledetails = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
		RegistrationDto targetProfile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userfavouriteProfile.LikedId));
		string message;
		bool isFavorite;
		if (existingFavorite == null)
		{
			UserFavouriteProfile newFavorite = new UserFavouriteProfile
			{
				UserId = userIdLong,
				LikedId = userfavouriteProfile.LikedId,
				Status = InterestStatus.Pending
			};
			await _userFavouriteProfileRepo.Add(newFavorite);
			message = "Interest request sent successfully.";
			isFavorite = true;
		}
		else if (existingFavorite.Status == InterestStatus.Declined)
		{
			existingFavorite.Status = InterestStatus.Pending;
			_userFavouriteProfileRepo.Update(existingFavorite);
			message = "Interest request sent successfully.";
			isFavorite = true;
		}
		else
		{
			message = "Profile already liked.";
			isFavorite = false;
		}
		if (isFavorite)
		{
			Notification notification = new Notification
			{
				UserId = userIdLong,
				Notify_Id = userfavouriteProfile.LikedId,
				Notify_Message = "Interest request from " + profiledetails.Name,
				CreatedOn = DateTime.UtcNow,
				ModifiedOn = DateTime.UtcNow
			};
			await _notificationRepo.Add(notification);
			Notification senderNotification = new Notification
			{
				UserId = userfavouriteProfile.LikedId,
				Notify_Id = userIdLong,
				Notify_Message = "Interest sent to " + targetProfile.Name + " — Pending",
				CreatedOn = DateTime.UtcNow,
				ModifiedOn = DateTime.UtcNow
			};
			await _notificationRepo.Add(senderNotification);
		}
		await _userFavouriteProfileRepo.SaveChanges();
		return Json(new
		{
			success = isFavorite,
			message = message
		});
	}

	[HttpPost("/AcceptInterest")]
	public async Task<IActionResult> AcceptInterest([FromForm] long LikedId)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		UserFavouriteProfile interest = await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => x.UserId == LikedId && x.LikedId == userIdLong);
		if (interest == null)
		{
			return Json(new
			{
				success = false,
				message = "Interest request not found."
			});
		}
		interest.Status = InterestStatus.Accepted;
		_userFavouriteProfileRepo.Update(interest);
		await _userFavouriteProfileRepo.SaveChanges();
		RegistrationDto receiverProfile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
		Notification notification = new Notification
		{
			UserId = userIdLong,
			Notify_Id = LikedId,
			Notify_Message = "Your interest has been accepted by " + receiverProfile.Name,
			CreatedOn = DateTime.UtcNow,
			ModifiedOn = DateTime.UtcNow
		};
		await _notificationRepo.Add(notification);
		await _notificationRepo.SaveChanges();
		return Json(new
		{
			success = true,
			interestStatus = "Accepted"
		});
	}

	[HttpPost("/DeclineInterest")]
	public async Task<IActionResult> DeclineInterest([FromForm] long LikedId)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		UserFavouriteProfile interest = await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => x.UserId == LikedId && x.LikedId == userIdLong);
		if (interest == null)
		{
			return Json(new
			{
				success = false,
				message = "Interest request not found."
			});
		}
		interest.Status = InterestStatus.Declined;
		_userFavouriteProfileRepo.Update(interest);
		await _userFavouriteProfileRepo.SaveChanges();
		RegistrationDto receiverProfile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
		Notification notification = new Notification
		{
			UserId = userIdLong,
			Notify_Id = LikedId,
			Notify_Message = "Your interest has been declined by " + receiverProfile.Name,
			CreatedOn = DateTime.UtcNow,
			ModifiedOn = DateTime.UtcNow
		};
		await _notificationRepo.Add(notification);
		await _notificationRepo.SaveChanges();
		return Json(new
		{
			success = true,
			interestStatus = "Declined"
		});
	}

	[HttpPost("/report-profile")]
	public async Task<IActionResult> ReportProfile([FromForm] UserReportDto userReportDto)
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			return Json(new
			{
				success = false,
				message = "User ID not found in cookies."
			});
		}
		try
		{
			long reporterId = userIdValue.Value;
			Registration registration = await _registrationRepo.Get(reporterId);
			if (registration == null)
			{
				return Json(new
				{
					success = false,
					message = "User not found."
				});
			}
			bool isPremium = registration.IsPremiumMember || registration.IsSpecialRequest;
			if (!isPremium)
			{
				isPremium = await _userService.IsPremiumUser(reporterId);
			}
			if (!isPremium)
			{
				return Json(new
				{
					success = false,
					message = "Only premium members can report profiles. Please upgrade your plan."
				});
			}
			if (!(await _userService.AreUserContactDetailsUnlocked(reporterId, userReportDto.ReportedUserId)))
			{
				return Json(new
				{
					success = false,
					message = "You must view the contact details before you can report this profile."
				});
			}
			if (await _userReportRepository.FirstOrDefaultActive((UserReport x) => x.ReporterUserId == reporterId && x.ReportedUserId == userReportDto.ReportedUserId) != null)
			{
				return Json(new
				{
					success = false,
					message = "You have already reported this profile."
				});
			}
			userReportDto.ReporterUserId = reporterId;
			userReportDto.Status = UserReportStatus.Pending;
			UserReport userReport = _mapper.Map<UserReport>(userReportDto);
			await _userReportRepository.Add(userReport);
			await _userReportRepository.SaveChanges();
		}
		catch (Exception)
		{
			return Json(new
			{
				success = false,
				message = "Something went wrong, please try again later."
			});
		}
		return Json(new
		{
			success = true,
			message = "This user has been reported."
		});
	}

	[HttpGet("/MyProfile")]
	public async Task<IActionResult> MyProfile()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		if (profile == null)
		{
			return RedirectToAction("Dashboard");
		}
		IActionResult redirectResult = CheckRegistrationComplete(profile);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		if (!profile.IsPremiumMember)
		{
			profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		}
		ImagesDto image = _mapper.Map<ImagesDto>(await _imagesRepo.FirstOrDefaultActive(x => x.UserId == profile.Id));
		Transaction transaction = await _transactionRepository.GetTransactionByUserIdAsync(profile.Id);
		HomeViewModel homeViewModel = new HomeViewModel
		{
			Registrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive(x => x.ShowOnHomePage)),
			ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive()),
			Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive()),
			MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
			Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive()),
			MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive()),
			Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0)),
			Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId != 0)),
			Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
			Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive()),
			FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive()),
			Registration = profile,
			Images = image ?? new ImagesDto(),
			Transaction = _mapper.Map<Transaction>(transaction)
		};
		return View(homeViewModel);
	}

	[HttpGet("/profile-verification")]
	public async Task<IActionResult> ProfileVerification()
	{
		string userIdStr = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userIdStr))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		long userId = Convert.ToInt64(userIdStr);
		Registration user = await _registrationRepo.Get(userId);
		if (user == null || user.IsDeleted)
		{
			return RedirectToAction("Dashboard");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(user);
		IActionResult redirectResult = CheckRegistrationComplete(profile);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		List<VerificationDocument> docs = (from x in await _verificationDocRepo.WhereActive((VerificationDocument x) => x.ProfileId == userId)
			orderby x.DisplayOrder, x.CreatedOn
			select x).ToList();
		FollowUp followUp = (await _followUpRepo.WhereActive((FollowUp f) => f.ProfileId == userId && (int)f.FollowUpType == 0)).FirstOrDefault();
		UserProfileVerificationViewModel model = new UserProfileVerificationViewModel
		{
			UserId = userId,
			Registration = profile,
			Documents = docs,
			DocumentVerificationEnabled = user.DocumentVerificationEnabled,
			DocumentVerificationComplete = user.DocumentVerificationComplete,
			DocumentVerificationRejected = user.DocumentVerificationRejected,
			DocumentVerificationFollowupApproved = user.DocumentVerificationFollowupApproved,
			FollowupStatus = followUp?.LatestProfileVerificationStatus?.ToString(),
			LatestRemarks = followUp?.LatestRemarks
		};
		return View(model);
	}

	[HttpPost("/User/UploadVerificationDocument")]
	public async Task<IActionResult> UploadVerificationDocument(IFormFile verificationDocument, string? documentType = null, long? replaceDocumentId = null)
	{
		string userIdStr = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userIdStr))
		{
			return Json(new
			{
				success = false,
				message = "User session expired. Please log in again."
			});
		}
		if (verificationDocument == null || verificationDocument.Length == 0L)
		{
			return Json(new
			{
				success = false,
				message = "Please select a valid document file to upload."
			});
		}
		long userId = Convert.ToInt64(userIdStr);
		Registration user = await _registrationRepo.Get(userId);
		if (user == null || user.IsDeleted)
		{
			return Json(new
			{
				success = false,
				message = "User profile not found."
			});
		}
		string relativePath = await _fileService.UploadFile(verificationDocument, "Uploads/VerificationDocuments");
		if (replaceDocumentId.HasValue && replaceDocumentId.Value > 0)
		{
			VerificationDocument existingDoc = await _verificationDocRepo.Get(replaceDocumentId.Value);
			if (existingDoc != null && existingDoc.ProfileId == userId && !existingDoc.IsDeleted)
			{
				existingDoc.DocumentUrl = relativePath;
				existingDoc.OriginalFileName = verificationDocument.FileName;
				existingDoc.ModifiedOn = DateTime.UtcNow;
				await _verificationDocRepo.Update(existingDoc);
			}
		}
		else
		{
			List<VerificationDocument> existingDocs = (await _verificationDocRepo.WhereActive((VerificationDocument d) => d.ProfileId == userId)).ToList();
			int nextIndex = existingDocs.Count + 1;
			string typeLabel = ((!string.IsNullOrWhiteSpace(documentType)) ? documentType : $"Document {nextIndex}");
			VerificationDocument newDoc = new VerificationDocument
			{
				ProfileId = userId,
				DocumentUrl = relativePath,
				DocumentType = typeLabel,
				OriginalFileName = verificationDocument.FileName,
				DisplayOrder = nextIndex,
				IsActive = true
			};
			await _verificationDocRepo.Add(newDoc);
		}
		await _verificationDocRepo.SaveChanges();
		user.VerificationDocumentUrl = relativePath;
		user.DocumentVerificationEnabled = true;
		user.DocumentVerificationRejected = false;
		user.DocumentVerificationComplete = false;
		await _registrationRepo.Update(user);
		await _registrationRepo.SaveChanges();
		FollowUp existingFollowUp = (await _followUpRepo.WhereActive((FollowUp f) => f.ProfileId == userId && (int)f.FollowUpType == 0)).FirstOrDefault();
		if (existingFollowUp == null)
		{
			FollowUp newFollowUp = new FollowUp
			{
				ProfileId = userId,
				FollowUpType = FollowUpType.ProfileVerification,
				LatestProfileVerificationStatus = ProfileVerificationStatus.DetailedVerifyRequest,
				LatestRemarks = "User uploaded verification document(s) directly from Profile Verification page.",
				AssignedStaffId = null,
				IsActive = true
			};
			await _followUpRepo.Add(newFollowUp);
			await _followUpRepo.SaveChanges();
			FollowUpTimeline timeline = new FollowUpTimeline
			{
				FollowUpId = newFollowUp.Id,
				StaffId = 0L,
				StaffName = "User (Direct Upload)",
				ProfileVerificationStatus = ProfileVerificationStatus.DetailedVerifyRequest,
				Remarks = "User submitted verification document(s) for review.",
				IsActive = true
			};
			await _followUpTimelineRepo.Add(timeline);
			await _followUpTimelineRepo.SaveChanges();
		}
		else if (existingFollowUp.LatestProfileVerificationStatus == ProfileVerificationStatus.Pending || existingFollowUp.LatestProfileVerificationStatus == ProfileVerificationStatus.Hold || !existingFollowUp.LatestProfileVerificationStatus.HasValue)
		{
			existingFollowUp.LatestProfileVerificationStatus = ProfileVerificationStatus.DetailedVerifyRequest;
			existingFollowUp.LatestRemarks = "User re-uploaded/submitted verification document(s).";
			await _followUpRepo.Update(existingFollowUp);
			await _followUpRepo.SaveChanges();
			FollowUpTimeline timeline = new FollowUpTimeline
			{
				FollowUpId = existingFollowUp.Id,
				StaffId = existingFollowUp.AssignedStaffId.GetValueOrDefault(),
				StaffName = "User (Direct Upload)",
				ProfileVerificationStatus = ProfileVerificationStatus.DetailedVerifyRequest,
				Remarks = "User uploaded/updated verification document(s).",
				IsActive = true
			};
			await _followUpTimelineRepo.Add(timeline);
			await _followUpTimelineRepo.SaveChanges();
		}
		return Json(new
		{
			success = true,
			message = "Verification document uploaded successfully.",
			verificationDocumentUrl = relativePath
		});
	}

	[HttpPost("/User/DeleteVerificationDocument")]
	public async Task<IActionResult> DeleteVerificationDocument([FromBody] DeleteDocRequest model)
	{
		string userIdStr = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userIdStr))
		{
			return Json(new
			{
				success = false,
				message = "User session expired."
			});
		}
		if (model == null || model.DocumentId <= 0)
		{
			return Json(new
			{
				success = false,
				message = "Invalid document ID."
			});
		}
		long userId = Convert.ToInt64(userIdStr);
		VerificationDocument doc = await _verificationDocRepo.Get(model.DocumentId);
		if (doc == null || doc.ProfileId != userId || doc.IsDeleted)
		{
			return Json(new
			{
				success = false,
				message = "Document not found."
			});
		}
		await _verificationDocRepo.SoftDelete(doc);
		await _verificationDocRepo.SaveChanges();
		IReadOnlyList<VerificationDocument> remainingDocs = await _verificationDocRepo.WhereActive((VerificationDocument d) => d.ProfileId == userId);
		Registration user = await _registrationRepo.Get(userId);
		if (user != null)
		{
			user.VerificationDocumentUrl = remainingDocs.OrderByDescending((VerificationDocument d) => d.CreatedOn).FirstOrDefault()?.DocumentUrl;
			await _registrationRepo.Update(user);
			await _registrationRepo.SaveChanges();
		}
		return Json(new
		{
			success = true,
			message = "Document deleted successfully."
		});
	}

	[HttpPost("/User/SaveCroppedVerificationDocument")]
	public async Task<IActionResult> SaveCroppedVerificationDocument(IFormFile croppedImage, long documentId)
	{
		string userIdStr = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userIdStr))
		{
			return Json(new
			{
				success = false,
				message = "User session expired."
			});
		}
		if (croppedImage == null || croppedImage.Length == 0L)
		{
			return Json(new
			{
				success = false,
				message = "No cropped image received."
			});
		}
		long userId = Convert.ToInt64(userIdStr);
		VerificationDocument doc = await _verificationDocRepo.Get(documentId);
		if (doc == null || doc.ProfileId != userId || doc.IsDeleted)
		{
			return Json(new
			{
				success = false,
				message = "Document not found."
			});
		}
		string relativePath = (doc.DocumentUrl = await _fileService.UploadFile(croppedImage, "Uploads/VerificationDocuments"));
		doc.ModifiedOn = DateTime.UtcNow;
		await _verificationDocRepo.Update(doc);
		await _verificationDocRepo.SaveChanges();
		Registration user = await _registrationRepo.Get(userId);
		if (user != null)
		{
			user.VerificationDocumentUrl = relativePath;
			await _registrationRepo.Update(user);
			await _registrationRepo.SaveChanges();
		}
		return Json(new
		{
			success = true,
			message = "Cropped document saved successfully.",
			documentUrl = relativePath
		});
	}

	[HttpGet("/details/{*url}")]
	public async Task<IActionResult> Details(long url)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		if (url < 0)
		{
			return RedirectToAction("Dashboard");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Id == url));
		RegistrationDto loggedInUser;
		if (profile == null)
		{
			if (await _registrationRepo.GetWithDeleted(url) != null)
			{
				loggedInUser = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
				if (!loggedInUser.IsPremiumMember)
				{
					loggedInUser.IsPremiumMember = await _userService.IsPremiumUser(loggedInUser.Id);
				}
				return View("ProfileUnavailable", new HomeViewModel
				{
					Registration = loggedInUser
				});
			}
			return RedirectToAction("Dashboard");
		}
		long userIdLong = Convert.ToInt64(userId);
		if (profile.Id != userIdLong && !profile.IsVisible)
		{
			loggedInUser = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
			if (!loggedInUser.IsPremiumMember)
			{
				loggedInUser.IsPremiumMember = await _userService.IsPremiumUser(loggedInUser.Id);
			}
			return View("ProfileUnavailable", new HomeViewModel
			{
				Registration = loggedInUser
			});
		}
		profile = await MapStarredProfiles(profile);
		profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		ImagesDto image = _mapper.Map<ImagesDto>(await _imagesRepo.FirstOrDefaultActive(x => x.UserId == profile.Id));
		HomeViewModel homeViewModel = new HomeViewModel
		{
			Registrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive(x => x.ShowOnHomePage)),
			ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive()),
			Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive()),
			MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
			Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive()),
			MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive()),
			Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0)),
			Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId != 0)),
			Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
			Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive()),
			FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive()),
			Registration = profile,
			Images = image ?? new ImagesDto()
		};
		return View(homeViewModel);
	}

	[HttpGet("/notifications")]
	public async Task<IActionResult> Notification()
	{
		string userId = GetCurrentUserId()?.ToString();
		RegistrationDto loggedInUserprofile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		IActionResult redirectResult = CheckRegistrationComplete(loggedInUserprofile);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		loggedInUserprofile.IsPremiumMember = await _userService.IsPremiumUser(loggedInUserprofile.Id);
		long userIdLong = Convert.ToInt64(userId);
		IReadOnlyList<Notification> unreadNotifications = await _notificationRepo.Where((Notification x) => x.Notify_Id == userIdLong && x.Is_Read == 0);
		if (unreadNotifications.Any())
		{
			foreach (Notification notif in unreadNotifications)
			{
				notif.Is_Read = 1;
				await _notificationRepo.Update(notif);
			}
			await _notificationRepo.SaveChanges();
		}
		List<Notification> notifications = await _notificationPageRepo.GetNotificationsBasedOnNotifyId(Convert.ToInt64(userId));
		List<long> profileIds = notifications.Select((Notification m) => m.UserId).Distinct().ToList();
		List<Registration> UserProfiles = await _registrationRepo.GetAllByIds(profileIds);
		List<RegistrationDto> allRegistrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive((Registration x) => x.IsActive));
		NotificationViewModel _notificationViewModel = new NotificationViewModel();
		IReadOnlyList<UserFavouriteProfile> userFavourites = await _userFavouriteProfileRepo.WhereActive((UserFavouriteProfile x) => x.UserId == loggedInUserprofile.Id || x.LikedId == loggedInUserprofile.Id);
		IReadOnlyList<PhotoUnlockRequest> photoUnlocks = await _photoUnlockRequestRepo.WhereActive((PhotoUnlockRequest x) => x.RequesterId == loggedInUserprofile.Id || x.OwnerId == loggedInUserprofile.Id);
		List<NotificationDto> notificationList = new List<NotificationDto>();
		foreach (Notification notification in notifications)
		{
			Registration userProfile = UserProfiles.FirstOrDefault((Registration profile) => profile.Id == notification.UserId);
			UserFavouriteProfile favToUs = userFavourites.FirstOrDefault((UserFavouriteProfile x) => x.UserId == notification.UserId && x.LikedId == loggedInUserprofile.Id);
			UserFavouriteProfile favFromUs = userFavourites.FirstOrDefault((UserFavouriteProfile x) => x.UserId == loggedInUserprofile.Id && x.LikedId == notification.UserId);
			string interestStatus = null;
			long? interestRecordId = null;
			if ((notification.Notify_Message == null || !notification.Notify_Message.Contains("viewed your profile")) && (notification.Notify_Message == null || !notification.Notify_Message.Contains("success story", StringComparison.OrdinalIgnoreCase)) && (notification.Notify_Message == null || !notification.Notify_Message.Contains("shortlisted by")))
			{
				if (notification.Notify_Message != null && (notification.Notify_Message.Contains("Photo unlock request") || notification.Notify_Message.Contains("photo unlock request")))
				{
					PhotoUnlockRequest photoReq = photoUnlocks.FirstOrDefault((PhotoUnlockRequest x) => (x.RequesterId == notification.UserId && x.OwnerId == loggedInUserprofile.Id) || (x.RequesterId == loggedInUserprofile.Id && x.OwnerId == notification.UserId));
					if (photoReq != null)
					{
						interestStatus = ((photoReq.Status == PhotoUnlockStatus.Approved) ? "Accepted" : ((photoReq.Status == PhotoUnlockStatus.Rejected) ? "Declined" : "Pending"));
						interestRecordId = photoReq.Id;
					}
				}
				else if (favToUs != null)
				{
					interestStatus = favToUs.Status.ToString();
					interestRecordId = favToUs.Id;
				}
				else if (favFromUs != null)
				{
					interestStatus = favFromUs.Status.ToString();
					interestRecordId = favFromUs.Id;
				}
			}
			NotificationDto dto = new NotificationDto
			{
				Id = notification.Id,
				Notify_Message = notification.Notify_Message,
				UserId = notification.UserId,
				Notify_Id = notification.Notify_Id,
				Is_Read = notification.Is_Read,
				CreatedOn = notification.CreatedOn,
				profileImageUrl = userProfile?.ImagePath,
				likedByUserName = userProfile?.Name,
				Gender = userProfile?.Gender,
				InterestStatus = interestStatus,
				InterestRecordId = interestRecordId
			};
			if (notification.Notify_Message != null && notification.Notify_Message.StartsWith("[STATUS_UPDATE_PROMPT]"))
			{
				dto.IsStatusUpdatePrompt = true;
				dto.StatusUpdatePartnerId = notification.UserId;
				dto.Notify_Message = notification.Notify_Message.Replace("[STATUS_UPDATE_PROMPT] ", "").Replace("[STATUS_UPDATE_PROMPT]", "");
				UserFavouriteProfile favRecord = userFavourites.FirstOrDefault((UserFavouriteProfile x) => (x.UserId == loggedInUserprofile.Id && x.LikedId == notification.UserId) || (x.UserId == notification.UserId && x.LikedId == loggedInUserprofile.Id));
				if (favRecord != null)
				{
					dto.StatusUpdateFavId = favRecord.Id;
					MatchStatusUpdate matchStatus = (await _matchStatusUpdateRepo.WhereActive((MatchStatusUpdate x) => x.UserId == loggedInUserprofile.Id && x.UserFavouriteProfileId == favRecord.Id)).FirstOrDefault();
					if (matchStatus != null)
					{
						dto.CurrentMatchStatus = (int)matchStatus.Status;
					}
				}
				SuccessStory existingStory = (await _successStoryRepo.WhereActive((SuccessStory x) => (x.SubmittedByUserId == loggedInUserprofile.Id && x.PartnerUserId == notification.UserId) || (x.SubmittedByUserId == notification.UserId && x.PartnerUserId == loggedInUserprofile.Id))).FirstOrDefault();
				dto.HasSubmittedSuccessStory = existingStory != null;
			}
			notificationList.Add(dto);
		}
		_notificationViewModel.notificationDto = notificationList;
		_notificationViewModel.Registration = loggedInUserprofile;
		_notificationViewModel.AllRegistrations = allRegistrations;
		return View("Notification", _notificationViewModel);
	}

	[HttpGet("/api/notifications/unread-count")]
	public async Task<IActionResult> GetUnreadNotificationCount()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				unreadCount = 0
			});
		}
		long userIdLong = Convert.ToInt64(userId);
		int unreadCount = (await _notificationRepo.Where((Notification x) => x.Notify_Id == userIdLong && x.Is_Read == 0)).Count();
		return Json(new { unreadCount });
	}

	[HttpGet("/profile-suspended")]
	public IActionResult ProfileSuspended()
	{
		return View("ProfileSuspended");
	}

	[HttpGet("/help-and-support")]
	public IActionResult HelpAndSupport()
	{
		return View("HelpAndSupport");
	}

	[HttpGet("/SearchProfile")]
	public async Task<IActionResult> SearchProfile()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		if (profile == null)
		{
			return RedirectToAction("Dashboard");
		}
		IActionResult redirectResult = CheckRegistrationComplete(profile);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		SearchViewModel searchViewModel = new SearchViewModel
		{
			Registration = profile,
			MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
			Districts = _mapper.Map<List<DistrictDto>>(await _districtRepository.GetAllActive()),
			Cities = _mapper.Map<List<CityDto>>(await _cityRepository.GetAllActive())
		};
		return View(searchViewModel);
	}

	[HttpGet("/SearchById")]
	public async Task<IActionResult> SearchById()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		if (profile == null || !profile.IsVisible)
		{
			return RedirectToAction("Dashboard");
		}
		IActionResult redirectResult = CheckRegistrationComplete(profile);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		SearchViewModel searchViewModel = new SearchViewModel
		{
			Registration = profile,
			MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive())
		};
		return View(searchViewModel);
	}

	[HttpGet("/SearchProfileResults")]
	public async Task<IActionResult> SearchProfileResults(SearchViewModel searchViewModel)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		long userIdLong = Convert.ToInt64(userId);
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
		profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		Task<IReadOnlyList<Registration>> activeProfilesTask = _registrationRepo.WhereActive(x => x.IsComplete == true && x.Gender != profile.Gender && x.IsVisible == true);
		Task<IReadOnlyList<BodyFeatures>> bodyFeaturesTask = _bodyFeaturesRepo.GetAllActive();
		await Task.WhenAll(activeProfilesTask, bodyFeaturesTask);
		List<RegistrationDto> allActiveProfiles = _mapper.Map<List<RegistrationDto>>(await activeProfilesTask);
		List<BodyFeaturesDto> allBodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await bodyFeaturesTask);
		bool viewerIsPremium = profile.IsPremiumMember;
		HashSet<long> acceptedInterestIds = new HashSet<long>((await _userFavouriteProfileRepo.WhereActive((UserFavouriteProfile x) => (x.UserId == userIdLong || x.LikedId == userIdLong) && (int)x.Status == 1)).Select((UserFavouriteProfile x) => (x.UserId != userIdLong) ? x.UserId : x.LikedId));
		IReadOnlyList<PhotoUnlockRequest> unlockRequests = await _photoUnlockRequestRepo.WhereActive((PhotoUnlockRequest x) => x.RequesterId == userIdLong);
		Dictionary<long, PhotoUnlockStatus> unlockRequestDict = new Dictionary<long, PhotoUnlockStatus>();
		foreach (PhotoUnlockRequest req in unlockRequests)
		{
			unlockRequestDict[req.OwnerId] = req.Status;
		}
		Dictionary<long, MatchingProfilesResponseDto> matchingProfilesDict = (from matchingProfilesResponseDto in await _matchingProfilesResponseDtoRepo.GetMatchingUsersWithPercentage(Convert.ToInt32(userIdLong))
			group matchingProfilesResponseDto by matchingProfilesResponseDto.Id).ToDictionary((IGrouping<long, MatchingProfilesResponseDto> g) => g.Key, (IGrouping<long, MatchingProfilesResponseDto> g) => g.Last());
		List<RegistrationDto> list = new List<RegistrationDto>();
		if (allActiveProfiles != null && allActiveProfiles.Count > 0)
		{
			foreach (RegistrationDto item in allActiveProfiles)
			{
				RegistrationDto partnerProfile = _mapper.Map<RegistrationDto>(item);
				if (partnerProfile == null)
				{
					continue;
				}
				DateTime dob = ConvertStringToDateTime(partnerProfile);
				int partnerAge = CalculateAge(dob);
				string heightInString = (from x in allBodyFeatures
					where x.Id == partnerProfile.HeightId
					select x.Title).FirstOrDefault().ToString();
				int partnerHeight = heightInInt(heightInString);
				if (partnerProfile == null)
				{
					continue;
				}
				bool matches = string.IsNullOrWhiteSpace(searchViewModel.ProfileId) || !(searchViewModel.ProfileId.ToLower().Trim() != partnerProfile.RegisterNumber.ToLower().Trim());
				if (!string.IsNullOrWhiteSpace(searchViewModel.SearchQuery))
				{
					string query = searchViewModel.SearchQuery.ToLower().Trim();
					if (!partnerProfile.RegisterNumber.ToLower().Contains(query) && !partnerProfile.Name.ToLower().Contains(query))
					{
						matches = false;
					}
				}
				if (searchViewModel.AgeFrom.HasValue && partnerAge <= searchViewModel.AgeFrom)
				{
					matches = false;
				}
				if (searchViewModel.AgeTo.HasValue && partnerAge >= searchViewModel.AgeTo)
				{
					matches = false;
				}
				if (searchViewModel.HeightFrom.HasValue && partnerHeight <= searchViewModel.HeightFrom)
				{
					matches = false;
				}
				if (searchViewModel.HeightTo.HasValue && partnerHeight >= searchViewModel.HeightTo)
				{
					matches = false;
				}
				if (searchViewModel.MaritalStatusId.HasValue && partnerProfile.MaritalStatusId != searchViewModel.MaritalStatusId)
				{
					matches = false;
				}
				if (searchViewModel.SelectedMaritalStatusIds != null && searchViewModel.SelectedMaritalStatusIds.Any() && !Enumerable.Contains(searchViewModel.SelectedMaritalStatusIds, partnerProfile.MaritalStatusId))
				{
					matches = false;
				}
				if (searchViewModel.CommunityId.HasValue && partnerProfile.CommunityId != searchViewModel.CommunityId)
				{
					matches = false;
				}
				if (!string.IsNullOrWhiteSpace(searchViewModel.HighestEducationTitle) && partnerProfile.HighestEducation?.ToLower().Trim() != searchViewModel.HighestEducationTitle.ToLower().Trim())
				{
					matches = false;
				}
				if (!string.IsNullOrWhiteSpace(searchViewModel.district) && partnerProfile.District?.ToLower().Trim() != searchViewModel.district.ToLower().Trim())
				{
					matches = false;
				}
				if (searchViewModel.SelectedDistricts != null && searchViewModel.SelectedDistricts.Any() && !searchViewModel.SelectedDistricts.Any((string d) => d.Equals(partnerProfile.District, StringComparison.OrdinalIgnoreCase)))
				{
					matches = false;
				}
				if (searchViewModel.SelectedBodyFeaturesIds != null && searchViewModel.SelectedBodyFeaturesIds.Any() && !searchViewModel.SelectedBodyFeaturesIds.Contains(partnerProfile.BodyTypeId))
				{
					matches = false;
				}
				if (!matches)
				{
					continue;
				}
				RegistrationDto starredlikedPartnerProfile = await MapLikedProfiles(await MapStarredProfiles(partnerProfile));
				bool partnerPhotosUnlocked = false;
				if (userIdLong == starredlikedPartnerProfile.Id)
				{
					partnerPhotosUnlocked = true;
				}
				else if (starredlikedPartnerProfile.PhotoVisibleToAll)
				{
					partnerPhotosUnlocked = true;
				}
				else
				{
					if (starredlikedPartnerProfile.PhotoVisibleToPremium & viewerIsPremium)
					{
						partnerPhotosUnlocked = true;
					}
					if (!partnerPhotosUnlocked && starredlikedPartnerProfile.PhotoVisibleToAccepted && acceptedInterestIds.Contains(starredlikedPartnerProfile.Id))
					{
						partnerPhotosUnlocked = true;
					}
					if (!partnerPhotosUnlocked && unlockRequestDict.TryGetValue(starredlikedPartnerProfile.Id, out var reqStatus) && reqStatus == PhotoUnlockStatus.Approved)
					{
						partnerPhotosUnlocked = true;
					}
				}
				int partnerUnlockStatus = -1;
				if (unlockRequestDict.TryGetValue(starredlikedPartnerProfile.Id, out var status))
				{
					partnerUnlockStatus = (int)status;
				}
				starredlikedPartnerProfile.PhotosUnlocked = partnerPhotosUnlocked;
				starredlikedPartnerProfile.PhotoUnlockRequestStatus = partnerUnlockStatus;
				if (matchingProfilesDict.TryGetValue(starredlikedPartnerProfile.Id, out var mp))
				{
					starredlikedPartnerProfile.total_matching_score = mp.total_matching_score;
				}
				list.Add(starredlikedPartnerProfile);
			}
		}
		if (!string.IsNullOrWhiteSpace(searchViewModel.SearchQuery))
		{
			string query = searchViewModel.SearchQuery.ToLower().Trim();
			list = list.OrderByDescending(p =>
			{
				string name = p.Name?.ToLower() ?? "";
				string regNumber = p.RegisterNumber?.ToLower() ?? "";
				if (regNumber == query)
				{
					return 100;
				}
				if (regNumber.StartsWith(query))
				{
					return 90;
				}
				if (name == query)
				{
					return 80;
				}
				if (name.StartsWith(query))
				{
					return 70;
				}
				return name.Contains(" " + query) ? 60 : 50;
			}).ToList();
		}
		ViewBag.SearchQuery = searchViewModel.SearchQuery;
		UserDashboardViewModel userDashboardViewModel = new UserDashboardViewModel
		{
			Registration = profile,
			RegistrationList = list,
			Profession = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive()),
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
			ReligionCaste = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0)),
			MaritalStatus = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			Community = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
			Districts = _mapper.Map<List<DistrictDto>>(await _districtRepository.GetAllActive()),
			SearchViewModel = searchViewModel
		};
		return View(userDashboardViewModel);
	}

	[HttpGet("/NoSearchResult")]
	public async Task<IActionResult> NoSearchResult()
	{
		return View();
	}

	[HttpGet("/change-password")]
	public async Task<IActionResult> ChangePassword()
	{
		string userId = GetCurrentUserId()?.ToString();
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		if (profile == null)
		{
			return RedirectToAction("Dashboard");
		}
		profile.IsPremiumMember = await _userService.IsPremiumUser(profile.Id);
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		return View(new ChangePasswordViewModel
		{
			Registration = profile
		});
	}

	[HttpPost("/change-password")]
	public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
		Registration user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Id == Convert.ToInt64(userId));
		model.Registration = _mapper.Map<RegistrationDto>(user);
		model.Registration.IsPremiumMember = await _userService.IsPremiumUser(user.Id);
		if (model.OldPassword != encryption.DecryptRijndael(user.Password, user.PasswordHash))
		{
			ModelState.AddModelError("error", "Incorrect old password");
			return View(model);
		}
		if (model.NewPassword != model.ConfirmPassword)
		{
			ModelState.AddModelError("error", "Password and confirm password are not matching");
			return View(model);
		}
		user.PasswordHash = encryption.CreateSalt();
		user.Password = encryption.EncryptRijndael(model.ConfirmPassword, user.PasswordHash);
		await _registrationRepo.Update(user);
		await _registrationRepo.SaveChanges();
		ModelState.AddModelError("error", "Password changed successfully");
		return View(model);
	}

	[HttpGet("/edit-profile")]
	public async Task<IActionResult> Edit()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RegistrationDto user = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		IActionResult redirectResult = CheckRegistrationComplete(user);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		ImagesDto image = _mapper.Map<ImagesDto>(await _imagesRepo.FirstOrDefaultActive((Images x) => x.UserId == Convert.ToInt64(userId)));
		HomeViewModel homeViewModel = new HomeViewModel
		{
			Registrations = _mapper.Map<List<RegistrationDto>>(await _registrationRepo.WhereActive(x => x.ShowOnHomePage)),
			ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAllActive()),
			MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAllActive()),
			BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAllActive()),
			Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAllActive()),
			MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAllActive()),
			Religions = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId == 0)),
			Castes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.WhereActive(x => x.ParentId != 0)),
			Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAllActive()),
			Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAllActive()),
			FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatus.GetAllActive()),
			Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAllActive()),
			States = _mapper.Map<List<StateDto>>(await _stateRepository.GetAllActive()),
			Districts = _mapper.Map<List<DistrictDto>>(await _districtRepository.GetAllActive()),
			Cities = _mapper.Map<List<CityDto>>(await _cityRepository.GetAllActive()),
			Registration = user,
			Images = image ?? new ImagesDto()
		};
		return View(homeViewModel);
	}

	[HttpPost("/edit-profile")]
	public async Task<IActionResult> Edit(RegistrationDto model, string? section = null)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			if (!string.IsNullOrEmpty(section))
			{
				return Json(new
				{
					success = false,
					message = "Session expired. Please log in again."
				});
			}
			return RedirectToAction("Login");
		}
		Registration user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Id == Convert.ToInt64(userId));
		if (user == null)
		{
			if (!string.IsNullOrEmpty(section))
			{
				return Json(new
				{
					success = false,
					message = "User not found."
				});
			}
			return RedirectToAction("Login");
		}
		if (string.IsNullOrEmpty(section))
		{
			user.HeightId = model.HeightId;
			user.WeightId = model.WeightId;
			user.ComplexionId = model.ComplexionId;
			user.BodyTypeId = model.BodyTypeId;
			user.IsPhysicallyChallenged = model.IsPhysicallyChallenged;
			user.PhysicallyChallengedDetail = (model.IsPhysicallyChallenged ? model.PhysicallyChallengedDetail : null);
			user.HighestEducation = model.HighestEducation;
			user.EducationType = model.EducationType;
			user.ProfessionId = model.ProfessionId;
			user.ProfessionType = model.ProfessionType;
			user.MotherTongueId = model.MotherTongueId;
			user.CommunityId = model.CommunityId;
			user.ReligiousnessId = model.ReligiousnessId;
			user.FinancialStatusId = model.FinancialStatusId;
			user.LandlineNumber = model.LandlineNumber;
			user.PresentCountry = model.PresentCountry;
			user.About = model.About;
			user.MaritalStatusId = model.MaritalStatusId;
			user.NumberOfChildrens = model.MaritalStatusId == 1 ? null : model.NumberOfChildrens;
		}
		else
		{
			switch (section.ToLower())
			{
			case "basic":
				user.MaritalStatusId = model.MaritalStatusId;
				user.MotherTongueId = model.MotherTongueId;
				user.About = model.About;
				user.NumberOfChildrens = model.MaritalStatusId == 1 ? null : model.NumberOfChildrens;
				break;
			case "physical":
				user.HeightId = model.HeightId;
				user.WeightId = model.WeightId;
				user.ComplexionId = model.ComplexionId;
				user.BodyTypeId = model.BodyTypeId;
				user.IsPhysicallyChallenged = model.IsPhysicallyChallenged;
				user.PhysicallyChallengedDetail = (model.IsPhysicallyChallenged ? model.PhysicallyChallengedDetail : null);
				break;
			case "education":
				user.HighestEducation = model.HighestEducation;
				user.EducationType = model.EducationType;
				user.ProfessionId = model.ProfessionId;
				user.ProfessionType = model.ProfessionType;
				break;
			case "religious":
				user.CommunityId = model.CommunityId;
				user.ReligiousnessId = model.ReligiousnessId;
				break;
			case "location":
				user.PresentCountry = model.PresentCountry;
				user.LandlineNumber = model.LandlineNumber;
				break;
			case "family":
				user.FinancialStatusId = model.FinancialStatusId;
				break;
			default:
				return Json(new
				{
					success = false,
					message = "Invalid section requested."
				});
			}
		}
		await _registrationRepo.Update(user);
		await _registrationRepo.SaveChanges();
		if (!string.IsNullOrEmpty(section))
		{
			return Json(new
			{
				success = true,
				message = "Profile section updated successfully!"
			});
		}
		return RedirectToAction("Edit");
	}

	[HttpGet("/edit-image")]
	public async Task<IActionResult> Image()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		long userIdLong = Convert.ToInt64(userId);
		Registration user = await _registrationRepo.Get(Convert.ToInt64(userId));
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		if (profile == null)
		{
			return RedirectToAction("Dashboard");
		}
		IActionResult redirectResult = CheckRegistrationComplete(profile);
		if (redirectResult != null)
		{
			return redirectResult;
		}
		Images image = await _imagesRepo.FirstOrDefault((Images x) => x.UserId == Convert.ToInt64(userId));
		UserImageEditViewModel model = new UserImageEditViewModel();
		model.ImagePath = user.ImagePath;
		model.UserId = userIdLong;
		model.Registration = profile;
		model.PhotoVisibleToAll = user.PhotoVisibleToAll;
		model.PhotoVisibleToPremium = user.PhotoVisibleToPremium;
		model.PhotoVisibleToAccepted = user.PhotoVisibleToAccepted;
		if (image != null)
		{
			model.Image1Path = image.Image1Path;
			model.Image2Path = image.Image2Path;
			model.Image3Path = image.Image3Path;
			model.Image4Path = image.Image4Path;
			model.Image5Path = image.Image5Path;
		}
		return View(model);
	}

	[HttpGet("/image/{userId}")]
	public async Task<IActionResult> GetImageById(long userId)
	{
		try
		{
			if (userId <= 0)
			{
				_logger.LogInformation("Invalid user ID");
				return RedirectToAction("Login");
			}
			Registration user = await _registrationRepo.Get(userId);
			if (user == null)
			{
				_logger.LogInformation("User not found");
				return RedirectToAction("Login");
			}
			Images image = await _imagesRepo.FirstOrDefault((Images x) => x.UserId == userId);
			UserImageEditViewModel model = new UserImageEditViewModel
			{
				ImagePath = user.ImagePath,
				Image1Path = image?.Image1Path,
				Image2Path = image?.Image2Path,
				Image3Path = image?.Image3Path,
				Image4Path = image?.Image4Path,
				Image5Path = image?.Image5Path,
				PhotoVisibleToAll = user.PhotoVisibleToAll,
				PhotoVisibleToPremium = user.PhotoVisibleToPremium,
				PhotoVisibleToAccepted = user.PhotoVisibleToAccepted
			};
			return Ok(model);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "An error occurred while updating profile image.");
			return StatusCode(500, "Internal server error");
		}
	}

	[HttpPost("/edit-image")]
	public async Task<IActionResult> Image(UserImageEditViewModel model)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		Registration user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Id == Convert.ToInt64(userId));
		Images image = await _imagesRepo.FirstOrDefault((Images x) => x.UserId == Convert.ToInt64(userId));
		ImagesDto imageDto = new ImagesDto
		{
			UserId = user.Id,
			Image1Path = model.Image1Path,
			Image1 = model.Image1,
			Image2Path = model.Image2Path,
			Image2 = model.Image2,
			Image3Path = model.Image3Path,
			Image3 = model.Image3,
			Image4Path = model.Image4Path,
			Image4 = model.Image4,
			Image5Path = model.Image5Path,
			Image5 = model.Image5
		};
		if (image == null)
		{
			Images data = new Images
			{
				UserId = user.Id,
				IsActive = true,
				IsDeleted = false
			};
			await _fileService.SaveAllFiles(data, imageDto, "Uploads/Registration");
			await _imagesRepo.Add(data);
			await _imagesRepo.SaveChanges();
		}
		else
		{
			await _fileService.SaveAllFiles(image, imageDto, "Uploads/Registration");
			await _imagesRepo.Update(image);
			await _imagesRepo.SaveChanges();
		}
		if (model.PhotoVisibleToAll.HasValue)
		{
			user.PhotoVisibleToAll = model.PhotoVisibleToAll.Value;
		}
		if (model.PhotoVisibleToPremium.HasValue)
		{
			user.PhotoVisibleToPremium = model.PhotoVisibleToPremium.Value;
		}
		if (model.PhotoVisibleToAccepted.HasValue)
		{
			user.PhotoVisibleToAccepted = model.PhotoVisibleToAccepted.Value;
		}
		RegistrationDto userDto = new RegistrationDto
		{
			Image = model.Image,
			ImagePath = model.ImagePath
		};
		await _fileService.SaveAllFiles(user, userDto, "Uploads/Registration");
		await _registrationRepo.Update(user);
		await _registrationRepo.SaveChanges();
		return RedirectToAction("Image");
	}

	[HttpDelete("/delete-image/{userId}")]
	public async Task<IActionResult> DeleteImage(long userId, [FromForm] bool deleteImage1, [FromForm] bool deleteImage2, [FromForm] bool deleteImage3, [FromForm] bool deleteImage4, [FromForm] bool deleteImage5)
	{
		if (userId == 0L)
		{
			return BadRequest("Invalid user ID");
		}
		if (await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Id == userId) == null)
		{
			return NotFound();
		}
		List<Images> userImages = (await _imagesRepo.GetAll()).Where((Images x) => x.UserId == userId).ToList();
		if (userImages == null || !userImages.Any())
		{
			return NotFound();
		}
		foreach (Images image in userImages)
		{
			await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", deleteImage1, deleteImage2, deleteImage3, deleteImage4, deleteImage5);
			if (deleteImage1)
			{
				image.Image1Path = null;
			}
			if (deleteImage2)
			{
				image.Image2Path = null;
			}
			if (deleteImage3)
			{
				image.Image3Path = null;
			}
			if (deleteImage4)
			{
				image.Image4Path = null;
			}
			if (deleteImage5)
			{
				image.Image5Path = null;
			}
			if (!string.IsNullOrEmpty(image.Image1Path) || !string.IsNullOrEmpty(image.Image2Path) || !string.IsNullOrEmpty(image.Image3Path) || !string.IsNullOrEmpty(image.Image4Path) || !string.IsNullOrEmpty(image.Image5Path))
			{
				await _imagesRepo.Update(image);
			}
			else
			{
				await _imagesRepo.Remove(image);
			}
		}
		await _imagesRepo.SaveChanges();
		return Ok(new
		{
			message = "Images deleted successfully."
		});
	}

	[HttpDelete("/delete-images/{userId}")]
	public async Task<IActionResult> DeleteImages(long userId, [FromForm] bool deleteImage1, [FromForm] bool deleteImage2, [FromForm] bool deleteImage3, [FromForm] bool deleteImage4, [FromForm] bool deleteImage5)
	{
		if (userId == 0L)
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid user ID"
			});
		}
		if (await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Id == userId) == null)
		{
			return NotFound(new
			{
				success = false,
				message = "User not found"
			});
		}
		List<Images> userImages = (await _imagesRepo.GetAll()).Where((Images x) => x.UserId == userId).ToList();
		if (userImages == null || !userImages.Any())
		{
			return NotFound(new
			{
				success = false,
				message = "No images found for the user"
			});
		}
		bool anyImageDeleted = false;
		foreach (Images image in userImages)
		{
			bool deleted = false;
			if (deleteImage1 && !string.IsNullOrEmpty(image.Image1Path))
			{
				await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", deleteImage1, deleteImage2: false, deleteImage3: false, deleteImage4: false, deleteImage5: false);
				image.Image1Path = null;
				deleted = true;
			}
			if (deleteImage2 && !string.IsNullOrEmpty(image.Image2Path))
			{
				await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", deleteImage1: false, deleteImage2, deleteImage3: false, deleteImage4: false, deleteImage5: false);
				image.Image2Path = null;
				deleted = true;
			}
			if (deleteImage3 && !string.IsNullOrEmpty(image.Image3Path))
			{
				await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", deleteImage1: false, deleteImage2: false, deleteImage3, deleteImage4: false, deleteImage5: false);
				image.Image3Path = null;
				deleted = true;
			}
			if (deleteImage4 && !string.IsNullOrEmpty(image.Image4Path))
			{
				await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", deleteImage1: false, deleteImage2: false, deleteImage3: false, deleteImage4, deleteImage5: false);
				image.Image4Path = null;
				deleted = true;
			}
			if (deleteImage5 && !string.IsNullOrEmpty(image.Image5Path))
			{
				await _fileService.DeleteSelectedFiles(image, "Uploads/Registration", deleteImage1: false, deleteImage2: false, deleteImage3: false, deleteImage4: false, deleteImage5);
				image.Image5Path = null;
				deleted = true;
			}
			if (deleted)
			{
				await _imagesRepo.Update(image);
				anyImageDeleted = true;
			}
		}
		await _imagesRepo.SaveChanges();
		return Ok(new
		{
			success = anyImageDeleted,
			message = (anyImageDeleted ? " Do you want to remove this picture?" : "No images were deleted.")
		});
	}

	[HttpPost("/updateimage/{userId}")]
	public async Task<IActionResult> UpdateProfileImage(long userId, [FromForm] IFormFile? image1, [FromForm] IFormFile? image2, [FromForm] IFormFile? image3, [FromForm] IFormFile? image4, [FromForm] IFormFile? image5, [FromForm] bool? photoVisibleToAll = null, [FromForm] bool? photoVisibleToPremium = null, [FromForm] bool? photoVisibleToAccepted = null)
	{
		try
		{
			if (userId <= 0)
			{
				_logger.LogInformation("Invalid user ID");
				return RedirectToAction("Login");
			}
			Registration user = await _registrationRepo.FirstOrDefaultActive((Registration x) => x.Id == userId);
			if (user == null)
			{
				_logger.LogInformation("User not found");
				return RedirectToAction("Login");
			}
			if (photoVisibleToAll.HasValue)
			{
				user.PhotoVisibleToAll = photoVisibleToAll.Value;
			}
			if (photoVisibleToPremium.HasValue)
			{
				user.PhotoVisibleToPremium = photoVisibleToPremium.Value;
			}
			if (photoVisibleToAccepted.HasValue)
			{
				user.PhotoVisibleToAccepted = photoVisibleToAccepted.Value;
			}
			await _registrationRepo.Update(user);
			await _registrationRepo.SaveChanges();
			string folderPath = "Uploads/Registration";
			ImagesDto imageDto = new ImagesDto
			{
				UserId = userId,
				Image1Path = image1 != null ? await _fileService.SaveFile(image1, folderPath) : null,
				Image2Path = image2 != null ? await _fileService.SaveFile(image2, folderPath) : null,
				Image3Path = image3 != null ? await _fileService.SaveFile(image3, folderPath) : null,
				Image4Path = image4 != null ? await _fileService.SaveFile(image4, folderPath) : null,
				Image5Path = image5 != null ? await _fileService.SaveFile(image5, folderPath) : null
			};
			Images existingImage = await _imagesRepo.FirstOrDefault(x => x.UserId == userId);
			if (existingImage == null)
			{
				Images newImage = new Images
				{
					UserId = userId,
					Image1Path = imageDto.Image1Path,
					Image2Path = imageDto.Image2Path,
					Image3Path = imageDto.Image3Path,
					Image4Path = imageDto.Image4Path,
					Image5Path = imageDto.Image5Path,
					CreatedOn = DateTime.UtcNow,
					ModifiedOn = DateTime.UtcNow,
					IsActive = true,
					IsDeleted = false
				};
				_imagesRepo.Add(newImage);
				await _imagesRepo.SaveChanges();
			}
			else
			{
				existingImage.Image1Path = imageDto.Image1Path ?? existingImage.Image1Path;
				existingImage.Image2Path = imageDto.Image2Path ?? existingImage.Image2Path;
				existingImage.Image3Path = imageDto.Image3Path ?? existingImage.Image3Path;
				existingImage.Image4Path = imageDto.Image4Path ?? existingImage.Image4Path;
				existingImage.Image5Path = imageDto.Image5Path ?? existingImage.Image5Path;
				existingImage.ModifiedOn = DateTime.UtcNow;
				_imagesRepo.Update(existingImage);
				await _imagesRepo.SaveChanges();
			}
			return RedirectToAction("Image");
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "An error occurred while updating profile image.");
			return StatusCode(500, "Internal server error");
		}
	}

	[HttpGet("/delete-account")]
	public async Task<IActionResult> DeleteAccount()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		RegistrationDto profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(Convert.ToInt64(userId)));
		if (profile == null)
		{
			return RedirectToAction("Dashboard");
		}
		return View(new HomeViewModel
		{
			Registration = profile
		});
	}

	public async Task<IActionResult> AccountDelete()
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			CookieOptions option = new CookieOptions();
			Response.Cookies.Append("id", string.Empty, option);
			Response.Cookies.Delete("id");
			_logger.LogInformation("User logged out");
			return RedirectToAction("Login");
		}
		Registration entity = await _registrationRepo.Get(Convert.ToInt64(userId));
		if (entity != null)
		{
			entity.IsActive = false;
			entity.IsDeleted = true;
			entity.DisabledReason = 1;
			await _registrationRepo.SaveChanges();
		}
		CookieOptions cookieOptions = new CookieOptions();
		Response.Cookies.Append("id", string.Empty, cookieOptions);
		Response.Cookies.Delete("id");
		_logger.LogInformation("User logged out");
		return RedirectToAction("Login");
	}

	public DateTime ConvertStringToDateTime(RegistrationDto user)
	{
		string dateOfBirth = !string.IsNullOrEmpty(user.DOB) ? user.DOB : "01/01/1990";
		dateOfBirth = dateOfBirth.Split('T')[0];
		string[] parts = dateOfBirth.Replace("-", "/").Split('/');
		string year = parts[2];
		string day = parts[0].Length >= 2 ? parts[0] : "0" + parts[0];
		string month = parts[1].Length >= 2 ? parts[1].Trim() : "0" + parts[1];
		if (day.Length > 2)
		{
			(day, year) = (year, day);
		}
		string dob = $"{day}/{month}/{year}";
		return DateTime.ParseExact(dob, "dd/MM/yyyy", CultureInfo.InvariantCulture);
	}

	public int CalculateAge(DateTime dateOfBirth)
	{
		DateTime today = DateTime.Today;
		int age = today.Year - dateOfBirth.Year;
		if (dateOfBirth.Date > today.AddYears(-age))
		{
			age--;
		}
		return age;
	}

	public async Task<RegistrationDto> MapStarredProfiles(RegistrationDto _partnerProfileDto)
	{
		if (_partnerProfileDto == null)
		{
			return null;
		}
		string userId = GetCurrentUserId()?.ToString();
		long userIdLong = Convert.ToInt64(userId);
		if (await _userStarProfileRepo.FirstOrDefaultActive((UserStarProfile x) => x.UserId == userIdLong && x.StarId == _partnerProfileDto.Id) == null)
		{
			_partnerProfileDto.IsStarred = false;
			return _partnerProfileDto;
		}
		_partnerProfileDto.IsStarred = true;
		return _partnerProfileDto;
	}

	public async Task<RegistrationDto> MapLikedProfiles(RegistrationDto _partnerProfileDto)
	{
		if (_partnerProfileDto == null)
		{
			return null;
		}
		string userId = GetCurrentUserId()?.ToString();
		long userIdLong = Convert.ToInt64(userId);
		if (await _userFavouriteProfileRepo.FirstOrDefaultActive((UserFavouriteProfile x) => x.UserId == userIdLong && x.LikedId == _partnerProfileDto.Id) == null)
		{
			_partnerProfileDto.IsLiked = false;
			return _partnerProfileDto;
		}
		_partnerProfileDto.IsLiked = true;
		return _partnerProfileDto;
	}

	public int heightInInt(string height)
	{
		if (string.IsNullOrWhiteSpace(height))
		{
			throw new ArgumentException("Height cannot be null or whitespace", "height");
		}
		string numericPart = new string(height.TakeWhile((char c) => char.IsDigit(c) || c == '.').ToArray());
		if (!string.IsNullOrWhiteSpace(numericPart) && int.TryParse(numericPart, out var heightInCm))
		{
			return heightInCm;
		}
		throw new FormatException("Invalid height format");
	}

	[HttpPost("/SavePhotoPrivacy")]
	public async Task<IActionResult> SavePhotoPrivacy([FromForm] bool photoVisibleToAll, [FromForm] bool photoVisibleToPremium, [FromForm] bool photoVisibleToAccepted)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User not logged in."
			});
		}
		Registration user = await _registrationRepo.Get(Convert.ToInt64(userId));
		if (user == null)
		{
			return Json(new
			{
				success = false,
				message = "User not found."
			});
		}
		user.PhotoVisibleToAll = photoVisibleToAll;
		user.PhotoVisibleToPremium = photoVisibleToPremium;
		user.PhotoVisibleToAccepted = photoVisibleToAccepted;
		await _registrationRepo.Update(user);
		await _registrationRepo.SaveChanges();
		return Json(new
		{
			success = true,
			message = "Photo privacy settings saved successfully."
		});
	}

	[HttpPost("/RequestPhotoUnlock")]
	public async Task<IActionResult> RequestPhotoUnlock([FromForm] long ownerId)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User not logged in."
			});
		}
		long requesterId = Convert.ToInt64(userId);
		if (requesterId == ownerId)
		{
			return Json(new
			{
				success = false,
				message = "You cannot request to unlock your own photos."
			});
		}
		PhotoUnlockRequest existingRequest = await _photoUnlockRequestRepo.FirstOrDefaultActive((PhotoUnlockRequest x) => x.RequesterId == requesterId && x.OwnerId == ownerId);
		if (existingRequest != null)
		{
			if (existingRequest.Status == PhotoUnlockStatus.Approved)
			{
				return Json(new
				{
					success = true,
					message = "Photos already unlocked."
				});
			}
			if (existingRequest.Status == PhotoUnlockStatus.Pending)
			{
				return Json(new
				{
					success = false,
					message = "Unlock request is already pending."
				});
			}
			existingRequest.Status = PhotoUnlockStatus.Pending;
			await _photoUnlockRequestRepo.Update(existingRequest);
			await _photoUnlockRequestRepo.SaveChanges();
		}
		else
		{
			PhotoUnlockRequest newRequest = new PhotoUnlockRequest
			{
				RequesterId = requesterId,
				OwnerId = ownerId,
				Status = PhotoUnlockStatus.Pending,
				CreatedOn = DateTime.UtcNow,
				ModifiedOn = DateTime.UtcNow
			};
			await _photoUnlockRequestRepo.Add(newRequest);
			await _photoUnlockRequestRepo.SaveChanges();
		}
		Registration requesterProfile = await _registrationRepo.Get(requesterId);
		Registration ownerProfile = await _registrationRepo.Get(ownerId);
		Notification notificationToOwner = new Notification
		{
			UserId = requesterId,
			Notify_Id = ownerId,
			Notify_Message = "Photo unlock request from " + requesterProfile.Name,
			CreatedOn = DateTime.UtcNow,
			ModifiedOn = DateTime.UtcNow
		};
		await _notificationRepo.Add(notificationToOwner);
		Notification notificationToRequester = new Notification
		{
			UserId = ownerId,
			Notify_Id = requesterId,
			Notify_Message = "Photo unlock request sent to " + ownerProfile.Name + " — Pending",
			CreatedOn = DateTime.UtcNow,
			ModifiedOn = DateTime.UtcNow
		};
		await _notificationRepo.Add(notificationToRequester);
		await _notificationRepo.SaveChanges();
		return Json(new
		{
			success = true,
			message = "Photo unlock request sent successfully."
		});
	}

	[HttpPost("/ApprovePhotoUnlock")]
	public async Task<IActionResult> ApprovePhotoUnlock([FromForm] long requesterId)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User not logged in."
			});
		}
		long ownerId = Convert.ToInt64(userId);
		PhotoUnlockRequest request = await _photoUnlockRequestRepo.FirstOrDefaultActive((PhotoUnlockRequest x) => x.RequesterId == requesterId && x.OwnerId == ownerId);
		if (request == null)
		{
			return Json(new
			{
				success = false,
				message = "Unlock request not found."
			});
		}
		request.Status = PhotoUnlockStatus.Approved;
		await _photoUnlockRequestRepo.Update(request);
		await _photoUnlockRequestRepo.SaveChanges();
		foreach (Notification notif in await _notificationRepo.Where((Notification x) => (x.UserId == requesterId && x.Notify_Id == ownerId && x.Notify_Message.Contains("Photo unlock request from")) || (x.UserId == ownerId && x.Notify_Id == requesterId && x.Notify_Message.Contains("Photo unlock request sent to"))))
		{
			await _notificationRepo.Remove(notif);
		}
		Registration ownerProfile = await _registrationRepo.Get(ownerId);
		Notification notification = new Notification
		{
			UserId = ownerId,
			Notify_Id = requesterId,
			Notify_Message = "Your photo unlock request has been approved by " + ownerProfile.Name,
			CreatedOn = DateTime.UtcNow,
			ModifiedOn = DateTime.UtcNow
		};
		await _notificationRepo.Add(notification);
		await _notificationRepo.SaveChanges();
		return Json(new
		{
			success = true,
			message = "Photo unlock request approved."
		});
	}

	[HttpPost("/RejectPhotoUnlock")]
	public async Task<IActionResult> RejectPhotoUnlock([FromForm] long requesterId)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User not logged in."
			});
		}
		long ownerId = Convert.ToInt64(userId);
		PhotoUnlockRequest request = await _photoUnlockRequestRepo.FirstOrDefaultActive((PhotoUnlockRequest x) => x.RequesterId == requesterId && x.OwnerId == ownerId);
		if (request == null)
		{
			return Json(new
			{
				success = false,
				message = "Unlock request not found."
			});
		}
		request.Status = PhotoUnlockStatus.Rejected;
		await _photoUnlockRequestRepo.Update(request);
		await _photoUnlockRequestRepo.SaveChanges();
		foreach (Notification notif in await _notificationRepo.Where((Notification x) => (x.UserId == requesterId && x.Notify_Id == ownerId && x.Notify_Message.Contains("Photo unlock request from")) || (x.UserId == ownerId && x.Notify_Id == requesterId && x.Notify_Message.Contains("Photo unlock request sent to"))))
		{
			await _notificationRepo.Remove(notif);
		}
		Registration ownerProfile = await _registrationRepo.Get(ownerId);
		Notification notification = new Notification
		{
			UserId = ownerId,
			Notify_Id = requesterId,
			Notify_Message = "Your photo unlock request has been rejected by " + ownerProfile.Name,
			CreatedOn = DateTime.UtcNow,
			ModifiedOn = DateTime.UtcNow
		};
		await _notificationRepo.Add(notification);
		await _notificationRepo.SaveChanges();
		return Json(new
		{
			success = true,
			message = "Photo unlock request rejected."
		});
	}

	[HttpPost("/NotifyLockedPhotoAttempt")]
	public async Task<IActionResult> NotifyLockedPhotoAttempt([FromForm] long ownerId)
	{
		string userId = GetCurrentUserId()?.ToString();
		if (string.IsNullOrEmpty(userId))
		{
			return Json(new
			{
				success = false,
				message = "User not logged in."
			});
		}
		long viewerId = Convert.ToInt64(userId);
		if (viewerId == ownerId)
		{
			return Json(new
			{
				success = false,
				message = "You cannot attempt to view your own locked photo."
			});
		}
		Registration viewerProfile = await _registrationRepo.Get(viewerId);
		Registration ownerProfile = await _registrationRepo.Get(ownerId);
		if (viewerProfile == null || ownerProfile == null)
		{
			return Json(new
			{
				success = false,
				message = "Profile not found."
			});
		}
		string message = viewerProfile.Name + " viewed your profile";
		Notification existingNotif = await _notificationRepo.FirstOrDefaultActive((Notification x) => x.UserId == viewerId && x.Notify_Id == ownerId && x.Notify_Message == message);
		if (existingNotif == null)
		{
			Notification notification = new Notification
			{
				UserId = viewerId,
				Notify_Id = ownerId,
				Notify_Message = message,
				CreatedOn = DateTime.UtcNow,
				ModifiedOn = DateTime.UtcNow
			};
			await _notificationRepo.Add(notification);
		}
		else
		{
			existingNotif.CreatedOn = DateTime.UtcNow;
			existingNotif.ModifiedOn = DateTime.UtcNow;
			await _notificationRepo.Update(existingNotif);
		}
		await _notificationRepo.SaveChanges();
		return Json(new
		{
			success = true
		});
	}

	[HttpGet("/user/suspended")]
	public async Task<IActionResult> Suspended()
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			return RedirectToAction("Login");
		}
		Registration user = await _registrationRepo.Get(userIdValue.Value);
		if (user == null || user.DisabledReason != 2)
		{
			return RedirectToAction("Dashboard");
		}
		IRepository<SupportRequest> supportRepo = HttpContext.RequestServices.GetRequiredService<IRepository<SupportRequest>>();
		bool hasPendingAppeal = (await supportRepo.Where((SupportRequest x) => x.UserId == user.Id && x.Status == "Pending")).Any();
		ViewBag.HasPendingAppeal = hasPendingAppeal;
		RegistrationDto loggedInUser = _mapper.Map<RegistrationDto>(user);
		return View("ProfileSuspended", new HomeViewModel
		{
			Registration = loggedInUser
		});
	}

	[HttpGet("/user/help-support")]
	public async Task<IActionResult> HelpSupport()
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			return RedirectToAction("Login");
		}
		Registration user = await _registrationRepo.Get(userIdValue.Value);
		if (user == null || user.DisabledReason != 2)
		{
			return RedirectToAction("Dashboard");
		}
		IRepository<SupportRequest> supportRepo = HttpContext.RequestServices.GetRequiredService<IRepository<SupportRequest>>();
		if ((await supportRepo.Where((SupportRequest x) => x.UserId == user.Id && x.Status == "Pending")).Any())
		{
			return RedirectToAction("Suspended");
		}
		RegistrationDto loggedInUser = _mapper.Map<RegistrationDto>(user);
		return View("HelpAndSupport", new HomeViewModel
		{
			Registration = loggedInUser
		});
	}

	[HttpPost("/user/help-support")]
	public async Task<IActionResult> SubmitSupport(string comments, IFormFile? myfile)
	{
		long? userIdValue = GetCurrentUserId();
		if (!userIdValue.HasValue)
		{
			return RedirectToAction("Login");
		}
		Registration user = await _registrationRepo.Get(userIdValue.Value);
		if (user == null || user.DisabledReason != 2)
		{
			return RedirectToAction("Dashboard");
		}
		IRepository<SupportRequest> supportRepo = HttpContext.RequestServices.GetRequiredService<IRepository<SupportRequest>>();
		if ((await supportRepo.Where((SupportRequest x) => x.UserId == user.Id && x.Status == "Pending")).Any())
		{
			TempData["Error"] = "You already have a pending appeal under review.";
			return RedirectToAction("Suspended");
		}
		if (string.IsNullOrWhiteSpace(comments))
		{
			TempData["Error"] = "Please enter your message.";
			return RedirectToAction("HelpSupport");
		}
		string attachmentPath = null;
		if (myfile != null)
		{
			attachmentPath = await _fileService.UploadFile(myfile, "Uploads/Support");
		}
		SupportRequest supportRequest = new SupportRequest
		{
			UserId = user.Id,
			Subject = "Profile Suspension Appeal",
			Message = comments,
			Status = "Pending",
			AttachmentPath = attachmentPath,
			CreatedOn = DateTime.UtcNow,
			IsActive = true
		};
		await supportRepo.Add(supportRequest);
		await supportRepo.SaveChanges();
		TempData["Success"] = "Your appeal has been submitted successfully. Our team will review it.";
		return RedirectToAction("Suspended");
	}

	private bool isKerala(string stateName)
	{
		return string.Equals(stateName?.Trim(), "KERALA", StringComparison.OrdinalIgnoreCase);
	}

	private bool isIndia(string countryName)
	{
		return string.Equals(countryName?.Trim(), "INDIA", StringComparison.OrdinalIgnoreCase);
	}
}
}