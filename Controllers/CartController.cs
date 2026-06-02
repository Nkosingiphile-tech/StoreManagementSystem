using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StoreManagementSystem.Controllers
{
    [Authorize] // Only logged-in users can have a cart
    public class CartController : Controller
    {
        private readonly StoreManagementDbContext _context;

        public CartController(StoreManagementDbContext context)
        {
            _context = context;
        }

        // GET: /Cart/
        // Displays the shopping cart page
        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity.Name;

            // 1. Pass the customer to ViewBag so the Cart can pre-fill their address!
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userEmail);
            ViewBag.Customer = customer;

            // 2. Load their cart items
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerEmail == userEmail)
                .ToListAsync();

            return View(cartItems);
        }

        // POST: /Cart/AddToCart
       // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userEmail = User.Identity.Name;

            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CustomerEmail == userEmail && c.ProductId == productId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
            }
            else
            {
                var newCartItem = new CartItem
                {
                    CustomerEmail = userEmail,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(newCartItem);
            }

            await _context.SaveChangesAsync();

            // NEW: Count the total items in the cart right now
            int cartTotal = await _context.CartItems
                .Where(c => c.CustomerEmail == userEmail)
                .SumAsync(c => c.Quantity);

            // Return a JSON message back to the webpage instead of reloading!
            return Json(new { success = true, newCount = cartTotal, message = "Item added to your cart!" });
        }

        // POST: /Cart/Remove
        // Deletes an item entirely from the cart
        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            
            if (cartItem != null && cartItem.CustomerEmail == User.Identity.Name)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}