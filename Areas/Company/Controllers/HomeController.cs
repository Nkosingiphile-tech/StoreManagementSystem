using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagementSystem.Models;
using StoreManagementSystem.ViewModels;

namespace StoreManagementSystem.Areas.Company.Controllers
{
    [Area("Company")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly StoreManagementDbContext _context;

        // Inject the database context
        public HomeController(StoreManagementDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Calculate real stats from your database tables
            var dashboardData = new DashboardViewModel
            {
                TotalProducts = _context.Products.Count(),
                ActiveCustomers = _context.Customers.Count(),
                PendingOrders = _context.Orders.Count(o => o.OrderStatus == "Pending"),

                // Sum the total amount of all orders (or just Completed ones)
                TotalRevenue = _context.Orders.Sum(o => o.TotalAmount)
            };

            return View(dashboardData);
        }
    }
}