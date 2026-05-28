using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;

namespace StoreManagementSystem.Areas.Company.Controllers
{
    // The Controller handles all user requests related to Customers and coordinates between the Views and the Models.
    [Area("Company")] // This attribute indicates that this controller is part of the "Company" area in the application
    [Authorize(Roles = "Admin")]// This attribute restricts access to authenticated users, ensuring that only logged-in users can manage customers
    public class CustomersController : Controller
    {
        // Private field to hold our database context for EF Core
        private readonly StoreManagementDbContext _context;

        // Constructor: Dependency Injection provides the database context to this controller
        public CustomersController(StoreManagementDbContext context)
        {
            _context = context;
        }

        // --- READ: Get all customers ---
        // GET: Customers
        // This method fetches the complete list of customers from the database and passes it to the Index view.
        public async Task<IActionResult> Index()
        {
            // LINQ: We use .ToListAsync() to execute the query asynchronously and return all customers.
            return View(await _context.Customers.ToListAsync());
        }

        // --- READ: Get a single customer ---
        // GET: Customers/Details/5
        // This method fetches a single customer based on the provided ID to show their full details.
        public async Task<IActionResult> Details(int? id)
        {
            // If no ID is provided in the URL, return a 404 Not Found error
            if (id == null)
            {
                return NotFound();
            }

            // LINQ: We use .FirstOrDefaultAsync() to find the first customer matching the provided ID.
            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerId == id);
            
            // If the database returns null (the customer doesn't exist), return 404 Not Found
            if (customer == null)
            {
                return NotFound();
            }

            // Return the Details view, passing in the specific customer's data
            return View(customer);
        }

        // --- CREATE: Show the form ---
        // GET: Customers/Create
        // This method simply displays the empty form for a user to input a new customer's details.
        public IActionResult Create()
        {
            return View();
        }

        // --- CREATE: Save the form data ---
        // POST: Customers/Create
        // This method receives the submitted form data and saves the new customer to the database.
        [HttpPost]
        [ValidateAntiForgeryToken] // Security measure to prevent Cross-Site Request Forgery (CSRF) attacks
        public async Task<IActionResult> Create([Bind("CustomerId,FirstName,LastName,Email,PhoneNumber")] Customer customer)
        {
            // Verify that the submitted data passes all validation rules defined in the Customer Model
            if (ModelState.IsValid)
            {
                // Add the new customer to the Entity Framework tracking context
                _context.Add(customer);
                
                // Save the changes asynchronously to the SQL Server database
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Customer added successfully";
                // Redirect the user back to the customer list (Index method)
                return RedirectToAction(nameof(Index));
            }
            
            // If validation fails, return the view with the user's data so they can see and fix their errors
            return View(customer);
        }

        // --- UPDATE: Show the form ---
        // GET: Customers/Edit/5
        // This method fetches a specific customer and populates a form so the user can edit their information.
        public async Task<IActionResult> Edit(int? id)
        {
            // If no ID is provided, return 404 Not Found
            if (id == null)
            {
                return NotFound();
            }

            // Find the customer in the database using their primary key
            var customer = await _context.Customers.FindAsync(id);
            
            // If the customer doesn't exist, return 404 Not Found
            if (customer == null)
            {
                return NotFound();
            }
            
            // Return the Edit view, passing in the customer data to pre-fill the form
            return View(customer);
        }

        // --- UPDATE: Save the form data ---
        // POST: Customers/Edit/5
        // This method receives the updated form data and applies the changes to the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,FirstName,LastName,Email,PhoneNumber")] Customer customer)
        {
            // Ensure the ID in the URL matches the ID of the submitted customer object
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            // Verify the submitted data is valid against the model
            if (ModelState.IsValid)
            {
                try
                {
                    // Update the customer in the tracking context
                    _context.Update(customer);
                    
                    // Save the updated changes to the database
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle concurrency issues (e.g., if another user deleted this customer at the exact same time)
                    if (!CustomerExists(customer.CustomerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                
                // Redirect back to the main customer list
                return RedirectToAction(nameof(Index));
            }
            
            // If validation fails, return the form with errors
            return View(customer);
        }

        // --- DELETE: Show confirmation screen ---
        // GET: Customers/Delete/5
        // This method fetches a customer and displays a screen asking the user to confirm deletion.
        public async Task<IActionResult> Delete(int? id)
        {
            // If no ID provided, return 404 Not Found
            if (id == null)
            {
                return NotFound();
            }

            // LINQ: Fetch the specific customer to display on the confirmation page
            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerId == id);
            
            if (customer == null)
            {
                return NotFound();
            }

            // Return the Delete confirmation view
            return View(customer);
        }

        // --- DELETE: Execute the deletion ---
        // POST: Customers/Delete/5
        // This method physically removes the customer from the database after the user clicks confirm.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Find the customer by their ID
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                // Remove the customer from the tracking context
                _context.Customers.Remove(customer);
            }

            // Save the deletion to the actual database
            await _context.SaveChangesAsync();
            
            // Redirect back to the customer list
            return RedirectToAction(nameof(Index));
        }

        // Helper Method: Checks if a customer exists in the database
        // This is used during the Edit process to handle concurrency exceptions.
        private bool CustomerExists(int id)
        {
            // LINQ: .Any() returns true if at least one customer matches the provided ID
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}