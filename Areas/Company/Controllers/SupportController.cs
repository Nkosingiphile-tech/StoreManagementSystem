using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StoreManagementSystem.Areas.Company.Controllers
{
    [Area("Company")]
    public class SupportController : Controller
    {
        private readonly StoreManagementDbContext _context;

        public SupportController(StoreManagementDbContext context)
        {
            _context = context;
        }

        // Loads the main Inbox page with a list of customers
        public async Task<IActionResult> Inbox()
        {
            var customers = await _context.ChatMessages
                .Where(m => !m.IsFromAdmin)
                .Select(m => m.SenderEmail)
                .Distinct()
                .ToListAsync();

            return View(customers);
        }

        // An API endpoint for the UI to quickly fetch a customer's chat history
        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string email)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.SenderEmail == email || m.ReceiverEmail == email)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Json(messages);
        }
    }
}