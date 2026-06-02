using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;
using Microsoft.AspNetCore.SignalR;
using StoreManagementSystem.Hubs;

namespace StoreManagementSystem.Areas.Company.Controllers
{
    
    // The Controller handles all user requests related to Orders and coordinates the Views.
    [Area("Company")] // This attribute indicates that this controller belongs to the "Company" area of the application
    [Authorize(Roles = "Admin")] // This attribute restricts access to authenticated users, ensuring that only logged-in users can manage orders

    
    public class OrdersController : Controller
    {
        // Private field for the Entity Framework database context
        private readonly StoreManagementDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        // Constructor: Dependency Injection provides the database context
        public OrdersController(StoreManagementDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // --- READ: Get all orders ---
        // GET: Orders
        // This method fetches all orders and their related customer information.
        public async Task<IActionResult> Index()
        {
            // LINQ: Eagerly load the related Customer data for each order so we can show customer names
            var storeManagementDbContext = _context.Orders.Include(o => o.Customer);
            return View(await storeManagementDbContext.ToListAsync());
        }

        // --- READ: Get a single order ---
        // GET: Orders/Details/5
        // This method fetches a specific order by ID to show its details.

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product) // This is the magic line that loads the product names!
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }
        // --- CREATE: Show the form ---
        // GET: Orders/Create
        // This method displays the empty form to create a new order.
        public IActionResult Create()
        {
            // Pass a list of Customers to the view for a dropdown selection menu
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email");
            return View();
        }

        // --- CREATE: Save the form data ---
        // POST: Orders/Create
        // This method receives the form data and saves the new order to the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,OrderDate,TotalAmount,OrderStatus,CustomerId")] Order order)
        {
            // Ensure the date is set to now if it's empty
            if (order.OrderDate == null) order.OrderDate = DateTime.Now;

            // Ignore validation for the navigation properties that aren't in the form
            ModelState.Remove("Customer");
            ModelState.Remove("OrderDetails");

            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Order shell created successfully!";
                return RedirectToAction(nameof(Index));
            }
            // Inside your Create POST method, after saving the order:
            foreach (var item in order.OrderDetails)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Subtract the ordered quantity from stock
                    product.StockQuantity -= item.Quantity;
                    _context.Products.Update(product);
                }
            }
            await _context.SaveChangesAsync();
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", order.CustomerId);
            return View(order);
        }

        // : Update Order Status ---
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> UpdateStatus(int orderId, string orderStatus)
        {
            // Include the Customer so we know who to notify
            var order = await _context.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // 1. Update the Order
            order.OrderStatus = orderStatus;

            // 2. Create the Notification for the Database
            string alertMessage = $"Good news! Your order #{orderId} is now {orderStatus}.";
            var notification = new Notification
            {
                UserId = order.Customer.Email, // Linking it via email
                Message = alertMessage,
                ActionUrl = $"/Orders/Details/{orderId}" // Link to the order
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

           // Fire the real-time SignalR WebSocket!
            // (For portfolio testing, we are broadcasting this to all connected clients so you can easily see it pop up in multiple browser tabs)
            await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", alertMessage);

            // Trigger the Admin's green success banner
            TempData["SuccessMessage"] = $"Order #{orderId} marked as {orderStatus}. Customer notified!";

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        // --- UPDATE: Show the form ---
        // GET: Orders/Edit/5
        // This method fetches a specific order and populates the edit form.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            // Populate the customer dropdown, pre-selecting the current customer
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", order.CustomerId);
            return View(order);
        }

        // --- UPDATE: Save the form data ---
        // POST: Orders/Edit/5
        // This method saves the edited order data to the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,OrderDate,TotalAmount,OrderStatus,CustomerId")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", order.CustomerId);
            return View(order);
        }

        // --- DELETE: Show confirmation screen ---
        // GET: Orders/Delete/5
        // This method displays a screen asking the user to confirm deleting the order.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // LINQ: Fetch the order and related customer for the confirmation screen
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // --- DELETE: Execute the deletion ---
        // POST: Orders/Delete/5
        // This method physically removes the order from the database.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
{
    // 1. Fetch the Order AND all of its related Order Details
    var order = await _context.Orders
        .Include(o => o.OrderDetails) 
        .FirstOrDefaultAsync(o => o.OrderId == id);

    if (order != null)
    {
        // 2. Safely delete the child items first so they don't become orphans
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            _context.OrderDetails.RemoveRange(order.OrderDetails);
        }

        // 3. Now it is safe to delete the parent Order!
        _context.Orders.Remove(order);
        
        await _context.SaveChangesAsync();
    }
    
    return RedirectToAction(nameof(Index));
}
        // GET: Orders/AddProduct/5 (5 is the OrderId)
        public IActionResult AddProduct(int id)
        {
            ViewBag.OrderId = id;
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "ProductName");
            return View();
        }

        // POST: Orders/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(OrderDetail detail)
        {
            // Fetch the product to get the current price
            var product = await _context.Products.FindAsync(detail.ProductId);
            detail.UnitPrice = product.Price; // Lock in the price at time of order

            _context.OrderDetails.Add(detail);

            // Auto-update total in Order
            var order = await _context.Orders.FindAsync(detail.OrderId);
            order.TotalAmount += (detail.UnitPrice * detail.Quantity);

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = detail.OrderId });
        }
        // Helper Method: Checks if an order exists to handle concurrent edit conflicts
        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}