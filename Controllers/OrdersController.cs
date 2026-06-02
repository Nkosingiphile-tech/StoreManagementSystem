using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StoreManagementSystem.Controllers
{
    [Authorize] // Only logged in users can see their orders
    public class OrdersController : Controller
    {
        private readonly StoreManagementDbContext _context;

        public OrdersController(StoreManagementDbContext context)
        {
            _context = context;
        }

        // 1. SHOW ALL ORDERS (History)
        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity.Name;
            
            // Find the customer profile
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userEmail);
            if (customer == null) 
            {
                return View(new List<Order>()); // Return empty if no profile exists yet
            }

            // Get all their orders, newest first
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.CustomerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // 2. SHOW SPECIFIC ORDER DETAILS & TRACKING
        public async Task<IActionResult> Details(int id)
        {
            var userEmail = User.Identity.Name;
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userEmail);

            if (customer == null) return NotFound();

            // Fetch the specific order, AND include all the products they bought in that order
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product) // Get the product images and names!
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customer.CustomerId);

            // If they try to look at an order that isn't theirs, block them
            if (order == null) return NotFound();

            return View(order);
        }
    }
}