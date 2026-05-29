using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;


namespace StoreManagementSystem.Controllers;

public class HomeController : Controller
{

    private readonly StoreManagementDbContext _context;
    private readonly ILogger<HomeController> _logger;

    // We ask for BOTH dependencies in a single constructor
    public HomeController(StoreManagementDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }
    public IActionResult Index()
    {
        // If the user logging in has the Admin role, route them instantly to the dashboard
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Home", new { area = "Company" });
        }
        return View();
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

        // 2. Endpoint to completely delete the customer's history
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
