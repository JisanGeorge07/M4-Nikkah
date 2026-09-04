//using Application.Helpers;
//using Application.Interfaces.Persistence;
//using Application.Models;
//using AutoMapper;
//using Domain;
//using Domain.Framework;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using URMARRY.Areas.Admin.Models;

//namespace URMARRY.Areas.Admin.Controllers
//{
//    [Authorize]
//    [Area("Admin")]
//    public class PendingRequestController : Controller
//    {

//        private readonly IRepository<Email> _emailRepo;
//        private readonly IRepository<Registration> _enquiryRepo;
//        private readonly IMapper _mapper;
//        private readonly IRepository<ProfileFor> _profileForRepo;
//        private readonly IRepository<Nationality> _nationalityRepo;
//        private readonly IRepository<MaritalStatus> _maritalStatusRepo;
//        private readonly IRepository<BodyFeatures> _bodyFeaturesRepo;
//        private readonly IRepository<Profession> _professionRepo;
//        private readonly IRepository<MotherTongue> _motherTongueRepo;
//        private readonly IRepository<ReligionCaste> _religionCasteRepo;
//        private readonly IRepository<Religiousness> _religiousnessRepo;
//        private readonly IRepository<Community> _communityRepo;
//        private readonly IRepository<FinancialStatus> _financialStatusRepo;
//        private readonly IRepository<HomeContent> _homeContentRepo;
//        private readonly IRepository<Contact> _contactRepo;
//        private readonly IRepository<MatchingProfiles> _matchingProfilesRepo;
//        internal Guid key = new Guid("F7AD797A-416C-4C56-8749-7017FBC90427");

//        public PendingRequestController(
//            IRepository<Email> emailRepo,
//            IRepository<Registration> enquiryRepo,
//            IMapper mapper,
//            IRepository<ProfileFor> profileForRepo,
//            IRepository<Nationality> nationalityRepo,
//            IRepository<MaritalStatus> maritalStatusRepo,
//            IRepository<BodyFeatures> bodyFeaturesRepo,
//            IRepository<Profession> professionRepo,
//            IRepository<MotherTongue> motherTongueRepo,
//            IRepository<ReligionCaste> religionCasteRepo,
//            IRepository<Religiousness> religiousnessRepo,
//            IRepository<Community> communityRepo,
//            IRepository<FinancialStatus> financialStatusRepo,
//            IRepository<HomeContent> homeContentRepo,
//            IRepository<Contact> contactRepo,
//            IRepository<MatchingProfiles> matchingProfilesRepo)
//        {
//            _emailRepo = emailRepo;
//            _enquiryRepo = enquiryRepo;
//            _mapper = mapper;
//            _maritalStatusRepo = maritalStatusRepo;
//            _bodyFeaturesRepo = bodyFeaturesRepo;
//            _professionRepo = professionRepo;
//            _motherTongueRepo = motherTongueRepo;
//            _religionCasteRepo = religionCasteRepo;
//            _religiousnessRepo = religiousnessRepo;
//            _communityRepo = communityRepo;
//            _financialStatusRepo = financialStatusRepo;
//            _profileForRepo = profileForRepo;
//            _nationalityRepo = nationalityRepo;
//            _homeContentRepo = homeContentRepo;
//            _contactRepo = contactRepo;
//            _matchingProfilesRepo = matchingProfilesRepo;
//        }

//        [HttpGet("/admin/enquiry/pending-enquiry")]
//        public async Task<IActionResult> GetAllPending()
//        {
//            return View(new EnquiryViewModel
//            {
//                Enquiries = _mapper.Map<List<RegistrationDto>>(await _enquiryRepo.WhereActive(x => !x.IsComplete))
//                    .OrderByDescending(x => x.CreatedOn)
//                    .ToList()
//            });
//        }
//        [HttpGet("/admin/enquiry/pending-enquiry/{id:long}")]
//        public async Task<IActionResult> GetPending(long id)
//        {
//            var enquiry = _mapper.Map<RegistrationDto>(await _enquiryRepo.Get(id));
//            return View(new EnquiryViewModel
//            {
//                Enquiry = enquiry,
//                ProfileFor = _mapper.Map<List<ProfileForDto>>(await _profileForRepo.GetAll()),
//                Nationalities = _mapper.Map<List<NationalityDto>>(await _nationalityRepo.GetAll()),
//                MaritalStatuses = _mapper.Map<List<MaritalStatusDto>>(await _maritalStatusRepo.GetAll()),
//                BodyFeatures = _mapper.Map<List<BodyFeaturesDto>>(await _bodyFeaturesRepo.GetAll()),
//                Professions = _mapper.Map<List<ProfessionDto>>(await _professionRepo.GetAll()),
//                MotherTongues = _mapper.Map<List<MotherTongueDto>>(await _motherTongueRepo.GetAll()),
//                ReligionCastes = _mapper.Map<List<ReligionCasteDto>>(await _religionCasteRepo.GetAll()),
//                Religiousnesses = _mapper.Map<List<ReligiousnessDto>>(await _religiousnessRepo.GetAll()),
//                Communities = _mapper.Map<List<CommunityDto>>(await _communityRepo.GetAll()),
//                FinancialStatuses = _mapper.Map<List<FinancialStatusDto>>(await _financialStatusRepo.GetAll()),
//            });
//        }

