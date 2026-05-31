using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StoreManagementSystem.Controllers
{
    [Authorize] // Only logged in users can checkout
    public class CheckoutController : Controller
    {
        private readonly StoreManagementDbContext _context;

        public CheckoutController(StoreManagementDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCheckout()
        {
            var userEmail = User.Identity.Name;

            // 1. Get all items from the user's cart
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerEmail == userEmail)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // 2. Find or Create the Customer profile based on their login email
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userEmail);
            if (customer == null)
            {
                customer = new Customer 
                { 
                    Email = userEmail, 
                    FirstName = "Valued", // Temporary fallback
                    LastName = "Customer" 
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync(); // Save to generate their CustomerId
            }

            // 3. Create the Main Order Receipt
            decimal subtotal = cartItems.Sum(c => c.Quantity * c.Product.Price);
            decimal totalWithVat = subtotal + (subtotal * 0.15m);

            var newOrder = new Order
            {
                CustomerId = customer.CustomerId,
                OrderDate = DateTime.Now,
                TotalAmount = totalWithVat,
                OrderStatus = "Paid" // (We will hook this to Stripe later!)
            };
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync(); // Save to generate the OrderId

            // 4. Create Order Details & DEDUCT STOCK (The Magic Step!)
            foreach (var item in cartItems)
            {
                // A. Add it to the permanent Order History
                var orderDetail = new OrderDetail
                {
                    OrderId = newOrder.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                _context.OrderDetails.Add(orderDetail);

                // B. REDUCE THE MASTER INVENTORY STOCK
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Minus the amount they bought from the database!
                    product.StockQuantity -= item.Quantity;
                    
                    // Safety protocol: Don't let stock go into negative numbers
                    if (product.StockQuantity < 0) 
                    {
                        product.StockQuantity = 0;
                    }
                }
            }

            // 5. Empty the user's shopping cart now that they bought it
            _context.CartItems.RemoveRange(cartItems);
            
            // 6. Save ALL updates (Orders, Details, Stock drops, Empty Cart) to SQL
            await _context.SaveChangesAsync();

            // 7. Take them to the success screen
            return RedirectToAction("Success");
        }

        // Show the Thank You page
        public IActionResult Success()
        {
            return View();
        }
    }
}