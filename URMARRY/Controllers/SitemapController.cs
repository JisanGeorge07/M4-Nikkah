using Application.Interfaces.Persistence;
using Domain;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Linq;

namespace URMARRY.Controllers
{
    public class SitemapController : Controller
    {
        private readonly IRepository<HomeContent> _homeContentRepo;
        private readonly IRepository<About> _aboutRepo;
        private readonly IRepository<Contact> _contactRepo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SitemapController> _logger;

        public SitemapController(
            IRepository<HomeContent> homeContentRepo,
            IRepository<About> aboutRepo,
            IRepository<Contact> contactRepo,
            IConfiguration configuration,
            ILogger<SitemapController> logger)
        {
            _homeContentRepo = homeContentRepo;
            _aboutRepo = aboutRepo;
            _contactRepo = contactRepo;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Index()
        {
            try
            {
                var baseUrl = _configuration["SiteSettings:BaseUrl"]?.TrimEnd('/')
                    ?? $"{Request.Scheme}://{Request.Host}";

                if (baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = "https://m4nikah.com";
                }

                // Query database to get accurate last-modified dates where available
                DateTime homeLastMod = DateTime.UtcNow;
                DateTime aboutLastMod = DateTime.UtcNow;
                DateTime contactLastMod = DateTime.UtcNow;

                try
                {
                    var homeContent = await _homeContentRepo.FirstOrDefault();
                    if (homeContent != null)
                    {
                        homeLastMod = homeContent.ModifiedOn != default ? homeContent.ModifiedOn : (homeContent.CreatedOn != default ? homeContent.CreatedOn : DateTime.UtcNow);
                    }
                }
                catch
                {
                    // Fallback to UtcNow
                }

                try
                {
                    var about = await _aboutRepo.FirstOrDefaultActive();
                    if (about != null)
                    {
                        aboutLastMod = about.ModifiedOn != default ? about.ModifiedOn : (about.CreatedOn != default ? about.CreatedOn : DateTime.UtcNow);
                    }
                }
                catch
                {
                    // Fallback to UtcNow
                }

                try
                {
                    var contact = await _contactRepo.FirstOrDefault();
                    if (contact != null)
                    {
                        contactLastMod = contact.ModifiedOn != default ? contact.ModifiedOn : (contact.CreatedOn != default ? contact.CreatedOn : DateTime.UtcNow);
                    }
                }
                catch
                {
                    // Fallback to UtcNow
                }

                XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

                var sitemapUrls = new List<XElement>
                {
                    // Homepage
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/"),
                        new XElement(ns + "lastmod", homeLastMod.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "daily"),
                        new XElement(ns + "priority", "1.0")
                    ),
                    // About Us
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/about"),
                        new XElement(ns + "lastmod", aboutLastMod.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.8")
                    ),
                    // Contact Us
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/contact"),
                        new XElement(ns + "lastmod", contactLastMod.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.8")
                    ),
                    // Terms and Conditions
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/Home/TermsConditions"),
                        new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "monthly"),
                        new XElement(ns + "priority", "0.5")
                    ),
                    // Privacy Policy
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/Home/Privacy"),
                        new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "monthly"),
                        new XElement(ns + "priority", "0.5")
                    ),
                    // Child Safety Policy
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/Home/ChildSafetyPolicy"),
                        new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "monthly"),
                        new XElement(ns + "priority", "0.5")
                    )
                };

                var sitemapDoc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", "yes"),
                    new XElement(ns + "urlset", sitemapUrls)
                );

                var sb = new StringBuilder();
                using (var writer = new System.IO.StringWriter(sb))
                {
                    sitemapDoc.Save(writer);
                }

                return Content(sb.ToString(), "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sitemap.xml");
                return StatusCode(500);
            }
        }

        [HttpGet("/robots.txt")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public IActionResult RobotsTxt()
        {
            var robotsContent = "User-agent: *\nAllow: /\n\nDisallow: /user/login\nDisallow: /admin/\nDisallow: /account/\nDisallow: /dashboard/\n\nSitemap: https://m4nikah.com/sitemap.xml\n";
            return Content(robotsContent, "text/plain", Encoding.UTF8);
        }
    }
}
