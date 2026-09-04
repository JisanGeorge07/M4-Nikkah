using Persistence;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class PremiumPackageController : Controller
    {
        private readonly AppDbContext _dbContext;

        public PremiumPackageController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("/admin/premiumpackages")]
        public async Task<IActionResult> Index()
        {
            var list = await _dbContext.PremiumPackages
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            return View(list);
        }

        [HttpGet("/admin/premiumpackages/{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            if (id > 0)
            {
                var package = await _dbContext.PremiumPackages.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
                if (package == null)
                {
                    return NotFound();
                }
                return View(package);
            }
            return View(new PremiumPackage { IsActive = true });
        }

        [HttpPost("/admin/premiumpackages")]
        public async Task<IActionResult> Post(PremiumPackage model)
        {
            if (!ModelState.IsValid)
            {
                return View("Get", model);
            }

            if (model.Id > 0)
            {
                var existing = await _dbContext.PremiumPackages.FirstOrDefaultAsync(p => p.Id == model.Id && !p.IsDeleted);
                if (existing == null)
                {
                    return NotFound();
                }
                existing.PackageName = model.PackageName;
                existing.Price = model.Price;
                existing.Description = model.Description;
                existing.IsActive = model.IsActive;

                _dbContext.PremiumPackages.Update(existing);
            }
            else
            {
                await _dbContext.PremiumPackages.AddAsync(model);
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/premiumpackages/delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var package = await _dbContext.PremiumPackages.FirstOrDefaultAsync(p => p.Id == id);
            if (package != null)
            {
                package.IsDeleted = true;
                _dbContext.PremiumPackages.Update(package);
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
