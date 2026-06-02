using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;
using System.Threading.Tasks;

namespace StoreManagementSystem.Controllers
{
    [Authorize] // Only logged-in users can access this
    public class ProfileController : Controller
    {
        private readonly StoreManagementDbContext _context;

        public ProfileController(StoreManagementDbContext context)
        {
            _context = context;
        }

        // 1. SHOW THE PROFILE PAGE
        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity.Name;
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userEmail);

            // If they just registered and don't exist in the Customer table yet, create a blank profile for them!
            if (customer == null)
            {
                customer = new Customer { Email = userEmail, FirstName = "", LastName = "" };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            return View(customer);
        }

        // 2. SAVE THE UPDATED ADDRESS
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(Customer updatedCustomer)
        {
            var userEmail = User.Identity.Name;
            var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userEmail);

            if (existingCustomer != null)
            {
                existingCustomer.FirstName = updatedCustomer.FirstName;
                existingCustomer.LastName = updatedCustomer.LastName;
                existingCustomer.PhoneNumber = updatedCustomer.PhoneNumber;
                existingCustomer.StreetAddress = updatedCustomer.StreetAddress;
                existingCustomer.City = updatedCustomer.City;
                existingCustomer.PostalCode = updatedCustomer.PostalCode;

                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Your profile and delivery address have been updated successfully!";
            }

            return RedirectToAction("Index");
        }
    }
}