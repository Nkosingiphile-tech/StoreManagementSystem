using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StoreManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly StoreManagementDbContext _context;
        private readonly ILogger<HomeController> _logger;

        // We ask for BOTH dependencies in a single constructor (Preserved)
        public HomeController(StoreManagementDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // UPDATED: Now it checks for Admin, AND fetches products for the landing page
        public async Task<IActionResult> Index()
        {
            // 1. If the user logging in has the Admin role, route them instantly to the dashboard
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home", new { area = "Company" });
            }

            // 2. Fetch the latest 8 products from the database for the Customer Landing Page
            var featuredProducts = await _context.Products
                .OrderByDescending(p => p.ProductId)
                .Take(8)
                .ToListAsync();

            // Hand the list of products to the HTML page
            return View(featuredProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // -------------------------------------------------------------
        // PRESERVED: Your Live Chat History Endpoints!
        // -------------------------------------------------------------
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyChatHistory()
        {
            var email = User.Identity.Name;
            var messages = await _context.ChatMessages
                .Where(m => m.SenderEmail == email || m.ReceiverEmail == email)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
            return Json(messages);
        }

        [Authorize]
        [HttpPost, IgnoreAntiforgeryToken] 
        public async Task<IActionResult> ClearMyChatHistory()
        {
            var email = User.Identity.Name;
            var messages = await _context.ChatMessages
                .Where(m => m.SenderEmail == email || m.ReceiverEmail == email)
                .ToListAsync();

            _context.ChatMessages.RemoveRange(messages);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}