//        [HttpPost("/admin/enquiry/pending-enquiry")]
//        public async Task<IActionResult> PostPending(RegistrationDto model)
//        {
//            var user = await _enquiryRepo.FirstOrDefaultActive(x => x.Id == Convert.ToInt64(model.Id));
//            if (user == null)
//            {
//                return RedirectToAction(nameof(GetAllPending));
//            }
//            user.Name = model.Name;
//            user.Email = model.Email;
//            if (string.IsNullOrEmpty(user.RegisterNumber))
//            {
//                user.RegisterNumber = "M4N" + Guid.NewGuid().ToString("N").Substring(0, 7);
//            }
//            user.ProfileForId = model.ProfileForId;
//            user.Gender = model.Gender;
//            user.DOB = model.DOB;
//            user.NationalityId = model.NationalityId;
//            user.Phone = model.Phone;
//            user.MaritalStatusId = model.MaritalStatusId;
//            user.HeightId = model.HeightId;
//            user.WeightId = model.WeightId;
//            user.ComplexionId = model.ComplexionId;
//            user.BodyTypeId = model.BodyTypeId;
//            user.IsPhysicallyChallenged = model.IsPhysicallyChallenged;
//            user.HighestEducation = model.HighestEducation;
//            user.EducationType = model.EducationType;
//            user.ProfessionId = model.ProfessionId;
//            user.ProfessionType = model.ProfessionType;
//            user.MotherTongueId = model.MotherTongueId;
//            user.ReligionId = model.ReligionId;
//            user.CommunityId = model.CommunityId;
//            user.ReligiousnessId = model.ReligiousnessId;
//            user.FinancialStatusId = model.FinancialStatusId;
//            user.FamilyName = model.FamilyName;
//            user.FatherName = model.FatherName;
//            user.Post = model.Post;
//            user.Village = model.Village;
//            user.PinCode = model.PinCode;
//            user.Country = model.Country;
//            user.State = model.State;
//            user.District = model.District;
//            user.PresentCountry = model.PresentCountry;
//            user.PresentState = model.PresentState;
//            user.PresentDistrict = model.PresentDistrict;
//            user.About = model.About;
//            user.IsComplete = model.IsComplete;
//            user.IsPremiumMember = model.IsPremiumMember;
//            user.IsSpecialRequest = model.IsSpecialRequest;
//            user.ShowOnHomePage = model.ShowOnHomePage;
//            user.IsActive = model.IsActive;
//            user.IsVerified = model.IsVerified;
//            if (model.IsComplete)
//            {
//                user.CompletedStep = "Step-6";
//            }
//            if (string.IsNullOrEmpty(user.Password))
//            {
//                RijndaelManagedEncryption encryption = new RijndaelManagedEncryption(key);
//                user.PasswordHash = encryption.CreateSalt();
//                user.Password = encryption.EncryptRijndael(model.Password, user.PasswordHash);
//            }
//            await _enquiryRepo.Update(user);
//            await _enquiryRepo.SaveChanges();
//            return RedirectToAction(nameof(GetPending), new { id = model.Id });
//        }

//        [HttpPost("/admin/enquiry/pending-enquiry/delete/{id:long}")]
//        public async Task<IActionResult> DeletePending(long id)
//        {
//            var entity = await _enquiryRepo.Get(id);
//            if (entity != null)
//            {
//                await _enquiryRepo.SoftDelete(entity);
//                await _enquiryRepo.SaveChanges();
//            }

//            return RedirectToAction(nameof(GetAllPending));
//        }
//    }
//}
