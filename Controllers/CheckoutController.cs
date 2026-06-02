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

        [HttpPost]
        // WE NOW ACCEPT THE ADDRESS DIRECTLY FROM THE CART FORM!
        public async Task<IActionResult> ProcessCheckout(string StreetAddress, string City, string PostalCode, string PhoneNumber)
        {
            var userEmail = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerEmail == userEmail)
                .ToListAsync();

            if (cartItems == null || !cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty. Please add items before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            // --- NEW: SAVE THE ADDRESS BEFORE THEY PAY ---
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userEmail);
            if (customer == null)
            {
                // If they just registered, create them now
                customer = new Customer { Email = userEmail, FirstName = "Valued", LastName = "Customer" };
                _context.Customers.Add(customer);
            }
            
            // Save the typed address into the database
            customer.StreetAddress = StreetAddress;
            customer.City = City;
            customer.PostalCode = PostalCode;
            customer.PhoneNumber = PhoneNumber;
            await _context.SaveChangesAsync();
            // ---------------------------------------------

            var domain = "http://localhost:5175"; 

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                CustomerEmail = userEmail,
                SuccessUrl = domain + "/Checkout/Success?session_id={CHECKOUT_SESSION_ID}", 
                CancelUrl = domain + "/Cart/Index", 
            };

            foreach (var item in cartItems)
            {
                decimal priceWithVat = item.Product.Price * 1.15m;

                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "zar",
                        UnitAmount = (long)(priceWithVat * 100), 
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.ProductName,
                        },
                    },
                    Quantity = item.Quantity,
                });
            }

            var service = new SessionService();
            Session session = service.Create(options);

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

                // 3. Calculate Database Total WITH 15% VAT to match Stripe perfectly
                decimal subtotal = cartItems.Sum(c => c.Quantity * c.Product.Price);
                decimal totalWithVat = subtotal * 1.15m;

                // 4. Create the Official Order with the Stripe Session ID
                var newOrder = new Order
                {
                    CustomerId = customer.CustomerId,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalWithVat, // Saves the final VAT amount to the DB
                    OrderStatus = "Paid",
                    StripeSessionId = session.Id 
                };
                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();

                // 5. Create Details & DEDUCT THE STOCK SAFELY
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

                // 6. Empty their Cart
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return View();
            }

            return RedirectToAction("Index", "Home");
        }
    }
}