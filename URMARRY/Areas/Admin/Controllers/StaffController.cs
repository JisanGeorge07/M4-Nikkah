using Application.Interfaces.Persistence;
using Application.Models;
using Application.Constants;
using Domain;
using Identity;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using URMARRY.Areas.Admin.Models;
using Application.Helpers;
using Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Configuration;
using Application.Models.Transactions;
using URMARRY.Services;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class StaffController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IdentityDbContext _identityDbContext;
        private readonly IRepository<Registration> _userRepository;
        private readonly IRepository<StaffProfileAssignment> _assignmentRepo;
        private readonly IRepository<FollowUp> _followUpRepo;
        private readonly IRepository<FollowUpTimeline> _followUpTimelineRepo;
        private readonly Persistence.AppDbContext _dbContext;
        private readonly IRepository<PlanPurchase> _planPurchaseRepo;
        private readonly IEmailService _emailService;
        private readonly EmailNotificationHelper _emailNotificationHelper;
        private readonly IConfiguration _configuration;
        private readonly IRepository<ColdLead> _coldLeadRepo;
        private readonly ITransactionRepository _transactionRepository;

        public StaffController(
            UserManager<ApplicationUser> userManager,
            IdentityDbContext identityDbContext,
            IRepository<Registration> userRepository,
            IRepository<StaffProfileAssignment> assignmentRepo,
            IRepository<FollowUp> followUpRepo,
            IRepository<FollowUpTimeline> followUpTimelineRepo,
            Persistence.AppDbContext dbContext,
            IRepository<PlanPurchase> planPurchaseRepo,
            IEmailService emailService,
            EmailNotificationHelper emailNotificationHelper,
            IConfiguration configuration,
            IRepository<ColdLead> coldLeadRepo,
            ITransactionRepository transactionRepository)
        {
            _userManager = userManager;
            _identityDbContext = identityDbContext;
            _userRepository = userRepository;
            _assignmentRepo = assignmentRepo;
            _followUpRepo = followUpRepo;
            _followUpTimelineRepo = followUpTimelineRepo;
            _dbContext = dbContext;
            _planPurchaseRepo = planPurchaseRepo;
            _emailService = emailService;
            _emailNotificationHelper = emailNotificationHelper;
            _configuration = configuration;
            _coldLeadRepo = coldLeadRepo;
            _transactionRepository = transactionRepository;
        }


        #region Staff CRUD

        [HttpGet("/admin/staff")]
        public async Task<IActionResult> GetAll()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staffUserIds = staffUsers.Select(u => u.Id).ToList();
            var staffDetailsMap = await _dbContext.StaffDetails
                .Where(s => staffUserIds.Contains(s.UserId) && !s.IsDeleted)
                .ToDictionaryAsync(s => s.UserId);

            var list = staffUsers.Select(u =>
            {
                staffDetailsMap.TryGetValue(u.Id, out var detail);
                return new StaffDto
                {
                    Id = u.Id,
                    StaffDetailId = detail?.Id ?? 0,
                    UserName = u.UserName,
                    StaffName = u.NormalizedUserName, // NormalizedUserName holds the Name
                    Email = u.Email,
                    MobileNumber = u.PhoneNumber,
                    IsActive = u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow,
                    Designation = detail?.Designation,
                    Department = detail?.Department,
                    JoiningDate = detail?.JoiningDate,
                    DisplayOrder = detail?.DisplayOrder ?? u.Id
                };
            }).OrderByDescending(s => s.Id).ToList();

            return View(list);


        }

        [HttpGet("/admin/staff/{id:long}")]
        public async Task<IActionResult> Get(long id, int step = 1)
        {
            var vm = new StaffFullRegistrationVm
            {
                ActiveStep = step > 0 && step <= 3 ? step : 1
            };

            if (id > 0)
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                var detail = await _dbContext.StaffDetails.FirstOrDefaultAsync(s => s.UserId == id && !s.IsDeleted);

                vm.BasicDetails = new StaffDto
                {
                    Id = user.Id,
                    StaffDetailId = detail?.Id ?? 0,
                    UserName = user.UserName,
                    StaffName = user.NormalizedUserName,
                    Email = user.Email,
                    MobileNumber = user.PhoneNumber,
                    IsActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow,
                    PasswordHash = string.Empty,
                    Designation = detail?.Designation,
                    Department = detail?.Department,
                    JoiningDate = detail?.JoiningDate,
                    DisplayOrder = detail?.DisplayOrder ?? user.Id
                };

                // Step 1 extra: Salary Config
                var salaryConfig = await _dbContext.StaffSalaryConfigs.FirstOrDefaultAsync(s => s.StaffId == id && !s.IsDeleted);
                if (salaryConfig != null)
                {
                    vm.SalaryConfig = new StaffSalaryConfigDto
                    {
                        Id = salaryConfig.Id,
                        StaffId = salaryConfig.StaffId,
                        BasicMonthlySalary = salaryConfig.BasicMonthlySalary,
                        SalaryEffectiveDate = salaryConfig.SalaryEffectiveDate,
                        IncentiveEffectiveDate = salaryConfig.IncentiveEffectiveDate,
                        PerDaySalary = salaryConfig.PerDaySalary,
                        IncentiveEligibility = salaryConfig.IncentiveEligibility
                    };
                }
                else
                {
                    vm.SalaryConfig.StaffId = id;
                }

                // Step 2: Incentive Config
                var incentiveConfig = await _dbContext.StaffIncentiveConfigs.FirstOrDefaultAsync(s => s.StaffId == id && !s.IsDeleted);
                if (incentiveConfig != null)
                {
                    vm.IncentiveConfig = new StaffIncentiveConfigDto
                    {
                        Id = incentiveConfig.Id,
                        StaffId = incentiveConfig.StaffId,
                        MaleVerificationType = incentiveConfig.MaleVerificationType ?? "TargetBasis",
                        MaleVerificationTarget = incentiveConfig.MaleVerificationTarget,
                        MaleVerificationAmount = incentiveConfig.MaleVerificationAmount,

                        FemaleGradeAVerificationType = incentiveConfig.FemaleGradeAVerificationType ?? "TargetBasis",
                        FemaleGradeAVerificationTarget = incentiveConfig.FemaleGradeAVerificationTarget,
                        FemaleGradeAVerificationAmount = incentiveConfig.FemaleGradeAVerificationAmount,

                        FemaleGradeBVerificationType = incentiveConfig.FemaleGradeBVerificationType ?? "TargetBasis",
                        FemaleGradeBVerificationTarget = incentiveConfig.FemaleGradeBVerificationTarget,
                        FemaleGradeBVerificationAmount = incentiveConfig.FemaleGradeBVerificationAmount,

                        FemaleGradeCVerificationType = incentiveConfig.FemaleGradeCVerificationType ?? "TargetBasis",
                        FemaleGradeCVerificationTarget = incentiveConfig.FemaleGradeCVerificationTarget,
                        FemaleGradeCVerificationAmount = incentiveConfig.FemaleGradeCVerificationAmount,

                        FemaleGradeDVerificationType = incentiveConfig.FemaleGradeDVerificationType ?? "TargetBasis",
                        FemaleGradeDVerificationTarget = incentiveConfig.FemaleGradeDVerificationTarget,
                        FemaleGradeDVerificationAmount = incentiveConfig.FemaleGradeDVerificationAmount,

                        FemaleVerificationType = incentiveConfig.FemaleVerificationType ?? "TargetBasis",
                        FemaleVerificationTarget = incentiveConfig.FemaleVerificationTarget,
                        FemaleVerificationAmount = incentiveConfig.FemaleVerificationAmount,

                        FemaleDocVerificationType = incentiveConfig.FemaleDocVerificationType ?? "TargetBasis",
                        FemaleDocVerificationTarget = incentiveConfig.FemaleDocVerificationTarget,
                        FemaleDocVerificationAmount = incentiveConfig.FemaleDocVerificationAmount,

                        MaleConversionType = incentiveConfig.MaleConversionType ?? "TargetBasis",
                        MaleConversionTarget = incentiveConfig.MaleConversionTarget,
                        MaleConversionAmount = incentiveConfig.MaleConversionAmount,

                        FemaleConversionType = incentiveConfig.FemaleConversionType ?? "TargetBasis",
                        FemaleConversionTarget = incentiveConfig.FemaleConversionTarget,
                        FemaleConversionAmount = incentiveConfig.FemaleConversionAmount
                    };
                }
                else
                {
                    vm.IncentiveConfig.StaffId = id;
                }

                // Step 3: Performance Target Config
                var targetConfig = await _dbContext.StaffPerformanceTargetConfigs.FirstOrDefaultAsync(s => s.StaffId == id && !s.IsDeleted);
                if (targetConfig != null)
                {
                    vm.PerformanceTargetConfig = new StaffPerformanceTargetConfigDto
                    {
                        Id = targetConfig.Id,
                        StaffId = targetConfig.StaffId,

                        MaleVerificationMonthlyTarget = targetConfig.MaleVerificationMonthlyTarget,
                        MaleVerificationDailyTarget = targetConfig.MaleVerificationDailyTarget,

                        FemaleVerificationMonthlyTarget = targetConfig.FemaleVerificationMonthlyTarget,
                        FemaleVerificationDailyTarget = targetConfig.FemaleVerificationDailyTarget,

                        MaleConversionMonthlyTarget = targetConfig.MaleConversionMonthlyTarget,
                        MaleConversionDailyTarget = targetConfig.MaleConversionDailyTarget,

                        FemaleConversionMonthlyTarget = targetConfig.FemaleConversionMonthlyTarget,
                        FemaleConversionDailyTarget = targetConfig.FemaleConversionDailyTarget
                    };
                }
                else
                {
                    vm.PerformanceTargetConfig.StaffId = id;
                }
            }
            else
            {
                vm.BasicDetails = new StaffDto
                {
                    IsActive = true,
                    Id = 0,
                    DisplayOrder = 1
                };
            }

            return View(vm);
        }

        [HttpPost("/admin/staff")]
        public async Task<IActionResult> Post(StaffFullRegistrationVm vm)
        {
            var model = vm.BasicDetails ?? new StaffDto();
            var salaryConfigModel = vm.SalaryConfig ?? new StaffSalaryConfigDto();

            if (model.Id <= 0)
            {
                if (!string.IsNullOrEmpty(model.StaffName))
                {
                    var normalizedSearchName = model.StaffName.Trim().ToLower();
                    var staffNameExists = await _userManager.Users.AnyAsync(u => u.NormalizedUserName != null && u.NormalizedUserName.ToLower() == normalizedSearchName);
                    if (staffNameExists)
                    {
                        ModelState.AddModelError(string.Empty, $"A staff member with the name '{model.StaffName}' already exists.");
                        vm.ActiveStep = 1;
                        return View("Get", vm);
                    }
                }

                var tempUserName = "temp_" + Guid.NewGuid().ToString("N").Substring(0, 10);
                var user = new ApplicationUser
                {
                    UserName = tempUserName,
                    Email = model.Email,
                    PhoneNumber = model.MobileNumber,
                    EmailConfirmed = true,
                    LockoutEnabled = true,
                    LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue
                };

                var result = await _userManager.CreateAsync(user, model.PasswordHash ?? string.Empty);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Staff");
                    user.NormalizedUserName = model.StaffName;
                    user.UserName = "M4S" + user.Id.ToString().PadLeft(6, '0');
                    await _identityDbContext.SaveChangesAsync();

                    var staffDetail = new StaffDetail
                    {
                        UserId = user.Id,
                        Designation = model.Designation,
                        Department = model.Department,
                        JoiningDate = model.JoiningDate,
                        DisplayOrder = model.DisplayOrder <= 0 ? user.Id : model.DisplayOrder,
                        IsActive = model.IsActive
                    };
                    await _dbContext.StaffDetails.AddAsync(staffDetail);

                    // Save Salary Config in Step 1
                    int daysInMonth = DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month);
                    decimal perDaySalary = daysInMonth > 0 ? Math.Round(salaryConfigModel.BasicMonthlySalary / daysInMonth, 2) : 0;

                    var salaryEntity = new StaffSalaryConfig
                    {
                        StaffId = user.Id,
                        BasicMonthlySalary = salaryConfigModel.BasicMonthlySalary,
                        SalaryEffectiveDate = salaryConfigModel.SalaryEffectiveDate,
                        IncentiveEffectiveDate = salaryConfigModel.IncentiveEffectiveDate,
                        PerDaySalary = perDaySalary,
                        IncentiveEligibility = salaryConfigModel.IncentiveEligibility
                    };
                    await _dbContext.StaffSalaryConfigs.AddAsync(salaryEntity);

                    await _dbContext.SaveChangesAsync();

                    // Redirect to Step 2 (Incentive Setup)
                    return RedirectToAction(nameof(Get), new { id = user.Id, step = 2 });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    vm.ActiveStep = 1;
                    return View("Get", vm);
                }
            }
            else
            {
                var user = await _userManager.FindByIdAsync(model.Id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                if (!string.IsNullOrEmpty(model.StaffName))
                {
                    var normalizedSearchName = model.StaffName.Trim().ToLower();
                    var staffNameExists = await _userManager.Users.AnyAsync(u => u.NormalizedUserName != null && u.NormalizedUserName.ToLower() == normalizedSearchName && u.Id != model.Id);
                    if (staffNameExists)
                    {
                        ModelState.AddModelError(string.Empty, $"A staff member with the name '{model.StaffName}' already exists.");
                        vm.ActiveStep = 1;
                        return View("Get", vm);
                    }
                }

                user.Email = model.Email;
                user.PhoneNumber = model.MobileNumber;
                user.LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue;

                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    var hasher = new PasswordHasher<ApplicationUser>();
                    user.PasswordHash = hasher.HashPassword(user, model.PasswordHash);
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    user.NormalizedUserName = model.StaffName;
                    await _identityDbContext.SaveChangesAsync();

                    var detail = await _dbContext.StaffDetails.FirstOrDefaultAsync(s => s.UserId == user.Id && !s.IsDeleted);
                    if (detail != null)
                    {
                        detail.Designation = model.Designation;
                        detail.Department = model.Department;
                        detail.JoiningDate = model.JoiningDate;
                        detail.DisplayOrder = model.DisplayOrder <= 0 ? user.Id : model.DisplayOrder;
                        detail.IsActive = model.IsActive;
                        _dbContext.StaffDetails.Update(detail);
                    }
                    else
                    {
                        detail = new StaffDetail
                        {
                            UserId = user.Id,
                            Designation = model.Designation,
                            Department = model.Department,
                            JoiningDate = model.JoiningDate,
                            DisplayOrder = model.DisplayOrder <= 0 ? user.Id : model.DisplayOrder,
                            IsActive = model.IsActive
                        };
                        await _dbContext.StaffDetails.AddAsync(detail);
                    }

                    // Save/Update Salary Config in Step 1
                    int daysInMonth = DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month);
                    decimal perDaySalary = daysInMonth > 0 ? Math.Round(salaryConfigModel.BasicMonthlySalary / daysInMonth, 2) : 0;

                    var salaryEntity = await _dbContext.StaffSalaryConfigs.FirstOrDefaultAsync(s => s.StaffId == user.Id && !s.IsDeleted);
                    if (salaryEntity != null)
                    {
                        salaryEntity.BasicMonthlySalary = salaryConfigModel.BasicMonthlySalary;
                        salaryEntity.SalaryEffectiveDate = salaryConfigModel.SalaryEffectiveDate;
                        salaryEntity.IncentiveEffectiveDate = salaryConfigModel.IncentiveEffectiveDate;
                        salaryEntity.PerDaySalary = perDaySalary;
                        salaryEntity.IncentiveEligibility = salaryConfigModel.IncentiveEligibility;
                        _dbContext.StaffSalaryConfigs.Update(salaryEntity);
                    }
                    else
                    {
                        salaryEntity = new StaffSalaryConfig
                        {
                            StaffId = user.Id,
                            BasicMonthlySalary = salaryConfigModel.BasicMonthlySalary,
                            SalaryEffectiveDate = salaryConfigModel.SalaryEffectiveDate,
                            IncentiveEffectiveDate = salaryConfigModel.IncentiveEffectiveDate,
                            PerDaySalary = perDaySalary,
                            IncentiveEligibility = salaryConfigModel.IncentiveEligibility
                        };
                        await _dbContext.StaffSalaryConfigs.AddAsync(salaryEntity);
                    }

                    await _dbContext.SaveChangesAsync();

                    return RedirectToAction(nameof(Get), new { id = user.Id, step = 2 });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    vm.ActiveStep = 1;
                    return View("Get", vm);
                }
            }
        }

        [HttpPost("/admin/staff/step2")]
        public async Task<IActionResult> PostStep2(StaffIncentiveConfigDto model)
        {
            if (model.StaffId <= 0) return BadRequest("Invalid Staff ID");

            var existing = await _dbContext.StaffIncentiveConfigs.FirstOrDefaultAsync(s => s.StaffId == model.StaffId && !s.IsDeleted);
            if (existing != null)
            {
                existing.MaleVerificationType = model.MaleVerificationType;
                existing.MaleVerificationTarget = model.MaleVerificationTarget;
                existing.MaleVerificationAmount = model.MaleVerificationAmount;

                existing.FemaleGradeAVerificationType = model.FemaleGradeAVerificationType;
                existing.FemaleGradeAVerificationTarget = model.FemaleGradeAVerificationTarget;
                existing.FemaleGradeAVerificationAmount = model.FemaleGradeAVerificationAmount;

                existing.FemaleGradeBVerificationType = model.FemaleGradeBVerificationType;
                existing.FemaleGradeBVerificationTarget = model.FemaleGradeBVerificationTarget;
                existing.FemaleGradeBVerificationAmount = model.FemaleGradeBVerificationAmount;

                existing.FemaleGradeCVerificationType = model.FemaleGradeCVerificationType;
                existing.FemaleGradeCVerificationTarget = model.FemaleGradeCVerificationTarget;
                existing.FemaleGradeCVerificationAmount = model.FemaleGradeCVerificationAmount;

                existing.FemaleGradeDVerificationType = model.FemaleGradeDVerificationType;
                existing.FemaleGradeDVerificationTarget = model.FemaleGradeDVerificationTarget;
                existing.FemaleGradeDVerificationAmount = model.FemaleGradeDVerificationAmount;

                existing.FemaleVerificationType = model.FemaleGradeBVerificationType;
                existing.FemaleVerificationTarget = model.FemaleGradeBVerificationTarget;
                existing.FemaleVerificationAmount = model.FemaleGradeBVerificationAmount;

                existing.FemaleDocVerificationType = model.FemaleGradeAVerificationType;
                existing.FemaleDocVerificationTarget = model.FemaleGradeAVerificationTarget;
                existing.FemaleDocVerificationAmount = model.FemaleGradeAVerificationAmount;

                existing.MaleConversionType = model.MaleConversionType;
                existing.MaleConversionTarget = model.MaleConversionTarget;
                existing.MaleConversionAmount = model.MaleConversionAmount;

                existing.FemaleConversionType = model.FemaleConversionType;
                existing.FemaleConversionTarget = model.FemaleConversionTarget;
                existing.FemaleConversionAmount = model.FemaleConversionAmount;

                _dbContext.StaffIncentiveConfigs.Update(existing);
            }
            else
            {
                var newEntity = new StaffIncentiveConfig
                {
                    StaffId = model.StaffId,
                    MaleVerificationType = model.MaleVerificationType,
                    MaleVerificationTarget = model.MaleVerificationTarget,
                    MaleVerificationAmount = model.MaleVerificationAmount,

                    FemaleGradeAVerificationType = model.FemaleGradeAVerificationType,
                    FemaleGradeAVerificationTarget = model.FemaleGradeAVerificationTarget,
                    FemaleGradeAVerificationAmount = model.FemaleGradeAVerificationAmount,

                    FemaleGradeBVerificationType = model.FemaleGradeBVerificationType,
                    FemaleGradeBVerificationTarget = model.FemaleGradeBVerificationTarget,
                    FemaleGradeBVerificationAmount = model.FemaleGradeBVerificationAmount,

                    FemaleGradeCVerificationType = model.FemaleGradeCVerificationType,
                    FemaleGradeCVerificationTarget = model.FemaleGradeCVerificationTarget,
                    FemaleGradeCVerificationAmount = model.FemaleGradeCVerificationAmount,

                    FemaleGradeDVerificationType = model.FemaleGradeDVerificationType,
                    FemaleGradeDVerificationTarget = model.FemaleGradeDVerificationTarget,
                    FemaleGradeDVerificationAmount = model.FemaleGradeDVerificationAmount,

                    FemaleVerificationType = model.FemaleGradeBVerificationType,
                    FemaleVerificationTarget = model.FemaleGradeBVerificationTarget,
                    FemaleVerificationAmount = model.FemaleGradeBVerificationAmount,

                    FemaleDocVerificationType = model.FemaleGradeAVerificationType,
                    FemaleDocVerificationTarget = model.FemaleGradeAVerificationTarget,
                    FemaleDocVerificationAmount = model.FemaleGradeAVerificationAmount,

                    MaleConversionType = model.MaleConversionType,
                    MaleConversionTarget = model.MaleConversionTarget,
                    MaleConversionAmount = model.MaleConversionAmount,

                    FemaleConversionType = model.FemaleConversionType,
                    FemaleConversionTarget = model.FemaleConversionTarget,
                    FemaleConversionAmount = model.FemaleConversionAmount
                };
                await _dbContext.StaffIncentiveConfigs.AddAsync(newEntity);
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Get), new { id = model.StaffId, step = 3 });
        }

        [HttpPost("/admin/staff/step3")]
        public async Task<IActionResult> PostStep3(StaffPerformanceTargetConfigDto model)
        {
            if (model.StaffId <= 0) return BadRequest("Invalid Staff ID");

            var existing = await _dbContext.StaffPerformanceTargetConfigs.FirstOrDefaultAsync(s => s.StaffId == model.StaffId && !s.IsDeleted);
            if (existing != null)
            {
                existing.MaleVerificationMonthlyTarget = model.MaleVerificationMonthlyTarget;
                existing.MaleVerificationDailyTarget = model.MaleVerificationDailyTarget;

                existing.FemaleVerificationMonthlyTarget = model.FemaleVerificationMonthlyTarget;
                existing.FemaleVerificationDailyTarget = model.FemaleVerificationDailyTarget;

                existing.MaleConversionMonthlyTarget = model.MaleConversionMonthlyTarget;
                existing.MaleConversionDailyTarget = model.MaleConversionDailyTarget;

                existing.FemaleConversionMonthlyTarget = model.FemaleConversionMonthlyTarget;
                existing.FemaleConversionDailyTarget = model.FemaleConversionDailyTarget;

                _dbContext.StaffPerformanceTargetConfigs.Update(existing);
            }
            else
            {
                var newEntity = new StaffPerformanceTargetConfig
                {
                    StaffId = model.StaffId,
                    MaleVerificationMonthlyTarget = model.MaleVerificationMonthlyTarget,
                    MaleVerificationDailyTarget = model.MaleVerificationDailyTarget,

                    FemaleVerificationMonthlyTarget = model.FemaleVerificationMonthlyTarget,
                    FemaleVerificationDailyTarget = model.FemaleVerificationDailyTarget,

                    MaleConversionMonthlyTarget = model.MaleConversionMonthlyTarget,
                    MaleConversionDailyTarget = model.MaleConversionDailyTarget,

                    FemaleConversionMonthlyTarget = model.FemaleConversionMonthlyTarget,
                    FemaleConversionDailyTarget = model.FemaleConversionDailyTarget
                };
                await _dbContext.StaffPerformanceTargetConfigs.AddAsync(newEntity);
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(GetAll));
        }


        [HttpPost("/admin/staff/delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                await _userManager.DeleteAsync(user);

                var detail = await _dbContext.StaffDetails.FirstOrDefaultAsync(s => s.UserId == id);
                if (detail != null)
                {
                    _dbContext.StaffDetails.Remove(detail);
                    await _dbContext.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(GetAll));
        }


        #endregion

        #region Staff CRM Profile Assignment

        [HttpGet("/admin/staff/assign-profiles")]
        public async Task<IActionResult> AssignProfiles()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.StaffList = staffUsers.Select(u => new
            {
                Id = u.Id,
                Name = u.NormalizedUserName ?? u.UserName
            }).ToList();

            return View();
        }

        [AcceptVerbs("GET", "POST")]
        [Route("/admin/staff/assign-profiles-data")]
        public async Task<IActionResult> AssignProfilesData(string assignmentFilter, string statusFilter, string searchName, long? staffId, string membershipFilter, string completionFilter)
        {
            try
            {
                var isPost = HttpContext.Request.Method == "POST";
                var drawVal = isPost ? Request.Form["draw"] : Request.Query["draw"];
                var startVal = isPost ? Request.Form["start"] : Request.Query["start"];
                var lengthVal = isPost ? Request.Form["length"] : Request.Query["length"];

                int draw = !string.IsNullOrEmpty(drawVal) ? Convert.ToInt32(drawVal) : 0;
                int start = !string.IsNullOrEmpty(startVal) ? Convert.ToInt32(startVal) : 0;
                int length = !string.IsNullOrEmpty(lengthVal) ? Convert.ToInt32(lengthVal) : 100;
                if (length <= 0) length = 100;

                string? searchValue = isPost ? Request.Form["search[value]"] : Request.Query["search[value]"];
                string? sortColumnIndex = isPost ? Request.Form["order[0][column]"] : Request.Query["order[0][column]"];
                string? sortDirection = isPost ? Request.Form["order[0][dir]"] : Request.Query["order[0][dir]"];

                if (string.IsNullOrEmpty(membershipFilter))
                {
                    membershipFilter = isPost ? Request.Form["membershipFilter"].ToString() : Request.Query["membershipFilter"].ToString();
                }

                if (string.IsNullOrEmpty(completionFilter))
                {
                    completionFilter = isPost ? Request.Form["completionFilter"].ToString() : Request.Query["completionFilter"].ToString();
                }

                var query = _userRepository.GetQueryable()
                    .Where(x => !x.IsDeleted && x.IsActive && (x.IsComplete || x.IsVerified));

                // Filter by assignment status
                if (assignmentFilter == "assigned")
                {
                    if (staffId.HasValue && staffId.Value > 0)
                    {
                        query = query.Where(x => _assignmentRepo.GetQueryable().Any(a => a.ProfileId == x.Id && a.StaffId == staffId.Value && !a.IsDeleted));
                    }
                    else
                    {
                        query = query.Where(x => _assignmentRepo.GetQueryable().Any(a => a.ProfileId == x.Id && !a.IsDeleted));
                    }
                }
                else if (assignmentFilter == "unassigned")
                {
                    query = query.Where(x => !_assignmentRepo.GetQueryable().Any(a => a.ProfileId == x.Id && !a.IsDeleted));
                }
                else // "all"
                {
                    if (staffId.HasValue && staffId.Value > 0)
                    {
                        query = query.Where(x => _assignmentRepo.GetQueryable().Any(a => a.ProfileId == x.Id && a.StaffId == staffId.Value && !a.IsDeleted));
                    }
                }

                // Filter by profile status (Pending, Active, Premium)
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
                {
                    if (statusFilter == "premium")
                    {
                        query = query.Where(x => x.IsPremiumMember);
                    }
                    else if (statusFilter == "active")
                    {
                        query = query.Where(x => !x.IsPremiumMember && x.IsComplete && x.IsVisible && x.IsVerified);
                    }
                    else if (statusFilter == "pending")
                    {
                        query = query.Where(x => !x.IsPremiumMember && !(x.IsComplete && x.IsVisible && x.IsVerified));
                    }
                }

                // Filter by profile completion (Complete, Incomplete)
                if (!string.IsNullOrEmpty(completionFilter) && completionFilter != "all")
                {
                    if (completionFilter == "complete")
                    {
                        query = query.Where(x => x.IsComplete);
                    }
                    else if (completionFilter == "incomplete")
                    {
                        query = query.Where(x => !x.IsComplete);
                    }
                }

                // Filter by membership expiry status (yet to expire, active plan, expired, free)
                if (!string.IsNullOrEmpty(membershipFilter) && membershipFilter != "all")
                {
                    var now = DateTime.UtcNow;
                    var tenDaysLater = now.AddDays(10);

                    if (membershipFilter == "yet_to_expire")
                    {
                        var expiringUserIds = _planPurchaseRepo.GetQueryable()
                            .Where(p => !p.IsDeleted && p.ExpiresAt > now && (p.ExpiresAt <= tenDaysLater || (p.ViewCreditsPurchased - p.ViewCreditsUsed) <= 5))
                            .Select(p => p.UserId);

                        query = query.Where(x => expiringUserIds.Contains(x.Id));
                    }
                    else if (membershipFilter == "active_plan" || membershipFilter == "active")
                    {
                        var expiringUserIds = _planPurchaseRepo.GetQueryable()
                            .Where(p => !p.IsDeleted && p.ExpiresAt > now && (p.ExpiresAt <= tenDaysLater || (p.ViewCreditsPurchased - p.ViewCreditsUsed) <= 5))
                            .Select(p => p.UserId);

                        var activeUserIds = _planPurchaseRepo.GetQueryable()
                            .Where(p => !p.IsDeleted && p.ExpiresAt > now)
                            .Select(p => p.UserId);

                        query = query.Where(x => (activeUserIds.Contains(x.Id) && !expiringUserIds.Contains(x.Id)) || (x.IsPremiumMember && !expiringUserIds.Contains(x.Id)));
                    }
                    else if (membershipFilter == "expired")
                    {
                        var activeUserIds = _planPurchaseRepo.GetQueryable()
                            .Where(p => !p.IsDeleted && p.ExpiresAt > now)
                            .Select(p => p.UserId);

                        var expiredUserIds = _planPurchaseRepo.GetQueryable()
                            .Where(p => !p.IsDeleted && p.ExpiresAt <= now)
                            .Select(p => p.UserId);

                        query = query.Where(x => !x.IsPremiumMember && expiredUserIds.Contains(x.Id) && !activeUserIds.Contains(x.Id));
                    }
                    else if (membershipFilter == "no_plan" || membershipFilter == "free")
                    {
                        var anyPlanUserIds = _planPurchaseRepo.GetQueryable()
                            .Where(p => !p.IsDeleted)
                            .Select(p => p.UserId);

                        query = query.Where(x => !x.IsPremiumMember && !anyPlanUserIds.Contains(x.Id));
                    }
                }

                // Search Filter
                string search = !string.IsNullOrEmpty(searchValue) ? searchValue : searchName;
                if (!string.IsNullOrEmpty(search))
                {
                    string lowerSearch = search.ToLower();
                    query = query.Where(x =>
                        (x.Name != null && x.Name.ToLower().Contains(lowerSearch)) ||
                        (x.Phone != null && x.Phone.Contains(search)) ||
                        (x.RegisterNumber != null && x.RegisterNumber.Contains(search))
                    );
                }

                int recordsTotal = await _userRepository.GetQueryable()
                    .Where(x => !x.IsDeleted && x.IsActive && (x.IsComplete || x.IsVerified))
                    .CountAsync();
                int recordsFiltered = await query.CountAsync();

                // Sort
                string sortColumn = sortColumnIndex switch
                {
                    "1" => "RegisterNumber",
                    "2" => "Name",
                    "3" => "Gender",
                    "7" => "CreatedOn",
                    _ => "CreatedOn"
                };
                bool isAscending = sortDirection == "asc";

                if (sortColumn == "RegisterNumber")
                {
                    query = isAscending ? query.OrderBy(x => x.RegisterNumber) : query.OrderByDescending(x => x.RegisterNumber);
                }
                else if (sortColumn == "Name")
                {
                    query = isAscending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name);
                }
                else if (sortColumn == "Gender")
                {
                    query = isAscending ? query.OrderBy(x => x.Gender) : query.OrderByDescending(x => x.Gender);
                }
                else
                {
                    query = isAscending ? query.OrderBy(x => x.CreatedOn) : query.OrderByDescending(x => x.CreatedOn);
                }

                var dbList = await query.Skip(start).Take(length).ToListAsync();

                // Load all staff and assignments in memory to join
                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = new Dictionary<long, string>();
                foreach (var u in staffUsers)
                {
                    staffDict[u.Id] = u.NormalizedUserName ?? u.UserName;
                }

                var pageProfileIds = dbList.Select(x => x.Id).ToList();
                var assignments = await _assignmentRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && pageProfileIds.Contains(x.ProfileId))
                    .ToListAsync();

                var assignmentDict = new Dictionary<long, long>();
                foreach (var a in assignments)
                {
                    assignmentDict[a.ProfileId] = a.StaffId;
                }

                var planPurchases = await _planPurchaseRepo.GetQueryable()
                    .Where(p => !p.IsDeleted && pageProfileIds.Contains(p.UserId))
                    .ToListAsync();

                var data = dbList.Select(x =>
                {
                    var assignedStaffName = "Unassigned";
                    if (assignmentDict.TryGetValue(x.Id, out var staffId))
                    {
                        if (staffDict.TryGetValue(staffId, out var name))
                        {
                            assignedStaffName = name;
                        }
                    }

                    var locationParts = new List<string>();
                    if (!string.IsNullOrEmpty(x.Village)) locationParts.Add(x.Village);
                    if (!string.IsNullOrEmpty(x.District)) locationParts.Add(x.District);
                    if (!string.IsNullOrEmpty(x.State)) locationParts.Add(x.State);
                    var locationStr = string.Join(", ", locationParts);

                    var userPlans = planPurchases.Where(p => p.UserId == x.Id).ToList();
                    var activePlans = userPlans.Where(p => p.ExpiresAt > DateTime.UtcNow).ToList();
                    var activePlan = activePlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();
                    var latestPlan = userPlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();

                    int? remainingCredits = activePlans.Count > 0 
                        ? activePlans.Sum(p => Math.Max(0, p.ViewCreditsPurchased - p.ViewCreditsUsed)) 
                        : (int?)null;

                    string membershipStatus;
                    string? expiryDate = null;
                    int? daysLeft = null;

                    if (activePlan != null)
                    {
                        expiryDate = activePlan.ExpiresAt.ToString("yyyy-MM-dd");
                        daysLeft = (int)Math.Ceiling((activePlan.ExpiresAt - DateTime.UtcNow).TotalDays);
                        if (daysLeft < 0) daysLeft = 0;

                        bool isExpiringSoon = activePlan.ExpiresAt <= DateTime.UtcNow.AddDays(10);
                        bool isLowCredits = (remainingCredits ?? 0) <= 5;

                        if (isExpiringSoon || isLowCredits)
                        {
                            membershipStatus = "Yet to Expire";
                        }
                        else
                        {
                            membershipStatus = "Active Plan";
                        }
                    }
                    else if (latestPlan != null)
                    {
                        membershipStatus = "Expired";
                        expiryDate = latestPlan.ExpiresAt.ToString("yyyy-MM-dd");
                    }
                    else if (x.IsPremiumMember)
                    {
                        membershipStatus = "Active Plan";
                        expiryDate = "Active";
                    }
                    else
                    {
                        membershipStatus = "No Plan";
                        expiryDate = null;
                    }

                    return new
                    {
                        x.Id,
                        x.RegisterNumber,
                        x.Name,
                        x.Gender,
                        Age = CalculateAge(x.DOB),
                        Location = locationStr,
                        x.Phone,
                        CreatedOn = x.CreatedOn.ToString("yyyy-MM-dd"),
                        Status = x.IsPremiumMember ? "Premium" : (x.IsComplete && x.IsVisible && x.IsVerified  ? "Active" : "Pending"),
                        x.IsComplete,
                        x.CompletedStep,
                        x.IsVerified,
                        x.IsVisible,
                        x.DocumentVerificationComplete,
                        x.DocumentVerificationFollowupApproved,
                        AssignedStaff = assignedStaffName,
                        MembershipStatus = membershipStatus,
                        ExpiryDate = expiryDate,
                        RemainingCredits = remainingCredits,
                        DaysLeft = daysLeft
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
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILogger<StaffController>)) as Microsoft.Extensions.Logging.ILogger<StaffController>;
                logger?.LogError(ex, "Error occurred in AssignProfilesData endpoint");
                
                var isPost = HttpContext.Request.Method == "POST";
                var drawVal = isPost ? Request.Form["draw"] : Request.Query["draw"];
                return Json(new
                {
                    draw = !string.IsNullOrEmpty(drawVal) ? Convert.ToInt32(drawVal) : 0,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        [HttpPost("/admin/staff/assign-profiles")]
        public async Task<IActionResult> AssignProfilesPost([FromBody] AssignProfilesModel model)
        {
            if (model == null || model.ProfileIds == null || model.ProfileIds.Count == 0)
            {
                return Json(new { success = false, message = "No profiles selected." });
            }

            try
            {
                var staffUser = await _userManager.FindByIdAsync(model.StaffId.ToString());
                string staffName = staffUser?.NormalizedUserName ?? staffUser?.UserName ?? "Staff";

                var distinctProfileIds = model.ProfileIds.Distinct().ToList();

                foreach (var profileId in distinctProfileIds)
                {
                    // 1. Staff Profile Assignment
                    var existingAssignment = (await _assignmentRepo.Where(x => x.ProfileId == profileId && !x.IsDeleted)).FirstOrDefault();
                    if (existingAssignment != null)
                    {
                        existingAssignment.StaffId = model.StaffId;
                        await _assignmentRepo.Update(existingAssignment);
                    }
                    else
                    {
                        var newAssignment = new StaffProfileAssignment
                        {
                            ProfileId = profileId,
                            StaffId = model.StaffId,
                            IsActive = true
                        };
                        await _assignmentRepo.Add(newAssignment);
                    }

                    // 2. Auto-generate or update Profile Verification Follow-up
                    var existingVerifFollowUp = (await _followUpRepo.Where(f => f.ProfileId == profileId && f.FollowUpType == FollowUpType.ProfileVerification && !f.IsDeleted)).FirstOrDefault();
                    if (existingVerifFollowUp != null)
                    {
                        if (existingVerifFollowUp.AssignedStaffId != model.StaffId)
                        {
                            existingVerifFollowUp.AssignedStaffId = model.StaffId;
                            await _followUpRepo.Update(existingVerifFollowUp);

                            var reassignVerifTimeline = new FollowUpTimeline
                            {
                                FollowUpId = existingVerifFollowUp.Id,
                                StaffId = model.StaffId,
                                StaffName = staffName,
                                ContactType = existingVerifFollowUp.LatestContactType,
                                CallStatus = existingVerifFollowUp.LatestCallStatus,
                                InterestStatus = existingVerifFollowUp.LatestInterestStatus,
                                ProfileVerificationStatus = existingVerifFollowUp.LatestProfileVerificationStatus,
                                RenewalInterestStatus = existingVerifFollowUp.LatestRenewalInterestStatus,
                                Remarks = $"Reassigned to {staffName} (Auto-generated)",
                                NextFollowUpDate = existingVerifFollowUp.NextFollowUpDate,
                                IsActive = true
                            };
                            await _followUpTimelineRepo.Add(reassignVerifTimeline);
                        }
                    }
                    else
                    {
                        var verifFollowUp = new FollowUp
                        {
                            ProfileId = profileId,
                            FollowUpType = FollowUpType.ProfileVerification,
                            LatestContactType = null,
                            LatestCallStatus = null,
                            LatestInterestStatus = null,
                            LatestProfileVerificationStatus = ProfileVerificationStatus.Pending,
                            LatestRenewalInterestStatus = null,
                            LatestRemarks = "Auto-generated on assignment",
                            NextFollowUpDate = null,
                            AssignedStaffId = model.StaffId,
                            IsActive = true
                        };
                        await _followUpRepo.Add(verifFollowUp);
                        await _followUpRepo.SaveChanges();

                        var verifTimeline = new FollowUpTimeline
                        {
                            FollowUpId = verifFollowUp.Id,
                            StaffId = model.StaffId,
                            StaffName = staffName,
                            ContactType = null,
                            CallStatus = null,
                            InterestStatus = null,
                            ProfileVerificationStatus = ProfileVerificationStatus.Pending,
                            RenewalInterestStatus = null,
                            Remarks = "Auto-generated on assignment",
                            NextFollowUpDate = null,
                            IsActive = true
                        };
                        await _followUpTimelineRepo.Add(verifTimeline);
                    }

                    // 3. Auto-generate or update Premium Follow-up (or Renewal Follow-up if already Premium)
                    var userProfile = await _userRepository.Get(profileId);
                    var existingPremFollowUp = (await _followUpRepo.Where(f => f.ProfileId == profileId && f.FollowUpType == FollowUpType.PremiumFollowUp && !f.IsDeleted)).FirstOrDefault();
                    bool isAlreadyConverted = (existingPremFollowUp != null && existingPremFollowUp.LatestInterestStatus == PremiumInterestStatus.Converted && existingPremFollowUp.LatestAdminApprovalStatus == AdminApprovalStatus.Approved) 
                        || (userProfile != null && userProfile.IsPremiumMember);

                    if (isAlreadyConverted)
                    {
                        // Profile is already premium: preserve historical PremiumFollowUp for original staff,
                        // and assign or update Renewal Follow-up for the new staff.
                        var existingRenewalFollowUp = (await _followUpRepo.Where(f => f.ProfileId == profileId && f.FollowUpType == FollowUpType.RenewalFollowUp && !f.IsDeleted)).FirstOrDefault();
                        if (existingRenewalFollowUp != null)
                        {
                            if (existingRenewalFollowUp.AssignedStaffId != model.StaffId)
                            {
                                existingRenewalFollowUp.AssignedStaffId = model.StaffId;
                                await _followUpRepo.Update(existingRenewalFollowUp);

                                var reassignRenewalTimeline = new FollowUpTimeline
                                {
                                    FollowUpId = existingRenewalFollowUp.Id,
                                    StaffId = model.StaffId,
                                    StaffName = staffName,
                                    ContactType = existingRenewalFollowUp.LatestContactType,
                                    CallStatus = existingRenewalFollowUp.LatestCallStatus,
                                    InterestStatus = existingRenewalFollowUp.LatestInterestStatus,
                                    ProfileVerificationStatus = existingRenewalFollowUp.LatestProfileVerificationStatus,
                                    RenewalInterestStatus = existingRenewalFollowUp.LatestRenewalInterestStatus,
                                    Remarks = $"Reassigned to {staffName} (Auto-generated)",
                                    NextFollowUpDate = existingRenewalFollowUp.NextFollowUpDate,
                                    IsActive = true
                                };
                                await _followUpTimelineRepo.Add(reassignRenewalTimeline);
                            }
                        }
                    }
                    else
                    {
                        if (existingPremFollowUp != null)
                        {
                            if (existingPremFollowUp.AssignedStaffId != model.StaffId)
                            {
                                existingPremFollowUp.AssignedStaffId = model.StaffId;
                                await _followUpRepo.Update(existingPremFollowUp);

                                var reassignPremTimeline = new FollowUpTimeline
                                {
                                    FollowUpId = existingPremFollowUp.Id,
                                    StaffId = model.StaffId,
                                    StaffName = staffName,
                                    ContactType = existingPremFollowUp.LatestContactType,
                                    CallStatus = existingPremFollowUp.LatestCallStatus,
                                    InterestStatus = existingPremFollowUp.LatestInterestStatus,
                                    ProfileVerificationStatus = existingPremFollowUp.LatestProfileVerificationStatus,
                                    RenewalInterestStatus = existingPremFollowUp.LatestRenewalInterestStatus,
                                    Remarks = $"Reassigned to {staffName} (Auto-generated)",
                                    NextFollowUpDate = existingPremFollowUp.NextFollowUpDate,
                                    IsActive = true
                                };
                                await _followUpTimelineRepo.Add(reassignPremTimeline);
                            }
                        }
                        else
                        {
                            var premFollowUp = new FollowUp
                            {
                                ProfileId = profileId,
                                FollowUpType = FollowUpType.PremiumFollowUp,
                                LatestContactType = null,
                                LatestCallStatus = null,
                                LatestInterestStatus = null,
                                LatestProfileVerificationStatus = null,
                                LatestRenewalInterestStatus = null,
                                LatestRemarks = "Auto-generated on assignment",
                                NextFollowUpDate = null,
                                AssignedStaffId = model.StaffId,
                                IsActive = true
                            };
                            await _followUpRepo.Add(premFollowUp);
                            await _followUpRepo.SaveChanges();

                            var premTimeline = new FollowUpTimeline
                            {
                                FollowUpId = premFollowUp.Id,
                                StaffId = model.StaffId,
                                StaffName = staffName,
                                ContactType = null,
                                CallStatus = null,
                                InterestStatus = null,
                                ProfileVerificationStatus = null,
                                RenewalInterestStatus = null,
                                Remarks = "Auto-generated on assignment",
                                NextFollowUpDate = null,
                                IsActive = true
                            };
                            await _followUpTimelineRepo.Add(premTimeline);
                        }
                    }
                }

                await _assignmentRepo.SaveChanges();
                await _followUpRepo.SaveChanges();
                await _followUpTimelineRepo.SaveChanges();

                return Json(new { success = true, message = "Profiles assigned successfully and follow-ups auto-generated." });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILogger<StaffController>)) as Microsoft.Extensions.Logging.ILogger<StaffController>;
                logger?.LogError(ex, "Error occurred in AssignProfilesPost endpoint");
                return Json(new { success = false, message = "An error occurred while assigning profiles: " + ex.Message });
            }
        }

        [HttpPost("/admin/staff/bulk-renewal-followup")]
        public async Task<IActionResult> BulkRenewalFollowUp([FromBody] AssignProfilesModel model)
        {
            if (model == null || model.ProfileIds == null || model.ProfileIds.Count == 0)
            {
                return Json(new { success = false, message = "No profiles selected." });
            }

            try
            {
                var distinctProfileIds = model.ProfileIds.Distinct().ToList();
                var now = DateTime.UtcNow;
                var tenDaysLater = now.AddDays(10);

                var profiles = await _userRepository.GetQueryable()
                    .Where(x => distinctProfileIds.Contains(x.Id) && !x.IsDeleted && x.IsActive)
                    .ToListAsync();

                var assignments = await _assignmentRepo.GetQueryable()
                    .Where(x => distinctProfileIds.Contains(x.ProfileId) && !x.IsDeleted && x.IsActive)
                    .ToListAsync();

                var planPurchases = await _planPurchaseRepo.GetQueryable()
                    .Where(x => distinctProfileIds.Contains(x.UserId) && !x.IsDeleted)
                    .ToListAsync();

                var existingFollowUps = await _followUpRepo.GetQueryable()
                    .Where(f => distinctProfileIds.Contains(f.ProfileId) && f.FollowUpType == FollowUpType.RenewalFollowUp && !f.IsDeleted)
                    .ToListAsync();

                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = new Dictionary<long, string>();
                foreach (var u in staffUsers)
                {
                    staffDict[u.Id] = u.NormalizedUserName ?? u.UserName ?? "Staff";
                }

                int addedCount = 0;
                int skippedUnassignedCount = 0;
                int skippedNotEligibleCount = 0;

                foreach (var profileId in distinctProfileIds)
                {
                    var profile = profiles.FirstOrDefault(p => p.Id == profileId);
                    if (profile == null)
                    {
                        skippedNotEligibleCount++;
                        continue;
                    }

                    // Condition 1: Profile must be assigned to a staff member
                    var assignment = assignments.FirstOrDefault(a => a.ProfileId == profileId);
                    if (assignment == null || assignment.StaffId <= 0)
                    {
                        skippedUnassignedCount++;
                        continue;
                    }

                    long staffId = assignment.StaffId;
                    string staffName = staffDict.TryGetValue(staffId, out var sName) ? sName : "Staff";

                    // Condition 2: Premium expires in 10 days OR credits left <= 5
                    var userPlans = planPurchases.Where(p => p.UserId == profileId).ToList();
                    var activePlans = userPlans.Where(p => p.ExpiresAt > now).ToList();
                    var activePlan = activePlans.OrderByDescending(p => p.ExpiresAt).FirstOrDefault();

                    int remainingCredits = activePlans.Sum(p => Math.Max(0, p.ViewCreditsPurchased - p.ViewCreditsUsed));

                    bool isEligibleForRenewal = false;
                    if (activePlan != null)
                    {
                        bool isExpiringSoon = activePlan.ExpiresAt <= tenDaysLater;
                        bool isLowCredits = remainingCredits <= 5;
                        if (isExpiringSoon || isLowCredits)
                        {
                            isEligibleForRenewal = true;
                        }
                    }
                    else if (userPlans.Any(p => p.ExpiresAt <= now))
                    {
                        // Expired plan: <= 10 days and <= 5 credits
                        isEligibleForRenewal = true;
                    }

                    if (!isEligibleForRenewal)
                    {
                        skippedNotEligibleCount++;
                        continue;
                    }

                    // Create or update Renewal Follow-up
                    // If an existing renewal follow-up is already Renewed, do not overwrite it — create a new follow-up for the new cycle!
                    var existingRenewal = existingFollowUps.FirstOrDefault(f => f.ProfileId == profileId && f.LatestRenewalInterestStatus != RenewalInterestStatus.Renewed);
                    if (existingRenewal != null)
                    {
                        existingRenewal.AssignedStaffId = staffId;
                        existingRenewal.IsActive = true;
                        existingRenewal.LatestRenewalInterestStatus = RenewalInterestStatus.Pending;
                        existingRenewal.LatestRemarks = "Bulk renewal follow-up added by Admin";
                        await _followUpRepo.Update(existingRenewal);

                        var timeline = new FollowUpTimeline
                        {
                            FollowUpId = existingRenewal.Id,
                            StaffId = staffId,
                            StaffName = staffName,
                            ContactType = existingRenewal.LatestContactType,
                            CallStatus = existingRenewal.LatestCallStatus,
                            InterestStatus = existingRenewal.LatestInterestStatus,
                            ProfileVerificationStatus = existingRenewal.LatestProfileVerificationStatus,
                            RenewalInterestStatus = RenewalInterestStatus.Pending,
                            Remarks = $"Bulk renewal follow-up assigned to {staffName}",
                            NextFollowUpDate = existingRenewal.NextFollowUpDate,
                            IsActive = true
                        };
                        await _followUpTimelineRepo.Add(timeline);
                    }
                    else
                    {
                        var newRenewal = new FollowUp
                        {
                            ProfileId = profileId,
                            FollowUpType = FollowUpType.RenewalFollowUp,
                            LatestContactType = null,
                            LatestCallStatus = null,
                            LatestInterestStatus = null,
                            LatestProfileVerificationStatus = null,
                            LatestRenewalInterestStatus = RenewalInterestStatus.Pending,
                            LatestRemarks = "Bulk renewal follow-up added by Admin",
                            NextFollowUpDate = null,
                            AssignedStaffId = staffId,
                            IsActive = true
                        };
                        await _followUpRepo.Add(newRenewal);
                        await _followUpRepo.SaveChanges();

                        var timeline = new FollowUpTimeline
                        {
                            FollowUpId = newRenewal.Id,
                            StaffId = staffId,
                            StaffName = staffName,
                            ContactType = null,
                            CallStatus = null,
                            InterestStatus = null,
                            ProfileVerificationStatus = null,
                            RenewalInterestStatus = RenewalInterestStatus.Pending,
                            Remarks = "Bulk renewal follow-up added by Admin",
                            NextFollowUpDate = null,
                            IsActive = true
                        };
                        await _followUpTimelineRepo.Add(timeline);
                    }

                    addedCount++;
                }

                await _followUpRepo.SaveChanges();
                await _followUpTimelineRepo.SaveChanges();

                if (addedCount == 0)
                {
                    string reason = "";
                    if (skippedUnassignedCount > 0 && skippedNotEligibleCount > 0)
                        reason = $"{skippedUnassignedCount} profile(s) are unassigned and {skippedNotEligibleCount} profile(s) do not have expiring premium/low credits.";
                    else if (skippedUnassignedCount > 0)
                        reason = $"{skippedUnassignedCount} profile(s) are not assigned to any staff member.";
                    else
                        reason = $"{skippedNotEligibleCount} profile(s) do not have premium expiring within 10 days or credits ≤ 5.";

                    return Json(new
                    {
                        success = false,
                        message = $"No renewal follow-ups were added. {reason}",
                        addedCount = 0,
                        skippedUnassignedCount = skippedUnassignedCount,
                        skippedNotEligibleCount = skippedNotEligibleCount
                    });
                }

                int totalSkipped = skippedUnassignedCount + skippedNotEligibleCount;
                string successMsg = $"{addedCount} profile(s) successfully added for Renewal Follow-up to their assigned staff.";
                if (totalSkipped > 0)
                {
                    successMsg += $" ({totalSkipped} profile(s) skipped: {skippedUnassignedCount} unassigned, {skippedNotEligibleCount} not meeting expiry/credit criteria).";
                }

                return Json(new
                {
                    success = true,
                    message = successMsg,
                    addedCount = addedCount,
                    skippedUnassignedCount = skippedUnassignedCount,
                    skippedNotEligibleCount = skippedNotEligibleCount
                });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILogger<StaffController>)) as Microsoft.Extensions.Logging.ILogger<StaffController>;
                logger?.LogError(ex, "Error occurred in BulkRenewalFollowUp endpoint");
                return Json(new { success = false, message = "An error occurred while adding bulk renewal follow-ups: " + ex.Message });
            }
        }

        [HttpPost("/admin/staff/trigger-auto-renewal-check")]
        public async Task<IActionResult> TriggerAutoRenewalCheck([FromServices] IRenewalFollowUpProcessor processor)
        {
            try
            {
                var result = await processor.ProcessAutoRenewalFollowUpsAsync();
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    eligibleCount = result.EligibleCount,
                    addedCount = result.AddedCount,
                    reopenedCount = result.ReopenedCount,
                    skippedActiveCount = result.SkippedActiveCount,
                    skippedNotInterestedCount = result.SkippedNotInterestedCount,
                    skippedNotEligibleCount = result.SkippedNotEligibleCount
                });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILogger<StaffController>)) as Microsoft.Extensions.Logging.ILogger<StaffController>;
                logger?.LogError(ex, "Error occurred in TriggerAutoRenewalCheck endpoint");
                return Json(new { success = false, message = "An error occurred while running the auto-renewal check: " + ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private int CalculateAge(string dobString)
        {
            if (string.IsNullOrEmpty(dobString)) return 0;
            try
            {
                var parts = dobString.Replace("-", "/").Split('/');
                if (parts.Length < 3) return 0;

                string day = parts[0].PadLeft(2, '0');
                string month = parts[1].PadLeft(2, '0');
                string year = parts[2];

                if (day.Length > 2)
                {
                    var temp = day;
                    day = year;
                    year = temp;
                }

                var parsedDate = DateTime.ParseExact($"{day}/{month}/{year}", "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var today = DateTime.UtcNow;
                int age = today.Year - parsedDate.Year;
                if (parsedDate > today.AddYears(-age))
                {
                    age--;
                }
                return age;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Read-Only Profile View
        
        [HttpGet("/admin/staff/profile/{id:long}")]
        [HttpGet("/admin/staff/view-profile/{id:long}")]
        public async Task<IActionResult> ProfileDetails(long id)
        {
            var user = await _dbContext.Registration.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            // Decrypt password
            string decryptedPassword = "";
            if (!string.IsNullOrEmpty(user.Password))
            {
                try
                {
                    var encryptionKey = new Guid("F7AD797A-416C-4C56-8749-7017FBC90427");
                    var encryption = new RijndaelManagedEncryption(encryptionKey);
                    decryptedPassword = encryption.DecryptRijndael(user.Password, user.PasswordHash);
                }
                catch
                {
                    decryptedPassword = "";
                }
            }

            // Lookups
            var profileFor = await _dbContext.ProfileFor.FirstOrDefaultAsync(x => x.Id == user.ProfileForId && !x.IsDeleted);
            var religion = await _dbContext.ReligionCaste.FirstOrDefaultAsync(x => x.Id == user.ReligionId && !x.IsDeleted);
            var nationality = await _dbContext.Nationality.FirstOrDefaultAsync(x => x.Id == user.NationalityId && !x.IsDeleted);
            var maritalStatus = await _dbContext.MaritalStatus.FirstOrDefaultAsync(x => x.Id == user.MaritalStatusId && !x.IsDeleted);
            var height = await _dbContext.BodyFeatures.FirstOrDefaultAsync(x => x.Id == user.HeightId && !x.IsDeleted);
            var weight = await _dbContext.BodyFeatures.FirstOrDefaultAsync(x => x.Id == user.WeightId && !x.IsDeleted);
            var complexion = await _dbContext.BodyFeatures.FirstOrDefaultAsync(x => x.Id == user.ComplexionId && !x.IsDeleted);
            var bodyType = await _dbContext.BodyFeatures.FirstOrDefaultAsync(x => x.Id == user.BodyTypeId && !x.IsDeleted);
            var profession = await _dbContext.Profession.FirstOrDefaultAsync(x => x.Id == user.ProfessionId && !x.IsDeleted);
            var motherTongue = await _dbContext.MotherTongue.FirstOrDefaultAsync(x => x.Id == user.MotherTongueId && !x.IsDeleted);
            var community = await _dbContext.Community.FirstOrDefaultAsync(x => x.Id == user.CommunityId && !x.IsDeleted);
            var religiousness = await _dbContext.Religiousness.FirstOrDefaultAsync(x => x.Id == user.ReligiousnessId && !x.IsDeleted);
            var financialStatus = await _dbContext.FinancialStatus.FirstOrDefaultAsync(x => x.Id == user.FinancialStatusId && !x.IsDeleted);

            // Photos
            var userImages = await _dbContext.Images.FirstOrDefaultAsync(x => x.UserId == id && !x.IsDeleted);

            // Active plan
            var activePlan = await _planPurchaseRepo.GetQueryable()
                .Where(p => p.UserId == id && !p.IsDeleted && p.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(p => p.ExpiresAt)
                .FirstOrDefaultAsync();

            // Assignment & Staff
            var assignment = await _assignmentRepo.GetQueryable()
                .FirstOrDefaultAsync(a => a.ProfileId == id && !a.IsDeleted);
            string assignedStaffName = "Unassigned";
            if (assignment != null)
            {
                var staffUser = await _userManager.FindByIdAsync(assignment.StaffId.ToString());
                if (staffUser != null)
                {
                    assignedStaffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Staff";
                }
            }

            // Followups
            var followUps = await _followUpRepo.GetQueryable()
                .Where(f => f.ProfileId == id && !f.IsDeleted)
                .Include(f => f.Timelines.Where(t => !t.IsDeleted))
                .OrderByDescending(f => f.CreatedOn)
                .ToListAsync();

            var verificationDocs = await _dbContext.VerificationDocuments
                .Where(d => d.ProfileId == id && !d.IsDeleted)
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.CreatedOn)
                .ToListAsync();

            var model = new StaffProfileViewDetailModel
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
                StateTitle = user.State,
                DistrictTitle = user.District,
                CityTitle = user.Village,
                PresentStateTitle = user.PresentState,
                PresentDistrictTitle = user.PresentDistrict,
                PresentCityTitle = user.PresentCity,
                UserImages = userImages,
                ActivePlan = activePlan,
                AssignedStaffName = assignedStaffName,
                AssignedStaffId = assignment?.StaffId,
                FollowUps = followUps,
                VerificationDocuments = verificationDocs,
                Age = CalculateAge(user.DOB),
                DecryptedPassword = decryptedPassword
            };

            return View("ProfileDetails", model);
        }

        #endregion

        #region Follow-up Management

        [HttpGet("/admin/staff/profile-details/{profileId:long}")]
        public async Task<IActionResult> GetProfileDetails(long profileId)
        {
            try
            {
                var profile = await _dbContext.Registration.FirstOrDefaultAsync(x => x.Id == profileId);
                if (profile == null)
                {
                    return Json(new { success = false, message = "Profile not found." });
                }

                // Get assigned staff
                var assignment = await _dbContext.StaffProfileAssignments
                    .FirstOrDefaultAsync(a => a.ProfileId == profileId && !a.IsDeleted);
                string assignedStaffName = "Unassigned";
                long assignedStaffId = 0;
                if (assignment != null && assignment.StaffId > 0)
                {
                    assignedStaffId = assignment.StaffId;
                    var staffUser = await _userManager.FindByIdAsync(assignment.StaffId.ToString());
                    if (staffUser != null)
                    {
                        assignedStaffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Staff";
                    }
                }

                // Resolve location
                var locationParts = new List<string>();
                if (!string.IsNullOrEmpty(profile.Village)) locationParts.Add(profile.Village);
                if (!string.IsNullOrEmpty(profile.District)) locationParts.Add(profile.District);
                if (!string.IsNullOrEmpty(profile.State)) locationParts.Add(profile.State);
                var locationStr = string.Join(", ", locationParts);

                // Fetch verification documents
                var verificationDocs = await _dbContext.VerificationDocuments
                    .Where(d => d.ProfileId == profileId && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToListAsync();

                // Fetch follow-ups with timelines
                var followUps = await _dbContext.FollowUps
                    .Where(f => f.ProfileId == profileId && !f.IsDeleted)
                    .Include(f => f.Profile)
                    .Include(f => f.Timelines)
                    .ToListAsync();

                var followUpData = followUps.Select(f => new
                {
                    f.Id,
                    FollowUpType = f.FollowUpType.ToString(),
                    FollowUpTypeEnum = (int)f.FollowUpType,
                    CreatedOn = f.CreatedOn.ToString("dd-MMM-yyyy"),
                    CreatedOnFull = f.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                    LatestContactType = f.LatestContactType?.ToString() ?? "N/A",
                    LatestCallStatus = f.LatestCallStatus?.ToString() ?? "N/A",
                    LatestInterestStatus = f.LatestInterestStatus?.ToString() ?? "N/A",
                    LatestProfileVerificationStatus = f.LatestProfileVerificationStatus?.ToString() ?? "N/A",
                    LatestRenewalInterestStatus = f.LatestRenewalInterestStatus?.ToString() ?? "N/A",
                    LatestRemarks = f.LatestRemarks ?? "No remarks yet",
                    NextFollowUpDate = f.NextFollowUpDate.HasValue ? f.NextFollowUpDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    VerificationDocumentUrl = f.Profile?.VerificationDocumentUrl ?? profile.VerificationDocumentUrl,
                    VerificationDocuments = verificationDocs.Select(d => new
                    {
                        d.Id,
                        d.DocumentUrl,
                        d.DocumentType,
                        d.OriginalFileName
                    }).ToList(),
                    DocumentVerificationEnabled = f.Profile?.DocumentVerificationEnabled ?? profile.DocumentVerificationEnabled,
                    DocumentVerificationComplete = f.Profile?.DocumentVerificationComplete ?? profile.DocumentVerificationComplete,
                    DocumentVerificationRejected = f.Profile?.DocumentVerificationRejected ?? profile.DocumentVerificationRejected,
                    DocumentVerificationFollowupApproved = f.Profile?.DocumentVerificationFollowupApproved ?? profile.DocumentVerificationFollowupApproved,
                    LatestAdminApprovalStatus = f.LatestAdminApprovalStatus?.ToString() ?? "None",
                    Timelines = (f.Timelines ?? new List<FollowUpTimeline>())
                        .Where(t => t != null && !t.IsDeleted)
                        .OrderByDescending(t => t.CreatedOn)
                        .Select(t => new
                        {
                            t.Id,
                            CreatedOn = t.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                            StaffName = t.StaffName ?? "Staff",
                            t.StaffId,
                            ContactType = t.ContactType?.ToString() ?? "N/A",
                            CallStatus = t.CallStatus?.ToString() ?? "N/A",
                            InterestStatus = t.InterestStatus?.ToString() ?? "N/A",
                            ProfileVerificationStatus = t.ProfileVerificationStatus?.ToString() ?? "N/A",
                            RenewalInterestStatus = t.RenewalInterestStatus?.ToString() ?? "N/A",
                            Remarks = t.Remarks ?? string.Empty,
                            NextFollowUpDate = t.NextFollowUpDate.HasValue ? t.NextFollowUpDate.Value.ToString("yyyy-MM-dd") : "N/A"
                        }).ToList()
                }).ToList();

                // Fetch plan purchase details for remaining credits and expiry date
                var planPurchases = await _dbContext.PlanPurchases
                    .Where(p => p.UserId == profileId && !p.IsDeleted)
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
                    ? activePlan.ExpiresAt.ToString("yyyy-MM-dd") 
                    : (latestPlan != null ? latestPlan.ExpiresAt.ToString("yyyy-MM-dd") + " (Expired)" : "N/A");

                var profileData = new
                {
                    profile.Id,
                    RegisterNumber = profile.RegisterNumber ?? "N/A",
                    Name = profile.Name ?? "N/A",
                    Email = profile.Email ?? "N/A",
                    Gender = profile.Gender ?? "N/A",
                    Age = CalculateAge(profile.DOB),
                    Location = string.IsNullOrEmpty(locationStr) ? "N/A" : locationStr,
                    Phone = profile.Phone ?? "N/A",
                    CountryCode = profile.CountryCode ?? "+91",
                    ImagePath = profile.ImagePath,
                    CreatedOn = profile.CreatedOn.ToString("yyyy-MM-dd"),
                    Status = profile.IsPremiumMember ? "Premium" : (profile.IsComplete && profile.IsVisible && profile.IsVerified ? "Active" : "Pending"),
                    IsPremiumMember = profile.IsPremiumMember,
                    IsActive = profile.IsActive,
                    IsVerified = profile.IsVerified,
                    IsComplete = profile.IsComplete,
                    IsVisible = profile.IsVisible,
                    DisabledReason = profile.DisabledReason,
                    DocumentVerificationEnabled = profile.DocumentVerificationEnabled,
                    VerificationDocumentUrl = profile.VerificationDocumentUrl,
                    VerificationDocuments = verificationDocs.Select(d => new
                    {
                        d.Id,
                        d.DocumentUrl,
                        d.DocumentType,
                        d.OriginalFileName
                    }).ToList(),
                    DocumentVerificationComplete = profile.DocumentVerificationComplete,
                    DocumentVerificationRejected = profile.DocumentVerificationRejected,
                    DocumentVerificationFollowupApproved = profile.DocumentVerificationFollowupApproved,
                    RemainingCredits = activeCredits.ToString(),
                    ExpiryDate = expiryDate,
                    AssignedStaff = assignedStaffName,
                    AssignedStaffId = assignedStaffId,
                    FollowUps = followUpData
                };

                return Json(new { success = true, data = profileData });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILogger<StaffController>)) as Microsoft.Extensions.Logging.ILogger<StaffController>;
                logger?.LogError(ex, "Error occurred in GetProfileDetails for profile {ProfileId}", profileId);
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost("/admin/staff/follow-up")]
        public async Task<IActionResult> SaveFollowUp([FromBody] SaveFollowUpModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            if (string.IsNullOrWhiteSpace(model.Remarks))
            {
                return Json(new { success = false, message = "Remarks / instructions are required." });
            }

            var followUpTypes = new List<FollowUpType>();
            if (model.FollowUpTypes != null && model.FollowUpTypes.Count > 0)
            {
                followUpTypes = model.FollowUpTypes.Distinct().ToList();
            }
            else if (model.FollowUpType.HasValue)
            {
                followUpTypes.Add(model.FollowUpType.Value);
            }

            if (followUpTypes.Count == 0)
            {
                return Json(new { success = false, message = "Please select at least one follow-up type." });
            }

            try
            {
                // Check if profile exists
                var profile = await _dbContext.Registration.FirstOrDefaultAsync(x => x.Id == model.ProfileId && !x.IsDeleted);
                if (profile == null)
                {
                    return Json(new { success = false, message = "Profile not found." });
                }

                // Get assigned staff
                var assignment = await _dbContext.StaffProfileAssignments
                    .FirstOrDefaultAsync(a => a.ProfileId == model.ProfileId && !a.IsDeleted);
                long staffId = 0;
                string staffName = "Admin";
                if (assignment != null && assignment.StaffId > 0)
                {
                    staffId = assignment.StaffId;
                    var staffUser = await _userManager.FindByIdAsync(staffId.ToString());
                    if (staffUser != null)
                    {
                        staffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Staff";
                    }
                }
                else
                {
                    // Fallback to logged-in user
                    var loggedInUser = await _userManager.GetUserAsync(User);
                    if (loggedInUser != null)
                    {
                        staffId = loggedInUser.Id;
                        staffName = loggedInUser.NormalizedUserName ?? loggedInUser.UserName ?? "Admin";
                    }
                }

                int createdCount = 0;
                int skippedCount = 0;

                foreach (var fType in followUpTypes)
                {
                    // Enforce one follow-up of each type per profile
                    var existing = await _dbContext.FollowUps
                        .FirstOrDefaultAsync(f => f.ProfileId == model.ProfileId && f.FollowUpType == fType && !f.IsDeleted);

                    if (existing != null)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Renewal follow-up cannot be added to non-premium/free users who never had a plan
                    if (fType == FollowUpType.RenewalFollowUp)
                    {
                        var hasPlanOrPremium = profile.IsPremiumMember || await _dbContext.PlanPurchases.AnyAsync(p => p.UserId == model.ProfileId && !p.IsDeleted);
                        if (!hasPlanOrPremium)
                        {
                            skippedCount++;
                            continue;
                        }
                    }

                    var followUp = new FollowUp
                    {
                        ProfileId = model.ProfileId,
                        FollowUpType = fType,
                        LatestContactType = null,
                        LatestCallStatus = null,
                        LatestInterestStatus = null,
                        LatestProfileVerificationStatus = fType == FollowUpType.ProfileVerification ? ProfileVerificationStatus.Pending : null,
                        LatestRenewalInterestStatus = fType == FollowUpType.RenewalFollowUp ? RenewalInterestStatus.Pending : null,
                        LatestRemarks = model.Remarks,
                        NextFollowUpDate = null,
                        AssignedStaffId = staffId > 0 ? staffId : null,
                        IsActive = true
                    };

                    await _dbContext.FollowUps.AddAsync(followUp);
                    await _dbContext.SaveChangesAsync();

                    var timeline = new FollowUpTimeline
                    {
                        FollowUpId = followUp.Id,
                        StaffId = staffId,
                        StaffName = staffName,
                        ContactType = null,
                        CallStatus = null,
                        InterestStatus = null,
                        ProfileVerificationStatus = fType == FollowUpType.ProfileVerification ? ProfileVerificationStatus.Pending : null,
                        RenewalInterestStatus = fType == FollowUpType.RenewalFollowUp ? RenewalInterestStatus.Pending : null,
                        Remarks = model.Remarks,
                        NextFollowUpDate = null,
                        IsActive = true
                    };

                    await _dbContext.FollowUpTimelines.AddAsync(timeline);
                    await _dbContext.SaveChangesAsync();

                    createdCount++;
                }

                if (createdCount == 0 && skippedCount > 0)
                {
                    return Json(new { success = false, message = "All selected follow-up type(s) already exist for this profile." });
                }

                string msg = createdCount == 1 
                    ? "Follow-up initialized successfully." 
                    : $"Successfully initialized {createdCount} follow-up(s).";

                if (skippedCount > 0)
                {
                    msg += $" ({skippedCount} skipped as already existing)";
                }

                return Json(new { success = true, message = msg, createdCount = createdCount });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILogger<StaffController>)) as Microsoft.Extensions.Logging.ILogger<StaffController>;
                logger?.LogError(ex, "Error in SaveFollowUp for profile {ProfileId}", model.ProfileId);
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost("/admin/staff/bulk-follow-up")]
        public async Task<IActionResult> BulkSaveFollowUp([FromBody] BulkFollowUpModel model)
        {
            if (model == null || model.ProfileIds == null || model.ProfileIds.Count == 0)
            {
                return Json(new { success = false, message = "No profiles selected." });
            }

            if (string.IsNullOrWhiteSpace(model.Remarks))
            {
                return Json(new { success = false, message = "Remarks / instructions are required." });
            }

            var followUpTypes = new List<FollowUpType>();
            if (model.FollowUpTypes != null && model.FollowUpTypes.Count > 0)
            {
                followUpTypes = model.FollowUpTypes.Distinct().ToList();
            }
            else if (model.FollowUpType.HasValue)
            {
                followUpTypes.Add(model.FollowUpType.Value);
            }

            if (followUpTypes.Count == 0)
            {
                return Json(new { success = false, message = "Please select at least one follow-up type." });
            }

            try
            {
                int successProfileCount = 0;
                int totalFollowUpsCreated = 0;
                int skippedDuplicateCount = 0;
                int skippedNotFoundCount = 0;
                int skippedUnassignedCount = 0;

                var distinctProfileIds = model.ProfileIds.Distinct().ToList();

                foreach (var profileId in distinctProfileIds)
                {
                    var profile = await _dbContext.Registration.FirstOrDefaultAsync(x => x.Id == profileId && !x.IsDeleted && x.IsActive && (x.IsComplete || x.IsVerified));
                    if (profile == null)
                    {
                        skippedNotFoundCount++;
                        continue;
                    }

                    // Enforce that profile must be assigned to a staff member
                    var assignment = await _dbContext.StaffProfileAssignments.FirstOrDefaultAsync(a => a.ProfileId == profileId && !a.IsDeleted);
                    if (assignment == null || assignment.StaffId <= 0)
                    {
                        skippedUnassignedCount++;
                        continue;
                    }

                    long staffId = assignment.StaffId;
                    string staffName = "Staff";
                    var staffUser = await _userManager.FindByIdAsync(staffId.ToString());
                    if (staffUser != null)
                    {
                        staffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Staff";
                    }

                    int createdForThisProfile = 0;

                    foreach (var fType in followUpTypes)
                    {
                        // Enforce one follow-up of each type per profile
                        var existing = await _dbContext.FollowUps.FirstOrDefaultAsync(f => f.ProfileId == profileId && f.FollowUpType == fType && !f.IsDeleted);
                        if (existing != null)
                        {
                            skippedDuplicateCount++;
                            continue;
                        }

                        // Renewal follow-up cannot be added to non-premium/free users who never had a plan
                        if (fType == FollowUpType.RenewalFollowUp)
                        {
                            var hasPlanOrPremium = profile.IsPremiumMember || await _dbContext.PlanPurchases.AnyAsync(p => p.UserId == profileId && !p.IsDeleted);
                            if (!hasPlanOrPremium)
                            {
                                continue;
                            }
                        }

                        var followUp = new FollowUp
                        {
                            ProfileId = profileId,
                            FollowUpType = fType,
                            LatestContactType = null,
                            LatestCallStatus = null,
                            LatestInterestStatus = null,
                            LatestProfileVerificationStatus = fType == FollowUpType.ProfileVerification ? ProfileVerificationStatus.Pending : null,
                            LatestRenewalInterestStatus = fType == FollowUpType.RenewalFollowUp ? RenewalInterestStatus.Pending : null,
                            LatestRemarks = model.Remarks,
                            NextFollowUpDate = null,
                            AssignedStaffId = staffId,
                            IsActive = true
                        };

                        await _dbContext.FollowUps.AddAsync(followUp);
                        await _dbContext.SaveChangesAsync();

                        var timeline = new FollowUpTimeline
                        {
                            FollowUpId = followUp.Id,
                            StaffId = staffId,
                            StaffName = staffName,
                            ContactType = null,
                            CallStatus = null,
                            InterestStatus = null,
                            ProfileVerificationStatus = fType == FollowUpType.ProfileVerification ? ProfileVerificationStatus.Pending : null,
                            RenewalInterestStatus = fType == FollowUpType.RenewalFollowUp ? RenewalInterestStatus.Pending : null,
                            Remarks = model.Remarks,
                            NextFollowUpDate = null,
                            IsActive = true
                        };

                        await _dbContext.FollowUpTimelines.AddAsync(timeline);
                        await _dbContext.SaveChangesAsync();

                        createdForThisProfile++;
                        totalFollowUpsCreated++;
                    }

                    if (createdForThisProfile > 0)
                    {
                        successProfileCount++;
                    }
                }

                if (totalFollowUpsCreated == 0 && skippedUnassignedCount > 0 && skippedDuplicateCount == 0 && skippedNotFoundCount == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Follow-ups can only be added to staff-assigned profiles. The selected profile(s) are not assigned to any staff member."
                    });
                }

                string message = $"Successfully initialized {totalFollowUpsCreated} follow-up(s) across {successProfileCount} profile(s).";
                var details = new List<string>();
                if (skippedUnassignedCount > 0)
                {
                    details.Add($"{skippedUnassignedCount} profile(s) skipped (not assigned to staff)");
                }
                if (skippedDuplicateCount > 0)
                {
                    details.Add($"{skippedDuplicateCount} follow-up(s) skipped (already existing)");
                }
                if (skippedNotFoundCount > 0)
                {
                    details.Add($"{skippedNotFoundCount} profile(s) skipped (not found)");
                }
                if (details.Count > 0)
                {
                    message += $" ({string.Join(", ", details)})";
                }

                return Json(new
                {
                    success = true,
                    message = message,
                    successCount = successProfileCount,
                    totalFollowUpsCreated = totalFollowUpsCreated,
                    skippedUnassignedCount = skippedUnassignedCount,
                    skippedDuplicateCount = skippedDuplicateCount,
                    skippedNotFoundCount = skippedNotFoundCount
                });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILogger<StaffController>)) as Microsoft.Extensions.Logging.ILogger<StaffController>;
                logger?.LogError(ex, "Error in BulkSaveFollowUp");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet("/admin/staff/profile-verifications")]
        public async Task<IActionResult> ProfileVerifications()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.StaffList = staffUsers.Select(u => new
            {
                Id = u.Id,
                Name = u.NormalizedUserName ?? u.UserName
            }).ToList();

            return View();
        }

        [AcceptVerbs("GET", "POST")]
        [Route("/admin/staff/profile-verifications-data")]
        public async Task<IActionResult> ProfileVerificationsData(
            long? staffId,
            int? callStatus,
            int? verificationStatus,
            int? interestStatus,
            string? scheduleFilter,
            string searchName,
            string? gender)
        {
            try
            {
                var isPost = HttpContext.Request.Method == "POST";
                var drawVal = isPost ? Request.Form["draw"] : Request.Query["draw"];
                var startVal = isPost ? Request.Form["start"] : Request.Query["start"];
                var lengthVal = isPost ? Request.Form["length"] : Request.Query["length"];

                int draw = !string.IsNullOrEmpty(drawVal) ? Convert.ToInt32(drawVal) : 0;
                int start = !string.IsNullOrEmpty(startVal) ? Convert.ToInt32(startVal) : 0;
                int length = !string.IsNullOrEmpty(lengthVal) ? Convert.ToInt32(lengthVal) : 100;
                if (length <= 0) length = 100;

                string? searchValue = isPost ? Request.Form["search[value]"] : Request.Query["search[value]"];
                string? sortColumnIndex = isPost ? Request.Form["order[0][column]"] : Request.Query["order[0][column]"];
                string? sortDirection = isPost ? Request.Form["order[0][dir]"] : Request.Query["order[0][dir]"];

                // Base query: Only ProfileVerification follow-ups
                IQueryable<FollowUp> query = _followUpRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.FollowUpType == FollowUpType.ProfileVerification)
                    .Include(x => x.Profile);

                // Filter by Gender
                if (!string.IsNullOrEmpty(gender))
                {
                    query = query.Where(x => x.Profile != null && x.Profile.Gender != null && x.Profile.Gender.ToLower() == gender.ToLower());
                }

                // Filter by Staff
                if (staffId.HasValue && staffId.Value > 0)
                {
                    query = query.Where(x => x.AssignedStaffId == staffId.Value);
                }

                // Filter by Call Status
                if (callStatus.HasValue)
                {
                    var status = (CallStatus)callStatus.Value;
                    query = query.Where(x => x.LatestCallStatus == status);
                }

                // Filter by Verification Status (ProfileVerificationStatus)
                var verifStatus = verificationStatus ?? interestStatus;
                if (verifStatus.HasValue)
                {
                    var status = (ProfileVerificationStatus)verifStatus.Value;
                    query = query.Where(x => x.LatestProfileVerificationStatus == status);
                }

                // Filter by Follow-up Schedule (Today's Follow-ups)
                string? schedule = !string.IsNullOrEmpty(scheduleFilter) 
                    ? scheduleFilter 
                    : (isPost ? Request.Form["scheduleFilter"].ToString() : Request.Query["scheduleFilter"].ToString());

                if (!string.IsNullOrEmpty(schedule) && string.Equals(schedule, "today", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime todayDate;
                    try
                    {
                        var tzi = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
                        todayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi).Date;
                    }
                    catch
                    {
                        todayDate = DateTime.UtcNow.AddHours(5.5).Date;
                    }

                    DateTime istStartUtc = todayDate.AddHours(-5.5);
                    DateTime istEndUtc = istStartUtc.AddDays(1);
                    DateTime localStart = todayDate;
                    DateTime localEnd = localStart.AddDays(1);
                    DateTime utcStart = DateTime.UtcNow.Date;
                    DateTime utcEnd = utcStart.AddDays(1);

                    query = query.Where(x =>
                        (x.CreatedOn >= istStartUtc && x.CreatedOn < istEndUtc) ||
                        (x.CreatedOn >= localStart && x.CreatedOn < localEnd) ||
                        (x.CreatedOn >= utcStart && x.CreatedOn < utcEnd) ||
                        (x.NextFollowUpDate.HasValue && (
                            (x.NextFollowUpDate.Value >= istStartUtc && x.NextFollowUpDate.Value < istEndUtc) ||
                            (x.NextFollowUpDate.Value >= localStart && x.NextFollowUpDate.Value < localEnd) ||
                            (x.NextFollowUpDate.Value >= utcStart && x.NextFollowUpDate.Value < utcEnd)
                        )) ||
                        x.Timelines.Any(t =>
                            (t.CreatedOn >= istStartUtc && t.CreatedOn < istEndUtc) ||
                            (t.CreatedOn >= localStart && t.CreatedOn < localEnd) ||
                            (t.CreatedOn >= utcStart && t.CreatedOn < utcEnd)
                        )
                    );
                }

                // Search Filter (by Name, ID, or Phone)
                string search = !string.IsNullOrEmpty(searchValue) ? searchValue : searchName;
                if (!string.IsNullOrEmpty(search))
                {
                    string lowerSearch = search.ToLower();
                    query = query.Where(x =>
                        (x.Profile != null && x.Profile.Name != null && x.Profile.Name.ToLower().Contains(lowerSearch)) ||
                        (x.Profile != null && x.Profile.RegisterNumber != null && x.Profile.RegisterNumber.Contains(search)) ||
                        (x.Profile != null && x.Profile.Phone != null && x.Profile.Phone.Contains(search))
                    );
                }

                int recordsTotal = await _followUpRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.FollowUpType == FollowUpType.ProfileVerification)
                    .CountAsync();
                int recordsFiltered = await query.CountAsync();

                // Sort columns mapping:
                // 0 -> Customer Name
                // 1 -> Profile ID (RegisterNumber)
                // 2 -> Gender
                // 3 -> Assigned Staff
                // 4 -> Contact Type
                // 5 -> Call Status
                // 6 -> Next Date
                // 7 -> Status
                string sortColumn = sortColumnIndex switch
                {
                    "0" => "Name",
                    "1" => "RegisterNumber",
                    "2" => "Gender",
                    "3" => "Staff",
                    "4" => "ContactType",
                    "5" => "CallStatus",
                    "6" => "NextFollowUpDate",
                    "7" => "Status",
                    _ => "CreatedOn"
                };
                bool isAscending = sortDirection == "asc";

                if (string.IsNullOrEmpty(sortColumnIndex))
                {
                    query = query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id);
                }
                else if (sortColumn == "Name")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.Name : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.Name : string.Empty);
                }
                else if (sortColumn == "RegisterNumber")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.RegisterNumber : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.RegisterNumber : string.Empty);
                }
                else if (sortColumn == "Gender")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.Gender : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.Gender : string.Empty);
                }
                else if (sortColumn == "Staff")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.AssignedStaffId) 
                        : query.OrderByDescending(x => x.AssignedStaffId);
                }
                else if (sortColumn == "ContactType")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.LatestContactType) 
                        : query.OrderByDescending(x => x.LatestContactType);
                }
                else if (sortColumn == "CallStatus")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.LatestCallStatus) 
                        : query.OrderByDescending(x => x.LatestCallStatus);
                }
                else if (sortColumn == "NextFollowUpDate")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.NextFollowUpDate) 
                        : query.OrderByDescending(x => x.NextFollowUpDate);
                }
                else if (sortColumn == "Status")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.LatestProfileVerificationStatus) 
                        : query.OrderByDescending(x => x.LatestProfileVerificationStatus);
                }
                else
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.CreatedOn).ThenBy(x => x.Id) 
                        : query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id);
                }

                var dbList = await query.Skip(start).Take(length).ToListAsync();

                // Load all staff
                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = staffUsers.ToDictionary(u => u.Id, u => u.NormalizedUserName ?? u.UserName);

                var profileIds = dbList.Select(x => x.ProfileId).Distinct().ToList();
                var docsList = await _dbContext.VerificationDocuments
                    .Where(d => profileIds.Contains(d.ProfileId) && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToListAsync();

                var docsDict = docsList.GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        d.Id,
                        d.DocumentUrl,
                        d.DocumentType,
                        d.OriginalFileName
                    }).ToList());

                var data = dbList.Select(x =>
                {
                    var staffName = "Unassigned";
                    if (x.AssignedStaffId.HasValue && staffDict.TryGetValue(x.AssignedStaffId.Value, out var name))
                    {
                        staffName = name;
                    }

                    docsDict.TryGetValue(x.ProfileId, out var userDocs);

                    return new
                    {
                        x.Id,
                        ProfileId = x.ProfileId,
                        AssignedStaffId = x.AssignedStaffId,
                        CustomerName = x.Profile?.Name ?? "N/A",
                        RegisterNumber = x.Profile?.RegisterNumber ?? "N/A",
                        Gender = x.Profile?.Gender ?? "N/A",
                        StaffName = staffName,
                        ContactType = x.LatestContactType?.ToString() ?? "None",
                        CallStatus = x.LatestCallStatus?.ToString() ?? "None",
                        VerificationStatus = x.LatestProfileVerificationStatus?.ToString() ?? "Pending",
                        NextFollowUpDate = x.NextFollowUpDate?.ToString("yyyy-MM-dd") ?? "N/A",
                        Status = x.LatestProfileVerificationStatus?.ToString() ?? "Pending",
                        VerificationDocumentUrl = x.Profile?.VerificationDocumentUrl,
                        VerificationDocuments = userDocs != null ? (object)userDocs : Array.Empty<object>(),
                        DocumentVerificationEnabled = x.Profile?.DocumentVerificationEnabled ?? false,
                        DocumentVerificationComplete = x.Profile?.DocumentVerificationComplete ?? false,
                        ApprovalStatus = x.LatestAdminApprovalStatus?.ToString() ?? "None"
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
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = 0,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        public class AssignStaffFollowUpModel
        {
            public long FollowUpId { get; set; }
            public long StaffId { get; set; }
        }

        [HttpPost("/admin/staff/assign-staff-to-followup")]
        public async Task<IActionResult> AssignStaffToFollowup([FromBody] AssignStaffFollowUpModel model)
        {
            if (model == null || model.FollowUpId <= 0 || model.StaffId <= 0)
            {
                return Json(new { success = false, message = "Invalid follow-up or staff selection." });
            }

            try
            {
                var followUp = await _followUpRepo.Get(model.FollowUpId);
                if (followUp == null || followUp.IsDeleted)
                {
                    return Json(new { success = false, message = "Follow-up not found." });
                }

                var staffUser = await _userManager.FindByIdAsync(model.StaffId.ToString());
                if (staffUser == null)
                {
                    return Json(new { success = false, message = "Selected staff member not found." });
                }

                string staffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Staff";
                followUp.AssignedStaffId = model.StaffId;
                await _followUpRepo.Update(followUp);
                await _followUpRepo.SaveChanges();

                // Create or update StaffProfileAssignment
                var existingAssignment = await _dbContext.StaffProfileAssignments
                    .FirstOrDefaultAsync(a => a.ProfileId == followUp.ProfileId && !a.IsDeleted);
                if (existingAssignment != null)
                {
                    existingAssignment.StaffId = model.StaffId;
                    existingAssignment.ModifiedOn = DateTime.UtcNow;
                    _dbContext.StaffProfileAssignments.Update(existingAssignment);
                }
                else
                {
                    var newAssignment = new StaffProfileAssignment
                    {
                        ProfileId = followUp.ProfileId,
                        StaffId = model.StaffId,
                        IsActive = true
                    };
                    await _dbContext.StaffProfileAssignments.AddAsync(newAssignment);
                }
                await _dbContext.SaveChangesAsync();

                // Add timeline entry
                var loggedInUser = await _userManager.GetUserAsync(User);
                var timeline = new FollowUpTimeline
                {
                    FollowUpId = followUp.Id,
                    StaffId = model.StaffId,
                    StaffName = staffName,
                    ProfileVerificationStatus = followUp.LatestProfileVerificationStatus,
                    InterestStatus = followUp.LatestInterestStatus,
                    RenewalInterestStatus = followUp.LatestRenewalInterestStatus,
                    Remarks = $"Staff assigned to {staffName} by {(loggedInUser?.UserName ?? "Admin")}.",
                    IsActive = true
                };
                await _followUpTimelineRepo.Add(timeline);
                await _followUpTimelineRepo.SaveChanges();

                return Json(new { success = true, message = $"Successfully assigned follow-up to {staffName}.", staffName = staffName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet("/admin/staff/premium-followups")]
        public async Task<IActionResult> PremiumFollowups()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.StaffList = staffUsers.Select(u => new
            {
                Id = u.Id,
                Name = u.NormalizedUserName ?? u.UserName
            }).ToList();

            return View();
        }

        [AcceptVerbs("GET", "POST")]
        [Route("/admin/staff/premium-followups-data")]
        public async Task<IActionResult> PremiumFollowupsData(
            long? staffId,
            int? callStatus,
            int? interestStatus,
            string? scheduleFilter,
            string searchName,
            string? gender)
        {
            try
            {
                var isPost = HttpContext.Request.Method == "POST";
                var drawVal = isPost ? Request.Form["draw"] : Request.Query["draw"];
                var startVal = isPost ? Request.Form["start"] : Request.Query["start"];
                var lengthVal = isPost ? Request.Form["length"] : Request.Query["length"];

                int draw = !string.IsNullOrEmpty(drawVal) ? Convert.ToInt32(drawVal) : 0;
                int start = !string.IsNullOrEmpty(startVal) ? Convert.ToInt32(startVal) : 0;
                int length = !string.IsNullOrEmpty(lengthVal) ? Convert.ToInt32(lengthVal) : 100;
                if (length <= 0) length = 100;

                string? searchValue = isPost ? Request.Form["search[value]"] : Request.Query["search[value]"];
                string? sortColumnIndex = isPost ? Request.Form["order[0][column]"] : Request.Query["order[0][column]"];
                string? sortDirection = isPost ? Request.Form["order[0][dir]"] : Request.Query["order[0][dir]"];

                // Base query: Only PremiumFollowUp follow-ups
                IQueryable<FollowUp> query = _followUpRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.FollowUpType == FollowUpType.PremiumFollowUp)
                    .Include(x => x.Profile);

                // Filter by Gender
                if (!string.IsNullOrEmpty(gender))
                {
                    query = query.Where(x => x.Profile != null && x.Profile.Gender != null && x.Profile.Gender.ToLower() == gender.ToLower());
                }

                // Filter by Staff
                if (staffId.HasValue && staffId.Value > 0)
                {
                    query = query.Where(x => x.AssignedStaffId == staffId.Value);
                }

                // Filter by Call Status
                if (callStatus.HasValue)
                {
                    var status = (CallStatus)callStatus.Value;
                    query = query.Where(x => x.LatestCallStatus == status);
                }

                // Filter by Interest Status (PremiumInterestStatus)
                if (interestStatus.HasValue)
                {
                    var interest = (PremiumInterestStatus)interestStatus.Value;
                    query = query.Where(x => x.LatestInterestStatus == interest);
                }

                // Filter by Follow-up Schedule (Today's Follow-ups)
                string? schedule = !string.IsNullOrEmpty(scheduleFilter) 
                    ? scheduleFilter 
                    : (isPost ? Request.Form["scheduleFilter"].ToString() : Request.Query["scheduleFilter"].ToString());

                if (!string.IsNullOrEmpty(schedule) && string.Equals(schedule, "today", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime todayDate;
                    try
                    {
                        var tzi = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
                        todayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi).Date;
                    }
                    catch
                    {
                        todayDate = DateTime.UtcNow.AddHours(5.5).Date;
                    }

                    DateTime istStartUtc = todayDate.AddHours(-5.5);
                    DateTime istEndUtc = istStartUtc.AddDays(1);
                    DateTime localStart = todayDate;
                    DateTime localEnd = localStart.AddDays(1);
                    DateTime utcStart = DateTime.UtcNow.Date;
                    DateTime utcEnd = utcStart.AddDays(1);

                    query = query.Where(x =>
                        (x.CreatedOn >= istStartUtc && x.CreatedOn < istEndUtc) ||
                        (x.CreatedOn >= localStart && x.CreatedOn < localEnd) ||
                        (x.CreatedOn >= utcStart && x.CreatedOn < utcEnd) ||
                        (x.NextFollowUpDate.HasValue && (
                            (x.NextFollowUpDate.Value >= istStartUtc && x.NextFollowUpDate.Value < istEndUtc) ||
                            (x.NextFollowUpDate.Value >= localStart && x.NextFollowUpDate.Value < localEnd) ||
                            (x.NextFollowUpDate.Value >= utcStart && x.NextFollowUpDate.Value < utcEnd)
                        )) ||
                        x.Timelines.Any(t =>
                            (t.CreatedOn >= istStartUtc && t.CreatedOn < istEndUtc) ||
                            (t.CreatedOn >= localStart && t.CreatedOn < localEnd) ||
                            (t.CreatedOn >= utcStart && t.CreatedOn < utcEnd)
                        )
                    );
                }

                // Search Filter (by Name, ID, or Phone)
                string search = !string.IsNullOrEmpty(searchValue) ? searchValue : searchName;
                if (!string.IsNullOrEmpty(search))
                {
                    string lowerSearch = search.ToLower();
                    query = query.Where(x =>
                        (x.Profile != null && x.Profile.Name != null && x.Profile.Name.ToLower().Contains(lowerSearch)) ||
                        (x.Profile != null && x.Profile.RegisterNumber != null && x.Profile.RegisterNumber.Contains(search)) ||
                        (x.Profile != null && x.Profile.Phone != null && x.Profile.Phone.Contains(search))
                    );
                }

                int recordsTotal = await _followUpRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.FollowUpType == FollowUpType.PremiumFollowUp)
                    .CountAsync();
                int recordsFiltered = await query.CountAsync();

                // Sorting
                // 0 -> Profile ID (RegisterNumber)
                // 1 -> Customer Name
                // 2 -> Gender
                // 3 -> Assigned Staff
                // 4 -> Package
                // 5 -> Payment
                // 6 -> Start Date
                // 7 -> Expiry Date
                // 8 -> Status
                string sortColumn = sortColumnIndex switch
                {
                    "0" => "RegisterNumber",
                    "1" => "Name",
                    "2" => "Gender",
                    "3" => "Staff",
                    "8" => "Status",
                    _ => "CreatedOn"
                };
                bool isAscending = sortDirection == "asc";

                if (string.IsNullOrEmpty(sortColumnIndex))
                {
                    query = query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id);
                }
                else if (sortColumn == "Name")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.Name : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.Name : string.Empty);
                }
                else if (sortColumn == "RegisterNumber")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.RegisterNumber : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.RegisterNumber : string.Empty);
                }
                else if (sortColumn == "Gender")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.Gender : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.Gender : string.Empty);
                }
                else if (sortColumn == "Staff")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.AssignedStaffId) 
                        : query.OrderByDescending(x => x.AssignedStaffId);
                }
                else if (sortColumn == "Status")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.LatestInterestStatus).ThenBy(x => x.LatestCallStatus) 
                        : query.OrderByDescending(x => x.LatestInterestStatus).ThenByDescending(x => x.LatestCallStatus);
                }
                else
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.CreatedOn).ThenBy(x => x.Id) 
                        : query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id);
                }

                var dbList = await query.Skip(start).Take(length).ToListAsync();

                // Batch fetch latest Transaction and PlanPurchase details for these profiles
                var profileIds = dbList.Select(x => x.ProfileId).ToList();

                var transactions = await _dbContext.Transaction
                    .Where(t => profileIds.Contains(t.userId) && t.Status == "success")
                    .ToListAsync();

                var plans = await _planPurchaseRepo.GetQueryable()
                    .Where(p => profileIds.Contains(p.UserId) && !p.IsDeleted)
                    .ToListAsync();

                // Load all staff
                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = staffUsers.ToDictionary(u => u.Id, u => u.NormalizedUserName ?? u.UserName);

                var data = dbList.Select(x =>
                {
                    var staffName = "Unassigned";
                    if (x.AssignedStaffId.HasValue && staffDict.TryGetValue(x.AssignedStaffId.Value, out var name))
                    {
                        staffName = name;
                    }

                    var latestTxn = transactions
                        .Where(t => t.userId == x.ProfileId)
                        .OrderByDescending(t => t.CreatedOn)
                        .FirstOrDefault();

                    var latestPlan = plans
                        .Where(p => p.UserId == x.ProfileId)
                        .OrderByDescending(p => p.CreatedOn)
                        .FirstOrDefault();

                    var packageName = (x.Profile != null && x.Profile.IsPremiumMember) ? "Premium" : "Non Premium";
                    var paymentInfo = latestTxn != null ? $"{latestTxn.Amount} ({latestTxn.PaymentType ?? "Offline"})" : "N/A";
                    var startDate = latestTxn?.CreatedOn.ToString("yyyy-MM-dd") ?? latestPlan?.CreatedOn.ToString("yyyy-MM-dd") ?? "N/A";
                    var expiryDate = latestPlan?.ExpiresAt.ToString("yyyy-MM-dd") ?? "N/A";

                    return new
                    {
                        x.Id,
                        ProfileId = x.ProfileId,
                        CustomerName = x.Profile?.Name ?? "N/A",
                        RegisterNumber = x.Profile?.RegisterNumber ?? "N/A",
                        Gender = x.Profile?.Gender ?? "N/A",
                        StaffName = staffName,
                        PackageName = packageName,
                        PaymentInfo = paymentInfo,
                        StartDate = startDate,
                        ExpiryDate = expiryDate,
                        ContactType = x.LatestContactType?.ToString() ?? "None",
                        CallStatus = x.LatestCallStatus?.ToString() ?? "None",
                        InterestStatus = x.LatestInterestStatus?.ToString() ?? "None",
                        NextFollowUpDate = x.NextFollowUpDate?.ToString("yyyy-MM-dd") ?? "N/A",
                        Status = x.LatestInterestStatus.HasValue ? x.LatestInterestStatus.Value.ToString() : (x.LatestCallStatus.HasValue ? x.LatestCallStatus.Value.ToString() : "Pending"),
                        PaymentMode = x.PaymentMode ?? "Online",
                        PaymentLinkSent = x.PaymentLinkSent,
                        PaymentLinkSentAt = x.PaymentLinkSentAt?.ToString("yyyy-MM-dd HH:mm"),
                        PaymentCompleted = x.PaymentCompleted,
                        ApprovalStatus = x.LatestAdminApprovalStatus?.ToString() ?? "None"
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
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = 0,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        [HttpGet("/admin/staff/renewal-followups")]
        public async Task<IActionResult> RenewalFollowups()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.StaffList = staffUsers.Select(u => new
            {
                Id = u.Id,
                Name = u.NormalizedUserName ?? u.UserName
            }).ToList();

            return View();
        }

        [AcceptVerbs("GET", "POST")]
        [Route("/admin/staff/renewal-followups-data")]
        public async Task<IActionResult> RenewalFollowupsData(
            long? staffId,
            int? callStatus,
            int? renewalStatus,
            int? interestStatus,
            string? scheduleFilter,
            string searchName,
            string? gender)
        {
            try
            {
                var isPost = HttpContext.Request.Method == "POST";
                var drawVal = isPost ? Request.Form["draw"] : Request.Query["draw"];
                var startVal = isPost ? Request.Form["start"] : Request.Query["start"];
                var lengthVal = isPost ? Request.Form["length"] : Request.Query["length"];

                int draw = !string.IsNullOrEmpty(drawVal) ? Convert.ToInt32(drawVal) : 0;
                int start = !string.IsNullOrEmpty(startVal) ? Convert.ToInt32(startVal) : 0;
                int length = !string.IsNullOrEmpty(lengthVal) ? Convert.ToInt32(lengthVal) : 100;
                if (length <= 0) length = 100;

                string? searchValue = isPost ? Request.Form["search[value]"] : Request.Query["search[value]"];
                string? sortColumnIndex = isPost ? Request.Form["order[0][column]"] : Request.Query["order[0][column]"];
                string? sortDirection = isPost ? Request.Form["order[0][dir]"] : Request.Query["order[0][dir]"];

                // Base query: Only RenewalFollowUp follow-ups
                IQueryable<FollowUp> query = _followUpRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.FollowUpType == FollowUpType.RenewalFollowUp)
                    .Include(x => x.Profile);

                // Filter by Gender
                if (!string.IsNullOrEmpty(gender))
                {
                    query = query.Where(x => x.Profile != null && x.Profile.Gender != null && x.Profile.Gender.ToLower() == gender.ToLower());
                }

                // Filter by Staff
                if (staffId.HasValue && staffId.Value > 0)
                {
                    query = query.Where(x => x.AssignedStaffId == staffId.Value);
                }

                // Filter by Call Status
                if (callStatus.HasValue)
                {
                    var status = (CallStatus)callStatus.Value;
                    query = query.Where(x => x.LatestCallStatus == status);
                }

                // Filter by Renewal Interest Status (RenewalInterestStatus)
                var renStatus = renewalStatus ?? interestStatus;
                if (renStatus.HasValue)
                {
                    var interest = (RenewalInterestStatus)renStatus.Value;
                    query = query.Where(x => x.LatestRenewalInterestStatus == interest);
                }

                // Filter by Follow-up Schedule (Today's Follow-ups)
                string? schedule = !string.IsNullOrEmpty(scheduleFilter) 
                    ? scheduleFilter 
                    : (isPost ? Request.Form["scheduleFilter"].ToString() : Request.Query["scheduleFilter"].ToString());

                if (!string.IsNullOrEmpty(schedule) && string.Equals(schedule, "today", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime todayDate;
                    try
                    {
                        var tzi = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
                        todayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi).Date;
                    }
                    catch
                    {
                        todayDate = DateTime.UtcNow.AddHours(5.5).Date;
                    }

                    DateTime istStartUtc = todayDate.AddHours(-5.5);
                    DateTime istEndUtc = istStartUtc.AddDays(1);
                    DateTime localStart = todayDate;
                    DateTime localEnd = localStart.AddDays(1);
                    DateTime utcStart = DateTime.UtcNow.Date;
                    DateTime utcEnd = utcStart.AddDays(1);

                    query = query.Where(x =>
                        (x.CreatedOn >= istStartUtc && x.CreatedOn < istEndUtc) ||
                        (x.CreatedOn >= localStart && x.CreatedOn < localEnd) ||
                        (x.CreatedOn >= utcStart && x.CreatedOn < utcEnd) ||
                        (x.NextFollowUpDate.HasValue && (
                            (x.NextFollowUpDate.Value >= istStartUtc && x.NextFollowUpDate.Value < istEndUtc) ||
                            (x.NextFollowUpDate.Value >= localStart && x.NextFollowUpDate.Value < localEnd) ||
                            (x.NextFollowUpDate.Value >= utcStart && x.NextFollowUpDate.Value < utcEnd)
                        )) ||
                        x.Timelines.Any(t =>
                            (t.CreatedOn >= istStartUtc && t.CreatedOn < istEndUtc) ||
                            (t.CreatedOn >= localStart && t.CreatedOn < localEnd) ||
                            (t.CreatedOn >= utcStart && t.CreatedOn < utcEnd)
                        )
                    );
                }

                // Search Filter (by Name, ID, or Phone)
                string search = !string.IsNullOrEmpty(searchValue) ? searchValue : searchName;
                if (!string.IsNullOrEmpty(search))
                {
                    string lowerSearch = search.ToLower();
                    query = query.Where(x =>
                        (x.Profile != null && x.Profile.Name != null && x.Profile.Name.ToLower().Contains(lowerSearch)) ||
                        (x.Profile != null && x.Profile.RegisterNumber != null && x.Profile.RegisterNumber.Contains(search)) ||
                        (x.Profile != null && x.Profile.Phone != null && x.Profile.Phone.Contains(search))
                    );
                }

                int recordsTotal = await _followUpRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.FollowUpType == FollowUpType.RenewalFollowUp)
                    .CountAsync();
                int recordsFiltered = await query.CountAsync();

                // Sorting
                // 0 -> Profile ID (RegisterNumber)
                // 1 -> Customer Name
                // 2 -> Gender
                // 3 -> Assigned Staff
                // 4 -> Package
                // 5 -> Payment
                // 6 -> Start Date
                // 7 -> Expiry Date
                // 8 -> Status
                string sortColumn = sortColumnIndex switch
                {
                    "0" => "RegisterNumber",
                    "1" => "Name",
                    "2" => "Gender",
                    "3" => "Staff",
                    "8" => "Status",
                    _ => "CreatedOn"
                };
                bool isAscending = sortDirection == "asc";

                if (string.IsNullOrEmpty(sortColumnIndex))
                {
                    query = query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id);
                }
                else if (sortColumn == "Name")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.Name : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.Name : string.Empty);
                }
                else if (sortColumn == "RegisterNumber")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.RegisterNumber : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.RegisterNumber : string.Empty);
                }
                else if (sortColumn == "Gender")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.Gender : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.Gender : string.Empty);
                }
                else if (sortColumn == "Staff")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.AssignedStaffId) 
                        : query.OrderByDescending(x => x.AssignedStaffId);
                }
                else if (sortColumn == "Status")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.LatestRenewalInterestStatus).ThenBy(x => x.LatestCallStatus) 
                        : query.OrderByDescending(x => x.LatestRenewalInterestStatus).ThenByDescending(x => x.LatestCallStatus);
                }
                else
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.CreatedOn).ThenBy(x => x.Id) 
                        : query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id);
                }

                var dbList = await query.Skip(start).Take(length).ToListAsync();

                // Batch fetch latest Transaction and PlanPurchase details for these profiles
                var profileIds = dbList.Select(x => x.ProfileId).ToList();

                var transactions = await _dbContext.Transaction
                    .Where(t => profileIds.Contains(t.userId) && t.Status == "success")
                    .ToListAsync();

                var plans = await _planPurchaseRepo.GetQueryable()
                    .Where(p => profileIds.Contains(p.UserId) && !p.IsDeleted)
                    .ToListAsync();

                // Load all staff
                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = staffUsers.ToDictionary(u => u.Id, u => u.NormalizedUserName ?? u.UserName);

                var data = dbList.Select(x =>
                {
                    var staffName = "Unassigned";
                    if (x.AssignedStaffId.HasValue && staffDict.TryGetValue(x.AssignedStaffId.Value, out var name))
                    {
                        staffName = name;
                    }

                    var latestTxn = transactions
                        .Where(t => t.userId == x.ProfileId)
                        .OrderByDescending(t => t.CreatedOn)
                        .FirstOrDefault();

                    var latestPlan = plans
                        .Where(p => p.UserId == x.ProfileId)
                        .OrderByDescending(p => p.CreatedOn)
                        .FirstOrDefault();

                    var packageName = (x.Profile != null && x.Profile.IsPremiumMember) ? "Premium" : "Non Premium";
                    var paymentInfo = latestTxn != null ? $"{latestTxn.Amount} ({latestTxn.PaymentType ?? "Offline"})" : "N/A";
                    var startDate = latestTxn?.CreatedOn.ToString("yyyy-MM-dd") ?? latestPlan?.CreatedOn.ToString("yyyy-MM-dd") ?? "N/A";
                    var expiryDate = latestPlan?.ExpiresAt.ToString("yyyy-MM-dd") ?? "N/A";

                    return new
                    {
                        x.Id,
                        ProfileId = x.ProfileId,
                        CustomerName = x.Profile?.Name ?? "N/A",
                        RegisterNumber = x.Profile?.RegisterNumber ?? "N/A",
                        Gender = x.Profile?.Gender ?? "N/A",
                        StaffName = staffName,
                        PackageName = packageName,
                        PaymentInfo = paymentInfo,
                        StartDate = startDate,
                        ExpiryDate = expiryDate,
                        ContactType = x.LatestContactType?.ToString() ?? "None",
                        CallStatus = x.LatestCallStatus?.ToString() ?? "None",
                        InterestStatus = x.LatestRenewalInterestStatus?.ToString() ?? "None",
                        RenewalInterestStatus = x.LatestRenewalInterestStatus?.ToString() ?? "None",
                        NextFollowUpDate = x.NextFollowUpDate?.ToString("yyyy-MM-dd") ?? "N/A",
                        Status = x.LatestRenewalInterestStatus.HasValue ? x.LatestRenewalInterestStatus.Value.ToString() : (x.LatestCallStatus.HasValue ? x.LatestCallStatus.Value.ToString() : "Pending"),
                        PaymentMode = x.PaymentMode ?? "Online",
                        PaymentLinkSent = x.PaymentLinkSent,
                        PaymentLinkSentAt = x.PaymentLinkSentAt?.ToString("yyyy-MM-dd HH:mm"),
                        PaymentCompleted = x.PaymentCompleted,
                        ApprovalStatus = x.LatestAdminApprovalStatus?.ToString() ?? "None"
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
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = 0,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        [HttpGet("/admin/staff/check-txnid-availability")]
        public async Task<IActionResult> CheckTxnIdAvailability([FromQuery] string txnId, [FromQuery] long? followUpId = null)
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

            var followUpQuery = _dbContext.FollowUps.Where(f => !f.IsDeleted && f.TransactionId != null && f.TransactionId.ToLower() == cleanTxnId.ToLower());
            if (followUpId.HasValue && followUpId.Value > 0)
            {
                followUpQuery = followUpQuery.Where(f => f.Id != followUpId.Value);
            }

            bool isUsedInFollowUp = await followUpQuery.AnyAsync();
            if (isUsedInFollowUp)
            {
                return Json(new { available = false, message = "This Transaction ID is already submitted in a staff follow-up payment." });
            }

            return Json(new { available = true, message = "Transaction ID is available." });
        }

        [HttpPost("/admin/staff/update-followup")]
        public async Task<IActionResult> UpdateFollowUp([FromBody] UpdateFollowUpModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            try
            {
                var followUp = await _followUpRepo.Get(model.FollowUpId);
                if (followUp == null || followUp.IsDeleted)
                {
                    return Json(new { success = false, message = "Follow-up not found." });
                }

                // Get currently logged-in staff member
                long staffId = 0;
                string staffName = "Unknown Staff";

                if (model.StaffId.HasValue && model.StaffId.Value > 0)
                {
                    staffId = model.StaffId.Value;
                    var staffUser = await _userManager.FindByIdAsync(staffId.ToString());
                    if (staffUser != null)
                    {
                        staffName = staffUser.NormalizedUserName ?? staffUser.UserName ?? "Unknown Staff";
                    }
                }
                else
                {
                    var loggedInUser = await _userManager.GetUserAsync(User);
                    staffId = loggedInUser?.Id ?? 0;
                    staffName = loggedInUser?.NormalizedUserName ?? loggedInUser?.UserName ?? "Admin";
                }

                // Create new timeline interaction entry
                var timeline = new FollowUpTimeline
                {
                    FollowUpId = followUp.Id,
                    StaffId = staffId,
                    StaffName = staffName,
                    ContactType = model.ContactType,
                    CallStatus = model.CallStatus,
                    Remarks = model.Remarks,
                    NextFollowUpDate = model.NextFollowUpDate,
                    IsActive = true
                };

                // Update latest follow-up fields
                followUp.LatestContactType = model.ContactType;
                followUp.LatestCallStatus = model.CallStatus;
                followUp.LatestRemarks = model.Remarks;
                followUp.NextFollowUpDate = model.NextFollowUpDate;

                // Map type-specific statuses
                bool hasReachedEndStatus = false;
                string? whatsappUrl = null;
                string successMessage = "Follow-up interaction logged successfully.";

                if (followUp.FollowUpType == FollowUpType.ProfileVerification)
                {
                    if (model.ProfileVerificationStatus == null)
                    {
                        return Json(new { success = false, message = "Profile verification status is required." });
                    }
                    timeline.ProfileVerificationStatus = model.ProfileVerificationStatus;
                    followUp.LatestProfileVerificationStatus = model.ProfileVerificationStatus;

                    if (model.ProfileVerificationStatus == ProfileVerificationStatus.DetailedVerifyRequest)
                    {
                        var userProfile = await _userRepository.Get(followUp.ProfileId);
                        if (userProfile != null)
                        {
                            userProfile.DocumentVerificationEnabled = true;
                            await _userRepository.Update(userProfile);
                            await _userRepository.SaveChanges();
                        }
                        hasReachedEndStatus = false;
                    }
                    else if (model.ProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify)
                    {
                        var userProfile = await _userRepository.Get(followUp.ProfileId);
                        if (userProfile != null)
                        {
                            userProfile.DocumentVerificationEnabled = true;
                            if (model.DocumentVerificationComplete.HasValue)
                            {
                                userProfile.DocumentVerificationComplete = model.DocumentVerificationComplete.Value;
                                userProfile.DocumentVerificationRejected = !model.DocumentVerificationComplete.Value;
                                hasReachedEndStatus = true;
                            }
                            await _userRepository.Update(userProfile);
                            await _userRepository.SaveChanges();
                        }
                    }
                    else if (model.ProfileVerificationStatus == ProfileVerificationStatus.Verify || 
                        model.ProfileVerificationStatus == ProfileVerificationStatus.Dismissed ||
                        model.ProfileVerificationStatus == ProfileVerificationStatus.Suspended)
                    {
                        hasReachedEndStatus = true;
                    }
                }
                else if (followUp.FollowUpType == FollowUpType.PremiumFollowUp)
                {
                    if (model.PremiumInterestStatus == null)
                    {
                        return Json(new { success = false, message = "Premium interest status is required." });
                    }
                    timeline.InterestStatus = model.PremiumInterestStatus;
                    followUp.LatestInterestStatus = model.PremiumInterestStatus;

                    if (model.PremiumInterestStatus == PremiumInterestStatus.Converted)
                    {
                        if (string.Equals(model.PaymentMode, "Offline", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(model.TransactionId))
                            {
                                return Json(new { success = false, message = "Transaction ID is required for offline payment." });
                            }

                            var cleanTxnId = model.TransactionId.Trim();
                            bool isUsedInTxn = await _transactionRepository.IsTransactionIdExistsAsync(cleanTxnId);
                            bool isUsedInFollowUp = await _dbContext.FollowUps.AnyAsync(f => !f.IsDeleted && f.Id != followUp.Id && f.TransactionId != null && f.TransactionId.ToLower() == cleanTxnId.ToLower());

                            if (isUsedInTxn || isUsedInFollowUp)
                            {
                                return Json(new { success = false, message = "This Transaction ID is already used for another transaction. Please enter a unique Transaction ID." });
                            }

                            followUp.PaymentMode = "Offline";
                            followUp.OfflinePaymentType = model.OfflinePaymentType;
                            followUp.TransactionId = cleanTxnId;
                            followUp.PaymentAmount = model.PaymentAmount;
                            followUp.PaymentCompleted = false;
                            followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Pending;

                            var adminApproval = new FollowUpAdminApproval
                            {
                                FollowUpId = followUp.Id,
                                Status = AdminApprovalStatus.Pending,
                                Remarks = $"Pending admin review for offline payment ({staffName}). TxnId: {cleanTxnId}, Type: {model.OfflinePaymentType}, Amount: {model.PaymentAmount}. Remarks: {model.Remarks}",
                                ActionDate = null,
                                ActionBy = null,
                                IsActive = true
                            };
                            await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                            await _dbContext.SaveChangesAsync();

                            timeline.Remarks = string.IsNullOrEmpty(timeline.Remarks)
                                ? $"Offline payment details submitted (Txn: {cleanTxnId}, Amount: {model.PaymentAmount})"
                                : $"{timeline.Remarks} [Offline Payment: {cleanTxnId}, Amount: {model.PaymentAmount}, Type: {model.OfflinePaymentType}]";

                            successMessage = "Follow-up converted with offline payment details and submitted for admin review. Membership will be activated once Admin approves.";
                        }
                        else
                        {
                            // Online Payment (Default)
                            followUp.PaymentMode = "Online";
                            followUp.PaymentLinkSent = true;
                            followUp.PaymentLinkSentAt = DateTime.UtcNow;
                            followUp.PaymentCompleted = false;
                            followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;

                            var adminApproval = new FollowUpAdminApproval
                            {
                                FollowUpId = followUp.Id,
                                Status = AdminApprovalStatus.Approved,
                                Remarks = $"Auto-approved on payment link generation ({staffName}). Remarks: {model.Remarks}",
                                ActionDate = DateTime.UtcNow,
                                ActionBy = staffName,
                                IsActive = true
                            };
                            await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                            await _dbContext.SaveChangesAsync();

                            var userProfile = await _userRepository.Get(followUp.ProfileId);
                            if (userProfile != null && !userProfile.IsDeleted)
                            {
                                bool sendEmail = model.SendEmail;
                                bool sendMessage = model.SendMessage;
                                bool sendWhatsApp = model.SendWhatsApp;

                                if (!sendEmail && !sendMessage && !sendWhatsApp)
                                {
                                    sendEmail = true;
                                    sendMessage = true;
                                }

                                whatsappUrl = await SendPaymentLinkNotification(userProfile, sendEmail, sendMessage, sendWhatsApp);

                                var sentChannels = new List<string>();
                                if (sendEmail) sentChannels.Add("Email");
                                if (sendMessage) sentChannels.Add("SMS");
                                if (sendWhatsApp) sentChannels.Add("WhatsApp");

                                timeline.Remarks = string.IsNullOrEmpty(timeline.Remarks)
                                    ? $"Online payment link sent via {string.Join(", ", sentChannels)}"
                                    : $"{timeline.Remarks} [Online Payment Link Sent via {string.Join(", ", sentChannels)}]";

                                successMessage = $"Follow-up converted successfully and payment link sent via {string.Join(", ", sentChannels)}.";
                            }
                        }
                    }
                    else if (model.PremiumInterestStatus == PremiumInterestStatus.NotInterested)
                    {
                        hasReachedEndStatus = true;
                    }
                    else
                    {
                        followUp.LatestAdminApprovalStatus = null;
                    }
                }
                else if (followUp.FollowUpType == FollowUpType.RenewalFollowUp)
                {
                    if (model.RenewalInterestStatus == null)
                    {
                        return Json(new { success = false, message = "Renewal interest status is required." });
                    }
                    timeline.RenewalInterestStatus = model.RenewalInterestStatus;
                    followUp.LatestRenewalInterestStatus = model.RenewalInterestStatus;

                    if (model.RenewalInterestStatus == RenewalInterestStatus.Renewed)
                    {
                        if (string.Equals(model.PaymentMode, "Offline", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(model.TransactionId))
                            {
                                return Json(new { success = false, message = "Transaction ID is required for offline renewal payment." });
                            }

                            var cleanTxnId = model.TransactionId.Trim();
                            bool isUsedInTxn = await _transactionRepository.IsTransactionIdExistsAsync(cleanTxnId);
                            bool isUsedInFollowUp = await _dbContext.FollowUps.AnyAsync(f => !f.IsDeleted && f.Id != followUp.Id && f.TransactionId != null && f.TransactionId.ToLower() == cleanTxnId.ToLower());

                            if (isUsedInTxn || isUsedInFollowUp)
                            {
                                return Json(new { success = false, message = "This Transaction ID is already used for another transaction. Please enter a unique Transaction ID." });
                            }

                            followUp.PaymentMode = "Offline";
                            followUp.OfflinePaymentType = model.OfflinePaymentType;
                            followUp.TransactionId = cleanTxnId;
                            followUp.PaymentAmount = model.PaymentAmount;
                            followUp.PaymentCompleted = false;
                            followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Pending;

                            var adminApproval = new FollowUpAdminApproval
                            {
                                FollowUpId = followUp.Id,
                                Status = AdminApprovalStatus.Pending,
                                Remarks = $"Pending admin review for offline renewal payment ({staffName}). TxnId: {cleanTxnId}, Type: {model.OfflinePaymentType}, Amount: {model.PaymentAmount}. Remarks: {model.Remarks}",
                                ActionDate = null,
                                ActionBy = null,
                                IsActive = true
                            };
                            await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                            await _dbContext.SaveChangesAsync();

                            timeline.Remarks = string.IsNullOrEmpty(timeline.Remarks)
                                ? $"Offline payment details submitted (Txn: {cleanTxnId}, Amount: {model.PaymentAmount})"
                                : $"{timeline.Remarks} [Offline Payment: {cleanTxnId}, Amount: {model.PaymentAmount}, Type: {model.OfflinePaymentType}]";

                            successMessage = "Follow-up renewed with offline payment details and submitted for admin review. Membership will be activated once Admin approves.";
                        }
                        else
                        {
                            // Online Payment (Default)
                            followUp.PaymentMode = "Online";
                            followUp.PaymentLinkSent = true;
                            followUp.PaymentLinkSentAt = DateTime.UtcNow;
                            followUp.PaymentCompleted = false;
                            followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;

                            var adminApproval = new FollowUpAdminApproval
                            {
                                FollowUpId = followUp.Id,
                                Status = AdminApprovalStatus.Approved,
                                Remarks = $"Auto-approved on payment link generation ({staffName}). Remarks: {model.Remarks}",
                                ActionDate = DateTime.UtcNow,
                                ActionBy = staffName,
                                IsActive = true
                            };
                            await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                            await _dbContext.SaveChangesAsync();

                            var userProfile = await _userRepository.Get(followUp.ProfileId);
                            if (userProfile != null && !userProfile.IsDeleted)
                            {
                                bool sendEmail = model.SendEmail;
                                bool sendMessage = model.SendMessage;
                                bool sendWhatsApp = model.SendWhatsApp;

                                if (!sendEmail && !sendMessage && !sendWhatsApp)
                                {
                                    sendEmail = true;
                                    sendMessage = true;
                                }

                                whatsappUrl = await SendPaymentLinkNotification(userProfile, sendEmail, sendMessage, sendWhatsApp);

                                var sentChannels = new List<string>();
                                if (sendEmail) sentChannels.Add("Email");
                                if (sendMessage) sentChannels.Add("SMS");
                                if (sendWhatsApp) sentChannels.Add("WhatsApp");

                                timeline.Remarks = string.IsNullOrEmpty(timeline.Remarks)
                                    ? $"Online payment link sent via {string.Join(", ", sentChannels)}"
                                    : $"{timeline.Remarks} [Online Payment Link Sent via {string.Join(", ", sentChannels)}]";

                                successMessage = $"Follow-up renewed successfully and payment link sent via {string.Join(", ", sentChannels)}.";
                            }
                        }
                    }
                    else if (model.RenewalInterestStatus == RenewalInterestStatus.NotInterested)
                    {
                        hasReachedEndStatus = true;
                    }
                    else
                    {
                        followUp.LatestAdminApprovalStatus = null;
                    }
                }

                if (hasReachedEndStatus)
                {
                    followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Pending;

                    var adminApproval = new FollowUpAdminApproval
                    {
                        FollowUpId = followUp.Id,
                        Status = AdminApprovalStatus.Pending,
                        Remarks = $"Pending admin review ({staffName}). Remarks: {model.Remarks}",
                        ActionDate = null,
                        ActionBy = null,
                        IsActive = true
                    };
                    await _dbContext.FollowUpAdminApprovals.AddAsync(adminApproval);
                    await _dbContext.SaveChangesAsync();
                }

                // Save changes to repositories
                await _followUpTimelineRepo.Add(timeline);
                await _followUpTimelineRepo.SaveChanges();

                await _followUpRepo.Update(followUp);
                await _followUpRepo.SaveChanges();

                return Json(new { success = true, message = successMessage, whatsappUrl = whatsappUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet("/admin/staff/admin-review-panel")]
        public async Task<IActionResult> AdminReviewPanel()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.StaffList = staffUsers.Select(u => new
            {
                Id = u.Id,
                Name = u.NormalizedUserName ?? u.UserName
            }).ToList();

            return View();
        }

        [AcceptVerbs("GET", "POST")]
        [Route("/admin/staff/admin-review-panel-data")]
        public async Task<IActionResult> AdminReviewPanelData(
            long? staffId,
            int? approvalStatus,
            string searchName)
        {
            try
            {
                var isPost = HttpContext.Request.Method == "POST";
                var drawVal = isPost ? Request.Form["draw"] : Request.Query["draw"];
                var startVal = isPost ? Request.Form["start"] : Request.Query["start"];
                var lengthVal = isPost ? Request.Form["length"] : Request.Query["length"];

                int draw = !string.IsNullOrEmpty(drawVal) ? Convert.ToInt32(drawVal) : 0;
                int start = !string.IsNullOrEmpty(startVal) ? Convert.ToInt32(startVal) : 0;
                int length = !string.IsNullOrEmpty(lengthVal) ? Convert.ToInt32(lengthVal) : 10;
                if (length <= 0) length = 10;

                string? searchValue = isPost ? Request.Form["search[value]"] : Request.Query["search[value]"];
                string? sortColumnIndex = isPost ? Request.Form["order[0][column]"] : Request.Query["order[0][column]"];
                string? sortDirection = isPost ? Request.Form["order[0][dir]"] : Request.Query["order[0][dir]"];

                // Base query: Only followups that have a LatestAdminApprovalStatus
                IQueryable<FollowUp> query = _followUpRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.LatestAdminApprovalStatus != null)
                    .Include(x => x.Profile);

                // Filter by Staff
                if (staffId.HasValue && staffId.Value > 0)
                {
                    query = query.Where(x => x.AssignedStaffId == staffId.Value);
                }

                // Filter by Approval Status
                if (approvalStatus.HasValue)
                {
                    var status = (AdminApprovalStatus)approvalStatus.Value;
                    query = query.Where(x => x.LatestAdminApprovalStatus == status);
                }

                // Search Filter (by Name, ID, or Phone)
                string search = !string.IsNullOrEmpty(searchValue) ? searchValue : searchName;
                if (!string.IsNullOrEmpty(search))
                {
                    string lowerSearch = search.ToLower();
                    query = query.Where(x =>
                        (x.Profile != null && x.Profile.Name != null && x.Profile.Name.ToLower().Contains(lowerSearch)) ||
                        (x.Profile != null && x.Profile.RegisterNumber != null && x.Profile.RegisterNumber.Contains(search)) ||
                        (x.Profile != null && x.Profile.Phone != null && x.Profile.Phone.Contains(search))
                    );
                }

                int recordsTotal = await _followUpRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.LatestAdminApprovalStatus != null)
                    .CountAsync();
                int recordsFiltered = await query.CountAsync();

                string sortColumn = sortColumnIndex switch
                {
                    "0" => "RegisterNumber",
                    "1" => "Name",
                    _ => "CreatedOn"
                };
                bool isAscending = sortDirection == "asc";

                if (string.IsNullOrEmpty(sortColumnIndex))
                {
                    query = query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id);
                }
                else if (sortColumn == "Name")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.Name : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.Name : string.Empty);
                }
                else if (sortColumn == "RegisterNumber")
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.Profile != null ? x.Profile.RegisterNumber : string.Empty) 
                        : query.OrderByDescending(x => x.Profile != null ? x.Profile.RegisterNumber : string.Empty);
                }
                else
                {
                    query = isAscending 
                        ? query.OrderBy(x => x.CreatedOn).ThenBy(x => x.Id) 
                        : query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id);
                }

                var dbList = await query.Skip(start).Take(length).ToListAsync();

                // Load all staff
                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = staffUsers.ToDictionary(u => u.Id, u => u.NormalizedUserName ?? u.UserName);

                var profileIds = dbList.Select(x => x.ProfileId).Distinct().ToList();
                var docsList = await _dbContext.VerificationDocuments
                    .Where(d => profileIds.Contains(d.ProfileId) && !d.IsDeleted)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.CreatedOn)
                    .ToListAsync();

                var docsDict = docsList.GroupBy(d => d.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(d => new
                    {
                        d.Id,
                        d.DocumentUrl,
                        d.DocumentType,
                        d.OriginalFileName
                    }).ToList());

                var data = dbList.Select(x =>
                {
                    var staffName = "Unassigned";
                    if (x.AssignedStaffId.HasValue && staffDict.TryGetValue(x.AssignedStaffId.Value, out var name))
                    {
                        staffName = name;
                    }

                    // Format end status description
                    string endStatusDesc = "N/A";
                    if (x.FollowUpType == FollowUpType.ProfileVerification)
                        endStatusDesc = $"Verification: {x.LatestProfileVerificationStatus}";
                    else if (x.FollowUpType == FollowUpType.PremiumFollowUp)
                        endStatusDesc = $"Premium: {x.LatestInterestStatus}";
                    else if (x.FollowUpType == FollowUpType.RenewalFollowUp)
                        endStatusDesc = $"Renewal: {x.LatestRenewalInterestStatus}";

                    docsDict.TryGetValue(x.ProfileId, out var userDocs);

                    return new
                    {
                        x.Id,
                        ProfileId = x.ProfileId,
                        AssignedStaffId = x.AssignedStaffId,
                        CustomerName = x.Profile?.Name ?? "N/A",
                        RegisterNumber = x.Profile?.RegisterNumber ?? "N/A",
                        Gender = x.Profile?.Gender ?? "N/A",
                        VerificationGrade = x.VerificationGrade,
                        StaffName = staffName,
                        FollowUpType = x.FollowUpType.ToString(),
                        EndStatus = endStatusDesc,
                        LatestRemarks = x.LatestRemarks ?? "N/A",
                        ApprovalStatus = x.LatestAdminApprovalStatus?.ToString() ?? "Pending",
                        LatestInterestStatus = x.LatestInterestStatus?.ToString(),
                        LatestRenewalInterestStatus = x.LatestRenewalInterestStatus?.ToString(),
                        LatestProfileVerificationStatus = x.LatestProfileVerificationStatus?.ToString(),
                        VerificationDocumentUrl = x.Profile?.VerificationDocumentUrl,
                        VerificationDocuments = userDocs != null ? (object)userDocs : Array.Empty<object>(),
                        DocumentVerificationEnabled = x.Profile?.DocumentVerificationEnabled ?? false,
                        DocumentVerificationComplete = x.Profile?.DocumentVerificationComplete ?? false,
                        PaymentMode = x.PaymentMode ?? "Online",
                        OfflinePaymentType = x.OfflinePaymentType?.ToString(),
                        OfflinePaymentTypeValue = (int?)x.OfflinePaymentType,
                        TransactionId = x.TransactionId,
                        PaymentAmount = x.PaymentAmount,
                        PaymentLinkSent = x.PaymentLinkSent,
                        PaymentLinkSentAt = x.PaymentLinkSentAt?.ToString("yyyy-MM-dd HH:mm"),
                        PaymentCompleted = x.PaymentCompleted
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
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = 0,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        [HttpPost("/admin/staff/admin-review-action")]
        public async Task<IActionResult> AdminReviewAction([FromBody] AdminApprovalModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            try
            {
                var followUp = await _followUpRepo.Get(model.FollowUpId);
                if (followUp == null || followUp.IsDeleted)
                {
                    return Json(new { success = false, message = "Follow-up not found." });
                }

                // Get currently logged-in admin user
                var loggedInUser = await _userManager.GetUserAsync(User);
                string adminName = loggedInUser?.NormalizedUserName ?? loggedInUser?.UserName ?? "Admin";

                var approvalStatus = model.IsApproved ? AdminApprovalStatus.Approved : AdminApprovalStatus.Rejected;

                // Update followUp's latest admin approval status
                followUp.LatestAdminApprovalStatus = approvalStatus;

                // Assign Verification Grade if approved for Profile Verification
                if (model.IsApproved && followUp.FollowUpType == FollowUpType.ProfileVerification && !string.IsNullOrWhiteSpace(model.VerificationGrade))
                {
                    followUp.VerificationGrade = model.VerificationGrade.Trim().ToUpper();
                }

                // Add FollowUpAdminApproval record
                var approvalRecord = new FollowUpAdminApproval
                {
                    FollowUpId = followUp.Id,
                    Status = approvalStatus,
                    Remarks = model.Remarks,
                    ActionDate = DateTime.UtcNow,
                    ActionBy = adminName,
                    IsActive = true
                };

                await _dbContext.FollowUpAdminApprovals.AddAsync(approvalRecord);
                await _dbContext.SaveChangesAsync();

                // Create a timeline entry for this decision
                string gradeText = (model.IsApproved && !string.IsNullOrEmpty(followUp.VerificationGrade)) ? $" (Grade {followUp.VerificationGrade})" : "";
                var timeline = new FollowUpTimeline
                {
                    FollowUpId = followUp.Id,
                    StaffId = loggedInUser?.Id ?? 0,
                    StaffName = $"Admin ({adminName})",
                    ContactType = null,
                    CallStatus = null,
                    Remarks = $"Admin {(model.IsApproved ? "Approved" : "Rejected")} this follow-up{gradeText}. Remarks: {model.Remarks}",
                    NextFollowUpDate = null,
                    ProfileVerificationStatus = followUp.LatestProfileVerificationStatus,
                    InterestStatus = followUp.LatestInterestStatus,
                    RenewalInterestStatus = followUp.LatestRenewalInterestStatus,
                    IsActive = true
                };
                await _followUpTimelineRepo.Add(timeline);
                await _followUpTimelineRepo.SaveChanges();

                await _followUpRepo.Update(followUp);
                await _followUpRepo.SaveChanges();

                string successMessage = $"Follow-up successfully {(model.IsApproved ? "approved" : "rejected")}.";

                // If admin approved and the follow-up type is profile verification, apply changes to user profile
                if (model.IsApproved && followUp.FollowUpType == FollowUpType.ProfileVerification)
                {
                    var userProfile = await _userRepository.Get(followUp.ProfileId);
                    if (userProfile != null && !userProfile.IsDeleted)
                    {
                        userProfile.DocumentVerificationFollowupApproved = true;

                        if (followUp.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify)
                        {
                            userProfile.IsVerified = true;
                            userProfile.IsVisible = true;
                            userProfile.IsComplete = true;
                            userProfile.CompletedStep = "Step-6";

                            await _userRepository.Update(userProfile);
                            await _userRepository.SaveChanges();
                        }
                        else if (followUp.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify)
                        {
                            if (userProfile.DocumentVerificationComplete)
                            {
                                userProfile.IsVerified = true;
                                userProfile.IsVisible = true;
                                userProfile.IsComplete = true;
                                userProfile.CompletedStep = "Step-6";
                                userProfile.DocumentVerificationRejected = false;
                            }
                            else
                            {
                                userProfile.IsActive = true;
                                userProfile.DocumentVerificationEnabled = true;
                                userProfile.DocumentVerificationComplete = false;
                                userProfile.DocumentVerificationRejected = true;
                            }

                            await _userRepository.Update(userProfile);
                            await _userRepository.SaveChanges();
                        }
                        else if (followUp.LatestProfileVerificationStatus == ProfileVerificationStatus.Suspended)
                        {
                            userProfile.IsActive = false;
                            userProfile.DisabledReason = DisabledReason.Recycled;
                            userProfile.IsVisible = false;

                            await _userRepository.Update(userProfile);
                            await _userRepository.SaveChanges();
                        }
                        else if (followUp.LatestProfileVerificationStatus == ProfileVerificationStatus.Dismissed)
                        {
                            // Remove reports associated with this profile
                            var reports = await _dbContext.UserReports.Where(x => x.ReportedUserId == userProfile.Id && !x.IsDeleted).ToListAsync();
                            foreach (var report in reports)
                            {
                                report.IsDeleted = true;
                            }
                            await _dbContext.SaveChangesAsync();

                            await _userRepository.SoftDelete(userProfile);
                            await _userRepository.SaveChanges();
                        }
                    }
                }
                string? whatsappUrl = null;
                if (model.IsApproved && (followUp.FollowUpType == FollowUpType.PremiumFollowUp || followUp.FollowUpType == FollowUpType.RenewalFollowUp))
                {
                    bool isConverted = (followUp.FollowUpType == FollowUpType.PremiumFollowUp && followUp.LatestInterestStatus == PremiumInterestStatus.Converted);
                    bool isRenewed = (followUp.FollowUpType == FollowUpType.RenewalFollowUp && followUp.LatestRenewalInterestStatus == RenewalInterestStatus.Renewed);

                    if (isConverted || isRenewed)
                    {
                        var userProfile = await _userRepository.Get(followUp.ProfileId);
                        if (userProfile != null && !userProfile.IsDeleted)
                        {
                            if (string.Equals(followUp.PaymentMode, "Offline", StringComparison.OrdinalIgnoreCase) || string.Equals(model.PaymentMode, "Offline", StringComparison.OrdinalIgnoreCase))
                            {
                                string txnId = !string.IsNullOrEmpty(followUp.TransactionId) ? followUp.TransactionId : ("OFFLINE_" + DateTime.UtcNow.Ticks);
                                string amountStr = followUp.PaymentAmount.HasValue ? followUp.PaymentAmount.Value.ToString("F2") : "0";
                                OfflinePaymentMethod offType = followUp.OfflinePaymentType ?? OfflinePaymentMethod.CashPayment;

                                var existingTxn = await _transactionRepository.GetTransactionByTxnIdAsync(txnId);
                                if (existingTxn == null)
                                {
                                    var transaction = new Transaction
                                    {
                                        userId = userProfile.Id,
                                        TxnId = txnId,
                                        Amount = amountStr,
                                        Status = "success",
                                        PaymentType = "Offline",
                                        OfflinePaymentType = offType,
                                        PaymentGateway = "Manual",
                                        Key = "",
                                        Udf1 = userProfile.Id.ToString(),
                                        ProductInfo = isRenewed ? "Manual Subscription Renewal" : "Manual Subscription",
                                        Hash = "",
                                        FirstName = userProfile.Name ?? "",
                                        Email = userProfile.Email ?? "",
                                        Phone = userProfile.Phone ?? "",
                                        CreatedBy = adminName,
                                        CreatedOn = DateTime.Now,
                                        ModifiedOn = DateTime.Now,
                                        ModifiedBy = adminName,
                                        Source = "Admin"
                                    };

                                    await _transactionRepository.AddPaymentResultAsync(transaction);
                                }

                                var plans = await _planPurchaseRepo.Where(x => x.UserId == userProfile.Id);
                                var latestPlan = plans.OrderByDescending(x => x.CreatedOn).FirstOrDefault();

                                if (latestPlan != null && (latestPlan.ViewCreditsPurchased - latestPlan.ViewCreditsUsed) > 0 && latestPlan.ExpiresAt > DateTime.UtcNow)
                                {
                                    latestPlan.ViewCreditsPurchased += 50;
                                    latestPlan.ExpiresAt = DateTime.UtcNow.AddDays(180);
                                    latestPlan.LowCreditNotificationSent = false;
                                    latestPlan.ExpiryNotificationSent = false;
                                    latestPlan.ModifiedOn = DateTime.Now;
                                    latestPlan.ModifiedBy = adminName;
                                    await _planPurchaseRepo.Update(latestPlan);
                                }
                                else
                                {
                                    var planPurchase = new PlanPurchase
                                    {
                                        UserId = userProfile.Id,
                                        ViewCreditsPurchased = 50,
                                        CreatedOn = DateTime.Now,
                                        ModifiedOn = DateTime.Now,
                                        CreatedBy = adminName,
                                        ModifiedBy = adminName
                                    };
                                    await _planPurchaseRepo.Add(planPurchase);
                                }
                                await _planPurchaseRepo.SaveChanges();

                                userProfile.IsPremiumMember = true;
                                await _userRepository.Update(userProfile);
                                await _userRepository.SaveChanges();

                                followUp.PaymentCompleted = true;
                                await _followUpRepo.Update(followUp);
                                await _followUpRepo.SaveChanges();

                                successMessage = $"Follow-up successfully approved. Manual subscription and credits activated for {userProfile.Name}.";
                            }
                            else
                            {
                                bool sendEmail = model.SendEmail;
                                bool sendMessage = model.SendMessage;
                                bool sendWhatsApp = model.SendWhatsApp;

                                if (!sendEmail && !sendMessage && !sendWhatsApp)
                                {
                                    sendEmail = true;
                                    sendMessage = true;
                                }

                                whatsappUrl = await SendPaymentLinkNotification(userProfile, sendEmail, sendMessage, sendWhatsApp);

                                var sentChannels = new List<string>();
                                if (sendEmail) sentChannels.Add("Email");
                                if (sendMessage) sentChannels.Add("SMS");
                                if (sendWhatsApp) sentChannels.Add("WhatsApp");

                                followUp.PaymentLinkSent = true;
                                followUp.PaymentLinkSentAt = DateTime.UtcNow;
                                await _followUpRepo.Update(followUp);
                                await _followUpRepo.SaveChanges();

                                successMessage = $"Follow-up successfully approved, and payment link sent via {string.Join(", ", sentChannels)}.";
                            }
                        }
                    }
                }

                return Json(new { success = true, message = successMessage, whatsappUrl = whatsappUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private async Task<string?> SendPaymentLinkNotification(Registration userProfile, bool sendEmail, bool sendMessage, bool sendWhatsApp)
        {
            string? whatsappUrl = null;
            try
            {
                // Retrieve encryption secret key and expiration minutes from appsettings.json
                string secretKey = _configuration.GetValue<string>("PaymentLinkSettings:SecretKey") ?? "M4NikkahPaymentSecretKey#2026";
                int expirationMinutes = _configuration.GetValue<int?>("PaymentLinkSettings:ExpirationMinutes") ?? 15;

                DateTime expiryTime = DateTime.UtcNow.AddMinutes(expirationMinutes);
                string payload = $"{userProfile.Id}|{expiryTime:o}";
                
                var crypt = new RijndaelCrypt(secretKey);
                string token = crypt.Encrypt(payload);
                string paymentLink = $"{Request.Scheme}://{Request.Host}/Transaction/MakePaymentDirect?token={Uri.EscapeDataString(token)}";

                // Send email
                if (sendEmail && !string.IsNullOrEmpty(userProfile.Email))
                {
                    string emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 8px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #7209b7, #3f37c9); color: white; padding: 25px; text-align: center; }}
        .content {{ padding: 30px; }}
        .footer {{ background-color: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #777; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: #7209b7; color: white !important; text-decoration: none; border-radius: 5px; margin-top: 20px; font-weight: bold; }}
        .highlight {{ color: #7209b7; font-weight: bold; }}
        .expiry-notice {{ color: #d9534f; font-weight: bold; margin-top: 15px; padding: 10px; background-color: #fdf7f7; border-left: 4px solid #d9534f; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin:0; color: white;'>M4Nikah Matrimony</h2>
        </div>
        <div class='content'>
            <p>Dear <strong>{userProfile.Name}</strong>,</p>
            <p>We are pleased to inform you that your request has been reviewed and approved by our administration.</p>
            <p>To activate or renew your premium benefits (which include viewing match contact details, sending unlimited messages, and receiving premium matches), please complete your payment using the link below:</p>
            <div class='expiry-notice'>
                ⏰ <strong>Note:</strong> This payment link is valid for <strong>{expirationMinutes} minutes</strong> only.
            </div>
            <center>
                <a href='{paymentLink}' class='button' style='color: white !important; text-decoration: none;'>Proceed to Payment</a>
            </center>
            <p style='margin-top: 25px;'>If the button doesn't work, you can copy and paste the following URL into your browser:</p>
            <p style='word-break: break-all;'><a href='{paymentLink}'>{paymentLink}</a></p>
            <br/>
            <p>Best Regards,<br/><strong>The M4Nikah Team</strong></p>
        </div>
        <div class='footer'>
            <p>Contact Us: info@m4nikah.com | +91 6238785898</p>
            <p>&copy; {DateTime.Now.Year} M4Nikah. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                    var htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                    _emailNotificationHelper.SendEmail(userProfile.Email, htmlView, "Complete Your Premium Payment - M4Nikah");
                }

                // Send SMS
                if (sendMessage && !string.IsNullOrEmpty(userProfile.Phone))
                {
                    await _emailService.SendSmsPaymentLinkAsync(paymentLink, userProfile.Phone);
                }

                // Send WhatsApp
                if (sendWhatsApp && !string.IsNullOrEmpty(userProfile.Phone))
                {
                    string rawCountryCode = userProfile.CountryCode ?? userProfile.Country;
                    string cleanCode = !string.IsNullOrEmpty(rawCountryCode) ? CountryCodeHelper.GetCountryCode(rawCountryCode).Replace("+", "").Replace(" ", "").Trim() : "91";
                    if (string.IsNullOrEmpty(cleanCode)) cleanCode = "91";
                    string cleanPhone = userProfile.Phone.Replace(" ", "").Replace("+", "").Trim();
                    string fullPhone = cleanPhone.StartsWith(cleanCode) ? cleanPhone : cleanCode + cleanPhone;
                    string waMessage = $" *M4Nikah Matrimony*\n\nDear *{userProfile.Name}*,\n\nGreat news! Your membership request has been *approved* by our administration.\n\n Click the secure link below to complete your payment:\n{paymentLink}\n\n *Note:* This payment link is active for 15 minutes.\n\nBest Regards,\n*The M4Nikah Team*";
                    whatsappUrl = $"https://wa.me/{fullPhone}?text={Uri.EscapeDataString(waMessage)}";
                }
            }
            catch (Exception ex)
            {
                // Log error but swallow so that sending failure doesn't break review actions
                Console.WriteLine($"Error sending payment link notification: {ex.Message}");
            }

            return whatsappUrl;
        }

        [HttpPost("/admin/staff/resend-payment-link")]
        public async Task<IActionResult> ResendPaymentLink([FromBody] ResendPaymentLinkModel model)
        {
            if (model == null || model.FollowUpId <= 0)
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            try
            {
                var followUp = await _followUpRepo.Get(model.FollowUpId);
                if (followUp == null || followUp.IsDeleted)
                {
                    return Json(new { success = false, message = "Follow-up not found." });
                }

                var userProfile = await _userRepository.Get(followUp.ProfileId);
                if (userProfile == null || userProfile.IsDeleted)
                {
                    return Json(new { success = false, message = "User profile not found." });
                }

                var loggedInUser = await _userManager.GetUserAsync(User);
                string staffName = loggedInUser?.NormalizedUserName ?? loggedInUser?.UserName ?? "Staff";

                bool sendEmail = model.SendEmail;
                bool sendMessage = model.SendMessage;
                bool sendWhatsApp = model.SendWhatsApp;

                if (!sendEmail && !sendMessage && !sendWhatsApp)
                {
                    sendEmail = true;
                    sendMessage = true;
                }

                string? whatsappUrl = await SendPaymentLinkNotification(userProfile, sendEmail, sendMessage, sendWhatsApp);

                followUp.PaymentMode = "Online";
                followUp.PaymentLinkSent = true;
                followUp.PaymentLinkSentAt = DateTime.UtcNow;
                followUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;

                var sentChannels = new List<string>();
                if (sendEmail) sentChannels.Add("Email");
                if (sendMessage) sentChannels.Add("SMS");
                if (sendWhatsApp) sentChannels.Add("WhatsApp");

                var timeline = new FollowUpTimeline
                {
                    FollowUpId = followUp.Id,
                    StaffId = loggedInUser?.Id ?? 0,
                    StaffName = staffName,
                    ContactType = followUp.LatestContactType,
                    CallStatus = followUp.LatestCallStatus,
                    Remarks = $"Payment link resent via {string.Join(", ", sentChannels)} by {staffName}.",
                    NextFollowUpDate = followUp.NextFollowUpDate,
                    InterestStatus = followUp.LatestInterestStatus,
                    RenewalInterestStatus = followUp.LatestRenewalInterestStatus,
                    IsActive = true
                };

                await _followUpTimelineRepo.Add(timeline);
                await _followUpTimelineRepo.SaveChanges();

                await _followUpRepo.Update(followUp);
                await _followUpRepo.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Payment link resent successfully via {string.Join(", ", sentChannels)}.",
                    whatsappUrl = whatsappUrl
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet("/admin/staff/performance-dashboard")]
        public async Task<IActionResult> PerformanceDashboard()
        {
            var model = new StaffPerformanceDashboardViewModel();

            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            model.TotalStaff = staffUsers.Count;

            var assignments = await _assignmentRepo.GetQueryable()
                .Where(x => !x.IsDeleted)
                .ToListAsync();
            
            var followups = await _followUpRepo.GetQueryable()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Profile)
                .ToListAsync();

            var timelines = await _followUpTimelineRepo.GetQueryable()
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            var planPurchases = await _planPurchaseRepo.GetQueryable()
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            var transactions = await _dbContext.Transaction
                .Where(t => t.Status == "success")
                .ToListAsync();

            model.TotalAssignedProfiles = assignments.Count;
            
            // Real stats counting
            foreach (var staff in staffUsers)
            {
                long staffId = staff.Id;
                var staffAssignments = assignments.Where(a => a.StaffId == staffId).ToList();
                var staffProfileIds = staffAssignments.Select(a => a.ProfileId).ToList();
                
                var staffFollowups = followups.Where(f => f.AssignedStaffId == staffId).ToList();
                
                // Verified count (Requires Admin Approval)
                int verified = staffFollowups.Count(f => f.FollowUpType == FollowUpType.ProfileVerification 
                    && (f.LatestProfileVerificationStatus == ProfileVerificationStatus.Verify || f.LatestProfileVerificationStatus == ProfileVerificationStatus.DetailedVerify)
                    && f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved);
                int assigned = staffAssignments.Count;
                int pending = assigned - verified;

                int staffTimelinesCount = timelines.Count(t => t.StaffId == staffId);

                int premInterested = staffFollowups.Count(f => f.FollowUpType == FollowUpType.PremiumFollowUp && f.LatestInterestStatus == PremiumInterestStatus.Interested);
                
                // Payment verification: only count if admin approved and customer has a successful transaction created on or after this follow-up was created
                int premConverted = staffFollowups.Count(f => f.FollowUpType == FollowUpType.PremiumFollowUp 
                    && (f.LatestInterestStatus == PremiumInterestStatus.Converted || (f.Profile != null && f.Profile.IsPremiumMember))
                    && (f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                        || f.PaymentCompleted
                        || transactions.Any(t => t.userId == f.ProfileId && t.CreatedOn >= f.CreatedOn)));
                
                // Expired premium
                // Count plan purchases belonging to assigned users that are expired
                int expired = planPurchases.Count(p => staffProfileIds.Contains(p.UserId) && p.ExpiresAt < DateTime.UtcNow);

                int renewalFollows = staffFollowups.Count(f => f.FollowUpType == FollowUpType.RenewalFollowUp);
                
                // Payment verification: only count renewal if admin approved and customer has a successful transaction created on or after this follow-up was created
                int renewalConvs = staffFollowups.Count(f => f.FollowUpType == FollowUpType.RenewalFollowUp 
                    && (f.LatestRenewalInterestStatus == RenewalInterestStatus.Renewed || (f.Profile != null && f.Profile.IsPremiumMember))
                    && (f.LatestAdminApprovalStatus == AdminApprovalStatus.Approved
                        || f.PaymentCompleted
                        || transactions.Any(t => t.userId == f.ProfileId && t.CreatedOn >= f.CreatedOn)));

                int convRate = assigned > 0 ? (int)Math.Round((double)premConverted * 100 / assigned) : 0;
                int renewalRate = renewalFollows > 0 ? (int)Math.Round((double)renewalConvs * 100 / renewalFollows) : 0;

                model.StaffPerformanceList.Add(new StaffPerformanceRowViewModel
                {
                    StaffId = staffId,
                    StaffName = staff.NormalizedUserName ?? staff.UserName ?? "Unknown",
                    RegistrationId = staff.UserName ?? "",
                    Assigned = assigned,
                    Pending = pending,
                    Verified = verified,
                    Followups = staffTimelinesCount,
                    PremiumInterested = premInterested,
                    PremiumConverted = premConverted,
                    ExpiredPremium = expired,
                    RenewalFollowups = renewalFollows,
                    RenewalConverted = renewalConvs,
                    ConvRate = convRate,
                    RenewalRate = renewalRate,
                    IsActive = staff.LockoutEnd == null || staff.LockoutEnd <= DateTimeOffset.UtcNow
                });
            }

            model.VerifiedProfiles = model.StaffPerformanceList.Sum(s => s.Verified);
            model.PremiumConverted = model.StaffPerformanceList.Sum(s => s.PremiumConverted);
            model.RenewalConverted = model.StaffPerformanceList.Sum(s => s.RenewalConverted);
            model.FollowupsCompleted = model.StaffPerformanceList.Sum(s => s.Followups);

            model.AvgConversionRate = model.TotalAssignedProfiles > 0 
                ? (int)Math.Round((double)model.PremiumConverted * 100 / model.TotalAssignedProfiles)
                : 0;

            int totalRenewalFollowups = model.StaffPerformanceList.Sum(s => s.RenewalFollowups);
            model.AvgRenewalRate = totalRenewalFollowups > 0
                ? (int)Math.Round((double)model.RenewalConverted * 100 / totalRenewalFollowups)
                : 0;

            return View(model);
        }

        #endregion

        #region Staff Cold Leads

        [HttpGet("/admin/staff/cold-leads")]
        public async Task<IActionResult> ColdLeads(long? staffId)
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.StaffList = staffUsers.Select(u => new
            {
                Id = u.Id,
                Name = u.NormalizedUserName ?? u.UserName
            }).ToList();

            ViewBag.SelectedStaffId = staffId;
            return View();
        }

        [AcceptVerbs("GET", "POST")]
        [Route("/admin/staff/cold-leads-data")]
        public async Task<IActionResult> StaffColdLeadsData(long? staffId, ColdLeadStatus? statusFilter)
        {
            try
            {
                var isPost = HttpContext.Request.Method == "POST";
                var drawVal = isPost ? Request.Form["draw"] : Request.Query["draw"];
                var startVal = isPost ? Request.Form["start"] : Request.Query["start"];
                var lengthVal = isPost ? Request.Form["length"] : Request.Query["length"];

                int draw = !string.IsNullOrEmpty(drawVal) ? Convert.ToInt32(drawVal) : 0;
                int start = !string.IsNullOrEmpty(startVal) ? Convert.ToInt32(startVal) : 0;
                int length = !string.IsNullOrEmpty(lengthVal) ? Convert.ToInt32(lengthVal) : 10;
                if (length <= 0) length = 10;

                string? searchValue = isPost ? Request.Form["search[value]"] : Request.Query["search[value]"];

                var query = _coldLeadRepo.GetQueryable().Where(x => !x.IsDeleted);

                if (staffId.HasValue && staffId.Value > 0)
                {
                    query = query.Where(x => x.AssignedStaffId == staffId.Value);
                }

                if (statusFilter.HasValue)
                {
                    query = query.Where(x => x.Status == statusFilter.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    var search = searchValue.Trim().ToLower();
                    query = query.Where(x => x.Name.ToLower().Contains(search) || x.PhoneNumber.ToLower().Contains(search));
                }

                int totalRecords = await query.CountAsync();

                var records = await query
                    .OrderByDescending(x => x.CreatedOn)
                    .Skip(start)
                    .Take(length)
                    .ToListAsync();

                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = staffUsers.ToDictionary(u => u.Id, u => u.NormalizedUserName ?? u.UserName);

                var data = records.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    phoneNumber = c.PhoneNumber,
                    assignedStaffId = c.AssignedStaffId,
                    assignedStaffName = staffDict.TryGetValue(c.AssignedStaffId, out var sName) ? sName : "Unassigned",
                    status = (int)c.Status,
                    statusName = c.Status.ToString(),
                    remarks = c.Remarks ?? "",
                    createdOn = c.CreatedOn.ToString("dd/MM/yyyy hh:mm tt")
                }).ToList();

                return Json(new
                {
                    draw = draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new object[0] });
            }
        }

        [HttpPost("/admin/staff/update-cold-lead-status")]
        public async Task<IActionResult> UpdateColdLeadStatus([FromForm] long id, [FromForm] ColdLeadStatus status, [FromForm] string? remarks)
        {
            try
            {
                var coldLead = await _coldLeadRepo.Get(id);
                if (coldLead == null || coldLead.IsDeleted)
                {
                    return Json(new { success = false, message = "Cold lead not found." });
                }

                coldLead.Status = status;
                if (!string.IsNullOrWhiteSpace(remarks))
                {
                    coldLead.Remarks = remarks.Trim();
                }

                await _coldLeadRepo.Update(coldLead);
                await _coldLeadRepo.SaveChanges();
                return Json(new { success = true, message = "Cold lead status updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating status." });
            }
        }

        #endregion

        #region Staff Leave & Complaint Management (Admin Panel)

        [HttpGet("/admin/staff/leave-records/{staffId:long}")]
        public async Task<IActionResult> GetAdminLeaveRecords(long staffId, int? year, int? month)
        {
            int filterYear = year ?? DateTime.UtcNow.Year;
            int filterMonth = month ?? DateTime.UtcNow.Month;

            var records = await _dbContext.StaffLeaveRecords
                .Where(l => l.StaffId == staffId && !l.IsDeleted && l.Year == filterYear && l.Month == filterMonth)
                .OrderByDescending(l => l.LeaveDate)
                .Select(l => new
                {
                    l.Id,
                    l.StaffId,
                    leaveDate = l.LeaveDate.ToString("yyyy-MM-dd"),
                    l.LeaveType,
                    l.Reason,
                    l.IsPaid,
                    l.IsApproved,
                    l.ApprovedBy
                })
                .ToListAsync();

            return Json(records);
        }

        [HttpPost("/admin/staff/leave-record/save")]
        public async Task<IActionResult> SaveAdminLeaveRecord([FromBody] SaveLeaveRecordApiModel model)
        {
            if (model.StaffId <= 0) return Json(new { success = false, message = "Invalid Staff ID." });
            DateTime leaveDate = model.LeaveDate != default ? model.LeaveDate : DateTime.UtcNow.Date;

            var currentUser = await _userManager.GetUserAsync(User);
            string adminName = currentUser?.NormalizedUserName ?? currentUser?.UserName ?? "Admin";

            if (model.Id > 0)
            {
                var existing = await _dbContext.StaffLeaveRecords.FirstOrDefaultAsync(l => l.Id == model.Id && !l.IsDeleted);
                if (existing == null) return Json(new { success = false, message = "Record not found." });

                existing.LeaveDate = leaveDate;
                if (!string.IsNullOrEmpty(model.LeaveType)) existing.LeaveType = model.LeaveType;
                existing.Reason = model.Reason;
                existing.IsPaid = model.IsPaid;
                existing.IsApproved = model.IsApproved;
                existing.ApprovedBy = model.IsApproved ? adminName : null;
                existing.Year = leaveDate.Year;
                existing.Month = leaveDate.Month;

                _dbContext.StaffLeaveRecords.Update(existing);
            }
            else
            {
                var newRecord = new StaffLeaveRecord
                {
                    StaffId = model.StaffId,
                    LeaveDate = leaveDate,
                    LeaveType = model.LeaveType,
                    Reason = model.Reason,
                    IsPaid = model.IsPaid,
                    IsApproved = model.IsApproved,
                    ApprovedBy = model.IsApproved ? adminName : null,
                    Year = leaveDate.Year,
                    Month = leaveDate.Month
                };
                await _dbContext.StaffLeaveRecords.AddAsync(newRecord);
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = "Leave record saved successfully." });
        }

        [HttpPost("/admin/staff/leave-record/delete/{id:long}")]
        public async Task<IActionResult> DeleteAdminLeaveRecord(long id)
        {
            var record = await _dbContext.StaffLeaveRecords.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (record == null) return Json(new { success = false, message = "Record not found." });

            record.IsDeleted = true;
            _dbContext.StaffLeaveRecords.Update(record);
            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Leave record deleted successfully." });
        }

        [HttpGet("/admin/staff/complaint-records/{staffId:long}")]
        public async Task<IActionResult> GetAdminComplaintRecords(long staffId, int? year, int? month)
        {
            int filterYear = year ?? DateTime.UtcNow.Year;
            int filterMonth = month ?? DateTime.UtcNow.Month;

            var records = await _dbContext.StaffComplaintRecords
                .Where(c => c.StaffId == staffId && !c.IsDeleted && c.Year == filterYear && c.Month == filterMonth)
                .OrderByDescending(c => c.ComplaintDate)
                .Select(c => new
                {
                    c.Id,
                    c.StaffId,
                    complaintDate = c.ComplaintDate.ToString("yyyy-MM-dd"),
                    c.ComplaintDescription,
                    c.ComplaintLevel,
                    c.DeductionAmount,
                    c.IsApproved,
                    c.ApprovedBy,
                    c.Resolution
                })
                .ToListAsync();

            return Json(records);
        }

        [HttpPost("/admin/staff/complaint-record/save")]
        public async Task<IActionResult> SaveAdminComplaintRecord([FromBody] SaveComplaintRecordApiModel model)
        {
            if (model.StaffId <= 0) return Json(new { success = false, message = "Invalid Staff ID." });
            DateTime complaintDate = model.ComplaintDate != default ? model.ComplaintDate : DateTime.UtcNow.Date;

            if (model.Id > 0)
            {
                var existing = await _dbContext.StaffComplaintRecords.FirstOrDefaultAsync(c => c.Id == model.Id && !c.IsDeleted);
                if (existing == null) return Json(new { success = false, message = "Record not found." });

                existing.ComplaintDate = complaintDate;
                existing.ComplaintDescription = model.ComplaintDescription;
                existing.ComplaintLevel = model.ComplaintLevel;
                existing.DeductionAmount = model.DeductionAmount;
                existing.IsApproved = model.IsApproved;
                existing.Resolution = model.Resolution;
                existing.Year = complaintDate.Year;
                existing.Month = complaintDate.Month;

                _dbContext.StaffComplaintRecords.Update(existing);
            }
            else
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var newRecord = new StaffComplaintRecord
                {
                    StaffId = model.StaffId,
                    ComplaintDate = complaintDate,
                    ComplaintDescription = model.ComplaintDescription,
                    ComplaintLevel = model.ComplaintLevel,
                    DeductionAmount = model.DeductionAmount,
                    IsApproved = model.IsApproved,
                    ApprovedBy = currentUser?.UserName ?? "Admin",
                    Resolution = model.Resolution,
                    Year = complaintDate.Year,
                    Month = complaintDate.Month
                };
                await _dbContext.StaffComplaintRecords.AddAsync(newRecord);
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = "Complaint record saved successfully." });
        }

        [HttpPost("/admin/staff/complaint-record/delete/{id:long}")]
        public async Task<IActionResult> DeleteAdminComplaintRecord(long id)
        {
            var record = await _dbContext.StaffComplaintRecords.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (record == null) return Json(new { success = false, message = "Record not found." });

            record.IsDeleted = true;
            _dbContext.StaffComplaintRecords.Update(record);
            return Json(new { success = true, message = "Complaint record deleted successfully." });
        }

        [HttpGet("/admin/staff/leaves-and-complaints")]
        public async Task<IActionResult> LeaveAndComplaints(long? staffId = null, int? year = null, int? month = null)
        {
            int filterYear = year ?? DateTime.UtcNow.Year;
            int filterMonth = month ?? DateTime.UtcNow.Month;

            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staffUserIds = staffUsers.Select(u => u.Id).ToList();

            var staffDetailsMap = await _dbContext.StaffDetails
                .Where(s => staffUserIds.Contains(s.UserId) && !s.IsDeleted)
                .ToDictionaryAsync(s => s.UserId);

            var staffOptions = staffUsers.Select(u =>
            {
                staffDetailsMap.TryGetValue(u.Id, out var detail);
                return new StaffSelectOption
                {
                    Id = u.Id,
                    StaffName = u.NormalizedUserName ?? u.UserName ?? "Unknown",
                    Department = detail?.Department ?? "General"
                };
            }).OrderBy(s => s.StaffName).ToList();

            long selectedId = staffId ?? (staffOptions.FirstOrDefault()?.Id ?? 0);

            var vm = new StaffLeaveAndComplaintViewModel
            {
                SelectedStaffId = selectedId,
                FilterYear = filterYear,
                FilterMonth = filterMonth,
                StaffOptions = staffOptions
            };

            if (selectedId > 0)
            {
                var selectedUser = staffUsers.FirstOrDefault(u => u.Id == selectedId);
                staffDetailsMap.TryGetValue(selectedId, out var selectedDetail);

                vm.StaffName = selectedUser?.NormalizedUserName ?? selectedUser?.UserName ?? "Unknown";
                vm.Department = selectedDetail?.Department ?? "General";

                int daysInFilteredMonth = DateTime.DaysInMonth(filterYear, filterMonth);
                vm.DaysInMonth = daysInFilteredMonth;

                var salaryConfig = await _dbContext.StaffSalaryConfigs.FirstOrDefaultAsync(s => s.StaffId == selectedId && !s.IsDeleted);
                if (salaryConfig != null)
                {
                    vm.BasicMonthlySalary = salaryConfig.BasicMonthlySalary;
                }

                var leaveConfig = await _dbContext.StaffLeaveDeductionConfigs.FirstOrDefaultAsync(l => l.StaffId == selectedId && !l.IsDeleted);
                if (leaveConfig != null)
                {
                    vm.PaidLeaveLimit = leaveConfig.PaidLeaveLimit;
                }

                // Load Leaves
                var leaves = await _dbContext.StaffLeaveRecords
                    .Where(l => l.StaffId == selectedId && !l.IsDeleted && l.Year == filterYear && l.Month == filterMonth)
                    .OrderByDescending(l => l.LeaveDate)
                    .ToListAsync();

                decimal perDaySalary = salaryConfig?.PerDaySalary > 0 
                    ? salaryConfig.PerDaySalary 
                    : (salaryConfig != null && salaryConfig.BasicMonthlySalary > 0 
                        ? Math.Round(salaryConfig.BasicMonthlySalary / daysInFilteredMonth, 2) 
                        : 0);

                vm.PerDaySalary = perDaySalary;

                int paidLeavesUsed = 0;
                decimal totalLeaveDed = 0;

                foreach (var l in leaves)
                {
                    decimal ded = 0;
                    if (!l.IsPaid && l.IsApproved)
                    {
                        ded = l.LeaveType switch
                        {
                            "HalfDay" => Math.Round(perDaySalary / 2, 2),
                            "FullDay" => perDaySalary,
                            "Late" => Math.Round(perDaySalary / 4, 2),
                            _ => perDaySalary
                        };
                        totalLeaveDed += ded;
                    }
                    else if (l.IsPaid)
                    {
                        paidLeavesUsed++;
                    }

                    vm.LeaveRecords.Add(new StaffLeaveRecordItem
                    {
                        Id = l.Id,
                        StaffId = l.StaffId,
                        LeaveDate = l.LeaveDate,
                        LeaveType = l.LeaveType,
                        Reason = l.Reason,
                        IsPaid = l.IsPaid,
                        IsApproved = l.IsApproved,
                        ApprovedBy = l.ApprovedBy ?? "Admin",
                        CalculatedDeduction = ded
                    });
                }
                vm.TotalLeaveDeduction = totalLeaveDed;

                // Load Complaints
                var complaints = await _dbContext.StaffComplaintRecords
                    .Where(c => c.StaffId == selectedId && !c.IsDeleted && c.Year == filterYear && c.Month == filterMonth)
                    .OrderByDescending(c => c.ComplaintDate)
                    .ToListAsync();

                decimal totalCompDed = 0;
                foreach (var c in complaints)
                {
                    if (c.IsApproved)
                    {
                        totalCompDed += c.DeductionAmount;
                    }
                    vm.ComplaintRecords.Add(new StaffComplaintRecordItem
                    {
                        Id = c.Id,
                        StaffId = c.StaffId,
                        ComplaintDate = c.ComplaintDate,
                        ComplaintDescription = c.ComplaintDescription,
                        ComplaintLevel = c.ComplaintLevel,
                        DeductionAmount = c.DeductionAmount,
                        IsApproved = c.IsApproved,
                        ApprovedBy = c.ApprovedBy ?? "Admin",
                        Resolution = c.Resolution
                    });
                }
                vm.TotalComplaintDeduction = totalCompDed;
            }

            return View("LeaveAndComplaints", vm);
        }

        #endregion
    }
}
