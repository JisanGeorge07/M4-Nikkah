using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class ColdLeadController : Controller
    {
        private readonly IRepository<ColdLead> _coldLeadRepo;
        private readonly IRepository<Registration> _registrationRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<ColdLeadController> _logger;

        public ColdLeadController(
            IRepository<ColdLead> coldLeadRepo,
            IRepository<Registration> registrationRepo,
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<ColdLeadController> logger)
        {
            _coldLeadRepo = coldLeadRepo;
            _registrationRepo = registrationRepo;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("/admin/cold-leads")]
        public async Task<IActionResult> Index()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.StaffList = staffUsers.Select(u => new
            {
                Id = u.Id,
                Name = u.NormalizedUserName ?? u.UserName
            }).ToList();

            return View();
        }

        [HttpGet("/admin/cold-lead/check-phone")]
        public async Task<IActionResult> CheckPhone([FromQuery] string phone, [FromQuery] long? excludeId = null)
        {
            try
            {
                var cleanPhone = (phone ?? "").Trim();
                if (string.IsNullOrWhiteSpace(cleanPhone))
                {
                    return Json(new { available = true, message = "" });
                }

                // 1. Check if a registered profile already exists with this phone number
                var existingProfile = await _registrationRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.Phone == cleanPhone && x.IsVerified)
                    .FirstOrDefaultAsync();

                if (existingProfile != null)
                {
                    return Json(new
                    {
                        available = false,
                        reason = "registered",
                        message = $"A registered profile ({existingProfile.Name ?? existingProfile.RegisterNumber}) already exists with this phone number ({cleanPhone}). You cannot create a cold lead for an existing profile."
                    });
                }

                // 2. Check if already exists in other Cold Leads
                var existingLead = await _coldLeadRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.PhoneNumber == cleanPhone && (!excludeId.HasValue || x.Id != excludeId.Value))
                    .FirstOrDefaultAsync();

                if (existingLead != null)
                {
                    return Json(new
                    {
                        available = false,
                        reason = "coldlead",
                        message = $"A cold lead already exists with this phone number ({cleanPhone}) (Lead: {existingLead.Name}, ID: #{existingLead.Id})."
                    });
                }

                return Json(new { available = true, message = "Phone number is available." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking phone availability for cold lead: {Phone}", phone);
                return Json(new { available = true, message = "" });
            }
        }

        [AcceptVerbs("GET", "POST")]
        [Route("/admin/cold-leads-data")]
        public async Task<IActionResult> GetColdLeadsData(long? staffId, ColdLeadStatus? statusFilter)
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
                    query = query.Where(x => x.Name.ToLower().Contains(search)
                                          || x.PhoneNumber.ToLower().Contains(search)
                                          || (x.Gender != null && x.Gender.ToLower().Contains(search)));
                }

                int totalRecords = await query.CountAsync();

                var records = await query
                    .OrderByDescending(x => x.CreatedOn)
                    .Skip(start)
                    .Take(length)
                    .ToListAsync();

                // Resolve staff names
                var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                var staffDict = staffUsers.ToDictionary(u => u.Id, u => u.NormalizedUserName ?? u.UserName);

                var data = records.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    phoneNumber = c.PhoneNumber,
                    gender = c.Gender ?? "",
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
                _logger.LogError(ex, "Error fetching cold leads data");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new object[0] });
            }
        }

        [HttpPost("/admin/cold-lead/create")]
        public async Task<IActionResult> Create(CreateColdLeadModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    return Json(new { success = false, message = "Name and Phone Number are required." });
                }

                if (!model.AssignedStaffId.HasValue || model.AssignedStaffId.Value <= 0)
                {
                    return Json(new { success = false, message = "Please select a staff member to assign." });
                }

                var cleanPhone = model.PhoneNumber.Trim();

                // 1. Check if a registered profile already exists with this phone number
                var existingProfile = await _registrationRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.Phone == cleanPhone && x.IsVerified)
                    .FirstOrDefaultAsync();

                if (existingProfile != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"A registered profile ({existingProfile.Name ?? existingProfile.RegisterNumber}) already exists with this phone number ({cleanPhone}). You cannot create a cold lead for an existing profile."
                    });
                }

                // 2. Check if another cold lead already exists with this phone number
                var existingColdLead = await _coldLeadRepo.GetQueryable()
                    .Where(x => !x.IsDeleted && x.PhoneNumber == cleanPhone)
                    .FirstOrDefaultAsync();

                if (existingColdLead != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"A cold lead already exists with this phone number ({cleanPhone}) (Lead: {existingColdLead.Name}, ID: #{existingColdLead.Id})."
                    });
                }

                var coldLead = new ColdLead
                {
                    Name = model.Name.Trim(),
                    PhoneNumber = cleanPhone,
                    Gender = string.IsNullOrWhiteSpace(model.Gender) ? null : model.Gender.Trim(),
                    AssignedStaffId = model.AssignedStaffId.Value,
                    Status = ColdLeadStatus.Pending,
                    Remarks = model.Remarks?.Trim(),
                    IsActive = true,
                    IsDeleted = false
                };

                await _coldLeadRepo.Add(coldLead);
                await _coldLeadRepo.SaveChanges();

                return Json(new { success = true, message = "Cold Lead created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cold lead");
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Error creating cold lead: " + detail });
            }
        }

        [HttpPost("/admin/cold-lead/update")]
        public async Task<IActionResult> Update(UpdateColdLeadModel model)
        {
            try
            {
                if (model == null || model.Id <= 0)
                {
                    return Json(new { success = false, message = "Invalid cold lead ID." });
                }

                var coldLead = await _coldLeadRepo.Get(model.Id);
                if (coldLead == null || coldLead.IsDeleted)
                {
                    return Json(new { success = false, message = "Cold Lead not found." });
                }

                var cleanPhone = model.PhoneNumber?.Trim();
                if (!string.IsNullOrWhiteSpace(cleanPhone) && cleanPhone != coldLead.PhoneNumber)
                {
                    // 1. Check if a registered profile exists with this new phone number
                    var existingProfile = await _registrationRepo.GetQueryable()
                        .Where(x => !x.IsDeleted && x.Phone == cleanPhone && x.IsVerified)
                        .FirstOrDefaultAsync();

                    if (existingProfile != null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"A registered profile ({existingProfile.Name ?? existingProfile.RegisterNumber}) already exists with this phone number ({cleanPhone})."
                        });
                    }

                    // 2. Check if another cold lead exists with this new phone number
                    var existingColdLead = await _coldLeadRepo.GetQueryable()
                        .Where(x => !x.IsDeleted && x.PhoneNumber == cleanPhone && x.Id != model.Id)
                        .FirstOrDefaultAsync();

                    if (existingColdLead != null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Another cold lead already exists with this phone number ({cleanPhone}) (Lead: {existingColdLead.Name}, ID: #{existingColdLead.Id})."
                        });
                    }

                    coldLead.PhoneNumber = cleanPhone;
                }

                if (!string.IsNullOrWhiteSpace(model.Name)) coldLead.Name = model.Name.Trim();
                coldLead.Gender = string.IsNullOrWhiteSpace(model.Gender) ? null : model.Gender.Trim();
                if (model.AssignedStaffId.HasValue && model.AssignedStaffId.Value > 0)
                {
                    coldLead.AssignedStaffId = model.AssignedStaffId.Value;
                }
                coldLead.Status = model.Status;
                coldLead.Remarks = model.Remarks?.Trim();

                await _coldLeadRepo.Update(coldLead);
                await _coldLeadRepo.SaveChanges();

                return Json(new { success = true, message = "Cold Lead updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cold lead ID: {Id}", model?.Id);
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Error updating cold lead: " + detail });
            }
        }

        [HttpPost("/admin/cold-lead/delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var coldLead = await _coldLeadRepo.Get(id);
                if (coldLead != null)
                {
                    await _coldLeadRepo.SoftDelete(coldLead);
                    await _coldLeadRepo.SaveChanges();
                }

                return Json(new { success = true, message = "Cold Lead deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cold lead ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while deleting cold lead." });
            }
        }
    }

    public class CreateColdLeadModel
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public long? AssignedStaffId { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateColdLeadModel
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public long? AssignedStaffId { get; set; }
        public ColdLeadStatus Status { get; set; }
        public string? Remarks { get; set; }
    }
}
