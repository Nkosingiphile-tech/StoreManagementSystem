using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StoreManagementSystem.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly StoreManagementDbContext _context;

        public CheckoutController(StoreManagementDbContext context)
        {
            _context = context;
        }

        // --- STEP 1: SEND THEM TO STRIPE ---
        [HttpPost]
        public async Task<IActionResult> ProcessCheckout()
        {
            var userEmail = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerEmail == userEmail)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            // Look at your browser URL bar! Ensure this matches your local port (e.g., 5175 or 5000)
            var domain = "http://localhost:5175"; 

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                CustomerEmail = userEmail,
                // Where Stripe sends them AFTER payment:
                SuccessUrl = domain + "/Checkout/Success?session_id={CHECKOUT_SESSION_ID}", 
                // Where Stripe sends them if they click the back button:
                CancelUrl = domain + "/Cart/Index", 
            };

            // Package up all their cart items for the Stripe Receipt
            foreach (var item in cartItems)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Product.Price * 100), // Stripe reads ZAR in cents (R10.00 = 1000)
                        Currency = "zar",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.ProductName,
                        },
                    },
                    Quantity = item.Quantity,
                });
            }

            // Generate the Stripe Payment Screen
            var service = new SessionService();
            Session session = service.Create(options);

            // Redirect the user out of our app and onto Stripe's secure servers!
            return Redirect(session.Url);
        }

        // --- STEP 2: THEY PAID! NOW WE DEDUCT STOCK ---
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id)) return RedirectToAction("Index", "Home");

            // 1. Verify with Stripe that this is a real, paid session
            var service = new SessionService();
            Session session = service.Get(session_id);

            if (session.PaymentStatus == "paid")
            {
                var userEmail = User.Identity.Name;
                
                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.CustomerEmail == userEmail)
                    .ToListAsync();

                // 2. Find or Create the Customer
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (customer == null)
                {
                    customer = new Customer { Email = userEmail, FirstName = "Valued", LastName = "Customer" };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // 3. Create the Official Order with the Stripe Session ID
                var newOrder = new Order
                {
                    CustomerId = customer.CustomerId,
                    OrderDate = DateTime.Now,
                    TotalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price), // Excludes VAT for simplicity right now
                    OrderStatus = "Paid",
                    StripeSessionId = session.Id 
                };
                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();

                // 4. Create Details & DEDUCT THE STOCK SAFELY
                foreach (var item in cartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = newOrder.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price
                    };
                    _context.OrderDetails.Add(orderDetail);

                    // Safely minus the stock now that we have their money!
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        if (product.StockQuantity < 0) product.StockQuantity = 0;
                    }
                }

                // 5. Empty their Cart
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return View();
            }

            return RedirectToAction("Index", "Home");
        }
    }
}